using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using CloudflareD1.NET.Linq.Mapping;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Visits expression trees and translates them into SQL WHERE clause fragments with parameterized queries.
    /// </summary>
    public class SqlExpressionVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _sql;
        private readonly List<object> _parameters;
        private readonly IEntityMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the SqlExpressionVisitor class.
        /// </summary>
        /// <param name="mapper">The entity mapper for property to column name conversion.</param>
        public SqlExpressionVisitor(IEntityMapper mapper)
        {
            _sql = new StringBuilder();
            _parameters = new List<object>();
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Gets the generated SQL string.
        /// </summary>
        /// <returns>The SQL WHERE clause fragment.</returns>
        public string GetSql() => _sql.ToString();

        /// <summary>
        /// Gets the parameters collected during expression translation.
        /// </summary>
        /// <returns>Array of parameter values.</returns>
        public object[] GetParameters() => _parameters.ToArray();

        /// <summary>
        /// Translates an expression into SQL.
        /// </summary>
        /// <param name="expression">The expression to translate.</param>
        /// <returns>The generated SQL string.</returns>
        public string Translate(Expression expression)
        {
            _sql.Clear();
            _parameters.Clear();
            Visit(expression);
            return _sql.ToString();
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            // Special handling for null comparisons
            if (IsNullConstant(node.Right))
            {
                Visit(node.Left);
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                        _sql.Append(" IS NULL");
                        return node;
                    case ExpressionType.NotEqual:
                        _sql.Append(" IS NOT NULL");
                        return node;
                }
            }

            if (IsNullConstant(node.Left))
            {
                Visit(node.Right);
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                        _sql.Append(" IS NULL");
                        return node;
                    case ExpressionType.NotEqual:
                        _sql.Append(" IS NOT NULL");
                        return node;
                }
            }

            _sql.Append("(");
            Visit(node.Left);

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    _sql.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    _sql.Append(" != ");
                    break;
                case ExpressionType.GreaterThan:
                    _sql.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _sql.Append(" >= ");
                    break;
                case ExpressionType.LessThan:
                    _sql.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    _sql.Append(" <= ");
                    break;
                case ExpressionType.AndAlso:
                    _sql.Append(" AND ");
                    break;
                case ExpressionType.OrElse:
                    _sql.Append(" OR ");
                    break;
                case ExpressionType.Add:
                    _sql.Append(" + ");
                    break;
                case ExpressionType.Subtract:
                    _sql.Append(" - ");
                    break;
                case ExpressionType.Multiply:
                    _sql.Append(" * ");
                    break;
                case ExpressionType.Divide:
                    _sql.Append(" / ");
                    break;
                default:
                    throw new NotSupportedException($"Binary operator '{node.NodeType}' is not supported.");
            }

            Visit(node.Right);
            _sql.Append(")");

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            // Handle property access like "u.Name" or "u.Age"
            if (node.Expression?.NodeType == ExpressionType.Parameter)
            {
                // Convert property name to column name using mapper
                var columnName = _mapper.GetColumnName(node.Member.Name);
                _sql.Append(columnName);
                return node;
            }

            // Handle constant member access like capturing variables
            var value = GetMemberValue(node);
            AddParameter(value);
            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            AddParameter(node.Value);
            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    _sql.Append("NOT ");
                    Visit(node.Operand);
                    break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    // Just visit the operand, ignore the conversion
                    Visit(node.Operand);
                    break;
                default:
                    throw new NotSupportedException($"Unary operator '{node.NodeType}' is not supported.");
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Handle string methods
            if (node.Method.DeclaringType == typeof(string))
            {
                switch (node.Method.Name)
                {
                    case "Contains":
                        Visit(node.Object);
                        _sql.Append(" LIKE ");
                        var containsValue = GetExpressionValue(node.Arguments[0]);
                        AddParameter($"%{containsValue}%");
                        return node;

                    case "StartsWith":
                        Visit(node.Object);
                        _sql.Append(" LIKE ");
                        var startsWithValue = GetExpressionValue(node.Arguments[0]);
                        AddParameter($"{startsWithValue}%");
                        return node;

                    case "EndsWith":
                        Visit(node.Object);
                        _sql.Append(" LIKE ");
                        var endsWithValue = GetExpressionValue(node.Arguments[0]);
                        AddParameter($"%{endsWithValue}");
                        return node;

                    case "ToLower":
                        _sql.Append("LOWER(");
                        Visit(node.Object);
                        _sql.Append(")");
                        return node;

                    case "ToUpper":
                        _sql.Append("UPPER(");
                        Visit(node.Object);
                        _sql.Append(")");
                        return node;
                }
            }

            // Handle Enumerable/Queryable methods
            if (node.Method.DeclaringType == typeof(Enumerable) ||
                node.Method.DeclaringType == typeof(Queryable))
            {
                switch (node.Method.Name)
                {
                    case "Contains":
                        // list.Contains(value) -> value IN (list)
                        var member = node.Arguments[1];
                        Visit(member);
                        _sql.Append(" IN (");

                        var collection = GetExpressionValue(node.Arguments[0]);
                        if (collection is System.Collections.IEnumerable enumerable)
                        {
                            var items = enumerable.Cast<object>().ToArray();
                            for (int i = 0; i < items.Length; i++)
                            {
                                if (i > 0) _sql.Append(", ");
                                AddParameter(items[i]);
                            }
                        }
                        _sql.Append(")");
                        return node;
                }
            }

            throw new NotSupportedException($"Method '{node.Method.Name}' is not supported.");
        }

        private void AddParameter(object value)
        {
            _parameters.Add(value);
            _sql.Append("?");
        }

        private static bool IsNullConstant(Expression expression)
        {
            if (expression is ConstantExpression constant)
            {
                return constant.Value == null;
            }
            return false;
        }

        private object GetExpressionValue(Expression expression)
        {
            if (expression is ConstantExpression constant)
                return constant.Value;

            if (expression is MemberExpression member)
                return GetMemberValue(member);

            // Compile and execute the expression to get the value
            var lambda = Expression.Lambda(expression);
            return lambda.Compile().DynamicInvoke();
        }

        private object GetMemberValue(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            return getterLambda.Compile()();
        }
    }
}
