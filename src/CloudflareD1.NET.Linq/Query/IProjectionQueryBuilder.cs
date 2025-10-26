using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Query builder interface for projected query results.
    /// </summary>
    /// <typeparam name="TResult">The projection result type.</typeparam>
    public interface IProjectionQueryBuilder<TResult> where TResult : class, new()
    {
        /// <summary>
        /// Adds a WHERE clause to the query.
        /// </summary>
        /// <param name="whereClause">The SQL WHERE condition (without the WHERE keyword). Use ? for parameters.</param>
        /// <param name="parameters">The parameter values to bind to the query.</param>
        /// <returns>The query builder for method chaining.</returns>
        IProjectionQueryBuilder<TResult> Where(string whereClause, params object[] parameters);

        /// <summary>
        /// Adds an ORDER BY clause to the query in ascending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IProjectionQueryBuilder<TResult> OrderBy(string column);

        /// <summary>
        /// Adds an ORDER BY clause to the query in descending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IProjectionQueryBuilder<TResult> OrderByDescending(string column);

        /// <summary>
        /// Adds an additional ORDER BY clause (after OrderBy/OrderByDescending) in ascending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IProjectionQueryBuilder<TResult> ThenBy(string column);

        /// <summary>
        /// Adds an additional ORDER BY clause (after OrderBy/OrderByDescending) in descending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IProjectionQueryBuilder<TResult> ThenByDescending(string column);

        /// <summary>
        /// Limits the number of results returned (SQL LIMIT).
        /// </summary>
        /// <param name="count">The maximum number of results to return.</param>
        /// <returns>The query builder for method chaining.</returns>
        IProjectionQueryBuilder<TResult> Take(int count);

        /// <summary>
        /// Skips a specified number of results (SQL OFFSET).
        /// </summary>
        /// <param name="count">The number of results to skip.</param>
        /// <returns>The query builder for method chaining.</returns>
        IProjectionQueryBuilder<TResult> Skip(int count);

        /// <summary>
        /// Executes the query and returns all matching projected results.
        /// </summary>
        /// <returns>A list of projected results matching the query.</returns>
        Task<IEnumerable<TResult>> ToListAsync();

        /// <summary>
        /// Executes the query and returns the first projected result, or null if no results.
        /// </summary>
        /// <returns>The first projected result or null.</returns>
        Task<TResult?> FirstOrDefaultAsync();

        /// <summary>
        /// Executes the query and returns exactly one projected result. Throws if zero or more than one result.
        /// </summary>
        /// <returns>The single projected result.</returns>
        Task<TResult> SingleAsync();

        /// <summary>
        /// Executes the query and returns exactly one projected result or null. Throws if more than one result.
        /// </summary>
        /// <returns>The single projected result or null.</returns>
        Task<TResult?> SingleOrDefaultAsync();

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
