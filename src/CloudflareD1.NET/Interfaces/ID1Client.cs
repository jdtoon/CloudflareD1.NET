using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Models;

namespace CloudflareD1.NET
{
    /// <summary>
    /// Interface for D1 database client operations.
    /// </summary>
    public interface ID1Client
    {
        /// <summary>
        /// Executes a SQL query and returns the results.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query (can be array for positional or object for named).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The query result containing rows and metadata.</returns>
        Task<D1QueryResult> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a SQL command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">Optional parameters for the command.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of rows affected and metadata.</returns>
        Task<D1QueryResult> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a batch of SQL statements as a single transaction.
        /// All statements will succeed or fail together.
        /// </summary>
        /// <param name="statements">The list of SQL statements to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An array of results, one for each statement.</returns>
        Task<D1QueryResult[]> BatchAsync(List<D1Statement> statements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes multiple SQL statements in a batch.
        /// Convenience method for simple batches without parameters.
        /// </summary>
        /// <param name="sqlStatements">The SQL statements to execute.</param>
        /// <returns>An array of results, one for each statement.</returns>
        Task<D1QueryResult[]> BatchAsync(params string[] sqlStatements);
    }

    /// <summary>
    /// Extended interface for D1 client with database management operations.
    /// Only available when using remote Cloudflare D1 mode.
    /// </summary>
    public interface ID1ManagementClient
    {
        /// <summary>
        /// Lists all D1 databases in the account.
        /// </summary>
        /// <param name="page">Page number for pagination (default: 1).</param>
        /// <param name="perPage">Number of results per page (default: 20).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of databases.</returns>
        Task<D1PaginatedResult<D1Database>> ListDatabasesAsync(int page = 1, int perPage = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a specific D1 database.
        /// </summary>
        /// <param name="databaseId">The database ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Database information.</returns>
        Task<D1Database> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new D1 database.
        /// </summary>
        /// <param name="name">The name for the new database.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created database information.</returns>
        Task<D1Database> CreateDatabaseAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a D1 database.
        /// </summary>
        /// <param name="databaseId">The database ID to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> DeleteDatabaseAsync(string databaseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries the database at a specific point in time (Time Travel).
        /// Only available for Cloudflare D1 (not local SQLite).
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="timestamp">The timestamp to query at (RFC3339 format or Unix timestamp).</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The query result from the specified point in time.</returns>
        Task<D1QueryResult> QueryAtTimestampAsync(string sql, string timestamp, object? parameters = null, CancellationToken cancellationToken = default);
    }
}
