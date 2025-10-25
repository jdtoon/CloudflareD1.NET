using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudflareD1.NET.Models
{
    /// <summary>
    /// Represents the result of a D1 query execution.
    /// </summary>
    public class D1QueryResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the query was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the query results.
        /// </summary>
        [JsonPropertyName("results")]
        public List<Dictionary<string, object?>>? Results { get; set; }

        /// <summary>
        /// Gets or sets metadata about the query execution.
        /// </summary>
        [JsonPropertyName("meta")]
        public D1QueryMeta? Meta { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred during query execution.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<D1Error>? Errors { get; set; }

        /// <summary>
        /// Gets or sets messages from the query execution.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<string>? Messages { get; set; }
    }

    /// <summary>
    /// Metadata about a D1 query execution.
    /// </summary>
    public class D1QueryMeta
    {
        /// <summary>
        /// Gets or sets the duration of the query in milliseconds.
        /// </summary>
        [JsonPropertyName("duration")]
        public double? Duration { get; set; }

        /// <summary>
        /// Gets or sets the number of rows read by the query.
        /// </summary>
        [JsonPropertyName("rows_read")]
        public long? RowsRead { get; set; }

        /// <summary>
        /// Gets or sets the number of rows written by the query.
        /// </summary>
        [JsonPropertyName("rows_written")]
        public long? RowsWritten { get; set; }

        /// <summary>
        /// Gets or sets the last row ID for INSERT operations.
        /// </summary>
        [JsonPropertyName("last_row_id")]
        public long? LastRowId { get; set; }

        /// <summary>
        /// Gets or sets the number of rows changed by the query.
        /// </summary>
        [JsonPropertyName("changes")]
        public long? Changes { get; set; }

        /// <summary>
        /// Gets or sets the size of the result set in bytes.
        /// </summary>
        [JsonPropertyName("size_after")]
        public long? SizeAfter { get; set; }

        /// <summary>
        /// Gets or sets the column information for the result set.
        /// </summary>
        [JsonPropertyName("columns")]
        public List<string>? Columns { get; set; }
    }

    /// <summary>
    /// Represents an error from the Cloudflare D1 API.
    /// </summary>
    public class D1Error
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// Represents a batch of queries to execute.
    /// </summary>
    public class D1BatchRequest
    {
        /// <summary>
        /// Gets or sets the SQL statements to execute.
        /// </summary>
        [JsonPropertyName("sql")]
        public List<D1Statement> Statements { get; set; } = new List<D1Statement>();
    }

    /// <summary>
    /// Represents a single SQL statement with optional parameters.
    /// </summary>
    public class D1Statement
    {
        /// <summary>
        /// Gets or sets the SQL query string.
        /// </summary>
        [JsonPropertyName("sql")]
        public string Sql { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameters for the SQL query.
        /// Parameters can be positional (array) or named (object).
        /// </summary>
        [JsonPropertyName("params")]
        public object? Params { get; set; }
    }

    /// <summary>
    /// Response from Cloudflare D1 API for query execution.
    /// </summary>
    public class D1ApiResponse<T>
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the result data.
        /// </summary>
        [JsonPropertyName("result")]
        public T? Result { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<D1Error>? Errors { get; set; }

        /// <summary>
        /// Gets or sets messages from the API.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<string>? Messages { get; set; }
    }

    /// <summary>
    /// Represents information about a D1 database.
    /// </summary>
    public class D1Database
    {
        /// <summary>
        /// Gets or sets the unique identifier of the database.
        /// </summary>
        [JsonPropertyName("uuid")]
        public string? Uuid { get; set; }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the database.
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; }

        /// <summary>
        /// Gets or sets the number of tables in the database.
        /// </summary>
        [JsonPropertyName("num_tables")]
        public int? NumTables { get; set; }
    }

    /// <summary>
    /// Request to create a new D1 database.
    /// </summary>
    public class CreateDatabaseRequest
    {
        /// <summary>
        /// Gets or sets the name of the database to create.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents paginated results from D1 API.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    public class D1PaginatedResult<T>
    {
        /// <summary>
        /// Gets or sets the list of items.
        /// </summary>
        [JsonPropertyName("result")]
        public List<T>? Result { get; set; }

        /// <summary>
        /// Gets or sets pagination information.
        /// </summary>
        [JsonPropertyName("result_info")]
        public D1ResultInfo? ResultInfo { get; set; }
    }

    /// <summary>
    /// Pagination information for D1 API responses.
    /// </summary>
    public class D1ResultInfo
    {
        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets the total count of items.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }
    }
}
