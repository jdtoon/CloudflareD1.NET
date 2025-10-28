using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.CodeFirst.Metadata;
using CloudflareD1.NET.Migrations;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Base class for database contexts using Code-First approach
/// </summary>
public abstract class D1Context
{
    private readonly D1Client _client;
    private readonly Dictionary<Type, object> _sets = new();
    private ModelMetadata? _model;
    private readonly ChangeTracker _changeTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="D1Context"/> class
    /// </summary>
    /// <param name="client">The D1 client</param>
    protected D1Context(D1Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _changeTracker = new ChangeTracker();
        InitializeSets();
    }

    /// <summary>
    /// Gets the D1 client
    /// </summary>
    protected D1Client Client => _client;

    /// <summary>
    /// Gets the model metadata for this context
    /// </summary>
    public ModelMetadata Model
    {
        get
        {
            if (_model == null)
            {
                var builder = new ModelBuilder();
                OnModelCreating(builder);
                _model = builder.Build(GetType());
            }
            return _model;
        }
    }

    /// <summary>
    /// Gets the model metadata for this context (for migration generation)
    /// </summary>
    /// <returns>The model metadata</returns>
    public ModelMetadata GetModelMetadata() => Model;

    /// <summary>
    /// Gets the change tracker for this context
    /// </summary>
    public ChangeTracker ChangeTracker => _changeTracker;

    /// <summary>
    /// Override this method to configure the model using the fluent API
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected virtual void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Default implementation - derived classes override
    }

    /// <summary>
    /// Gets a D1Set for the specified entity type
    /// </summary>
    protected D1Set<TEntity> Set<TEntity>() where TEntity : class, new()
    {
        var type = typeof(TEntity);
        if (_sets.TryGetValue(type, out var set))
        {
            return (D1Set<TEntity>)set;
        }

        var tableName = GetTableName<TEntity>();
        var newSet = new D1Set<TEntity>(_client, tableName, _changeTracker, () => Model);
        _sets[type] = newSet;
        return newSet;
    }

    /// <summary>
    /// Gets the table name for an entity type
    /// </summary>
    private string GetTableName<TEntity>() where TEntity : class, new()
    {
        var type = typeof(TEntity);
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        if (tableAttr != null)
        {
            return tableAttr.Name;
        }

        // Default: pluralize type name and convert to snake_case
        var name = type.Name;
        if (!name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            name += "s";
        }
        return ToSnakeCase(name);
    }

    /// <summary>
    /// Converts PascalCase to snake_case
    /// </summary>
    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLower(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLower(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Initializes all D1Set properties
    /// </summary>
    private void InitializeSets()
    {
        var contextType = GetType();
        var setProperties = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(D1Set<>));

        foreach (var prop in setProperties)
        {
            var entityType = prop.PropertyType.GetGenericArguments()[0];
            var setMethod = typeof(D1Context).GetMethod(nameof(Set), BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = setMethod!.MakeGenericMethod(entityType);
            var setInstance = genericMethod.Invoke(this, null);
            prop.SetValue(this, setInstance);
        }
    }

    /// <summary>
    /// Applies pending migrations to the database
    /// </summary>
    public async Task<List<string>> MigrateAsync()
    {
        var migrations = GetMigrations();
        var runner = new MigrationRunner(_client, migrations);
    return await runner.MigrateAsync();
    }

    /// <summary>
    /// Saves all tracked changes to the database.
    /// Returns the number of affected rows.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Build a batch of SQL statements based on tracked entries
        var statements = new List<CloudflareD1.NET.Models.D1Statement>();
        var postProcessors = new List<Action<CloudflareD1.NET.Models.D1QueryResult>>();

        // Order operations: INSERT, UPDATE, DELETE
        var entries = _changeTracker.Entries.ToList();

        // INSERTS
        foreach (var entry in entries.Where(e => e.State == EntityState.Added))
        {
            BuildInsert(entry, statements, postProcessors);
        }

        // UPDATES
        foreach (var entry in entries.Where(e => e.State == EntityState.Modified))
        {
            BuildUpdate(entry, statements);
        }

        // DELETES
        foreach (var entry in entries.Where(e => e.State == EntityState.Deleted))
        {
            BuildDelete(entry, statements);
        }

        if (statements.Count == 0)
        {
            return 0;
        }

        var results = await _client.BatchAsync(statements, cancellationToken).ConfigureAwait(false);

        // Apply post-processors in order for statements that need it (e.g., LastRowId -> entity key)
        int totalChanges = 0;
        for (int i = 0; i < results.Length; i++)
        {
            var res = results[i];
            if (res.Meta?.Changes != null)
            {
                totalChanges += (int)res.Meta.Changes.Value;
            }
            if (i < postProcessors.Count)
            {
                postProcessors[i](res);
            }
        }

        // Mark entries as Unchanged after successful save
        _changeTracker.AcceptAllChanges();

        return totalChanges;
    }

    private void BuildInsert(ITrackedEntry entry, List<CloudflareD1.NET.Models.D1Statement> statements, List<Action<CloudflareD1.NET.Models.D1QueryResult>> postProcessors)
    {
        var meta = entry.Metadata;
        var entity = entry.EntityObject;
        var paramDict = new Dictionary<string, object?>();

        var columns = new List<string>();
        foreach (var prop in meta.Properties)
        {
            var value = prop.PropertyInfo.GetValue(entity);
            // Skip auto-increment PK when not set
            if (prop.IsPrimaryKey && prop.IsAutoIncrement)
            {
                if (IsDefaultValue(value, prop.PropertyInfo.PropertyType))
                {
                    continue;
                }
            }

            columns.Add(prop.ColumnName);
            paramDict[$"p_{prop.ColumnName}"] = value;
        }

        var columnList = string.Join(", ", columns);
        var valuesList = string.Join(", ", columns.Select(c => $"@p_{c}"));
        var sql = $"INSERT INTO {meta.TableName} ({columnList}) VALUES ({valuesList})";

        statements.Add(new CloudflareD1.NET.Models.D1Statement { Sql = sql, Params = paramDict });

        // If we have a single auto-increment key and we didn't include it, capture LastRowId
        if (meta.PrimaryKey.Count == 1 && meta.PrimaryKey[0].IsAutoIncrement)
        {
            var pk = meta.PrimaryKey[0];
            var wasPkIncluded = columns.Contains(pk.ColumnName);
            if (!wasPkIncluded)
            {
                postProcessors.Add(result =>
                {
                    var id = result.Meta?.LastRowId;
                    if (id != null)
                    {
                        object converted = Convert.ChangeType(id.Value, Nullable.GetUnderlyingType(pk.PropertyInfo.PropertyType) ?? pk.PropertyInfo.PropertyType);
                        pk.PropertyInfo.SetValue(entity, converted);
                    }
                });
            }
            else
            {
                // maintain indexing alignment
                postProcessors.Add(_ => { });
            }
        }
        else
        {
            postProcessors.Add(_ => { });
        }
    }

    private void BuildUpdate(ITrackedEntry entry, List<CloudflareD1.NET.Models.D1Statement> statements)
    {
        var meta = entry.Metadata;
        var entity = entry.EntityObject;
        if (meta.PrimaryKey.Count == 0)
            throw new InvalidOperationException($"Entity {meta.ClrType.Name} does not have a primary key configured.");

        var setColumns = new List<string>();
        var paramDict = new Dictionary<string, object?>();

        foreach (var prop in meta.Properties)
        {
            if (prop.IsPrimaryKey) continue;
            setColumns.Add($"{prop.ColumnName} = @p_{prop.ColumnName}");
            paramDict[$"p_{prop.ColumnName}"] = prop.PropertyInfo.GetValue(entity);
        }

        var whereParts = new List<string>();
        foreach (var pk in meta.PrimaryKey)
        {
            whereParts.Add($"{pk.ColumnName} = @k_{pk.ColumnName}");
            paramDict[$"k_{pk.ColumnName}"] = pk.PropertyInfo.GetValue(entity);
        }

        var sql = $"UPDATE {meta.TableName} SET {string.Join(", ", setColumns)} WHERE {string.Join(" AND ", whereParts)}";
        statements.Add(new CloudflareD1.NET.Models.D1Statement { Sql = sql, Params = paramDict });
    }

    private void BuildDelete(ITrackedEntry entry, List<CloudflareD1.NET.Models.D1Statement> statements)
    {
        var meta = entry.Metadata;
        var entity = entry.EntityObject;
        if (meta.PrimaryKey.Count == 0)
            throw new InvalidOperationException($"Entity {meta.ClrType.Name} does not have a primary key configured.");

        var whereParts = new List<string>();
        var paramDict = new Dictionary<string, object?>();
        foreach (var pk in meta.PrimaryKey)
        {
            whereParts.Add($"{pk.ColumnName} = @k_{pk.ColumnName}");
            paramDict[$"k_{pk.ColumnName}"] = pk.PropertyInfo.GetValue(entity);
        }

        var sql = $"DELETE FROM {meta.TableName} WHERE {string.Join(" AND ", whereParts)}";
        statements.Add(new CloudflareD1.NET.Models.D1Statement { Sql = sql, Params = paramDict });
    }

    private static bool IsDefaultValue(object? value, Type type)
    {
        if (value == null) return true;
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (!t.IsValueType) return false;
        return value.Equals(Activator.CreateInstance(t));
    }

    /// <summary>
    /// Gets all migration types from the same assembly as this context
    /// </summary>
    private List<Migration> GetMigrations()
    {
        var assembly = GetType().Assembly;
        return assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .Select(t => (Migration)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Id)
            .ToList();
    }
}
