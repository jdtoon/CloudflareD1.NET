using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Linq;
using CloudflareD1.NET.Linq.Query;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Represents a collection of entities of a given type
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public class D1Set<TEntity> where TEntity : class, new()
{
    private readonly D1Client _client;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="D1Set{TEntity}"/> class
    /// </summary>
    /// <param name="client">The D1 client</param>
    /// <param name="tableName">The table name</param>
    internal D1Set(D1Client client, string tableName)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
    }

    /// <summary>
    /// Gets a queryable interface for this entity set
    /// </summary>
    public IQueryBuilder<TEntity> AsQueryable()
    {
        return _client.Query<TEntity>(_tableName);
    }

    /// <summary>
    /// Gets all entities from this set
    /// </summary>
    public Task<IEnumerable<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
    {
        return _client.Query<TEntity>(_tableName).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds an entity by its primary key
    /// </summary>
    public Task<TEntity?> FindAsync(object id, CancellationToken cancellationToken = default)
    {
        // TODO: Get primary key column name from metadata
        return _client.Query<TEntity>(_tableName)
            .Where("id = ?", id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the table name for this entity set
    /// </summary>
    public string TableName => _tableName;
}
