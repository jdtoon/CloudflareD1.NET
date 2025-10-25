using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Exceptions;
using CloudflareD1.NET.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudflareD1.NET.Providers
{
    /// <summary>
    /// Local SQLite implementation of D1 operations for development and testing.
    /// Mimics D1 API behavior using a local SQLite database file.
    /// </summary>
    internal class LocalSqliteProvider : IDisposable
    {
        private readonly D1Options _options;
        private readonly ILogger<LocalSqliteProvider> _logger;
        private SqliteConnection? _connection;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        public LocalSqliteProvider(IOptions<D1Options> options, ILogger<LocalSqliteProvider> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ensures the database connection is open.
        /// </summary>
        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                return;
            }

            await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_connection != null && _connection.State == ConnectionState.Open)
                {
                    return;
                }

                _connection?.Dispose();

                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = _options.LocalDatabasePath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Shared
                }.ToString();

                _connection = new SqliteConnection(connectionString);
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Opened local SQLite database at {Path}", _options.LocalDatabasePath);
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// Executes a SQL query and returns the results.
        /// </summary>
        public async Task<D1QueryResult> QueryAsync(string sql, object? parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));
            }

            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var startTime = DateTime.UtcNow;
            try
            {
                using var command = _connection!.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, parameters);

                var results = new List<Dictionary<string, object?>>();
                var columns = new List<string>();

                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Get column names
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columns.Add(reader.GetName(i));
                    }

                    // Read rows
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var row = new Dictionary<string, object?>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row[columns[i]] = value;
                        }
                        results.Add(row);
                    }
                }

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                return new D1QueryResult
                {
                    Success = true,
                    Results = results,
                    Meta = new D1QueryMeta
                    {
                        Duration = duration,
                        RowsRead = results.Count,
                        Columns = columns
                    }
                };
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "SQLite query failed: {Message}", ex.Message);
                throw new D1QueryException($"Query failed: {ex.Message}", ex, sql);
            }
        }

        /// <summary>
        /// Executes a SQL command and returns the result.
        /// </summary>
        public async Task<D1QueryResult> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("SQL command cannot be null or empty.", nameof(sql));
            }

            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var startTime = DateTime.UtcNow;
            try
            {
                using var command = _connection!.CreateCommand();
                command.CommandText = sql;
                AddParameters(command, parameters);

                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Get last insert row id
                long? lastRowId = null;
                if (sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                {
                    using var lastIdCommand = _connection.CreateCommand();
                    lastIdCommand.CommandText = "SELECT last_insert_rowid()";
                    var result = await lastIdCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    if (result != null)
                    {
                        lastRowId = Convert.ToInt64(result);
                    }
                }

                return new D1QueryResult
                {
                    Success = true,
                    Results = new List<Dictionary<string, object?>>(),
                    Meta = new D1QueryMeta
                    {
                        Duration = duration,
                        Changes = rowsAffected,
                        RowsWritten = rowsAffected,
                        LastRowId = lastRowId
                    }
                };
            }
            catch (SqliteException ex)
            {
                _logger.LogError(ex, "SQLite command execution failed: {Message}", ex.Message);
                throw new D1QueryException($"Command execution failed: {ex.Message}", ex, sql);
            }
        }

        /// <summary>
        /// Executes a batch of SQL statements as a transaction.
        /// </summary>
        public async Task<D1QueryResult[]> BatchAsync(List<D1Statement> statements, CancellationToken cancellationToken)
        {
            if (statements == null || statements.Count == 0)
            {
                throw new ArgumentException("Statements list cannot be null or empty.", nameof(statements));
            }

            await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);

            var results = new List<D1QueryResult>();
            using var transaction = _connection!.BeginTransaction();

            try
            {
                foreach (var statement in statements)
                {
                    var isQuery = statement.Sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

                    D1QueryResult result;
                    if (isQuery)
                    {
                        result = await QueryInTransactionAsync(statement.Sql, statement.Params, transaction, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        result = await ExecuteInTransactionAsync(statement.Sql, statement.Params, transaction, cancellationToken).ConfigureAwait(false);
                    }

                    results.Add(result);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Batch of {Count} statements executed successfully", statements.Count);

                return results.ToArray();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError(ex, "Batch execution failed, transaction rolled back");
                throw new D1QueryException("Batch execution failed, transaction rolled back", ex);
            }
        }

        /// <summary>
        /// Executes a query within an existing transaction.
        /// </summary>
        private async Task<D1QueryResult> QueryInTransactionAsync(string sql, object? parameters, SqliteTransaction transaction, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            using var command = _connection!.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            AddParameters(command, parameters);

            var results = new List<Dictionary<string, object?>>();
            var columns = new List<string>();

            using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columns.Add(reader.GetName(i));
                }

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[columns[i]] = value;
                    }
                    results.Add(row);
                }
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new D1QueryResult
            {
                Success = true,
                Results = results,
                Meta = new D1QueryMeta
                {
                    Duration = duration,
                    RowsRead = results.Count,
                    Columns = columns
                }
            };
        }

        /// <summary>
        /// Executes a command within an existing transaction.
        /// </summary>
        private async Task<D1QueryResult> ExecuteInTransactionAsync(string sql, object? parameters, SqliteTransaction transaction, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            using var command = _connection!.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            AddParameters(command, parameters);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            long? lastRowId = null;
            if (sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                using var lastIdCommand = _connection.CreateCommand();
                lastIdCommand.CommandText = "SELECT last_insert_rowid()";
                lastIdCommand.Transaction = transaction;
                var result = await lastIdCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    lastRowId = Convert.ToInt64(result);
                }
            }

            return new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object?>>(),
                Meta = new D1QueryMeta
                {
                    Duration = duration,
                    Changes = rowsAffected,
                    RowsWritten = rowsAffected,
                    LastRowId = lastRowId
                }
            };
        }

        /// <summary>
        /// Adds parameters to a SQLite command.
        /// Supports both positional (array/list) and named (dictionary) parameters.
        /// </summary>
        private void AddParameters(SqliteCommand command, object? parameters)
        {
            if (parameters == null)
            {
                return;
            }

            // Handle array/list for positional parameters
            if (parameters is System.Collections.IEnumerable enumerable && !(parameters is string) && !(parameters is System.Collections.IDictionary))
            {
                int index = 1;
                foreach (var param in enumerable)
                {
                    command.Parameters.AddWithValue($"@p{index}", param ?? DBNull.Value);
                    index++;
                }
                return;
            }

            // Handle dictionary for named parameters
            if (parameters is IDictionary<string, object?> dict)
            {
                foreach (var kvp in dict)
                {
                    var paramName = kvp.Key.StartsWith("@") || kvp.Key.StartsWith("$") || kvp.Key.StartsWith(":")
                        ? kvp.Key
                        : $"@{kvp.Key}";
                    command.Parameters.AddWithValue(paramName, kvp.Value ?? DBNull.Value);
                }
                return;
            }

            // Handle anonymous objects via reflection
            var properties = parameters.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(parameters);
                command.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connectionLock?.Dispose();
        }
    }
}
