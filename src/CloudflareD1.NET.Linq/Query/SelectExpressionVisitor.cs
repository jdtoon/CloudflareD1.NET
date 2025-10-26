using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CloudflareD1.NET.Linq.Mapping;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Visitor that parses Select() expression trees to extract column names and aliases.
    /// </summary>
    public class SelectExpressionVisitor : ExpressionVisitor
    {
        private readonly IEntityMapper _mapper;
        private readonly List<(string Column, string? Alias)> _columns;
        private readonly List<object> _parameters;

        /// <summary>
        /// Initializes a new instance of the SelectExpressionVisitor class.
        /// </summary>
        /// <param name="mapper">The entity mapper for column name conversion.</param>
        public SelectExpressionVisitor(IEntityMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _columns = new List<(string, string?)>();
            _parameters = new List<object>();
        }

        /// <summary>
        /// Parses a Select expression and returns the column selections.
        /// </summary>
        /// <param name="expression">The expression to parse.</param>
        /// <returns>List of (ColumnName, Alias) tuples.</returns>
        public List<(string Column, string? Alias)> GetColumns(Expression expression)
        {
            _columns.Clear();
            _parameters.Clear();
            Visit(expression);
            return _columns;
        }

        /// <summary>
        /// Gets the parameters collected during expression parsing.
        /// </summary>
        /// <returns>Array of parameter values.</returns>
        public object[] GetParameters() => _parameters.ToArray();

        /// <summary>
        /// Visits a MemberInit expression (anonymous type initialization).
        /// Example: new { u.Id, u.Name, Adult = u.Age >= 18 }
        /// </summary>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            foreach (var binding in node.Bindings)
            {
                if (binding is MemberAssignment assignment)
                {
                    var alias = assignment.Member.Name;

                    // Check if this is a simple property access or computed property
                    if (assignment.Expression is MemberExpression memberExpr)
                    {
                        // Simple property: new { u.Name } or new { FullName = u.Name }
                        var columnName = _mapper.GetColumnName(memberExpr.Member.Name);
                        _columns.Add((columnName, alias));
                    }
                    else
                    {
                        // Computed property: new { Adult = u.Age >= 18 }
                        // Parse the expression to generate SQL
                        var sqlVisitor = new SqlExpressionVisitor(_mapper);
                        var sqlExpression = sqlVisitor.Translate(assignment.Expression);
                        _columns.Add((sqlExpression, alias));
                        // Capture parameters from computed expression
                        _parameters.AddRange(sqlVisitor.GetParameters());
                    }
                }
            }
            return node;
        }

        /// <summary>
        /// Visits a New expression (named type constructor).
        /// Example: new UserDto(u.Id, u.Name)
        /// </summary>
        protected override Expression VisitNew(NewExpression node)
        {
            // Check if this is an anonymous type without explicit names
            // Example: new { u.Id, u.Name } compiles to new <>f__AnonymousType0(u.Id, u.Name)
            if (node.Members != null)
            {
                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    var argument = node.Arguments[i];
                    var member = node.Members[i];
                    var alias = member.Name;

                    if (argument is MemberExpression memberExpr)
                    {
                        var columnName = _mapper.GetColumnName(memberExpr.Member.Name);
                        _columns.Add((columnName, alias));
                    }
                    else
                    {
                        // Computed property
                        var sqlVisitor = new SqlExpressionVisitor(_mapper);
                        var sqlExpression = sqlVisitor.Translate(argument);
                        _columns.Add((sqlExpression, alias));
                        // Capture parameters from computed expression
                        _parameters.AddRange(sqlVisitor.GetParameters());
                    }
                }
            }
            else
            {
                // Named type constructor - use parameter names or positions
                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    var argument = node.Arguments[i];

                    if (argument is MemberExpression memberExpr)
                    {
                        var columnName = _mapper.GetColumnName(memberExpr.Member.Name);
                        // For named types, we use the property name as the alias
                        _columns.Add((columnName, memberExpr.Member.Name));
                    }
                    else
                    {
                        // This is a more complex case - for now, throw
                        throw new NotSupportedException(
                            "Computed properties in named type constructors are not yet supported. Use anonymous types instead.");
                    }
                }
            }
            return node;
        }

        /// <summary>
        /// Visits a simple member expression (single property selection).
        /// Example: u => u.Name
        /// </summary>
        protected override Expression VisitMember(MemberExpression node)
        {
            var columnName = _mapper.GetColumnName(node.Member.Name);
            _columns.Add((columnName, null));
            return node;
        }
    }
}
