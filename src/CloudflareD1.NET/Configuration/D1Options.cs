using System;

namespace CloudflareD1.NET.Configuration
{
    /// <summary>
    /// Configuration options for Cloudflare D1 client.
    /// </summary>
    public class D1Options
    {
        /// <summary>
        /// Gets or sets the Cloudflare Account ID. Required for remote D1 operations.
        /// </summary>
        public string? AccountId { get; set; }

        /// <summary>
        /// Gets or sets the Cloudflare API Token for authentication.
        /// Recommended authentication method. Can be Account-level or with D1 permissions.
        /// </summary>
        public string? ApiToken { get; set; }

        /// <summary>
        /// Gets or sets the Cloudflare API Key for authentication.
        /// Legacy authentication method, requires <see cref="Email"/> to be set.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the Cloudflare account email for API Key authentication.
        /// Required when using <see cref="ApiKey"/>.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the Database ID for D1 operations.
        /// Required for database-specific operations.
        /// </summary>
        public string? DatabaseId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use local SQLite mode.
        /// When true, all operations will use a local SQLite database file instead of Cloudflare D1 API.
        /// Default is true for easier local development.
        /// </summary>
        public bool UseLocalMode { get; set; } = true;

        /// <summary>
        /// Gets or sets the local SQLite database file path.
        /// Only used when <see cref="UseLocalMode"/> is true.
        /// Default is "local.db" in the current directory.
        /// </summary>
        public string LocalDatabasePath { get; set; } = "local.db";

        /// <summary>
        /// Gets or sets the Cloudflare API base URL.
        /// Default is "https://api.cloudflare.com/client/v4/".
        /// </summary>
        public string ApiBaseUrl { get; set; } = "https://api.cloudflare.com/client/v4/";

        /// <summary>
        /// Gets or sets the HTTP request timeout in seconds.
        /// Default is 30 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (UseLocalMode)
            {
                if (string.IsNullOrWhiteSpace(LocalDatabasePath))
                {
                    throw new InvalidOperationException("LocalDatabasePath must be specified when UseLocalMode is true.");
                }
                return;
            }

            // Remote mode validation
            if (string.IsNullOrWhiteSpace(AccountId))
            {
                throw new InvalidOperationException("AccountId is required for remote Cloudflare D1 operations.");
            }

            if (string.IsNullOrWhiteSpace(DatabaseId))
            {
                throw new InvalidOperationException("DatabaseId is required for remote Cloudflare D1 operations.");
            }

            // Check authentication
            var hasApiToken = !string.IsNullOrWhiteSpace(ApiToken);
            var hasApiKey = !string.IsNullOrWhiteSpace(ApiKey);
            var hasEmail = !string.IsNullOrWhiteSpace(Email);

            if (!hasApiToken && !hasApiKey)
            {
                throw new InvalidOperationException("Either ApiToken or ApiKey must be provided for authentication.");
            }

            if (hasApiKey && !hasEmail)
            {
                throw new InvalidOperationException("Email is required when using ApiKey authentication.");
            }
        }
    }
}
