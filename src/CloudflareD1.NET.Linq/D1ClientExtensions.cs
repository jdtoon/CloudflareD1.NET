using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Linq.Mapping;
using CloudflareD1.NET.Linq.Query;

namespace CloudflareD1.NET.Linq
{
    /// <summary>
    /// Extension methods for D1Client that add generic query support with automatic entity mapping.
    /// </summary>
    public static class D1ClientExtensions
    {
        private static readonly IEntityMapper DefaultMapper = new DefaultEntityMapper();

        /// <summary>
        /// Creates a fluent query builder for the specified table and entity type.
        /// </summary>
        /// <typeparam name="T">The entity type to query.</typeparam>
        /// <param name="client">The D1Client instance.</param>
        /// <param name="tableName">The name of the table to query.</param>
        /// <param name="mapper">Optional custom entity mapper. Uses DefaultEntityMapper if not provided.</param>
        /// <returns>A query builder for constructing and executing queries.</returns>
        public static IQueryBuilder<T> Query<T>(
            this ID1Client client,
            string tableName,
            IEntityMapper? mapper = null) where T : class, new()
        {
            return new QueryBuilder<T>(client, tableName, mapper);
        }

        /// <summary>
        /// Creates an IQueryable for the specified table with deferred execution.
        /// The query will not execute until you enumerate the results or call a terminal operation like ToListAsync().
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="client">The D1 client.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="mapper">Optional custom entity mapper.</param>
        /// <returns>An IQueryable that can be composed with LINQ operators.</returns>
        /// <example>
        /// <code>
        /// // Create queryable - no query sent yet
        /// IQueryable&lt;User&gt; query = client.AsQueryable&lt;User&gt;("users");
        ///
        /// // Compose query - still no query sent
        /// var adults = query
        ///     .Where(u => u.Age >= 18)
        ///     .OrderBy(u => u.Name)
        ///     .Select(u => new { u.Id, u.Name });
        ///
        /// // Now execute - query is sent to D1
        /// var results = await ((D1Queryable&lt;User&gt;)adults).ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> AsQueryable<T>(this ID1Client client, string tableName, IEntityMapper? mapper = null)
            where T : class, new()
        {
            var queryBuilder = new QueryBuilder<T>(client, tableName, mapper);
            var provider = new D1QueryProvider(client, tableName, mapper);
            return new D1Queryable<T>(queryBuilder, provider);
        }

        /// <summary>
        /// Executes a SQL query and maps the results to a collection of entities of type T.
        /// </summary>
        /// <typeparam name="T">The entity type to map results to.</typeparam>
        /// <param name="client">The D1Client instance.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="mapper">Optional custom entity mapper. Uses DefaultEntityMapper if not provided.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of T instances populated with query results.</returns>
        public static async Task<IEnumerable<T>> QueryAsync<T>(
            this ID1Client client,
            string sql,
            object? parameters = null,
            IEntityMapper? mapper = null,
            CancellationToken cancellationToken = default) where T : new()
        {
            var result = await client.QueryAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
            var entityMapper = mapper ?? DefaultMapper;

            var rows = ConvertResultsToRows(result.Results);
            return entityMapper.MapMany<T>(rows);
        }

        /// <summary>
        /// Executes a SQL query and returns the first result mapped to type T, or null if no results.
        /// </summary>
        /// <typeparam name="T">The entity type to map the result to.</typeparam>
        /// <param name="client">The D1Client instance.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="mapper">Optional custom entity mapper. Uses DefaultEntityMapper if not provided.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>First T instance or null if no results.</returns>
        public static async Task<T?> QueryFirstOrDefaultAsync<T>(
            this ID1Client client,
            string sql,
            object? parameters = null,
            IEntityMapper? mapper = null,
            CancellationToken cancellationToken = default) where T : new()
        {
            var results = await QueryAsync<T>(client, sql, parameters, mapper, cancellationToken).ConfigureAwait(false);
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Executes a SQL query and returns a single result mapped to type T.
        /// Throws if no results or more than one result is found.
        /// </summary>
        /// <typeparam name="T">The entity type to map the result to.</typeparam>
        /// <param name="client">The D1Client instance.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="mapper">Optional custom entity mapper. Uses DefaultEntityMapper if not provided.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Single T instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when query returns zero or more than one result.</exception>
        public static async Task<T> QuerySingleAsync<T>(
            this ID1Client client,
            string sql,
            object? parameters = null,
            IEntityMapper? mapper = null,
            CancellationToken cancellationToken = default) where T : new()
        {
            var results = await QueryAsync<T>(client, sql, parameters, mapper, cancellationToken).ConfigureAwait(false);
            return results.Single();
        }

        /// <summary>
        /// Executes a SQL query and returns a single result mapped to type T, or null if no results.
        /// Throws if more than one result is found.
        /// </summary>
        /// <typeparam name="T">The entity type to map the result to.</typeparam>
        /// <param name="client">The D1Client instance.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="mapper">Optional custom entity mapper. Uses DefaultEntityMapper if not provided.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Single T instance or null if no results.</returns>
        /// <exception cref="InvalidOperationException">Thrown when query returns more than one result.</exception>
        public static async Task<T?> QuerySingleOrDefaultAsync<T>(
            this ID1Client client,
            string sql,
            object? parameters = null,
            IEntityMapper? mapper = null,
            CancellationToken cancellationToken = default) where T : new()
        {
            var results = await QueryAsync<T>(client, sql, parameters, mapper, cancellationToken).ConfigureAwait(false);
            return results.SingleOrDefault();
        }

        /// <summary>
        /// Converts D1 query results (which can be List of objects or JsonElement) to dictionaries.
        /// </summary>
        internal static IEnumerable<Dictionary<string, object?>> ConvertResultsToRows(object? results)
        {
            if (results == null)
                return Enumerable.Empty<Dictionary<string, object?>>();

            // Handle JsonElement (deserialized results)
            if (results is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    return jsonElement.EnumerateArray()
                        .Select(ConvertJsonElementToRow)
                        .ToList();
                }
            }

            // Handle IEnumerable<object>
            if (results is System.Collections.IEnumerable enumerable)
            {
                var rows = new List<Dictionary<string, object?>>();
                foreach (var item in enumerable)
                {
                    if (item is JsonElement itemElement)
                    {
                        rows.Add(ConvertJsonElementToRow(itemElement));
                    }
                    else if (item is Dictionary<string, object?> dict)
                    {
                        rows.Add(dict);
                    }
                }
                return rows;
            }

            return Enumerable.Empty<Dictionary<string, object?>>();
        }

        /// <summary>
        /// Converts a JsonElement object to a dictionary of column name to value.
        /// </summary>
        private static Dictionary<string, object?> ConvertJsonElementToRow(JsonElement element)
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    row[property.Name] = GetJsonElementValue(property.Value);
                }
            }

            return row;
        }

        /// <summary>
        /// Extracts the actual value from a JsonElement based on its type.
        /// </summary>
        private static object? GetJsonElementValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Array:
                case JsonValueKind.Object:
                    // Return the JsonElement itself for complex types
                    return element;

                default:
                    return null;
            }
        }
    }
}
