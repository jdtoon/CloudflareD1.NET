using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// IQueryable implementation for projected query results.
    /// </summary>
    /// <typeparam name="TResult">The projection result type.</typeparam>
    public class D1ProjectionQueryable<TResult> : IOrderedQueryable<TResult> where TResult : class, new()
    {
        private readonly IProjectionQueryBuilder<TResult> _queryBuilder;
        private readonly D1QueryProvider _provider;

        /// <summary>
        /// Initializes a new instance of the D1ProjectionQueryable class.
        /// </summary>
        public D1ProjectionQueryable(IProjectionQueryBuilder<TResult> queryBuilder, D1QueryProvider provider)
        {
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = Expression.Constant(this);
        }

        /// <summary>
        /// Initializes a new instance with an expression.
        /// </summary>
        public D1ProjectionQueryable(IProjectionQueryBuilder<TResult> queryBuilder, D1QueryProvider provider, Expression expression)
        {
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <summary>
        /// Gets the query builder for executing queries.
        /// </summary>
        internal IProjectionQueryBuilder<TResult> QueryBuilder => _queryBuilder;

        /// <inheritdoc />
        public Type ElementType => typeof(TResult);

        /// <inheritdoc />
        public Expression Expression { get; }

        /// <inheritdoc />
        public IQueryProvider Provider => _provider;

        /// <inheritdoc />
        public IEnumerator<TResult> GetEnumerator()
        {
            // Execute the query synchronously (blocks)
            // Note: This is not ideal for async code, but IQueryable requires sync enumeration
            var task = _queryBuilder.ToListAsync();
            task.Wait();
            return task.Result.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Executes the query asynchronously and returns all results.
        /// This is the preferred way to execute queries (avoids blocking).
        /// </summary>
        public Task<IEnumerable<TResult>> ToListAsync()
        {
            return _queryBuilder.ToListAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first result or null.
        /// </summary>
        public Task<TResult?> FirstOrDefaultAsync()
        {
            return _queryBuilder.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns exactly one result.
        /// </summary>
        public Task<TResult> SingleAsync()
        {
            return _queryBuilder.SingleAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns exactly one result or null.
        /// </summary>
        public Task<TResult?> SingleOrDefaultAsync()
        {
            return _queryBuilder.SingleOrDefaultAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns the count of matching records.
        /// </summary>
        public Task<int> CountAsync()
        {
            return _queryBuilder.CountAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns whether any records match.
        /// </summary>
        public Task<bool> AnyAsync()
        {
            return _queryBuilder.AnyAsync();
        }
    }
}
