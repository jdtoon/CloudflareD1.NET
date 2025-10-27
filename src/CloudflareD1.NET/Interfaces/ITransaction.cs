using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Models;

namespace CloudflareD1.NET
{
    /// <summary>
    /// Represents a database transaction that groups multiple operations into an atomic unit.
    /// All operations will succeed or fail together.
    /// </summary>
    public interface ITransaction : IAsyncDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the transaction is active (not committed or rolled back).
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Executes a SQL query within the transaction.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional parameters for the query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The query result containing rows and metadata.</returns>
        Task<D1QueryResult> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a SQL command (INSERT, UPDATE, DELETE) within the transaction.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="parameters">Optional parameters for the command.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of rows affected and metadata.</returns>
        Task<D1QueryResult> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the transaction, making all changes permanent.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction, discarding all changes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
