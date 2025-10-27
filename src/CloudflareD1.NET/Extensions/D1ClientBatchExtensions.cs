using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Models;

namespace CloudflareD1.NET
{
    /// <summary>
    /// Extension methods for batch operations on ID1Client.
    /// </summary>
    public static class D1ClientBatchExtensions
    {
        /// <summary>
        /// Inserts multiple entities into the specified table in a single batch operation.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="client">The D1 client.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Array of results for each insert operation.</returns>
        public static async Task<D1QueryResult[]> BatchInsertAsync<T>(
            this ID1Client client,
            string tableName,
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default) where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return Array.Empty<D1QueryResult>();

            // Get properties from first entity
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToList();

            if (properties.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).Name} has no readable properties");

            // Build INSERT statements
            var statements = new List<D1Statement>();
            
            foreach (var entity in entityList)
            {
                // Exclude properties with default/zero values for potential auto-increment columns
                var valuesToInsert = properties
                    .Select(p => new { Property = p, Value = p.GetValue(entity) })
                    .Where(x => !IsDefaultValue(x.Value))
                    .ToList();

                if (valuesToInsert.Count == 0)
                    throw new InvalidOperationException($"Entity has no non-default values to insert");

                var columns = string.Join(", ", valuesToInsert.Select(x => ToSnakeCase(x.Property.Name)));
                var placeholders = string.Join(", ", Enumerable.Range(0, valuesToInsert.Count).Select(_ => "?"));
                var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({placeholders})";
                
                var parameters = valuesToInsert.Select(x => x.Value).ToArray();
                
                statements.Add(new D1Statement
                {
                    Sql = sql,
                    Params = parameters
                });
            }

            return await client.BatchAsync(statements, cancellationToken);
        }

        private static bool IsDefaultValue(object? value)
        {
            if (value == null)
                return true;

            var type = value.GetType();
            if (!type.IsValueType)
                return false;

            // Check if value equals the default for its type
            var defaultValue = Activator.CreateInstance(type);
            return value.Equals(defaultValue);
        }

        /// <summary>
        /// Updates multiple entities in the specified table based on a key selector.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="client">The D1 client.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="entities">The entities to update.</param>
        /// <param name="keySelector">Function to select the key property for WHERE clause.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Array of results for each update operation.</returns>
        public static async Task<D1QueryResult[]> BatchUpdateAsync<T>(
            this ID1Client client,
            string tableName,
            IEnumerable<T> entities,
            Func<T, object> keySelector,
            CancellationToken cancellationToken = default) where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return Array.Empty<D1QueryResult>();

            // Get properties from first entity (excluding key)
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .ToList();

            if (properties.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).Name} has no readable/writable properties");

            // Build UPDATE statements
            var statements = new List<D1Statement>();
            
            foreach (var entity in entityList)
            {
                var keyValue = keySelector(entity);
                var keyName = "id"; // Default assumption
                
                // Try to find key property name
                foreach (var prop in properties)
                {
                    if (object.Equals(prop.GetValue(entity), keyValue))
                    {
                        keyName = ToSnakeCase(prop.Name);
                        break;
                    }
                }

                var setClauses = properties
                    .Where(p => !object.Equals(p.GetValue(entity), keyValue)) // Exclude key from SET
                    .Select(p => $"{ToSnakeCase(p.Name)} = ?")
                    .ToList();

                if (setClauses.Count == 0)
                    continue; // Skip if no properties to update

                var sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyName} = ?";
                
                var parameters = properties
                    .Where(p => !object.Equals(p.GetValue(entity), keyValue))
                    .Select(p => p.GetValue(entity))
                    .Concat(new[] { keyValue })
                    .ToArray();
                
                statements.Add(new D1Statement
                {
                    Sql = sql,
                    Params = parameters
                });
            }

            if (statements.Count == 0)
                return Array.Empty<D1QueryResult>();

            return await client.BatchAsync(statements, cancellationToken);
        }

        /// <summary>
        /// Deletes multiple entities from the specified table by their IDs.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="client">The D1 client.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="ids">The IDs of entities to delete.</param>
        /// <param name="keyColumnName">The name of the key column (default: "id").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Array of results for each delete operation.</returns>
        public static async Task<D1QueryResult[]> BatchDeleteAsync<TKey>(
            this ID1Client client,
            string tableName,
            IEnumerable<TKey> ids,
            string keyColumnName = "id",
            CancellationToken cancellationToken = default)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (idList.Count == 0)
                return Array.Empty<D1QueryResult>();

            // Build DELETE statements
            var statements = idList.Select(id => new D1Statement
            {
                Sql = $"DELETE FROM {tableName} WHERE {keyColumnName} = ?",
                Params = new object?[] { id }
            }).ToList();

            return await client.BatchAsync(statements, cancellationToken);
        }

        /// <summary>
        /// Inserts an entity or replaces it if it already exists (upsert operation).
        /// Uses SQLite's INSERT OR REPLACE syntax.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="client">The D1 client.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="entity">The entity to upsert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The query result.</returns>
        public static async Task<D1QueryResult> UpsertAsync<T>(
            this ID1Client client,
            string tableName,
            T entity,
            CancellationToken cancellationToken = default) where T : class
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));
            
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Get properties
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToList();

            if (properties.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).Name} has no readable properties");

            var columns = string.Join(", ", properties.Select(p => ToSnakeCase(p.Name)));
            var placeholders = string.Join(", ", Enumerable.Range(0, properties.Count).Select(_ => "?"));
            var sql = $"INSERT OR REPLACE INTO {tableName} ({columns}) VALUES ({placeholders})";
            
            var parameters = properties.Select(p => p.GetValue(entity)).ToArray();

            return await client.ExecuteAsync(sql, parameters, cancellationToken);
        }

        /// <summary>
        /// Converts a PascalCase property name to snake_case column name.
        /// </summary>
        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLower(name[0]));

            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(name[i]));
                }
                else
                {
                    result.Append(name[i]);
                }
            }

            return result.ToString();
        }
    }
}
