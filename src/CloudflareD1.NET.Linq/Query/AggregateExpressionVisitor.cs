using System;
using System.Linq.Expressions;
using System.Text;
using CloudflareD1.NET.Linq.Mapping;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Expression visitor that translates aggregate function calls (Count, Sum, Average, Min, Max)
    /// within GroupBy Select projections into SQL aggregate functions.
    /// </summary>
    internal class AggregateExpressionVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _sql;
        private readonly IEntityMapper _mapper;
        private readonly Type _sourceType;
        private bool _insideAggregateMethod;
        private string? _currentAggregateFunction;

        public AggregateExpressionVisitor(StringBuilder sql, IEntityMapper mapper, Type sourceType)
        {
            _sql = sql ?? throw new ArgumentNullException(nameof(sql));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Check if this is an aggregate method on IGrouping
            if (node.Method.DeclaringType?.Name == "Enumerable" ||
                node.Method.DeclaringType?.Name.StartsWith("IGrouping") == true ||
                node.Method.DeclaringType?.Name.StartsWith("ID1Grouping") == true ||
                (node.Object != null && IsGroupingType(node.Object.Type)))
            {
                switch (node.Method.Name)
                {
                    case "Count":
                        HandleCount(node);
                        return node;

                    case "Sum":
                        HandleAggregate(node, "SUM");
                        return node;

                    case "Average":
                        HandleAggregate(node, "AVG");
                        return node;

                    case "Min":
                        HandleAggregate(node, "MIN");
                        return node;

                    case "Max":
                        HandleAggregate(node, "MAX");
                        return node;
                }
            }

            return base.VisitMethodCall(node);
        }

        private bool IsGroupingType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericDef = type.GetGenericTypeDefinition();
            return genericDef.Name.Contains("Grouping") || genericDef.Name.Contains("IEnumerable");
        }

        private void HandleCount(MethodCallExpression node)
        {
            // Count() or Count(predicate)
            if (node.Arguments.Count == 1)
            {
                // Count() - no predicate
                _sql.Append("COUNT(*)");
            }
            else if (node.Arguments.Count == 2)
            {
                // Count(predicate) - for now, just count all
                // Full support would require translating the predicate to WHERE
                _sql.Append("COUNT(*)");
            }
        }

        private void HandleAggregate(MethodCallExpression node, string sqlFunction)
        {
            // Sum/Average/Min/Max(selector)
            if (node.Arguments.Count >= 2)
            {
                var selectorArg = node.Arguments[1];

                // Unwrap the Quote expression if present
                if (selectorArg is UnaryExpression unary && unary.NodeType == ExpressionType.Quote)
                {
                    selectorArg = unary.Operand;
                }

                if (selectorArg is LambdaExpression lambda)
                {
                    _sql.Append(sqlFunction);
                    _sql.Append("(");

                    _insideAggregateMethod = true;
                    _currentAggregateFunction = sqlFunction;

                    // Visit the lambda body to get the column reference
                    Visit(lambda.Body);

                    _insideAggregateMethod = false;
                    _currentAggregateFunction = null;

                    _sql.Append(")");
                }
            }
            else
            {
                throw new NotSupportedException($"{sqlFunction} requires a selector expression");
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (_insideAggregateMethod)
            {
                // Inside an aggregate - just output the column name
                var columnName = _mapper.GetColumnName(node.Member.Name);
                _sql.Append(columnName);
                return node;
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (_insideAggregateMethod)
            {
                // Handle binary expressions inside aggregates (e.g., Sum(x => x.Price * x.Quantity))
                _sql.Append("(");
                Visit(node.Left);

                switch (node.NodeType)
                {
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
                        throw new NotSupportedException($"Binary operator {node.NodeType} not supported in aggregates");
                }

                Visit(node.Right);
                _sql.Append(")");
                return node;
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (_insideAggregateMethod)
            {
                // Constants inside aggregates
                if (node.Value != null)
                {
                    _sql.Append(node.Value.ToString());
                }
                return node;
            }

            return base.VisitConstant(node);
        }
    }
}
