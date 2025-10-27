using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Linq.Mapping;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Query builder for set operations (UNION, INTERSECT, EXCEPT).
    /// </summary>
    /// <typeparam name="T">The entity type to query.</typeparam>
    internal class SetOperationQueryBuilder<T> : ISetOperationQueryBuilder<T> where T : class, new()
    {
        private readonly ID1Client _client;
        private readonly IEntityMapper _mapper;
        private readonly List<(SetOperationType Type, QueryBuilder<T> Query)> _operations;
        private readonly QueryBuilder<T> _baseQuery;

        /// <summary>
        /// Initializes a new instance of the SetOperationQueryBuilder class.
        /// </summary>
        /// <param name="client">The D1 client for executing queries.</param>
        /// <param name="mapper">The entity mapper for converting results.</param>
        /// <param name="baseQuery">The base query to start with.</param>
        /// <param name="operationType">The type of set operation.</param>
        /// <param name="otherQuery">The other query to combine.</param>
        internal SetOperationQueryBuilder(
            ID1Client client,
            IEntityMapper mapper,
            QueryBuilder<T> baseQuery,
            SetOperationType operationType,
            QueryBuilder<T> otherQuery)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _baseQuery = baseQuery ?? throw new ArgumentNullException(nameof(baseQuery));

            _operations = new List<(SetOperationType, QueryBuilder<T>)>
            {
                (operationType, otherQuery ?? throw new ArgumentNullException(nameof(otherQuery)))
            };
        }

        /// <inheritdoc />
        public ISetOperationQueryBuilder<T> Union(IQueryBuilder<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is QueryBuilder<T> queryBuilder))
                throw new ArgumentException("Query must be a QueryBuilder instance.", nameof(other));

            _operations.Add((SetOperationType.Union, queryBuilder));
            return this;
        }

        /// <inheritdoc />
        public ISetOperationQueryBuilder<T> UnionAll(IQueryBuilder<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is QueryBuilder<T> queryBuilder))
                throw new ArgumentException("Query must be a QueryBuilder instance.", nameof(other));

            _operations.Add((SetOperationType.UnionAll, queryBuilder));
            return this;
        }

        /// <inheritdoc />
        public ISetOperationQueryBuilder<T> Intersect(IQueryBuilder<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is QueryBuilder<T> queryBuilder))
                throw new ArgumentException("Query must be a QueryBuilder instance.", nameof(other));

            _operations.Add((SetOperationType.Intersect, queryBuilder));
            return this;
        }

        /// <inheritdoc />
        public ISetOperationQueryBuilder<T> Except(IQueryBuilder<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is QueryBuilder<T> queryBuilder))
                throw new ArgumentException("Query must be a QueryBuilder instance.", nameof(other));

            _operations.Add((SetOperationType.Except, queryBuilder));
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> ToListAsync()
        {
            var (sql, parameters) = BuildSql();
            var result = await _client.QueryAsync(sql, parameters);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results == null || result.Results.Count == 0)
            {
                return Enumerable.Empty<T>();
            }

            return _mapper.MapMany<T>(result.Results);
        }

        /// <inheritdoc />
        public async Task<T?> FirstOrDefaultAsync()
        {
            var results = await ToListAsync();
            return results.FirstOrDefault();
        }

        /// <inheritdoc />
        public async Task<int> CountAsync()
        {
            var (sql, parameters) = BuildSql();
            var countSql = $"SELECT COUNT(*) as count FROM ({sql}) as subquery";
            var result = await _client.QueryAsync(countSql, parameters);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results != null && result.Results.Count > 0 && result.Results[0].ContainsKey("count"))
            {
                var countValue = result.Results[0]["count"];
                if (countValue is long longValue)
                    return (int)longValue;
                if (countValue is int intValue)
                    return intValue;
            }

            return 0;
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync()
        {
            var count = await CountAsync();
            return count > 0;
        }

        /// <summary>
        /// Builds the SQL query with set operations.
        /// </summary>
        /// <returns>A tuple containing the SQL query and parameters.</returns>
        private (string Sql, object[] Parameters) BuildSql()
        {
            var allParameters = new List<object>();
            var sqlParts = new List<string>();

            // Add base query
            var (baseSql, baseParams) = _baseQuery.BuildSqlInternal();
            sqlParts.Add(baseSql);
            allParameters.AddRange(baseParams);

            // Add set operations
            foreach (var (type, query) in _operations)
            {
                var (querySql, queryParams) = query.BuildSqlInternal();

                var operatorKeyword = type switch
                {
                    SetOperationType.Union => "UNION",
                    SetOperationType.UnionAll => "UNION ALL",
                    SetOperationType.Intersect => "INTERSECT",
                    SetOperationType.Except => "EXCEPT",
                    _ => throw new InvalidOperationException($"Unknown set operation type: {type}")
                };

                sqlParts.Add(operatorKeyword);
                sqlParts.Add(querySql);
                allParameters.AddRange(queryParams);
            }

            var finalSql = string.Join(" ", sqlParts);
            return (finalSql, allParameters.ToArray());
        }
    }
}
