using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CloudflareD1.NET.Linq.Mapping;
using CloudflareD1.NET.Models;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Query builder for JOIN operations.
    /// </summary>
    internal class JoinQueryBuilder<TOuter, TInner, TKey> : IJoinQueryBuilder<TOuter, TInner, TKey>
        where TOuter : class, new()
        where TInner : class, new()
    {
        private readonly ID1Client _client;
        private readonly string _outerTableName;
        private readonly IQueryBuilder<TInner> _innerQueryBuilder;
        private readonly IEntityMapper _mapper;
        private readonly Expression<Func<TOuter, TKey>> _outerKeySelector;
        private readonly Expression<Func<TInner, TKey>> _innerKeySelector;
        private readonly JoinType _joinType;
        private readonly List<string> _whereClauses;
        private readonly List<object> _parameters;

        public JoinQueryBuilder(
            ID1Client client,
            string outerTableName,
            IQueryBuilder<TInner> innerQueryBuilder,
            IEntityMapper mapper,
            Expression<Func<TOuter, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector,
            JoinType joinType,
            List<string> whereClauses,
            List<object> parameters)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _outerTableName = outerTableName ?? throw new ArgumentNullException(nameof(outerTableName));
            _innerQueryBuilder = innerQueryBuilder ?? throw new ArgumentNullException(nameof(innerQueryBuilder));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _outerKeySelector = outerKeySelector ?? throw new ArgumentNullException(nameof(outerKeySelector));
            _innerKeySelector = innerKeySelector ?? throw new ArgumentNullException(nameof(innerKeySelector));
            _joinType = joinType;
            _whereClauses = whereClauses ?? new List<string>();
            _parameters = parameters ?? new List<object>();
        }

        /// <inheritdoc />
        public IJoinProjectionQueryBuilder<TResult> Select<TResult>(
            Expression<Func<TOuter, TInner, TResult>> selector)
            where TResult : class, new()
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            // Get inner table name from the QueryBuilder
            var innerTableName = GetInnerTableName();

            return new JoinProjectionQueryBuilder<TResult>(
                _client,
                _outerTableName,
                innerTableName,
                _mapper,
                selector,
                _outerKeySelector,
                _innerKeySelector,
                _joinType,
                _whereClauses,
                _parameters);
        }

        private string GetInnerTableName()
        {
            // Try to extract table name from inner QueryBuilder
            // This is a simplified implementation - in real code, we'd need to pass the table name
            if (_innerQueryBuilder is QueryBuilder<TInner> qb)
            {
                // Use reflection to get the private _tableName field
                var tableNameField = typeof(QueryBuilder<TInner>).GetField("_tableName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (tableNameField != null)
                {
                    return tableNameField.GetValue(qb) as string ?? typeof(TInner).Name.ToLower();
                }
            }

            // Fallback: use type name as table name
            return typeof(TInner).Name.ToLower();
        }
    }

    /// <summary>
    /// Query builder for projected JOIN results.
    /// </summary>
    internal class JoinProjectionQueryBuilder<TResult> : IJoinProjectionQueryBuilder<TResult>
        where TResult : class, new()
    {
        private readonly ID1Client _client;
        private readonly string _outerTableName;
        private readonly string _innerTableName;
        private readonly IEntityMapper _mapper;
        private readonly Expression _selector;
        private readonly Expression _outerKeySelector;
        private readonly Expression _innerKeySelector;
        private readonly JoinType _joinType;
        private readonly List<string> _whereClauses;
        private readonly List<object> _parameters;
        private readonly List<(string Column, bool Descending)> _orderByClauses;
        private int? _takeCount;
        private int? _skipCount;

        public JoinProjectionQueryBuilder(
            ID1Client client,
            string outerTableName,
            string innerTableName,
            IEntityMapper mapper,
            Expression selector,
            Expression outerKeySelector,
            Expression innerKeySelector,
            JoinType joinType,
            List<string> whereClauses,
            List<object> parameters)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _outerTableName = outerTableName ?? throw new ArgumentNullException(nameof(outerTableName));
            _innerTableName = innerTableName ?? throw new ArgumentNullException(nameof(innerTableName));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _outerKeySelector = outerKeySelector ?? throw new ArgumentNullException(nameof(outerKeySelector));
            _innerKeySelector = innerKeySelector ?? throw new ArgumentNullException(nameof(innerKeySelector));
            _joinType = joinType;
            _whereClauses = whereClauses ?? new List<string>();
            _parameters = parameters ?? new List<object>();
            _orderByClauses = new List<(string, bool)>();
        }

        /// <inheritdoc />
        public IJoinProjectionQueryBuilder<TResult> Where(Expression<Func<TResult, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var visitor = new SqlExpressionVisitor(_mapper);
            var sql = visitor.Translate(predicate);
            var whereParams = visitor.GetParameters();

            _whereClauses.Add(sql);
            _parameters.AddRange(whereParams);

            return this;
        }

        /// <inheritdoc />
        public IJoinProjectionQueryBuilder<TResult> OrderBy(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            _orderByClauses.Add((column, false));
            return this;
        }

        /// <inheritdoc />
        public IJoinProjectionQueryBuilder<TResult> OrderByDescending(string column)
        {
            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column name cannot be empty.", nameof(column));

            _orderByClauses.Add((column, true));
            return this;
        }

        /// <inheritdoc />
        public IJoinProjectionQueryBuilder<TResult> Take(int count)
        {
            if (count <= 0)
                throw new ArgumentException("Take count must be greater than zero.", nameof(count));

            _takeCount = count;
            return this;
        }

        /// <inheritdoc />
        public IJoinProjectionQueryBuilder<TResult> Skip(int count)
        {
            if (count < 0)
                throw new ArgumentException("Skip count cannot be negative.", nameof(count));

            _skipCount = count;
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<TResult>> ToListAsync()
        {
            var sql = BuildSelectSql();
            var result = await _client.QueryAsync(sql, _parameters.ToArray());

            if (!result.Success)
            {
                throw new InvalidOperationException($"Join query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results == null)
            {
                return Enumerable.Empty<TResult>();
            }

            return _mapper.MapMany<TResult>(result.Results);
        }

        /// <inheritdoc />
        public async Task<TResult?> FirstOrDefaultAsync()
        {
            // Add LIMIT 1 for efficiency
            var originalTake = _takeCount;
            _takeCount = 1;

            var results = await ToListAsync();

            _takeCount = originalTake;
            return results.FirstOrDefault();
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
            var sql = BuildCountSql();
            var result = await _client.QueryAsync(sql, _parameters.ToArray());

            if (!result.Success)
            {
                throw new InvalidOperationException($"Count query failed: {string.Join(", ", result.Errors)}");
            }

            if (result.Results?.Count > 0)
            {
                var firstRow = result.Results[0];
                if (firstRow.TryGetValue("count", out var countValue))
                {
                    if (countValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
                    {
                        return jsonElement.GetInt32();
                    }
                    return Convert.ToInt32(countValue);
                }
            }

            return 0;
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

            // Build SELECT clause from the selector expression
            var selectClause = BuildSelectClause();
            sql.Append(selectClause);

            // FROM clause with JOIN
            sql.Append($" FROM {_outerTableName}");

            // JOIN clause
            var joinKeyword = _joinType switch
            {
                JoinType.Inner => "INNER JOIN",
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                _ => "INNER JOIN"
            };

            sql.Append($" {joinKeyword} {_innerTableName}");

            // ON clause
            var outerKey = GetKeyColumn(_outerKeySelector);
            var innerKey = GetKeyColumn(_innerKeySelector);
            sql.Append($" ON {_outerTableName}.{outerKey} = {_innerTableName}.{innerKey}");

            // WHERE clause
            if (_whereClauses.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", _whereClauses));
            }

            // ORDER BY clause
            if (_orderByClauses.Count > 0)
            {
                sql.Append(" ORDER BY ");
                var orderParts = _orderByClauses.Select(o => $"{o.Column}{(o.Descending ? " DESC" : "")}");
                sql.Append(string.Join(", ", orderParts));
            }

            // LIMIT clause
            if (_takeCount.HasValue)
            {
                sql.Append($" LIMIT {_takeCount.Value}");
            }

            // OFFSET clause
            if (_skipCount.HasValue)
            {
                sql.Append($" OFFSET {_skipCount.Value}");
            }

            return sql.ToString();
        }

        private string BuildCountSql()
        {
            var sql = new StringBuilder();
            sql.Append("SELECT COUNT(*) as count");
            sql.Append($" FROM {_outerTableName}");

            // JOIN clause
            var joinKeyword = _joinType switch
            {
                JoinType.Inner => "INNER JOIN",
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                _ => "INNER JOIN"
            };

            sql.Append($" {joinKeyword} {_innerTableName}");

            // ON clause
            var outerKey = GetKeyColumn(_outerKeySelector);
            var innerKey = GetKeyColumn(_innerKeySelector);
            sql.Append($" ON {_outerTableName}.{outerKey} = {_innerTableName}.{innerKey}");

            // WHERE clause
            if (_whereClauses.Count > 0)
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", _whereClauses));
            }

            return sql.ToString();
        }

        private string BuildSelectClause()
        {
            // Extract column selections from the lambda expression
            // For: (outer, inner) => new Result { Prop1 = outer.Prop1, Prop2 = inner.Prop2 }

            if (_selector is LambdaExpression lambda)
            {
                // Handle MemberInitExpression (object initializer syntax)
                if (lambda.Body is MemberInitExpression memberInit)
                {
                    var parts = new List<string>();

                    foreach (var binding in memberInit.Bindings)
                    {
                        if (binding is MemberAssignment assignment)
                        {
                            var propertyName = assignment.Member.Name;
                            var columnAlias = _mapper.GetColumnName(propertyName);

                            if (assignment.Expression is MemberExpression memberExpr)
                            {
                                // Determine which table this property belongs to
                                var tableName = GetTableForParameter(memberExpr.Expression);
                                var columnName = _mapper.GetColumnName(memberExpr.Member.Name);
                                parts.Add($"{tableName}.{columnName} AS {columnAlias}");
                            }
                            else if (assignment.Expression is ConstantExpression constExpr)
                            {
                                // Constant value
                                parts.Add($"{FormatConstant(constExpr.Value)} AS {columnAlias}");
                            }
                        }
                    }

                    if (parts.Count > 0)
                    {
                        return string.Join(", ", parts);
                    }
                }
                // Handle NewExpression (constructor syntax)
                else if (lambda.Body is NewExpression newExpr)
                {
                    var parts = new List<string>();

                    for (int i = 0; i < newExpr.Arguments.Count; i++)
                    {
                        var arg = newExpr.Arguments[i];
                        var member = newExpr.Members?[i];
                        // Use snake_case for the alias so the mapper can find it
                        var columnAlias = member != null ? _mapper.GetColumnName(member.Name) : $"column{i}";

                        if (arg is MemberExpression memberExpr)
                        {
                            // Determine which table this property belongs to
                            var tableName = GetTableForParameter(memberExpr.Expression);
                            var columnName = _mapper.GetColumnName(memberExpr.Member.Name);
                            parts.Add($"{tableName}.{columnName} AS {columnAlias}");
                        }
                        else if (arg is ConstantExpression constExpr)
                        {
                            // Constant value
                            parts.Add($"{FormatConstant(constExpr.Value)} AS {columnAlias}");
                        }
                    }

                    if (parts.Count > 0)
                    {
                        return string.Join(", ", parts);
                    }
                }
            }

            // Fallback: select all columns from both tables
            return $"{_outerTableName}.*, {_innerTableName}.*";
        }

        private string GetTableForParameter(Expression? expression)
        {
            if (expression is ParameterExpression paramExpr)
            {
                // Check parameter name or position to determine which table
                // Typically: first parameter = outer table, second parameter = inner table
                var parameterIndex = GetParameterIndex(paramExpr);
                return parameterIndex == 0 ? _outerTableName : _innerTableName;
            }

            return _outerTableName;
        }

        private int GetParameterIndex(ParameterExpression parameter)
        {
            if (_selector is LambdaExpression lambda)
            {
                for (int i = 0; i < lambda.Parameters.Count; i++)
                {
                    if (lambda.Parameters[i] == parameter)
                        return i;
                }
            }
            return 0;
        }

        private string GetKeyColumn(Expression keySelector)
        {
            if (keySelector is LambdaExpression lambda && lambda.Body is MemberExpression memberExpr)
            {
                return _mapper.GetColumnName(memberExpr.Member.Name);
            }

            throw new NotSupportedException("Key selector must be a simple member expression (e.g., x => x.Id)");
        }

        private static string FormatConstant(object? value)
        {
            if (value == null)
                return "NULL";

            if (value is string str)
                return $"'{str.Replace("'", "''")}'";

            if (value is bool b)
                return b ? "1" : "0";

            if (value is DateTime dt)
                return $"'{dt:yyyy-MM-dd HH:mm:ss}'";

            return value.ToString() ?? "NULL";
        }
    }
}
