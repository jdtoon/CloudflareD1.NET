using System;
using System.Linq;
using System.Linq.Expressions;
using CloudflareD1.NET.Linq.Mapping;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// IQueryProvider implementation for D1 LINQ queries.
    /// </summary>
    public class D1QueryProvider : IQueryProvider
    {
        private readonly ID1Client _client;
        private readonly string _tableName;
        private readonly IEntityMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the D1QueryProvider class.
        /// </summary>
        public D1QueryProvider(ID1Client client, string tableName, IEntityMapper? mapper = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _mapper = mapper ?? new DefaultEntityMapper();
        }

        /// <inheritdoc />
        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            Type elementType = expression.Type.GetGenericArguments()[0];

            // Use reflection to call CreateQueryInternal with the element type
            var method = GetType().GetMethod(nameof(CreateQueryInternal),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method!.MakeGenericMethod(elementType);

            try
            {
                return (IQueryable)genericMethod.Invoke(this, new object[] { expression })!;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        /// <inheritdoc />
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            // Use reflection to call the constrained CreateQueryInternal method
            var method = GetType().GetMethod(nameof(CreateQueryInternal),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method!.MakeGenericMethod(typeof(TElement));

            try
            {
                return (IQueryable<TElement>)genericMethod.Invoke(this, new object[] { expression })!;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        /// <summary>
        /// Internal method with proper constraints for creating queries.
        /// </summary>
        internal IQueryable<TElement> CreateQueryInternal<TElement>(Expression expression) where TElement : class, new()
        {
            // Check if the expression is a Select projection at the top level
            if (expression is MethodCallExpression methodCall && methodCall.Method.Name == "Select")
            {
                // This is a projection - we need to handle it differently
                // Get the source and result types from the method's generic arguments
                // Select has signature: Select<TSource, TResult>(IQueryable<TSource>, Expression<Func<TSource, TResult>>)
                var genericArgs = methodCall.Method.GetGenericArguments();
                if (genericArgs.Length != 2)
                {
                    throw new InvalidOperationException($"Expected Select to have 2 generic arguments, but got {genericArgs.Length}");
                }

                var sourceType = genericArgs[0]; // TSource
                var resultType = genericArgs[1]; // TResult (should match TElement)

                // Verify resultType matches TElement
                if (resultType != typeof(TElement))
                {
                    throw new InvalidOperationException($"Result type mismatch: expected {typeof(TElement)}, got {resultType}");
                }

                // Create a QueryBuilder for the source type
                var queryBuilderType = typeof(QueryBuilder<>).MakeGenericType(sourceType);
                var queryBuilder = Activator.CreateInstance(queryBuilderType, _client, _tableName, _mapper);

                // Apply all operations except Select to the source QueryBuilder
                if (methodCall.Arguments.Count > 0)
                {
                    var applyMethod = GetType().GetMethod(nameof(ApplyExpression),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                        .MakeGenericMethod(sourceType);
                    queryBuilder = applyMethod.Invoke(this, new[] { queryBuilder, methodCall.Arguments[0] });
                }

                // Now apply the Select projection
                if (methodCall.Arguments.Count >= 2 && methodCall.Arguments[1] is UnaryExpression unary &&
                    unary.Operand is LambdaExpression lambda)
                {
                    // Call Select<TResult> on the QueryBuilder
                    var selectMethod = queryBuilderType.GetMethod(nameof(QueryBuilder<object>.Select))!
                        .MakeGenericMethod(resultType);
                    var projectionBuilder = selectMethod.Invoke(queryBuilder, new object[] { lambda });

                    // Return a D1ProjectionQueryable<TResult>
                    var projectionQueryableType = typeof(D1ProjectionQueryable<>).MakeGenericType(resultType);
                    return (IQueryable<TElement>)Activator.CreateInstance(projectionQueryableType, projectionBuilder, this, expression)!;
                }

                throw new InvalidOperationException("Select expression is missing selector lambda");
            }

            // Normal query without projection
            var normalQueryBuilder = new QueryBuilder<TElement>(_client, _tableName, _mapper);

            // Apply expression tree to query builder
            var appliedBuilder = ApplyExpression(normalQueryBuilder, expression);

            if (appliedBuilder == null)
            {
                throw new InvalidOperationException("ApplyExpression returned null");
            }

            return new D1Queryable<TElement>(appliedBuilder, this, expression);
        }

        /// <inheritdoc />
        public object? Execute(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            // For now, throw - we want users to use ExecuteAsync
            throw new NotSupportedException(
                "Synchronous execution is not supported. Use ToListAsync(), FirstOrDefaultAsync(), etc. instead of ToList(), FirstOrDefault().");
        }

        /// <inheritdoc />
        public TResult Execute<TResult>(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            // For now, throw - we want users to use ExecuteAsync
            throw new NotSupportedException(
                "Synchronous execution is not supported. Use ToListAsync(), FirstOrDefaultAsync(), etc. instead of ToList(), FirstOrDefault().");
        }

        private object CreateQueryBuilder(Type elementType)
        {
            return Activator.CreateInstance(
                typeof(QueryBuilder<>).MakeGenericType(elementType),
                _client, _tableName, _mapper)!;
        }

        private QueryBuilder<TElement> ApplyExpression<TElement>(QueryBuilder<TElement> builder, Expression expression)
            where TElement : class, new()
        {
            // Parse the expression tree and apply operations to QueryBuilder
            // This is a simplified implementation - full LINQ would require much more work

            // If we hit a constant expression (the base queryable), just return the builder as-is
            if (expression is ConstantExpression)
            {
                return builder;
            }

            if (expression is MethodCallExpression methodCall)
            {
                // Apply the previous expression first
                if (methodCall.Arguments.Count > 0)
                {
                    builder = ApplyExpression(builder, methodCall.Arguments[0]);
                }

                // Apply the current method
                switch (methodCall.Method.Name)
                {
                    case "Where":
                        if (methodCall.Arguments.Count >= 2 && methodCall.Arguments[1] is UnaryExpression unary &&
                            unary.Operand is LambdaExpression lambda)
                        {
                            var predicate = (Expression<Func<TElement, bool>>)lambda;
                            builder = (QueryBuilder<TElement>)builder.Where(predicate);
                        }
                        break;

                    case "Select":
                        // Select is handled separately in CreateQueryInternal for projection type changes
                        // When Select is encountered, it should be the terminating operation
                        // This case shouldn't be reached in normal flow as Select changes the element type
                        throw new NotSupportedException("Select projection should be handled at the top level of the expression tree. Ensure Select is the last operation in your query chain.");

                    case "OrderBy":
                    case "ThenBy":
                        if (methodCall.Arguments.Count >= 2 && methodCall.Arguments[1] is UnaryExpression orderUnary &&
                            orderUnary.Operand is LambdaExpression orderLambda)
                        {
                            // Extract the key selector type
                            var keySelectorType = orderLambda.Type.GetGenericArguments()[1];

                            // Find the generic OrderBy method on QueryBuilder<TElement>
                            var methods = typeof(QueryBuilder<TElement>).GetMethods()
                                .Where(m => m.Name == methodCall.Method.Name && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
                                .ToArray();

                            if (methods.Length > 0)
                            {
                                var orderByMethod = methods[0].MakeGenericMethod(keySelectorType);
                                var result = orderByMethod.Invoke(builder, new object[] { orderLambda });
                                if (result == null)
                                {
                                    throw new InvalidOperationException($"OrderBy method invocation returned null for method {methodCall.Method.Name}");
                                }
                                builder = (QueryBuilder<TElement>)result;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Could not find OrderBy method for type {keySelectorType.Name}");
                            }
                        }
                        break;

                    case "OrderByDescending":
                    case "ThenByDescending":
                        if (methodCall.Arguments.Count >= 2 && methodCall.Arguments[1] is UnaryExpression descUnary &&
                            descUnary.Operand is LambdaExpression descLambda)
                        {
                            var keySelectorType = descLambda.Type.GetGenericArguments()[1];

                            // Find the generic OrderByDescending method on QueryBuilder<TElement>
                            var methods = typeof(QueryBuilder<TElement>).GetMethods()
                                .Where(m => m.Name == methodCall.Method.Name && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
                                .ToArray();

                            if (methods.Length > 0)
                            {
                                var orderByDescMethod = methods[0].MakeGenericMethod(keySelectorType);
                                var result = orderByDescMethod.Invoke(builder, new object[] { descLambda });
                                if (result == null)
                                {
                                    throw new InvalidOperationException($"OrderByDescending method invocation returned null for method {methodCall.Method.Name}");
                                }
                                builder = (QueryBuilder<TElement>)result;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Could not find OrderByDescending method for type {keySelectorType.Name}");
                            }
                        }
                        break;

                    case "Take":
                        if (methodCall.Arguments.Count >= 2 && methodCall.Arguments[1] is ConstantExpression takeConstant)
                        {
                            builder = (QueryBuilder<TElement>)builder.Take((int)takeConstant.Value!);
                        }
                        break;

                    case "Skip":
                        if (methodCall.Arguments.Count >= 2 && methodCall.Arguments[1] is ConstantExpression skipConstant)
                        {
                            builder = (QueryBuilder<TElement>)builder.Skip((int)skipConstant.Value!);
                        }
                        break;
                }
            }

            return builder;
        }
    }
}
