using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Query builder interface for GroupBy operations with aggregations.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TKey">The grouping key type.</typeparam>
    public interface IGroupByQueryBuilder<TSource, TKey>
        where TSource : class, new()
    {
        /// <summary>
        /// Projects grouped results with aggregations into a new form.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="selector">Expression defining the projection with aggregations.</param>
        /// <returns>A query builder for the projected grouped results.</returns>
        IGroupByProjectionQueryBuilder<TResult> Select<TResult>(Expression<Func<ID1Grouping<TKey, TSource>, TResult>> selector)
            where TResult : class, new();

        /// <summary>
        /// Filters grouped results based on aggregate conditions (SQL HAVING).
        /// </summary>
        /// <param name="predicate">Expression defining the filter condition.</param>
        /// <returns>The query builder for method chaining.</returns>
        IGroupByQueryBuilder<TSource, TKey> Having(Expression<Func<ID1Grouping<TKey, TSource>, bool>> predicate);
    }

    /// <summary>
    /// Query builder interface for projected GroupBy results.
    /// </summary>
    /// <typeparam name="TResult">The projection result type.</typeparam>
    public interface IGroupByProjectionQueryBuilder<TResult> where TResult : class, new()
    {
        /// <summary>
        /// Orders the grouped results by a column in ascending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IGroupByProjectionQueryBuilder<TResult> OrderBy(string column);

        /// <summary>
        /// Orders the grouped results by a column in descending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IGroupByProjectionQueryBuilder<TResult> OrderByDescending(string column);

        /// <summary>
        /// Limits the number of results returned (SQL LIMIT).
        /// </summary>
        /// <param name="count">The maximum number of results to return.</param>
        /// <returns>The query builder for method chaining.</returns>
        IGroupByProjectionQueryBuilder<TResult> Take(int count);

        /// <summary>
        /// Skips a specified number of results (SQL OFFSET).
        /// </summary>
        /// <param name="count">The number of results to skip.</param>
        /// <returns>The query builder for method chaining.</returns>
        IGroupByProjectionQueryBuilder<TResult> Skip(int count);

        /// <summary>
        /// Executes the query asynchronously and returns all results.
        /// </summary>
        /// <returns>Collection of grouped results.</returns>
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
        /// Executes the query asynchronously and returns the count of grouped results.
        /// </summary>
        /// <returns>The number of groups.</returns>
        Task<int> CountAsync();

        /// <summary>
        /// Executes the query asynchronously and returns whether any groups exist.
        /// </summary>
        /// <returns>True if any groups exist, false otherwise.</returns>
        Task<bool> AnyAsync();
    }
}
