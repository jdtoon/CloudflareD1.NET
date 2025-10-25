using System;
using System.Net.Http;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudflareD1.NET.Extensions
{
    /// <summary>
    /// Extension methods for configuring Cloudflare D1 services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class D1ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Cloudflare D1 client services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOptions">An action to configure <see cref="D1Options"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <example>
        /// <code>
        /// services.AddCloudflareD1(options =>
        /// {
        ///     options.UseLocalMode = true;
        ///     options.LocalDatabasePath = "local.db";
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddCloudflareD1(this IServiceCollection services, Action<D1Options> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            // Configure options
            services.Configure(configureOptions);

            // Register D1Client as both interfaces
            services.TryAddSingleton<D1Client>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<D1Options>>();
                var logger = sp.GetRequiredService<ILogger<D1Client>>();
                
                // Try to use HttpClientFactory if available, otherwise null
                HttpClient? httpClient = null;
                try
                {
                    var factory = sp.GetService(typeof(System.Net.Http.IHttpClientFactory));
                    if (factory != null)
                    {
                        httpClient = ((System.Net.Http.IHttpClientFactory)factory).CreateClient("CloudflareD1");
                    }
                }
                catch
                {
                    // HttpClientFactory not available, will create HttpClient internally
                }
                
                return new D1Client(options, logger, httpClient);
            });

            services.TryAddSingleton<ID1Client>(sp => sp.GetRequiredService<D1Client>());
            services.TryAddSingleton<ID1ManagementClient>(sp => sp.GetRequiredService<D1Client>());

            return services;
        }

        /// <summary>
        /// Adds Cloudflare D1 client services with configuration from a configuration section.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration section containing D1 options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <example>
        /// <code>
        /// // In appsettings.json:
        /// // "CloudflareD1": {
        /// //   "UseLocalMode": true,
        /// //   "LocalDatabasePath": "local.db"
        /// // }
        /// 
        /// services.AddCloudflareD1(configuration.GetSection("CloudflareD1"));
        /// </code>
        /// </example>
        public static IServiceCollection AddCloudflareD1(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Configure options from configuration
            Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<D1Options>(services, configuration);

            // Register D1Client as both interfaces
            services.TryAddSingleton<D1Client>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<D1Options>>();
                var logger = sp.GetRequiredService<ILogger<D1Client>>();
                
                // Try to use HttpClientFactory if available, otherwise null
                HttpClient? httpClient = null;
                try
                {
                    var factory = sp.GetService(typeof(System.Net.Http.IHttpClientFactory));
                    if (factory != null)
                    {
                        httpClient = ((System.Net.Http.IHttpClientFactory)factory).CreateClient("CloudflareD1");
                    }
                }
                catch
                {
                    // HttpClientFactory not available, will create HttpClient internally
                }
                
                return new D1Client(options, logger, httpClient);
            });

            services.TryAddSingleton<ID1Client>(sp => sp.GetRequiredService<D1Client>());
            services.TryAddSingleton<ID1ManagementClient>(sp => sp.GetRequiredService<D1Client>());

            return services;
        }

        /// <summary>
        /// Adds Cloudflare D1 client services for local development mode.
        /// This is a convenience method that configures the client to use a local SQLite database.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="localDatabasePath">The path to the local SQLite database file. Defaults to "local.db".</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <example>
        /// <code>
        /// services.AddCloudflareD1Local("myapp.db");
        /// </code>
        /// </example>
        public static IServiceCollection AddCloudflareD1Local(this IServiceCollection services, string localDatabasePath = "local.db")
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCloudflareD1(options =>
            {
                options.UseLocalMode = true;
                options.LocalDatabasePath = localDatabasePath;
            });
        }

        /// <summary>
        /// Adds Cloudflare D1 client services for remote Cloudflare D1 mode.
        /// This is a convenience method that configures the client to connect to Cloudflare D1.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="accountId">The Cloudflare Account ID.</param>
        /// <param name="databaseId">The D1 Database ID.</param>
        /// <param name="apiToken">The Cloudflare API Token for authentication.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <example>
        /// <code>
        /// services.AddCloudflareD1Remote(
        ///     accountId: "your-account-id",
        ///     databaseId: "your-database-id",
        ///     apiToken: "your-api-token"
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddCloudflareD1Remote(
            this IServiceCollection services,
            string accountId,
            string databaseId,
            string apiToken)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentException("Account ID cannot be null or empty.", nameof(accountId));
            }

            if (string.IsNullOrWhiteSpace(databaseId))
            {
                throw new ArgumentException("Database ID cannot be null or empty.", nameof(databaseId));
            }

            if (string.IsNullOrWhiteSpace(apiToken))
            {
                throw new ArgumentException("API Token cannot be null or empty.", nameof(apiToken));
            }

            return services.AddCloudflareD1(options =>
            {
                options.UseLocalMode = false;
                options.AccountId = accountId;
                options.DatabaseId = databaseId;
                options.ApiToken = apiToken;
            });
        }

        /// <summary>
        /// Adds Cloudflare D1 client services for remote Cloudflare D1 mode using API Key authentication.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="accountId">The Cloudflare Account ID.</param>
        /// <param name="databaseId">The D1 Database ID.</param>
        /// <param name="apiKey">The Cloudflare API Key for authentication.</param>
        /// <param name="email">The Cloudflare account email.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <example>
        /// <code>
        /// services.AddCloudflareD1RemoteWithApiKey(
        ///     accountId: "your-account-id",
        ///     databaseId: "your-database-id",
        ///     apiKey: "your-api-key",
        ///     email: "your-email@example.com"
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddCloudflareD1RemoteWithApiKey(
            this IServiceCollection services,
            string accountId,
            string databaseId,
            string apiKey,
            string email)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentException("Account ID cannot be null or empty.", nameof(accountId));
            }

            if (string.IsNullOrWhiteSpace(databaseId))
            {
                throw new ArgumentException("Database ID cannot be null or empty.", nameof(databaseId));
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API Key cannot be null or empty.", nameof(apiKey));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            return services.AddCloudflareD1(options =>
            {
                options.UseLocalMode = false;
                options.AccountId = accountId;
                options.DatabaseId = databaseId;
                options.ApiKey = apiKey;
                options.Email = email;
            });
        }
    }
}
