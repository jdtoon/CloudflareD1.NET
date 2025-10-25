using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Fluent query builder interface for constructing SQL queries against Cloudflare D1.
    /// </summary>
    /// <typeparam name="T">The entity type to query.</typeparam>
    public interface IQueryBuilder<T> where T : class, new()
    {
        /// <summary>
        /// Adds a WHERE clause to the query.
        /// </summary>
        /// <param name="whereClause">The SQL WHERE condition (without the WHERE keyword). Use ? for parameters.</param>
        /// <param name="parameters">The parameter values to bind to the query.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> Where(string whereClause, params object[] parameters);

        /// <summary>
        /// Adds an ORDER BY clause to the query in ascending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> OrderBy(string column);

        /// <summary>
        /// Adds an ORDER BY clause to the query in descending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> OrderByDescending(string column);

        /// <summary>
        /// Adds an additional ORDER BY clause (after OrderBy/OrderByDescending) in ascending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> ThenBy(string column);

        /// <summary>
        /// Adds an additional ORDER BY clause (after OrderBy/OrderByDescending) in descending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> ThenByDescending(string column);

        /// <summary>
        /// Limits the number of results returned (SQL LIMIT).
        /// </summary>
        /// <param name="count">The maximum number of results to return.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> Take(int count);

        /// <summary>
        /// Skips a specified number of results (SQL OFFSET).
        /// </summary>
        /// <param name="count">The number of results to skip.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> Skip(int count);

        /// <summary>
        /// Executes the query and returns all matching entities.
        /// </summary>
        /// <returns>A list of entities matching the query.</returns>
        Task<IEnumerable<T>> ToListAsync();

        /// <summary>
        /// Executes the query and returns the first entity, or null if no results.
        /// </summary>
        /// <returns>The first entity or null.</returns>
        Task<T?> FirstOrDefaultAsync();

        /// <summary>
        /// Executes the query and returns exactly one entity. Throws if zero or more than one result.
        /// </summary>
        /// <returns>The single entity.</returns>
        Task<T> SingleAsync();

        /// <summary>
        /// Executes the query and returns exactly one entity or null. Throws if more than one result.
        /// </summary>
        /// <returns>The single entity or null.</returns>
        Task<T?> SingleOrDefaultAsync();

        /// <summary>
        /// Executes the query and returns the count of matching records.
        /// </summary>
        /// <returns>The number of matching records.</returns>
        Task<int> CountAsync();

        /// <summary>
        /// Executes the query and returns whether any records match.
        /// </summary>
        /// <returns>True if any records match, false otherwise.</returns>
        Task<bool> AnyAsync();
    }
}
