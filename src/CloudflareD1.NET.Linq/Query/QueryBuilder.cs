using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        private bool _isDistinct;

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

        /// <summary>
        /// Projects the query results into a new form using a lambda expression.
        /// </summary>
        /// <typeparam name="TResult">The type to project to.</typeparam>
        /// <param name="selector">Expression defining the projection.</param>
        /// <returns>A new query builder for the projected type.</returns>
        public IProjectionQueryBuilder<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) where TResult : class, new()
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            var visitor = new SelectExpressionVisitor(_mapper);
            var columns = visitor.GetColumns(selector.Body);
            var selectParameters = visitor.GetParameters();

            // Merge select parameters with WHERE clause parameters
            // Select parameters come first (in SELECT clause), then WHERE parameters
            var allParameters = new List<object>();
            allParameters.AddRange(selectParameters);
            allParameters.AddRange(_parameters);

            return new ProjectionQueryBuilder<TResult>(
                _client,
                _tableName,
                _mapper,
                columns,
                _whereClauses,
                allParameters,
                _orderByClauses,
                _takeCount,
                _skipCount);
        }

        /// <summary>
        /// Performs an inner join with another table.
        /// </summary>
        public IJoinQueryBuilder<T, TInner, TKey> Join<TInner, TKey>(
            IQueryBuilder<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector)
            where TInner : class, new()
        {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));
            if (outerKeySelector == null)
                throw new ArgumentNullException(nameof(outerKeySelector));
            if (innerKeySelector == null)
                throw new ArgumentNullException(nameof(innerKeySelector));

            return new JoinQueryBuilder<T, TInner, TKey>(
                _client,
                _tableName,
                inner,
                _mapper,
                outerKeySelector,
                innerKeySelector,
                JoinType.Inner,
                _whereClauses,
                _parameters);
        }

        /// <summary>
        /// Performs a left outer join with another table.
        /// </summary>
        public IJoinQueryBuilder<T, TInner, TKey> LeftJoin<TInner, TKey>(
            IQueryBuilder<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector)
            where TInner : class, new()
        {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));
            if (outerKeySelector == null)
                throw new ArgumentNullException(nameof(outerKeySelector));
            if (innerKeySelector == null)
                throw new ArgumentNullException(nameof(innerKeySelector));

            return new JoinQueryBuilder<T, TInner, TKey>(
                _client,
                _tableName,
                inner,
                _mapper,
                outerKeySelector,
                innerKeySelector,
                JoinType.Left,
                _whereClauses,
                _parameters);
        }

        /// <summary>
        /// Groups the query results by a key selector (SQL GROUP BY).
        /// </summary>
        /// <typeparam name="TKey">The type of the grouping key.</typeparam>
        /// <param name="keySelector">Expression to select the grouping key.</param>
        /// <returns>A group query builder for aggregations and projections.</returns>
        public IGroupByQueryBuilder<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            return new GroupByQueryBuilder<T, TKey>(
                _client,
                _tableName,
                _mapper,
                keySelector,
                _whereClauses,
                _parameters,
                _orderByClauses);
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

        /// <summary>
        /// Adds a WHERE clause using a lambda expression predicate.
        /// </summary>
        /// <param name="predicate">The predicate expression to filter results.</param>
        /// <returns>The query builder for method chaining.</returns>
        public IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var visitor = new SqlExpressionVisitor(_mapper);
            var sql = visitor.Translate(predicate.Body);
            var parameters = visitor.GetParameters();

            _whereClauses.Add(sql);
            if (parameters.Length > 0)
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

        /// <summary>
        /// Orders results by a property selected via lambda expression.
        /// </summary>
        /// <param name="keySelector">Expression to select the property to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        public IQueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var column = GetColumnNameFromExpression(keySelector);
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

        /// <summary>
        /// Orders results by a property selected via lambda expression in descending order.
        /// </summary>
        /// <param name="keySelector">Expression to select the property to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        public IQueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var column = GetColumnNameFromExpression(keySelector);
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

        /// <summary>
        /// Then orders results by a property selected via lambda expression.
        /// </summary>
        /// <param name="keySelector">Expression to select the property to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        public IQueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if (_orderByClauses.Count == 0)
                throw new InvalidOperationException("ThenBy must be called after OrderBy or OrderByDescending.");

            var column = GetColumnNameFromExpression(keySelector);
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

        /// <summary>
        /// Then orders results by a property selected via lambda expression in descending order.
        /// </summary>
        /// <param name="keySelector">Expression to select the property to order by.</param>
        /// <returns>The query builder for method chaining.</returns>
        public IQueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if (_orderByClauses.Count == 0)
                throw new InvalidOperationException("ThenByDescending must be called after OrderBy or OrderByDescending.");

            var column = GetColumnNameFromExpression(keySelector);
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

        /// <summary>
        /// Removes duplicate rows from the result set (SQL DISTINCT).
        /// </summary>
        /// <returns>The query builder for method chaining.</returns>
        public IQueryBuilder<T> Distinct()
        {
            _isDistinct = true;
            return this;
        }

        /// <summary>
        /// Combines this query with another using UNION (removes duplicates).
        /// </summary>
        /// <param name="other">The other query to combine with.</param>
        /// <returns>A set operation query builder.</returns>
        public ISetOperationQueryBuilder<T> Union(IQueryBuilder<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is QueryBuilder<T> queryBuilder))
                throw new ArgumentException("Query must be a QueryBuilder instance.", nameof(other));

            return new SetOperationQueryBuilder<T>(_client, _mapper, this, SetOperationType.Union, queryBuilder);
        }

        /// <summary>
        /// Combines this query with another using UNION ALL (keeps duplicates).
        /// </summary>
        /// <param name="other">The other query to combine with.</param>
        /// <returns>A set operation query builder.</returns>
        public ISetOperationQueryBuilder<T> UnionAll(IQueryBuilder<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is QueryBuilder<T> queryBuilder))
                throw new ArgumentException("Query must be a QueryBuilder instance.", nameof(other));

            return new SetOperationQueryBuilder<T>(_client, _mapper, this, SetOperationType.UnionAll, queryBuilder);
        }

        /// <summary>
        /// Returns rows that appear in both this query and another (INTERSECT).
        /// </summary>
        /// <param name="other">The other query to intersect with.</param>
        /// <returns>A set operation query builder.</returns>
        public ISetOperationQueryBuilder<T> Intersect(IQueryBuilder<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is QueryBuilder<T> queryBuilder))
                throw new ArgumentException("Query must be a QueryBuilder instance.", nameof(other));

            return new SetOperationQueryBuilder<T>(_client, _mapper, this, SetOperationType.Intersect, queryBuilder);
        }

        /// <summary>
        /// Returns rows from this query that don't appear in another (EXCEPT).
        /// </summary>
        /// <param name="other">The other query to exclude.</param>
        /// <returns>A set operation query builder.</returns>
        public ISetOperationQueryBuilder<T> Except(IQueryBuilder<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is QueryBuilder<T> queryBuilder))
                throw new ArgumentException("Query must be a QueryBuilder instance.", nameof(other));

            return new SetOperationQueryBuilder<T>(_client, _mapper, this, SetOperationType.Except, queryBuilder);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            var sql = BuildSql();
            var parameters = _parameters.ToArray();

            // Use QueryAsync for SELECT statements, not ExecuteAsync
            var result = await _client.QueryAsync(sql, parameters, cancellationToken);

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
        public async IAsyncEnumerable<T> ToAsyncEnumerable([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sql = BuildSql();
            var parameters = _parameters.ToArray();

            // Execute query to get results
            var result = await _client.QueryAsync(sql, parameters, cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results == null || result.Results.Count == 0)
            {
                yield break;
            }

            // Yield entities one at a time
            foreach (var row in result.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return _mapper.Map<T>(row);
            }
        }

        /// <inheritdoc />
        public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            // Optimize by only taking 1 result
            var originalTake = _takeCount;
            _takeCount = 1;

            try
            {
                var results = await ToListAsync(cancellationToken);
                return results.FirstOrDefault();
            }
            finally
            {
                _takeCount = originalTake;
            }
        }

        /// <inheritdoc />
        public async Task<T> SingleAsync(CancellationToken cancellationToken = default)
        {
            // Optimize by only taking 2 results (to detect if there's more than one)
            var originalTake = _takeCount;
            _takeCount = 2;

            try
            {
                var results = await ToListAsync(cancellationToken);
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
        public async Task<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            // Optimize by only taking 2 results (to detect if there's more than one)
            var originalTake = _takeCount;
            _takeCount = 2;

            try
            {
                var results = await ToListAsync(cancellationToken);
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
        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            var sql = BuildCountSql();
            var result = await _client.QueryAsync(sql, _parameters.ToArray(), cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Count query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results == null || result.Results.Count == 0)
            {
                return 0;
            }

            var firstRow = result.Results.FirstOrDefault();
            if (firstRow == null)
            {
                return 0;
            }

            // Try various possible key names for the count column
            object? countValue = null;
            if (firstRow.ContainsKey("count"))
            {
                countValue = firstRow["count"];
            }
            else if (firstRow.ContainsKey("COUNT(*)"))
            {
                countValue = firstRow["COUNT(*)"];
            }
            else if (firstRow.Count > 0)
            {
                // If no specific key found, try the first value
                countValue = firstRow.Values.FirstOrDefault();
            }

            if (countValue == null)
            {
                return 0;
            }

            // Handle JsonElement from System.Text.Json
            if (countValue is JsonElement jsonElement)
            {
                return jsonElement.GetInt32();
            }

            return Convert.ToInt32(countValue);
        }

        /// <summary>
        /// Extracts the column name from a lambda expression property selector.
        /// </summary>
        private string GetColumnNameFromExpression<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector.Body is MemberExpression memberExpr)
            {
                return _mapper.GetColumnName(memberExpr.Member.Name);
            }

            if (keySelector.Body is UnaryExpression unaryExpr &&
                unaryExpr.Operand is MemberExpression unaryMemberExpr)
            {
                return _mapper.GetColumnName(unaryMemberExpr.Member.Name);
            }

            throw new ArgumentException("Expression must be a simple property accessor.", nameof(keySelector));
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        {
            // Optimize by only checking if at least 1 result exists
            var originalTake = _takeCount;
            _takeCount = 1;

            try
            {
                var results = await ToListAsync(cancellationToken);
                return results.Any();
            }
            finally
            {
                _takeCount = originalTake;
            }
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            // Clone the WHERE clauses and parameters to preserve the current query state
            var whereClauses = new List<string>(_whereClauses);
            var parameters = new List<object>(_parameters);

            // Translate the predicate to SQL
            var visitor = new SqlExpressionVisitor(_mapper);
            var predicateSql = visitor.Translate(predicate.Body);
            var predicateParams = visitor.GetParameters();

            whereClauses.Add(predicateSql);
            parameters.AddRange(predicateParams);

            // Build subquery: SELECT 1 FROM table WHERE conditions
            var sql = $"SELECT 1 FROM {_tableName}";
            if (whereClauses.Count > 0)
            {
                sql += $" WHERE {string.Join(" AND ", whereClauses)}";
            }

            // Wrap in EXISTS check
            var existsSql = $"SELECT EXISTS({sql}) as result";

            // Execute the query
            var result = await _client.QueryAsync(existsSql, parameters.ToArray(), cancellationToken);
            var firstResult = result.Results.FirstOrDefault();
            if (firstResult != null && firstResult.TryGetValue("result", out var value))
            {
                return Convert.ToInt32(value) == 1;
            }
            return false;
        }

        /// <inheritdoc />
        public async Task<bool> AllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            // Clone the WHERE clauses and parameters to preserve the current query state
            var whereClauses = new List<string>(_whereClauses);
            var parameters = new List<object>(_parameters);

            // Negate the predicate: NOT (predicate)
            var negatedPredicate = Expression.Lambda<Func<T, bool>>(
                Expression.Not(predicate.Body),
                predicate.Parameters
            );

            // Translate the negated predicate to SQL
            var visitor = new SqlExpressionVisitor(_mapper);
            var predicateSql = visitor.Translate(negatedPredicate.Body);
            var predicateParams = visitor.GetParameters();

            whereClauses.Add(predicateSql);
            parameters.AddRange(predicateParams);

            // Build subquery: SELECT 1 FROM table WHERE conditions AND NOT predicate
            var sql = $"SELECT 1 FROM {_tableName}";
            if (whereClauses.Count > 0)
            {
                sql += $" WHERE {string.Join(" AND ", whereClauses)}";
            }

            // Wrap in NOT EXISTS check
            var notExistsSql = $"SELECT NOT EXISTS({sql}) as result";

            // Execute the query
            var result = await _client.QueryAsync(notExistsSql, parameters.ToArray(), cancellationToken);
            var firstResult = result.Results.FirstOrDefault();
            if (firstResult != null && firstResult.TryGetValue("result", out var value))
            {
                return Convert.ToInt32(value) == 1;
            }
            return false;
        }

        /// <summary>
        /// Builds the SQL query string from the current query builder state.
        /// </summary>
        /// <returns>The SQL query string.</returns>
        private string BuildSql()
        {
            var sql = new StringBuilder();
            sql.Append($"SELECT {(_isDistinct ? "DISTINCT " : "")}* FROM {_tableName}");

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
                // SQLite requires LIMIT before OFFSET, use a very large limit if not specified
                sql.Append(" LIMIT -1");
            }

            if (_skipCount.HasValue)
            {
                sql.Append($" OFFSET {_skipCount.Value}");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Builds the SQL query and returns both the SQL string and parameters.
        /// Used internally by set operation builders.
        /// </summary>
        /// <returns>A tuple containing the SQL query and parameters.</returns>
        internal (string Sql, object[] Parameters) BuildSqlInternal()
        {
            var sql = BuildSql();

            // If the query has ORDER BY, LIMIT, or OFFSET, wrap it as a subquery for set operations
            // This is required by SQLite - these clauses must come after the set operation, not before
            if (_orderByClauses.Count > 0 || _takeCount.HasValue || _skipCount.HasValue)
            {
                sql = $"SELECT * FROM ({sql})";
            }

            return (sql, _parameters.ToArray());
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
