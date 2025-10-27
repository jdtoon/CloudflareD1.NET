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

    /// <summary>
    /// Initializes a new instance of the <see cref="D1Context"/> class
    /// </summary>
    /// <param name="client">The D1 client</param>
    protected D1Context(D1Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
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
        var newSet = new D1Set<TEntity>(_client, tableName);
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
