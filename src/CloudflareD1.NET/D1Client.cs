using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Exceptions;
using CloudflareD1.NET.Models;
using CloudflareD1.NET.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudflareD1.NET
{
    /// <summary>
    /// Main client for interacting with Cloudflare D1 databases.
    /// Supports both local SQLite mode for development and remote Cloudflare D1 mode for production.
    /// </summary>
    public class D1Client : ID1Client, ID1ManagementClient, IDisposable
    {
        private readonly D1Options _options;
        private readonly ILogger<D1Client> _logger;
        private readonly LocalSqliteProvider? _localProvider;
        private readonly CloudflareD1Provider? _remoteProvider;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="D1Client"/> class.
        /// </summary>
        /// <param name="options">Configuration options for the D1 client.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="httpClient">Optional HttpClient for remote operations.</param>
        public D1Client(IOptions<D1Options> options, ILogger<D1Client> logger, HttpClient? httpClient = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate configuration
            try
            {
                _options.Validate();
            }
            catch (InvalidOperationException ex)
            {
                throw new D1ConfigurationException("D1 configuration is invalid", ex);
            }

            // Initialize appropriate provider based on mode
            if (_options.UseLocalMode)
            {
                _logger.LogInformation("Initializing D1 client in local SQLite mode with database at {Path}", 
                    _options.LocalDatabasePath);
                
                var localLogger = new LoggerFactory().CreateLogger<LocalSqliteProvider>();
                _localProvider = new LocalSqliteProvider(options, localLogger);
            }
            else
            {
                _logger.LogInformation("Initializing D1 client in remote Cloudflare mode for account {AccountId}, database {DatabaseId}",
                    _options.AccountId, _options.DatabaseId);
                
                var remoteLogger = new LoggerFactory().CreateLogger<CloudflareD1Provider>();
                _remoteProvider = new CloudflareD1Provider(options, remoteLogger, httpClient);
            }
        }

        #region ID1Client Implementation

        /// <inheritdoc/>
        public async Task<D1QueryResult> QueryAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));
            }

            _logger.LogDebug("Executing query in {Mode} mode: {Sql}", 
                _options.UseLocalMode ? "local" : "remote", sql);

            try
            {
                if (_options.UseLocalMode)
                {
                    return await _localProvider!.QueryAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await _remoteProvider!.QueryAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!(ex is D1Exception))
            {
                _logger.LogError(ex, "Unexpected error executing query");
                throw new D1QueryException("An unexpected error occurred while executing the query", ex, sql);
            }
        }

        /// <inheritdoc/>
        public async Task<D1QueryResult> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("SQL command cannot be null or empty.", nameof(sql));
            }

            _logger.LogDebug("Executing command in {Mode} mode: {Sql}",
                _options.UseLocalMode ? "local" : "remote", sql);

            try
            {
                if (_options.UseLocalMode)
                {
                    return await _localProvider!.ExecuteAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await _remoteProvider!.ExecuteAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!(ex is D1Exception))
            {
                _logger.LogError(ex, "Unexpected error executing command");
                throw new D1QueryException("An unexpected error occurred while executing the command", ex, sql);
            }
        }

        /// <inheritdoc/>
        public async Task<D1QueryResult[]> BatchAsync(List<D1Statement> statements, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (statements == null || statements.Count == 0)
            {
                throw new ArgumentException("Statements list cannot be null or empty.", nameof(statements));
            }

            _logger.LogDebug("Executing batch of {Count} statements in {Mode} mode",
                statements.Count, _options.UseLocalMode ? "local" : "remote");

            try
            {
                if (_options.UseLocalMode)
                {
                    return await _localProvider!.BatchAsync(statements, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await _remoteProvider!.BatchAsync(statements, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!(ex is D1Exception))
            {
                _logger.LogError(ex, "Unexpected error executing batch");
                throw new D1QueryException("An unexpected error occurred while executing the batch", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<D1QueryResult[]> BatchAsync(params string[] sqlStatements)
        {
            if (sqlStatements == null || sqlStatements.Length == 0)
            {
                throw new ArgumentException("SQL statements cannot be null or empty.", nameof(sqlStatements));
            }

            var statements = sqlStatements.Select(sql => new D1Statement { Sql = sql }).ToList();
            return await BatchAsync(statements, CancellationToken.None).ConfigureAwait(false);
        }

        #endregion

        #region ID1ManagementClient Implementation

        /// <inheritdoc/>
        public async Task<D1PaginatedResult<D1Database>> ListDatabasesAsync(int page = 1, int perPage = 20, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureRemoteMode();

            _logger.LogDebug("Listing databases (page {Page}, per_page {PerPage})", page, perPage);

            return await _remoteProvider!.ListDatabasesAsync(page, perPage, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<D1Database> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureRemoteMode();

            if (string.IsNullOrWhiteSpace(databaseId))
            {
                throw new ArgumentException("Database ID cannot be null or empty.", nameof(databaseId));
            }

            _logger.LogDebug("Getting database info for {DatabaseId}", databaseId);

            return await _remoteProvider!.GetDatabaseAsync(databaseId, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<D1Database> CreateDatabaseAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureRemoteMode();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Database name cannot be null or empty.", nameof(name));
            }

            _logger.LogInformation("Creating new database: {Name}", name);

            return await _remoteProvider!.CreateDatabaseAsync(name, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDatabaseAsync(string databaseId, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureRemoteMode();

            if (string.IsNullOrWhiteSpace(databaseId))
            {
                throw new ArgumentException("Database ID cannot be null or empty.", nameof(databaseId));
            }

            _logger.LogWarning("Deleting database: {DatabaseId}", databaseId);

            return await _remoteProvider!.DeleteDatabaseAsync(databaseId, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<D1QueryResult> QueryAtTimestampAsync(string sql, string timestamp, object? parameters = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureRemoteMode();

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));
            }

            if (string.IsNullOrWhiteSpace(timestamp))
            {
                throw new ArgumentException("Timestamp cannot be null or empty.", nameof(timestamp));
            }

            _logger.LogDebug("Executing time travel query at {Timestamp}: {Sql}", timestamp, sql);

            return await _remoteProvider!.QueryAtTimestampAsync(sql, timestamp, parameters, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Helper Methods

        private void EnsureRemoteMode()
        {
            if (_options.UseLocalMode)
            {
                throw new D1NotSupportedException(
                    "This operation is only supported in remote Cloudflare D1 mode. " +
                    "Set UseLocalMode to false in your D1Options configuration.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(D1Client));
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes the D1 client and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _localProvider?.Dispose();
            _remoteProvider?.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
