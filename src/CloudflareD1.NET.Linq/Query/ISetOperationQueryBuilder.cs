using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Query builder interface for set operations (UNION, INTERSECT, EXCEPT).
    /// </summary>
    /// <typeparam name="T">The entity type to query.</typeparam>
    public interface ISetOperationQueryBuilder<T> where T : class, new()
    {
        /// <summary>
        /// Combines this query with another using UNION (removes duplicates).
        /// </summary>
        /// <param name="other">The other query to combine with.</param>
        /// <returns>A set operation query builder for method chaining.</returns>
        ISetOperationQueryBuilder<T> Union(IQueryBuilder<T> other);

        /// <summary>
        /// Combines this query with another using UNION ALL (keeps duplicates).
        /// </summary>
        /// <param name="other">The other query to combine with.</param>
        /// <returns>A set operation query builder for method chaining.</returns>
        ISetOperationQueryBuilder<T> UnionAll(IQueryBuilder<T> other);

        /// <summary>
        /// Returns rows that appear in both this query and another (INTERSECT).
        /// </summary>
        /// <param name="other">The other query to intersect with.</param>
        /// <returns>A set operation query builder for method chaining.</returns>
        ISetOperationQueryBuilder<T> Intersect(IQueryBuilder<T> other);

        /// <summary>
        /// Returns rows from this query that don't appear in another (EXCEPT).
        /// </summary>
        /// <param name="other">The other query to exclude.</param>
        /// <returns>A set operation query builder for method chaining.</returns>
        ISetOperationQueryBuilder<T> Except(IQueryBuilder<T> other);

        /// <summary>
        /// Executes the set operation query and returns all matching entities.
        /// </summary>
        /// <returns>A list of entities matching the query.</returns>
        Task<IEnumerable<T>> ToListAsync();

        /// <summary>
        /// Executes the query and returns the first entity, or null if no results.
        /// </summary>
        /// <returns>The first entity or null.</returns>
        Task<T?> FirstOrDefaultAsync();

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
