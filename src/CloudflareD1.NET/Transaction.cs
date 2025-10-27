using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Models;

namespace CloudflareD1.NET
{
    /// <summary>
    /// Represents a database transaction that executes multiple SQL statements atomically.
    /// Uses D1's batch API to ensure all-or-nothing execution.
    /// </summary>
    public class Transaction : ITransaction
    {
        private readonly ID1Client _client;
        private readonly List<D1Statement> _statements;
        private bool _isActive;
        private bool _isDisposed;

        internal Transaction(ID1Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _statements = new List<D1Statement>();
            _isActive = true;
        }

        /// <inheritdoc/>
        public bool IsActive => _isActive && !_isDisposed;

        /// <inheritdoc/>
        public Task<D1QueryResult> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            EnsureActive();
            
            // Add to batch
            _statements.Add(new D1Statement
            {
                Sql = sql,
                Params = parameters
            });

            // Return a placeholder result - actual execution happens on Commit
            return Task.FromResult(new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>(),
                Meta = new D1QueryMeta
                {
                    Duration = 0,
                    RowsRead = 0,
                    RowsWritten = 0
                }
            });
        }

        /// <inheritdoc/>
        public Task<D1QueryResult> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            EnsureActive();
            
            // Add to batch
            _statements.Add(new D1Statement
            {
                Sql = sql,
                Params = parameters
            });

            // Return a placeholder result - actual execution happens on Commit
            return Task.FromResult(new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>(),
                Meta = new D1QueryMeta
                {
                    Duration = 0,
                    RowsRead = 0,
                    RowsWritten = 0,
                    Changes = 0
                }
            });
        }

        /// <inheritdoc/>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            EnsureActive();

            try
            {
                if (_statements.Count > 0)
                {
                    // Execute all statements as a batch (atomic operation in D1)
                    await _client.BatchAsync(_statements, cancellationToken);
                }

                _isActive = false;
            }
            catch
            {
                // On error, mark as inactive and rethrow
                _isActive = false;
                throw;
            }
        }

        /// <inheritdoc/>
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            EnsureActive();

            // Clear all pending statements
            _statements.Clear();
            _isActive = false;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                return;

            // Auto-rollback if still active
            if (_isActive)
            {
                try
                {
                    await RollbackAsync();
                }
                catch
                {
                    // Suppress disposal exceptions
                }
            }

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }

        private void EnsureActive()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(Transaction));

            if (!_isActive)
                throw new InvalidOperationException("Transaction is not active. It has already been committed or rolled back.");
        }
    }
}
