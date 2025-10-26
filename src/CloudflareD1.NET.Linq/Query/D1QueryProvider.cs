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
            try
            {
                return (IQueryable)Activator.CreateInstance(
                    typeof(D1Queryable<>).MakeGenericType(elementType),
                    new object[] { CreateQueryBuilder(elementType), this, expression })!;
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException!;
            }
        }

        /// <inheritdoc />
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            // Create QueryBuilder using reflection since we can't add where constraint
            var queryBuilderType = typeof(QueryBuilder<>).MakeGenericType(typeof(TElement));
            var queryBuilder = Activator.CreateInstance(queryBuilderType, _client, _tableName, _mapper);
            
            // Apply expression tree to query builder (if needed)
            // For now, just create the queryable wrapper

            return (IQueryable<TElement>)Activator.CreateInstance(
                typeof(D1Queryable<>).MakeGenericType(typeof(TElement)),
                queryBuilder, this, expression)!;
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

                    case "OrderBy":
                    case "ThenBy":
                        if (methodCall.Arguments.Count >= 2 && methodCall.Arguments[1] is UnaryExpression orderUnary &&
                            orderUnary.Operand is LambdaExpression orderLambda)
                        {
                            // Extract the key selector
                            var keySelectorType = orderLambda.Type.GetGenericArguments()[1];
                            var orderByMethod = typeof(QueryBuilder<TElement>).GetMethod(methodCall.Method.Name,
                                new[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TElement), keySelectorType)) });
                            
                            if (orderByMethod != null)
                            {
                                builder = (QueryBuilder<TElement>)orderByMethod.Invoke(builder, new object[] { orderLambda })!;
                            }
                        }
                        break;

                    case "OrderByDescending":
                    case "ThenByDescending":
                        if (methodCall.Arguments.Count >= 2 && methodCall.Arguments[1] is UnaryExpression descUnary &&
                            descUnary.Operand is LambdaExpression descLambda)
                        {
                            var keySelectorType = descLambda.Type.GetGenericArguments()[1];
                            var orderByDescMethod = typeof(QueryBuilder<TElement>).GetMethod(methodCall.Method.Name,
                                new[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TElement), keySelectorType)) });
                            
                            if (orderByDescMethod != null)
                            {
                                builder = (QueryBuilder<TElement>)orderByDescMethod.Invoke(builder, new object[] { descLambda })!;
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
