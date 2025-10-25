using System.Collections.Generic;

namespace CloudflareD1.NET.Linq.Mapping
{
    /// <summary>
    /// Interface for mapping database query results to entity objects.
    /// </summary>
    public interface IEntityMapper
    {
        /// <summary>
        /// Maps a single row (dictionary of column names to values) to an entity of type T.
        /// </summary>
        /// <typeparam name="T">The entity type to map to.</typeparam>
        /// <param name="row">Dictionary containing column names and their values.</param>
        /// <returns>An instance of T with properties populated from the row data.</returns>
        T Map<T>(Dictionary<string, object?> row) where T : new();

        /// <summary>
        /// Maps multiple rows to a collection of entities of type T.
        /// </summary>
        /// <typeparam name="T">The entity type to map to.</typeparam>
        /// <param name="rows">Collection of dictionaries containing column names and their values.</param>
        /// <returns>Collection of T instances with properties populated from the row data.</returns>
        IEnumerable<T> MapMany<T>(IEnumerable<Dictionary<string, object?>> rows) where T : new();
    }
}
