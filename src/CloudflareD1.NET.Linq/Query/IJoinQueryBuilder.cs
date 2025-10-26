using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Query builder interface for Join operations.
    /// </summary>
    /// <typeparam name="TOuter">The outer (left) entity type.</typeparam>
    /// <typeparam name="TInner">The inner (right) entity type.</typeparam>
    /// <typeparam name="TKey">The join key type.</typeparam>
    public interface IJoinQueryBuilder<TOuter, TInner, TKey>
        where TOuter : class, new()
        where TInner : class, new()
    {
        /// <summary>
        /// Projects the joined results into a new form.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="selector">Expression defining how to combine outer and inner elements.</param>
        /// <returns>A query builder for the projected joined results.</returns>
        IJoinProjectionQueryBuilder<TResult> Select<TResult>(
            Expression<Func<TOuter, TInner, TResult>> selector)
            where TResult : class, new();
    }

    /// <summary>
    /// Query builder interface for projected Join results.
    /// </summary>
    /// <typeparam name="TResult">The projection result type.</typeparam>
    public interface IJoinProjectionQueryBuilder<TResult> where TResult : class, new()
    {
        /// <summary>
        /// Filters the joined results.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <returns>The query builder for method chaining.</returns>
        IJoinProjectionQueryBuilder<TResult> Where(Expression<Func<TResult, bool>> predicate);

        /// <summary>
        /// Orders the joined results by a column in ascending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IJoinProjectionQueryBuilder<TResult> OrderBy(string column);

        /// <summary>
        /// Orders the joined results by a column in descending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IJoinProjectionQueryBuilder<TResult> OrderByDescending(string column);

        /// <summary>
        /// Limits the number of results returned (SQL LIMIT).
        /// </summary>
        /// <param name="count">The maximum number of results to return.</param>
        /// <returns>The query builder for method chaining.</returns>
        IJoinProjectionQueryBuilder<TResult> Take(int count);

        /// <summary>
        /// Skips a specified number of results (SQL OFFSET).
        /// </summary>
        /// <param name="count">The number of results to skip.</param>
        /// <returns>The query builder for method chaining.</returns>
        IJoinProjectionQueryBuilder<TResult> Skip(int count);

        /// <summary>
        /// Executes the query asynchronously and returns all results.
        /// </summary>
        /// <returns>Collection of joined results.</returns>
        Task<IEnumerable<TResult>> ToListAsync();

        /// <summary>
        /// Executes the query asynchronously and returns the first result or null.
        /// </summary>
        /// <returns>The first result or null.</returns>
        Task<TResult?> FirstOrDefaultAsync();

        /// <summary>
        /// Executes the query asynchronously and returns exactly one result.
        /// </summary>
        /// <returns>The single result.</returns>
        Task<TResult> SingleAsync();

        /// <summary>
        /// Executes the query asynchronously and returns exactly one result or null.
        /// </summary>
        /// <returns>The single result or null.</returns>
        Task<TResult?> SingleOrDefaultAsync();

        /// <summary>
        /// Executes the query asynchronously and returns the count of joined results.
        /// </summary>
        /// <returns>The number of results.</returns>
        Task<int> CountAsync();

        /// <summary>
        /// Executes the query asynchronously and returns whether any results exist.
        /// </summary>
        /// <returns>True if any results exist, false otherwise.</returns>
        Task<bool> AnyAsync();
    }
}
