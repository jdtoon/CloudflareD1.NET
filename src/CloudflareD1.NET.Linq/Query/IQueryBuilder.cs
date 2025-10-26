using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        /// Adds a WHERE clause using a lambda expression predicate.
        /// </summary>
        /// <param name="predicate">The predicate expression to filter results.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds an ORDER BY clause to the query in ascending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> OrderBy(string column);

        /// <summary>
        /// Orders results by a property selected via lambda expression.
        /// </summary>
        /// <param name="keySelector">Expression to select the property to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Adds an ORDER BY clause to the query in descending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> OrderByDescending(string column);

        /// <summary>
        /// Orders results by a property selected via lambda expression in descending order.
        /// </summary>
        /// <param name="keySelector">Expression to select the property to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Adds an additional ORDER BY clause (after OrderBy/OrderByDescending) in ascending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> ThenBy(string column);

        /// <summary>
        /// Then orders results by a property selected via lambda expression.
        /// </summary>
        /// <param name="keySelector">Expression to select the property to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Adds an additional ORDER BY clause (after OrderBy/OrderByDescending) in descending order.
        /// </summary>
        /// <param name="column">The column name to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> ThenByDescending(string column);

        /// <summary>
        /// Then orders results by a property selected via lambda expression in descending order.
        /// </summary>
        /// <param name="keySelector">Expression to select the property to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Projects the query results into a new form using a lambda expression.
        /// </summary>
        /// <typeparam name="TResult">The type to project to.</typeparam>
        /// <param name="selector">Expression defining the projection.</param>
        /// <returns>A new query builder for the projected type.</returns>
        IProjectionQueryBuilder<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) where TResult : class, new();

        /// <summary>
        /// Performs an inner join with another table.
        /// </summary>
        /// <typeparam name="TInner">The type of the inner (right) entity to join.</typeparam>
        /// <typeparam name="TKey">The type of the join key.</typeparam>
        /// <param name="inner">The inner query builder.</param>
        /// <param name="outerKeySelector">Expression to select the key from the outer entity.</param>
        /// <param name="innerKeySelector">Expression to select the key from the inner entity.</param>
        /// <returns>A join query builder for projection.</returns>
        IJoinQueryBuilder<T, TInner, TKey> Join<TInner, TKey>(
            IQueryBuilder<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector)
            where TInner : class, new();

        /// <summary>
        /// Performs a left outer join with another table.
        /// </summary>
        /// <typeparam name="TInner">The type of the inner (right) entity to join.</typeparam>
        /// <typeparam name="TKey">The type of the join key.</typeparam>
        /// <param name="inner">The inner query builder.</param>
        /// <param name="outerKeySelector">Expression to select the key from the outer entity.</param>
        /// <param name="innerKeySelector">Expression to select the key from the inner entity.</param>
        /// <returns>A join query builder for projection.</returns>
        IJoinQueryBuilder<T, TInner, TKey> LeftJoin<TInner, TKey>(
            IQueryBuilder<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector)
            where TInner : class, new();

        /// <summary>
        /// Groups the query results by a key selector (SQL GROUP BY).
        /// </summary>
        /// <typeparam name="TKey">The type of the grouping key.</typeparam>
        /// <param name="keySelector">Expression to select the grouping key.</param>
        /// <returns>A group query builder for aggregations and projections.</returns>
        IGroupByQueryBuilder<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector);

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
        /// Removes duplicate rows from the result set (SQL DISTINCT).
        /// </summary>
        /// <returns>The query builder for method chaining.</returns>
        IQueryBuilder<T> Distinct();

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
