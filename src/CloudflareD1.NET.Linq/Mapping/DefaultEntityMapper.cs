using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace CloudflareD1.NET.Linq.Mapping
{
    /// <summary>
    /// Default implementation of IEntityMapper using reflection with caching for performance.
    /// Supports mapping from snake_case column names to PascalCase property names.
    /// </summary>
    public class DefaultEntityMapper : IEntityMapper
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyMappingCache = new();

        /// <summary>
        /// Maps a single row to an entity of type T.
        /// </summary>
        public T Map<T>(Dictionary<string, object?> row) where T : new()
        {
            var entity = new T();
            var properties = GetCachedProperties(typeof(T));

            foreach (var kvp in row)
            {
                var property = FindProperty(typeof(T), kvp.Key);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        var value = ConvertValue(kvp.Value, property.PropertyType);
                        property.SetValue(entity, value);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to map column '{kvp.Key}' to property '{property.Name}' on type '{typeof(T).Name}'.", ex);
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// Maps multiple rows to a collection of entities.
        /// </summary>
        public IEnumerable<T> MapMany<T>(IEnumerable<Dictionary<string, object?>> rows) where T : new()
        {
            return rows.Select(Map<T>);
        }

        /// <summary>
        /// Converts a property name to a column name (PascalCase to snake_case).
        /// </summary>
        public string GetColumnName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return propertyName;

            return ConvertPascalCaseToSnakeCase(propertyName);
        }

        private static string ConvertPascalCaseToSnakeCase(string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return pascalCase;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLowerInvariant(pascalCase[0]));

            for (int i = 1; i < pascalCase.Length; i++)
            {
                if (char.IsUpper(pascalCase[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLowerInvariant(pascalCase[i]));
                }
                else
                {
                    result.Append(pascalCase[i]);
                }
            }

            return result.ToString();
        }

        private static PropertyInfo[] GetCachedProperties(Type type)
        {
            return PropertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToArray());
        }

        private static PropertyInfo? FindProperty(Type type, string columnName)
        {
            return PropertyMappingCache.GetOrAdd((type, columnName), key =>
            {
                var properties = GetCachedProperties(key.Item1);

                // Try exact match first
                var exactMatch = properties.FirstOrDefault(p =>
                    string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

                if (exactMatch != null)
                    return exactMatch;

                // Try snake_case to PascalCase conversion
                var pascalCase = ConvertSnakeCaseToPascalCase(columnName);
                return properties.FirstOrDefault(p =>
                    string.Equals(p.Name, pascalCase, StringComparison.OrdinalIgnoreCase));
            });
        }

        private static string ConvertSnakeCaseToPascalCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return snakeCase;

            var parts = snakeCase.Split('_');
            return string.Join("", parts.Select(part =>
                string.IsNullOrEmpty(part) ? "" :
                char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant()));
        }

        private static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null || value is DBNull)
                return null;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
                targetType = underlyingType;

            // Direct type match
            if (targetType.IsInstanceOfType(value))
                return value;

            // Handle JsonElement (from System.Text.Json)
            if (value is JsonElement jsonElement)
            {
                return ConvertJsonElement(jsonElement, targetType);
            }

            // Handle numeric conversions
            if (IsNumericType(targetType) && IsNumericType(value.GetType()))
            {
                return Convert.ChangeType(value, targetType);
            }

            // Handle string conversions
            if (value is string stringValue)
            {
                if (targetType == typeof(Guid))
                    return Guid.Parse(stringValue);

                if (targetType == typeof(DateTime))
                    return DateTime.Parse(stringValue);

                if (targetType == typeof(DateTimeOffset))
                    return DateTimeOffset.Parse(stringValue);

                if (targetType == typeof(TimeSpan))
                    return TimeSpan.Parse(stringValue);

                if (targetType.IsEnum)
                    return Enum.Parse(targetType, stringValue, ignoreCase: true);
            }

            // Handle boolean from int (SQLite stores booleans as 0/1)
            if (targetType == typeof(bool) && value is long longValue)
            {
                return longValue != 0;
            }

            // Try direct conversion
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                throw new InvalidCastException(
                    $"Cannot convert value of type '{value.GetType().Name}' to type '{targetType.Name}'.");
            }
        }

        private static object? ConvertJsonElement(JsonElement element, Type targetType)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;

                case JsonValueKind.String:
                    var stringValue = element.GetString();
                    if (targetType == typeof(string))
                        return stringValue;
                    return ConvertValue(stringValue, targetType);

                case JsonValueKind.Number:
                    // Handle boolean from SQLite (0/1)
                    if (targetType == typeof(bool) || targetType == typeof(bool?))
                        return element.GetInt32() != 0;

                    if (targetType == typeof(int) || targetType == typeof(int?))
                        return element.GetInt32();
                    if (targetType == typeof(long) || targetType == typeof(long?))
                        return element.GetInt64();
                    if (targetType == typeof(double) || targetType == typeof(double?))
                        return element.GetDouble();
                    if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                        return element.GetDecimal();
                    if (targetType == typeof(float) || targetType == typeof(float?))
                        return element.GetSingle();
                    break;

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;
            }

            throw new InvalidCastException(
                $"Cannot convert JsonElement of kind '{element.ValueKind}' to type '{targetType.Name}'.");
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }
    }
}
