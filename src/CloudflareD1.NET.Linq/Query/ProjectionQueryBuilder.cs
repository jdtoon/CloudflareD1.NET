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
    /// Query builder for projected query results (e.g., Select() projections).
    /// </summary>
    /// <typeparam name="TResult">The projection result type.</typeparam>
    public class ProjectionQueryBuilder<TResult> : IProjectionQueryBuilder<TResult> where TResult : class, new()
    {
        private readonly ID1Client _client;
        private readonly IEntityMapper _mapper;
        private readonly string _tableName;
        private readonly List<(string Column, string? Alias)> _selectColumns;
        private readonly List<string> _whereClauses;
        private readonly List<object> _parameters;
        private readonly List<(string Column, bool Descending)> _orderByClauses;
        private int? _takeCount;
        private int? _skipCount;

        /// <summary>
        /// Initializes a new instance of the ProjectionQueryBuilder class.
        /// </summary>
        public ProjectionQueryBuilder(
            ID1Client client,
            string tableName,
            IEntityMapper mapper,
            List<(string Column, string? Alias)> selectColumns,
            List<string> whereClauses,
            List<object> parameters,
            List<(string Column, bool Descending)> orderByClauses,
            int? takeCount,
            int? skipCount)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _selectColumns = selectColumns ?? throw new ArgumentNullException(nameof(selectColumns));

            // Clone the collections to avoid mutation
            _whereClauses = new List<string>(whereClauses ?? new List<string>());
            _parameters = new List<object>(parameters ?? new List<object>());
            _orderByClauses = new List<(string, bool)>(orderByClauses ?? new List<(string, bool)>());
            _takeCount = takeCount;
            _skipCount = skipCount;
        }

        /// <inheritdoc />
        public IProjectionQueryBuilder<TResult> Where(string whereClause, params object[] parameters)
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
        public IProjectionQueryBuilder<TResult> OrderBy(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            _orderByClauses.Add((column, false));
            return this;
        }

        /// <inheritdoc />
        public IProjectionQueryBuilder<TResult> OrderByDescending(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            _orderByClauses.Add((column, true));
            return this;
        }

        /// <inheritdoc />
        public IProjectionQueryBuilder<TResult> ThenBy(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            _orderByClauses.Add((column, false));
            return this;
        }

        /// <inheritdoc />
        public IProjectionQueryBuilder<TResult> ThenByDescending(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            _orderByClauses.Add((column, true));
            return this;
        }

        /// <inheritdoc />
        public IProjectionQueryBuilder<TResult> Take(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Take count must be non-negative.");

            _takeCount = count;
            return this;
        }

        /// <inheritdoc />
        public IProjectionQueryBuilder<TResult> Skip(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Skip count must be non-negative.");

            _skipCount = count;
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<TResult>> ToListAsync()
        {
            var sql = BuildSql();
            var parameters = _parameters.Count > 0 ? _parameters.ToArray() : (object?)null;
            var result = await _client.QueryAsync(sql, parameters);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results == null)
            {
                return Enumerable.Empty<TResult>();
            }

            return result.Results.Select(row => _mapper.Map<TResult>(row));
        }

        /// <inheritdoc />
        public async Task<TResult?> FirstOrDefaultAsync()
        {
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
        public async Task<TResult> SingleAsync()
        {
            var originalTake = _takeCount;
            _takeCount = 2;

            try
            {
                var resultsList = (await ToListAsync()).ToList();

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
        public async Task<TResult?> SingleOrDefaultAsync()
        {
            var originalTake = _takeCount;
            _takeCount = 2;

            try
            {
                var resultsList = (await ToListAsync()).ToList();

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
            var parameters = _parameters.Count > 0 ? _parameters.ToArray() : (object?)null;
            var result = await _client.QueryAsync(sql, parameters);

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

            if (countValue is JsonElement jsonElement)
            {
                return jsonElement.GetInt32();
            }

            return Convert.ToInt32(countValue);
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync()
        {
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
        private string BuildSql()
        {
            var sql = new StringBuilder();
            sql.Append("SELECT ");

            // Build column list with aliases
            var columnParts = _selectColumns.Select(col =>
            {
                if (string.IsNullOrEmpty(col.Alias))
                {
                    return col.Column;
                }
                else
                {
                    // Add alias: SELECT column AS alias
                    return $"{col.Column} AS {col.Alias}";
                }
            });
            sql.Append(string.Join(", ", columnParts));

            sql.Append($" FROM {_tableName}");

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
            else if (_skipCount.HasValue)
            {
                sql.Append(" LIMIT -1");
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
