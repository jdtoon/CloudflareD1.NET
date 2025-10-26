using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Linq.Mapping;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Query builder for GroupBy operations with aggregations.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TKey">The grouping key type.</typeparam>
    public class GroupByQueryBuilder<TSource, TKey> : IGroupByQueryBuilder<TSource, TKey>
        where TSource : class, new()
    {
        private readonly ID1Client _client;
        private readonly string _tableName;
        private readonly IEntityMapper _mapper;
        private readonly Expression<Func<TSource, TKey>> _keySelector;
        private readonly List<string> _whereClauses;
        private readonly List<object> _parameters;
        private readonly List<(string Column, bool Descending)> _orderByClauses;
        private readonly List<string> _havingClauses;

        /// <summary>
        /// Initializes a new instance of the GroupByQueryBuilder class.
        /// </summary>
        public GroupByQueryBuilder(
            ID1Client client,
            string tableName,
            IEntityMapper mapper,
            Expression<Func<TSource, TKey>> keySelector,
            List<string> whereClauses,
            List<object> parameters,
            List<(string Column, bool Descending)> orderByClauses)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _whereClauses = whereClauses ?? new List<string>();
            _parameters = parameters ?? new List<object>();
            _orderByClauses = orderByClauses ?? new List<(string, bool)>();
            _havingClauses = new List<string>();
        }

        /// <inheritdoc />
        public IGroupByProjectionQueryBuilder<TResult> Select<TResult>(
            Expression<Func<ID1Grouping<TKey, TSource>, TResult>> selector)
            where TResult : class, new()
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return new GroupByProjectionQueryBuilder<TResult>(
                _client,
                _tableName,
                _mapper,
                _keySelector,
                selector,
                _whereClauses,
                _parameters,
                _havingClauses,
                _orderByClauses);
        }

        /// <inheritdoc />
        public IGroupByQueryBuilder<TSource, TKey> Having(
            Expression<Func<ID1Grouping<TKey, TSource>, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            // Translate the Having predicate to SQL
            // This is simplified - a full implementation would need HavingExpressionVisitor
            var havingClause = TranslateHavingPredicate(predicate);
            _havingClauses.Add(havingClause);

            return this;
        }

        private string TranslateHavingPredicate(Expression<Func<ID1Grouping<TKey, TSource>, bool>> predicate)
        {
            // For now, throw - full Having() translation needs more work
            throw new NotSupportedException(
                "Having() with complex predicates is not yet fully implemented. " +
                "Use Select() to project groups and filter client-side for now.");
        }
    }

    /// <summary>
    /// Query builder for projected GroupBy results.
    /// </summary>
    /// <typeparam name="TResult">The projection result type.</typeparam>
    public class GroupByProjectionQueryBuilder<TResult> : IGroupByProjectionQueryBuilder<TResult>
        where TResult : class, new()
    {
        private readonly ID1Client _client;
        private readonly string _tableName;
        private readonly IEntityMapper _mapper;
        private readonly object _keySelector; // Expression for grouping key
        private readonly object _selector; // Expression for projection
        private readonly List<string> _whereClauses;
        private readonly List<object> _parameters;
        private readonly List<string> _havingClauses;
        private List<(string Column, bool Descending)> _orderByClauses;
        private int? _takeCount;
        private int? _skipCount;

        /// <summary>
        /// Initializes a new instance of the GroupByProjectionQueryBuilder class.
        /// </summary>
        public GroupByProjectionQueryBuilder(
            ID1Client client,
            string tableName,
            IEntityMapper mapper,
            object keySelector,
            object selector,
            List<string> whereClauses,
            List<object> parameters,
            List<string> havingClauses,
            List<(string Column, bool Descending)> orderByClauses)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _whereClauses = whereClauses ?? new List<string>();
            _parameters = parameters ?? new List<object>();
            _havingClauses = havingClauses ?? new List<string>();
            _orderByClauses = orderByClauses ?? new List<(string, bool)>();
        }

        /// <inheritdoc />
        public IGroupByProjectionQueryBuilder<TResult> OrderBy(string column)
        {
            _orderByClauses.Add((column, false));
            return this;
        }

        /// <inheritdoc />
        public IGroupByProjectionQueryBuilder<TResult> OrderByDescending(string column)
        {
            _orderByClauses.Add((column, true));
            return this;
        }

        /// <inheritdoc />
        public IGroupByProjectionQueryBuilder<TResult> Take(int count)
        {
            _takeCount = count;
            return this;
        }

        /// <inheritdoc />
        public IGroupByProjectionQueryBuilder<TResult> Skip(int count)
        {
            _skipCount = count;
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<TResult>> ToListAsync()
        {
            var sql = BuildSelectSql();
            var parameters = _parameters.Count > 0 ? _parameters.ToArray() : (object?)null;
            var result = await _client.QueryAsync(sql, parameters);

            if (!result.Success)
            {
                throw new InvalidOperationException($"GroupBy query failed: {string.Join(", ", result.Errors)}");
            }

            var rows = ConvertResultsToRows(result.Results);
            return _mapper.MapMany<TResult>(rows);
        }

        /// <inheritdoc />
        public async Task<TResult?> FirstOrDefaultAsync()
        {
            // Temporarily set Take(1) for this query
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
            var results = await ToListAsync();
            return results.Single();
        }

        /// <inheritdoc />
        public async Task<TResult?> SingleOrDefaultAsync()
        {
            var results = await ToListAsync();
            return results.SingleOrDefault();
        }

        /// <inheritdoc />
        public async Task<int> CountAsync()
        {
            // Count the number of groups, not the number of records
            var sql = BuildCountSql();
            var parameters = _parameters.Count > 0 ? _parameters.ToArray() : (object?)null;
            var result = await _client.QueryAsync(sql, parameters);

            if (!result.Success)
            {
                throw new InvalidOperationException($"GroupBy count failed: {string.Join(", ", result.Errors)}");
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
                countValue = firstRow.Values.FirstOrDefault();
            }

            if (countValue == null)
            {
                return 0;
            }

            if (countValue is JsonElement jsonElement)
            {
                return jsonElement.GetInt32();
            }

            return Convert.ToInt32(countValue);
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync()
        {
            var count = await CountAsync();
            return count > 0;
        }

        private string BuildSelectSql()
        {
            var sql = new StringBuilder();
            sql.Append("SELECT ");

            // Build the SELECT clause with grouping key and aggregates
            var selectClause = BuildSelectClause();
            sql.Append(selectClause);

            sql.Append($" FROM {_tableName}");

            // WHERE clause
            if (_whereClauses.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", _whereClauses));
            }

            // GROUP BY clause
            sql.Append(" GROUP BY ");
            var groupByColumns = GetGroupByColumns();
            sql.Append(string.Join(", ", groupByColumns));

            // HAVING clause
            if (_havingClauses.Count > 0)
            {
                sql.Append(" HAVING ");
                sql.Append(string.Join(" AND ", _havingClauses));
            }

            // ORDER BY clause
            if (_orderByClauses.Count > 0)
            {
                sql.Append(" ORDER BY ");
                var orderParts = _orderByClauses.Select(o => $"{o.Column}{(o.Descending ? " DESC" : "")}");
                sql.Append(string.Join(", ", orderParts));
            }

            // LIMIT/OFFSET
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

        private string BuildCountSql()
        {
            // Count groups: SELECT COUNT(*) FROM (SELECT ... GROUP BY ...)
            var innerSql = new StringBuilder();
            innerSql.Append("SELECT ");

            var groupByColumns = GetGroupByColumns();
            innerSql.Append(string.Join(", ", groupByColumns));

            innerSql.Append($" FROM {_tableName}");

            if (_whereClauses.Count > 0)
            {
                innerSql.Append(" WHERE ");
                innerSql.Append(string.Join(" AND ", _whereClauses));
            }

            innerSql.Append(" GROUP BY ");
            innerSql.Append(string.Join(", ", groupByColumns));

            if (_havingClauses.Count > 0)
            {
                innerSql.Append(" HAVING ");
                innerSql.Append(string.Join(" AND ", _havingClauses));
            }

            return $"SELECT COUNT(*) as count FROM ({innerSql})";
        }

        private string BuildSelectClause()
        {
            // Parse the selector lambda to build SELECT with aggregates
            if (_selector is LambdaExpression lambda)
            {
                var parts = new List<string>();

                // Handle different selector types
                if (lambda.Body is MemberInitExpression memberInit)
                {
                    // new { Category = g.Key, Total = g.Sum(...) }
                    foreach (var binding in memberInit.Bindings)
                    {
                        if (binding is MemberAssignment assignment)
                        {
                            var columnName = _mapper.GetColumnName(assignment.Member.Name);
                            var sqlExpression = TranslateExpression(assignment.Expression);
                            parts.Add($"{sqlExpression} AS {columnName}");
                        }
                    }
                }
                else if (lambda.Body is NewExpression newExpr)
                {
                    // Parametered constructor: new Result(g.Key, g.Sum(...))
                    for (int i = 0; i < newExpr.Arguments.Count; i++)
                    {
                        var arg = newExpr.Arguments[i];
                        var member = newExpr.Members?[i];
                        var columnName = member != null ? _mapper.GetColumnName(member.Name) : $"Column{i}";
                        var sqlExpression = TranslateExpression(arg);
                        parts.Add($"{sqlExpression} AS {columnName}");
                    }
                }

                return string.Join(", ", parts);
            }

            throw new NotSupportedException("Unsupported selector expression type");
        }

        private string TranslateExpression(Expression expression)
        {
            var sql = new StringBuilder();

            // Handle g.Key
            if (expression is MemberExpression memberExpr &&
                memberExpr.Member.Name == "Key")
            {
                // Get the grouping key column(s)
                var keyColumns = GetGroupByColumns();
                return keyColumns.Count == 1 ? keyColumns[0] : $"({string.Join(", ", keyColumns)})";
            }

            // Handle aggregate methods (g.Sum, g.Count, etc.)
            if (expression is MethodCallExpression methodCall)
            {
                var sourceType = typeof(TResult).Assembly.GetTypes()
                    .FirstOrDefault(t => t.IsClass && !t.IsAbstract);

                // Get source type from key selector
                if (_keySelector is LambdaExpression keySelectorLambda)
                {
                    sourceType = keySelectorLambda.Parameters[0].Type;
                }

                var visitor = new AggregateExpressionVisitor(sql, _mapper, sourceType ?? typeof(TResult));
                visitor.Visit(methodCall);
                return sql.ToString();
            }

            // Fallback
            return expression.ToString();
        }

        private List<string> GetGroupByColumns()
        {
            var columns = new List<string>();

            if (_keySelector is LambdaExpression keySelectorLambda)
            {
                var body = keySelectorLambda.Body;

                if (body is MemberExpression memberExpr)
                {
                    // Single column: p => p.Category
                    columns.Add(_mapper.GetColumnName(memberExpr.Member.Name));
                }
                else if (body is NewExpression newExpr)
                {
                    // Composite key: p => new { p.Category, p.Status }
                    foreach (var arg in newExpr.Arguments)
                    {
                        if (arg is MemberExpression member)
                        {
                            columns.Add(_mapper.GetColumnName(member.Member.Name));
                        }
                    }
                }
            }

            return columns;
        }

        private static IEnumerable<Dictionary<string, object?>> ConvertResultsToRows(
            IEnumerable<IDictionary<string, object>>? results)
        {
            if (results == null)
                return Enumerable.Empty<Dictionary<string, object?>>();

            return results.Select(result =>
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in result)
                {
                    row[kvp.Key] = kvp.Value;
                }
                return row;
            });
        }
    }
}
