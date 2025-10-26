using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CloudflareD1.NET.Linq.Mapping;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// IQueryable implementation that wraps QueryBuilder for LINQ support.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class D1Queryable<T> : IOrderedQueryable<T> where T : class, new()
    {
        private readonly QueryBuilder<T> _queryBuilder;
        private readonly D1QueryProvider _provider;

        /// <summary>
        /// Initializes a new instance of the D1Queryable class.
        /// </summary>
        public D1Queryable(QueryBuilder<T> queryBuilder, D1QueryProvider provider)
        {
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = Expression.Constant(this);
        }

        /// <summary>
        /// Initializes a new instance with an expression.
        /// </summary>
        public D1Queryable(QueryBuilder<T> queryBuilder, D1QueryProvider provider, Expression expression)
        {
            _queryBuilder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <summary>
        /// Gets the query builder for executing queries.
        /// </summary>
        internal QueryBuilder<T> QueryBuilder => _queryBuilder;

        /// <inheritdoc />
        public Type ElementType => typeof(T);

        /// <inheritdoc />
        public Expression Expression { get; }

        /// <inheritdoc />
        public IQueryProvider Provider => _provider;

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
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
        public Task<IEnumerable<T>> ToListAsync()
        {
            return _queryBuilder.ToListAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns the first result or null.
        /// </summary>
        public Task<T?> FirstOrDefaultAsync()
        {
            return _queryBuilder.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns a single result.
        /// </summary>
        public Task<T> SingleAsync()
        {
            return _queryBuilder.SingleAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns a single result or null.
        /// </summary>
        public Task<T?> SingleOrDefaultAsync()
        {
            return _queryBuilder.SingleOrDefaultAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns the count.
        /// </summary>
        public Task<int> CountAsync()
        {
            return _queryBuilder.CountAsync();
        }

        /// <summary>
        /// Executes the query asynchronously and returns whether any results exist.
        /// </summary>
        public Task<bool> AnyAsync()
        {
            return _queryBuilder.AnyAsync();
        }
    }
}
