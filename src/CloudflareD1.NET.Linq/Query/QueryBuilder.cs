using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Linq.Mapping;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Fluent query builder for constructing and executing SQL queries against Cloudflare D1.
    /// </summary>
    /// <typeparam name="T">The entity type to query.</typeparam>
    public class QueryBuilder<T> : IQueryBuilder<T> where T : class, new()
    {
        private readonly ID1Client _client;
        private readonly IEntityMapper _mapper;
        private readonly string _tableName;
        private readonly List<string> _whereClauses;
        private readonly List<object> _parameters;
        private readonly List<(string Column, bool Descending)> _orderByClauses;
        private int? _takeCount;
        private int? _skipCount;

        /// <summary>
        /// Initializes a new instance of the QueryBuilder class.
        /// </summary>
        /// <param name="client">The D1 client to execute queries with.</param>
        /// <param name="tableName">The name of the table to query.</param>
        /// <param name="mapper">The entity mapper to use for result mapping.</param>
        public QueryBuilder(ID1Client client, string tableName, IEntityMapper? mapper = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _mapper = mapper ?? new DefaultEntityMapper();
            _whereClauses = new List<string>();
            _parameters = new List<object>();
            _orderByClauses = new List<(string, bool)>();
        }

        /// <inheritdoc />
        public IQueryBuilder<T> Where(string whereClause, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(whereClause))
                throw new ArgumentException("WHERE clause cannot be empty.", nameof(whereClause));

            _whereClauses.Add(whereClause);
            if (parameters != null && parameters.Length > 0)
            {
                _parameters.AddRange(parameters);
            }

            return this;
        }

        /// <inheritdoc />
        public IQueryBuilder<T> OrderBy(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            _orderByClauses.Add((column, false));
            return this;
        }

        /// <inheritdoc />
        public IQueryBuilder<T> OrderByDescending(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            _orderByClauses.Add((column, true));
            return this;
        }

        /// <inheritdoc />
        public IQueryBuilder<T> ThenBy(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            if (_orderByClauses.Count == 0)
                throw new InvalidOperationException("ThenBy must be called after OrderBy or OrderByDescending.");

            _orderByClauses.Add((column, false));
            return this;
        }

        /// <inheritdoc />
        public IQueryBuilder<T> ThenByDescending(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            if (_orderByClauses.Count == 0)
                throw new InvalidOperationException("ThenByDescending must be called after OrderBy or OrderByDescending.");

            _orderByClauses.Add((column, true));
            return this;
        }

        /// <inheritdoc />
        public IQueryBuilder<T> Take(int count)
        {
            if (count <= 0)
                throw new ArgumentException("Take count must be greater than zero.", nameof(count));

            _takeCount = count;
            return this;
        }

        /// <inheritdoc />
        public IQueryBuilder<T> Skip(int count)
        {
            if (count < 0)
                throw new ArgumentException("Skip count cannot be negative.", nameof(count));

            _skipCount = count;
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> ToListAsync()
        {
            var sql = BuildSql();
            var parameters = _parameters.ToArray();

            // Use QueryAsync for SELECT statements, not ExecuteAsync
            var result = await _client.QueryAsync(sql, parameters);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results == null || result.Results.Count == 0)
            {
                return Enumerable.Empty<T>();
            }

            // D1QueryResult.Results is already List<Dictionary<string, object?>>
            return _mapper.MapMany<T>(result.Results);
        }

        /// <inheritdoc />
        public async Task<T?> FirstOrDefaultAsync()
        {
            // Optimize by only taking 1 result
            var originalTake = _takeCount;
            _takeCount = 1;

            try
            {
                var results = await ToListAsync();
                return results.FirstOrDefault();
            }
            finally
            {
                _takeCount = originalTake;
            }
        }

        /// <inheritdoc />
        public async Task<T> SingleAsync()
        {
            // Optimize by only taking 2 results (to detect if there's more than one)
            var originalTake = _takeCount;
            _takeCount = 2;

            try
            {
                var results = await ToListAsync();
                var resultsList = results.ToList();

                if (resultsList.Count == 0)
                {
                    throw new InvalidOperationException("Sequence contains no elements.");
                }

                if (resultsList.Count > 1)
                {
                    throw new InvalidOperationException("Sequence contains more than one element.");
                }

                return resultsList[0];
            }
            finally
            {
                _takeCount = originalTake;
            }
        }

        /// <inheritdoc />
        public async Task<T?> SingleOrDefaultAsync()
        {
            // Optimize by only taking 2 results (to detect if there's more than one)
            var originalTake = _takeCount;
            _takeCount = 2;

            try
            {
                var results = await ToListAsync();
                var resultsList = results.ToList();

                if (resultsList.Count == 0)
                {
                    return null;
                }

                if (resultsList.Count > 1)
                {
                    throw new InvalidOperationException("Sequence contains more than one element.");
                }

                return resultsList[0];
            }
            finally
            {
                _takeCount = originalTake;
            }
        }

        /// <inheritdoc />
        public async Task<int> CountAsync()
        {
            var sql = BuildCountSql();
            var result = await _client.QueryAsync(sql, _parameters.ToArray());

            if (!result.Success)
            {
                throw new InvalidOperationException($"Count query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results == null || result.Results.Count == 0)
            {
                return 0;
            }

            var firstRow = result.Results.FirstOrDefault();
            if (firstRow == null || !firstRow.ContainsKey("count"))
            {
                return 0;
            }

            var countValue = firstRow["count"];

            // Handle JsonElement from System.Text.Json
            if (countValue is JsonElement jsonElement)
            {
                return jsonElement.GetInt32();
            }

            return Convert.ToInt32(countValue);
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync()
        {
            // Optimize by only checking if at least 1 result exists
            var originalTake = _takeCount;
            _takeCount = 1;

            try
            {
                var results = await ToListAsync();
                return results.Any();
            }
            finally
            {
                _takeCount = originalTake;
            }
        }

        /// <summary>
        /// Builds the SQL query string from the current query builder state.
        /// </summary>
        /// <returns>The SQL query string.</returns>
        private string BuildSql()
        {
            var sql = new StringBuilder();
            sql.Append($"SELECT * FROM {_tableName}");

            if (_whereClauses.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", _whereClauses));
            }

            if (_orderByClauses.Count > 0)
            {
                sql.Append(" ORDER BY ");
                var orderByParts = _orderByClauses.Select(clause =>
                    $"{clause.Column}{(clause.Descending ? " DESC" : " ASC")}");
                sql.Append(string.Join(", ", orderByParts));
            }

            if (_takeCount.HasValue)
            {
                sql.Append($" LIMIT {_takeCount.Value}");
            }

            if (_skipCount.HasValue)
            {
                sql.Append($" OFFSET {_skipCount.Value}");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Builds a COUNT SQL query string from the current query builder state.
        /// </summary>
        /// <returns>The COUNT SQL query string.</returns>
        private string BuildCountSql()
        {
            var sql = new StringBuilder();
            sql.Append($"SELECT COUNT(*) as count FROM {_tableName}");

            if (_whereClauses.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", _whereClauses));
            }

            return sql.ToString();
        }
    }
}
