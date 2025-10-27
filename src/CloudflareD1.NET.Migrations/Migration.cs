using System;

namespace CloudflareD1.NET.Migrations
{
    /// <summary>
    /// Base class for database migrations.
    /// Each migration represents a versioned schema change.
    /// </summary>
    public abstract class Migration
    {
        /// <summary>
        /// Gets the unique identifier for this migration.
        /// This is typically a timestamp in the format: YYYYMMDDHHMMSS
        /// Example: 20250127120000
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// Gets the descriptive name of this migration.
        /// Example: "CreateUsersTable", "AddEmailToUsers"
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Defines the forward migration (applying the changes).
        /// Use the MigrationBuilder to define schema changes.
        /// </summary>
        /// <param name="builder">The migration builder for defining schema changes.</param>
        public abstract void Up(MigrationBuilder builder);

        /// <summary>
        /// Defines the rollback migration (reverting the changes).
        /// Should undo what the Up method does.
        /// </summary>
        /// <param name="builder">The migration builder for defining schema changes.</param>
        public abstract void Down(MigrationBuilder builder);

        /// <summary>
        /// Gets the full migration identifier in the format: {Id}_{Name}
        /// Example: "20250127120000_CreateUsersTable"
        /// </summary>
        public string GetFullName() => $"{Id}_{Name}";
    }
}
