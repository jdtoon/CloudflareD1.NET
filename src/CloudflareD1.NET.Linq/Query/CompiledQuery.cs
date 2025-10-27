using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Linq.Mapping;
using CloudflareD1.NET.Models;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Represents a compiled LINQ query that has been pre-processed for efficient repeated execution.
    /// Expression trees are compiled once and cached, avoiding repeated translation overhead.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <typeparam name="TResult">The result type returned by the query.</typeparam>
    public sealed class CompiledQuery<T, TResult> where T : class, new()
    {
        private readonly string _sql;
        private readonly object[] _parameters;
        private readonly IEntityMapper _mapper;
        private readonly Type _resultType;
        private readonly List<(string Column, string? Alias)>? _projections;

        internal CompiledQuery(
            string sql,
            object[] parameters,
            IEntityMapper mapper,
            Type resultType,
            List<(string Column, string? Alias)>? projections = null)
        {
            _sql = sql;
            _parameters = parameters;
            _mapper = mapper;
            _resultType = resultType;
            _projections = projections;
        }

        /// <summary>
        /// Gets the compiled SQL query string.
        /// </summary>
        public string Sql => _sql;

        /// <summary>
        /// Executes the compiled query.
        /// </summary>
        /// <param name="client">The D1 client to execute the query with.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The query results.</returns>
        public async Task<TResult> ExecuteAsync(
            ID1Client client,
            CancellationToken cancellationToken = default)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var result = await client.QueryAsync(_sql, _parameters, cancellationToken);

            // Handle different result types
            if (_resultType == typeof(List<T>))
            {
                var entities = _mapper.MapMany<T>(result.Results);
                return (TResult)(object)entities.ToList();
            }
            else if (_resultType.IsGenericType && _resultType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // Projected result type (List<TProjection>)
                var projectionType = _resultType.GetGenericArguments()[0];
                var mapMethod = typeof(IEntityMapper).GetMethod(nameof(IEntityMapper.MapMany))!
                    .MakeGenericMethod(projectionType);
                var entities = (IEnumerable<object>)mapMethod.Invoke(_mapper, new object[] { result.Results })!;
                var listType = typeof(List<>).MakeGenericType(projectionType);
                var list = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add")!;
                foreach (var entity in entities)
                {
                    addMethod.Invoke(list, new[] { entity });
                }
                return (TResult)list!;
            }
            else if (_resultType == typeof(int))
            {
                // Count/aggregate result
                return (TResult)(object)result.Results.Count;
            }
            else if (_resultType == typeof(bool))
            {
                // Any/All result
                return (TResult)(object)(result.Results.Count > 0);
            }
            else if (_resultType == typeof(T) || (_resultType.IsClass && _resultType != typeof(string)))
            {
                // Single entity or projection
                var entities = _mapper.MapMany<T>(result.Results);
                var first = entities.FirstOrDefault();
                return (TResult)(object)first!;
            }

            throw new InvalidOperationException($"Unsupported result type: {_resultType.Name}");
        }
    }

    /// <summary>
    /// Factory for creating compiled queries with expression tree caching.
    /// </summary>
    public static class CompiledQuery
    {
        private static readonly ConcurrentDictionary<ExpressionCacheKey, object> _cache = new();
        private static long _cacheHits;
        private static long _cacheMisses;

        /// <summary>
        /// Creates a compiled query that returns a list of entities.
        /// </summary>
        /// <typeparam name="T">The entity type to query.</typeparam>
        /// <param name="tableName">The table name to query.</param>
        /// <param name="queryBuilder">A function that builds the query using the query builder.</param>
        /// <param name="mapper">Optional entity mapper.</param>
        /// <returns>A compiled query that can be executed multiple times.</returns>
        public static CompiledQuery<T, List<T>> Create<T>(
            string tableName,
            Func<IQueryBuilder<T>, IQueryBuilder<T>> queryBuilder,
            IEntityMapper? mapper = null)
            where T : class, new()
        {
            mapper ??= new DefaultEntityMapper();

            // Create a dummy client for compilation
            var dummyClient = new DummyD1Client();
            var builder = new QueryBuilder<T>(dummyClient, tableName, mapper);
            var finalBuilder = queryBuilder(builder);

            // Extract SQL and parameters
            var (sql, parameters) = ExtractQueryInfo(finalBuilder as QueryBuilder<T>);

            return new CompiledQuery<T, List<T>>(
                sql,
                parameters,
                mapper,
                typeof(List<T>));
        }

        /// <summary>
        /// Creates a compiled query with a projection.
        /// </summary>
        /// <typeparam name="T">The entity type to query.</typeparam>
        /// <typeparam name="TResult">The projection result type.</typeparam>
        /// <param name="tableName">The table name to query.</param>
        /// <param name="queryBuilder">A function that builds the query with projection.</param>
        /// <param name="mapper">Optional entity mapper.</param>
        /// <returns>A compiled query that returns projected results.</returns>
        public static CompiledQuery<T, List<TResult>> Create<T, TResult>(
            string tableName,
            Func<IQueryBuilder<T>, IProjectionQueryBuilder<TResult>> queryBuilder,
            IEntityMapper? mapper = null)
            where T : class, new()
            where TResult : class, new()
        {
            mapper ??= new DefaultEntityMapper();

            var dummyClient = new DummyD1Client();
            var builder = new QueryBuilder<T>(dummyClient, tableName, mapper);
            var finalBuilder = queryBuilder(builder);

            var (sql, parameters) = ExtractProjectionQueryInfo(finalBuilder as ProjectionQueryBuilder<TResult>);

            return new CompiledQuery<T, List<TResult>>(
                sql,
                parameters,
                mapper,
                typeof(List<TResult>));
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public static (long CacheHits, long CacheMisses, int CacheSize) GetStatistics()
        {
            return (
                Interlocked.Read(ref _cacheHits),
                Interlocked.Read(ref _cacheMisses),
                _cache.Count
            );
        }

        /// <summary>
        /// Clears the expression cache.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);
        }

        private static (string Sql, object[] Parameters) ExtractQueryInfo<T>(QueryBuilder<T>? builder) where T : class, new()
        {
            if (builder == null)
                throw new ArgumentException("Query builder is invalid");

            // Use reflection to access private BuildSql method
            var buildSqlMethod = typeof(QueryBuilder<T>).GetMethod("BuildSql",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (buildSqlMethod == null)
                throw new InvalidOperationException("Could not find BuildSql method");

            var sql = (string)buildSqlMethod.Invoke(builder, null)!;

            var parametersField = typeof(QueryBuilder<T>).GetField("_parameters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var parameters = (List<object>)parametersField!.GetValue(builder)!;

            return (sql, parameters.ToArray());
        }

        private static (string Sql, object[] Parameters) ExtractProjectionQueryInfo<TResult>(ProjectionQueryBuilder<TResult>? builder)
            where TResult : class, new()
        {
            if (builder == null)
                throw new ArgumentException("Projection query builder is invalid");

            var buildSqlMethod = typeof(ProjectionQueryBuilder<TResult>).GetMethod("BuildSql",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (buildSqlMethod == null)
                throw new InvalidOperationException("Could not find BuildSql method");

            var sql = (string)buildSqlMethod.Invoke(builder, null)!;

            var parametersField = typeof(ProjectionQueryBuilder<TResult>).GetField("_parameters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var parameters = (List<object>)parametersField!.GetValue(builder)!;

            return (sql, parameters.ToArray());
        }

        private struct ExpressionCacheKey : IEquatable<ExpressionCacheKey>
        {
            public string ExpressionString { get; }
            public Type EntityType { get; }

            public ExpressionCacheKey(string expressionString, Type entityType)
            {
                ExpressionString = expressionString;
                EntityType = entityType;
            }

            public bool Equals(ExpressionCacheKey other) =>
                ExpressionString == other.ExpressionString &&
                EntityType == other.EntityType;

            public override bool Equals(object? obj) =>
                obj is ExpressionCacheKey key && Equals(key);

            public override int GetHashCode() =>
                HashCode.Combine(ExpressionString, EntityType);
        }

        /// <summary>
        /// Dummy D1Client for compilation only (doesn't execute queries).
        /// </summary>
        private class DummyD1Client : ID1Client
        {
            public Task<D1QueryResult> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("DummyD1Client is for compilation only");

            public Task<D1QueryResult> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("DummyD1Client is for compilation only");

            public Task<D1QueryResult[]> BatchAsync(List<D1Statement> statements, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("DummyD1Client is for compilation only");

            public Task<D1QueryResult[]> BatchAsync(params string[] sqlStatements)
                => throw new NotSupportedException("DummyD1Client is for compilation only");
        }
    }
}
