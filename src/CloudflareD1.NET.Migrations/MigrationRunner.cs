using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudflareD1.NET.Migrations
{
    /// <summary>
    /// Interface for running database migrations.
    /// </summary>
    public interface IMigrationRunner
    {
        /// <summary>
        /// Applies all pending migrations to the database.
        /// </summary>
        /// <returns>List of migration IDs that were applied.</returns>
        Task<List<string>> MigrateAsync();

        /// <summary>
        /// Applies migrations up to a specific migration.
        /// </summary>
        /// <param name="targetMigrationId">The target migration ID to migrate to.</param>
        /// <returns>List of migration IDs that were applied.</returns>
        Task<List<string>> MigrateToAsync(string targetMigrationId);

        /// <summary>
        /// Rolls back the last applied migration.
        /// </summary>
        /// <returns>The migration ID that was rolled back.</returns>
        Task<string?> RollbackAsync();

        /// <summary>
        /// Rolls back to a specific migration.
        /// </summary>
        /// <param name="targetMigrationId">The migration ID to rollback to.</param>
        /// <returns>List of migration IDs that were rolled back.</returns>
        Task<List<string>> RollbackToAsync(string targetMigrationId);

        /// <summary>
        /// Gets all applied migrations from the database.
        /// </summary>
        /// <returns>List of applied migration IDs.</returns>
        Task<List<string>> GetAppliedMigrationsAsync();

        /// <summary>
        /// Gets all pending migrations that haven't been applied yet.
        /// </summary>
        /// <returns>List of pending migration IDs.</returns>
        Task<List<string>> GetPendingMigrationsAsync();
    }

    /// <summary>
    /// Runs database migrations against a D1 database.
    /// </summary>
    public class MigrationRunner : IMigrationRunner
    {
        private const string MigrationsTableName = "__migrations";
        private readonly ID1Client _client;
        private readonly List<Migration> _migrations;

        /// <summary>
        /// Creates a new instance of MigrationRunner.
        /// </summary>
        /// <param name="client">The D1 client to use for executing migrations.</param>
        /// <param name="migrations">The list of migrations to manage.</param>
        public MigrationRunner(ID1Client client, IEnumerable<Migration> migrations)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _migrations = migrations?.OrderBy(m => m.Id).ToList() ?? throw new ArgumentNullException(nameof(migrations));
        }

        /// <inheritdoc/>
        public async Task<List<string>> MigrateAsync()
        {
            await EnsureMigrationsTableExistsAsync();

            var appliedMigrations = await GetAppliedMigrationsAsync();
            var pendingMigrations = _migrations
                .Where(m => !appliedMigrations.Contains(m.Id))
                .OrderBy(m => m.Id)
                .ToList();

            var applied = new List<string>();

            foreach (var migration in pendingMigrations)
            {
                await ApplyMigrationAsync(migration);
                applied.Add(migration.Id);
            }

            return applied;
        }

        /// <inheritdoc/>
        public async Task<List<string>> MigrateToAsync(string targetMigrationId)
        {
            if (string.IsNullOrWhiteSpace(targetMigrationId))
                throw new ArgumentException("Target migration ID cannot be null or empty.", nameof(targetMigrationId));

            await EnsureMigrationsTableExistsAsync();

            var targetMigration = _migrations.FirstOrDefault(m => m.Id == targetMigrationId);
            if (targetMigration == null)
                throw new InvalidOperationException($"Migration with ID '{targetMigrationId}' not found.");

            var appliedMigrations = await GetAppliedMigrationsAsync();
            var pendingMigrations = _migrations
                .Where(m => !appliedMigrations.Contains(m.Id) && string.Compare(m.Id, targetMigrationId, StringComparison.Ordinal) <= 0)
                .OrderBy(m => m.Id)
                .ToList();

            var applied = new List<string>();

            foreach (var migration in pendingMigrations)
            {
                await ApplyMigrationAsync(migration);
                applied.Add(migration.Id);
            }

            return applied;
        }

        /// <inheritdoc/>
        public async Task<string?> RollbackAsync()
        {
            await EnsureMigrationsTableExistsAsync();

            var appliedMigrations = await GetAppliedMigrationsAsync();
            if (appliedMigrations.Count == 0)
                return null;

            var lastMigrationId = appliedMigrations.Last();
            var migration = _migrations.FirstOrDefault(m => m.Id == lastMigrationId);

            if (migration == null)
                throw new InvalidOperationException($"Migration with ID '{lastMigrationId}' not found in migration list.");

            await RollbackMigrationAsync(migration);
            return lastMigrationId;
        }

        /// <inheritdoc/>
        public async Task<List<string>> RollbackToAsync(string targetMigrationId)
        {
            if (string.IsNullOrWhiteSpace(targetMigrationId))
                throw new ArgumentException("Target migration ID cannot be null or empty.", nameof(targetMigrationId));

            await EnsureMigrationsTableExistsAsync();

            var targetMigration = _migrations.FirstOrDefault(m => m.Id == targetMigrationId);
            if (targetMigration == null)
                throw new InvalidOperationException($"Migration with ID '{targetMigrationId}' not found.");

            var appliedMigrations = await GetAppliedMigrationsAsync();
            var migrationsToRollback = appliedMigrations
                .Where(id => string.Compare(id, targetMigrationId, StringComparison.Ordinal) > 0)
                .OrderByDescending(id => id)
                .ToList();

            var rolledBack = new List<string>();

            foreach (var migrationId in migrationsToRollback)
            {
                var migration = _migrations.FirstOrDefault(m => m.Id == migrationId);
                if (migration == null)
                    throw new InvalidOperationException($"Migration with ID '{migrationId}' not found in migration list.");

                await RollbackMigrationAsync(migration);
                rolledBack.Add(migrationId);
            }

            return rolledBack;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetAppliedMigrationsAsync()
        {
            await EnsureMigrationsTableExistsAsync();

            var result = await _client.QueryAsync($"SELECT migration_id FROM {MigrationsTableName} ORDER BY applied_at");

            var migrations = new List<string>();
            if (result.Results != null)
            {
                foreach (var row in result.Results)
                {
                    if (row.TryGetValue("migration_id", out var migrationId) && migrationId != null)
                    {
                        migrations.Add(migrationId.ToString()!);
                    }
                }
            }

            return migrations;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetPendingMigrationsAsync()
        {
            var appliedMigrations = await GetAppliedMigrationsAsync();
            return _migrations
                .Where(m => !appliedMigrations.Contains(m.Id))
                .Select(m => m.Id)
                .OrderBy(id => id)
                .ToList();
        }

        private async Task EnsureMigrationsTableExistsAsync()
        {
            await _client.ExecuteAsync($@"
                CREATE TABLE IF NOT EXISTS {MigrationsTableName} (
                    migration_id TEXT PRIMARY KEY,
                    migration_name TEXT NOT NULL,
                    applied_at TEXT NOT NULL DEFAULT (datetime('now'))
                )
            ");
        }

        private async Task ApplyMigrationAsync(Migration migration)
        {
            var builder = new MigrationBuilder();
            migration.Up(builder);

            // Execute all statements generated by the migration
            foreach (var statement in builder.Statements)
            {
                await _client.ExecuteAsync(statement);
            }

            // Record the migration as applied
            await _client.ExecuteAsync(
                $"INSERT INTO {MigrationsTableName} (migration_id, migration_name) VALUES (@id, @name)",
                new { id = migration.Id, name = migration.Name }
            );
        }

        private async Task RollbackMigrationAsync(Migration migration)
        {
            var builder = new MigrationBuilder();
            migration.Down(builder);

            // Execute all rollback statements
            foreach (var statement in builder.Statements)
            {
                await _client.ExecuteAsync(statement);
            }

            // Remove the migration record
            await _client.ExecuteAsync(
                $"DELETE FROM {MigrationsTableName} WHERE migration_id = @id",
                new { id = migration.Id }
            );
        }
    }
}
