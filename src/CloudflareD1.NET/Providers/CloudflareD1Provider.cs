using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Exceptions;
using CloudflareD1.NET.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudflareD1.NET.Providers
{
    /// <summary>
    /// Remote Cloudflare D1 API implementation.
    /// Communicates with Cloudflare's REST API for D1 operations.
    /// </summary>
    internal class CloudflareD1Provider : IDisposable
    {
        private readonly D1Options _options;
        private readonly ILogger<CloudflareD1Provider> _logger;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;

        public CloudflareD1Provider(IOptions<D1Options> options, ILogger<CloudflareD1Provider> logger, HttpClient? httpClient = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (httpClient != null)
            {
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
            else
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_options.ApiBaseUrl),
                    Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
                };
                _ownsHttpClient = true;
            }

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Set authentication headers
            if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
            }
            else if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !string.IsNullOrWhiteSpace(_options.Email))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Auth-Key", _options.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("X-Auth-Email", _options.Email);
            }
        }

        /// <summary>
        /// Executes a SQL query against the D1 database.
        /// </summary>
        public async Task<D1QueryResult> QueryAsync(string sql, object? parameters, CancellationToken cancellationToken)
        {
            var statement = new D1Statement { Sql = sql, Params = parameters };
            var results = await ExecuteStatementsAsync(new[] { statement }, cancellationToken).ConfigureAwait(false);
            return results.First();
        }

        /// <summary>
        /// Executes a SQL command against the D1 database.
        /// </summary>
        public async Task<D1QueryResult> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken)
        {
            return await QueryAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a batch of SQL statements.
        /// </summary>
        public async Task<D1QueryResult[]> BatchAsync(List<D1Statement> statements, CancellationToken cancellationToken)
        {
            return await ExecuteStatementsAsync(statements, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes statements against the D1 API.
        /// </summary>
        private async Task<D1QueryResult[]> ExecuteStatementsAsync(IEnumerable<D1Statement> statements, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId) || string.IsNullOrWhiteSpace(_options.DatabaseId))
            {
                throw new D1ConfigurationException("AccountId and DatabaseId must be configured for remote operations.");
            }

            var url = $"/accounts/{_options.AccountId}/d1/database/{_options.DatabaseId}/query";
            var payload = statements.Select(s => new { sql = s.Sql, @params = s.Params }).ToList();

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            _logger.LogDebug("Executing D1 query: {Url}", url);

            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("D1 API request failed with status {StatusCode}: {Content}",
                        (int)response.StatusCode, responseContent);

                    D1ApiResponse<D1QueryResult[]>? errorResponse = null;
                    try
                    {
                        errorResponse = JsonSerializer.Deserialize<D1ApiResponse<D1QueryResult[]>>(responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch { }

                    throw new D1ApiException(
                        $"D1 API request failed with status {response.StatusCode}",
                        errorResponse?.Errors,
                        (int)response.StatusCode);
                }

                var apiResponse = JsonSerializer.Deserialize<D1ApiResponse<D1QueryResult[]>>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse == null || !apiResponse.Success || apiResponse.Result == null)
                {
                    throw new D1ApiException("D1 API returned unsuccessful response", apiResponse?.Errors);
                }

                return apiResponse.Result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to D1 API failed");
                throw new D1ApiException("Failed to communicate with D1 API", ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                _logger.LogWarning("D1 API request was cancelled");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "D1 API request timed out");
                throw new D1ApiException("D1 API request timed out", ex);
            }
        }

        /// <summary>
        /// Lists all D1 databases in the account.
        /// </summary>
        public async Task<D1PaginatedResult<D1Database>> ListDatabasesAsync(int page, int perPage, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId))
            {
                throw new D1ConfigurationException("AccountId must be configured for database management operations.");
            }

            var url = $"/accounts/{_options.AccountId}/d1/database?page={page}&per_page={perPage}";

            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new D1ApiException($"Failed to list databases: {response.StatusCode}", statusCode: (int)response.StatusCode);
                }

                var result = JsonSerializer.Deserialize<D1ApiResponse<D1PaginatedResult<D1Database>>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Success != true || result.Result == null)
                {
                    throw new D1ApiException("Failed to list databases", result?.Errors);
                }

                return result.Result;
            }
            catch (HttpRequestException ex)
            {
                throw new D1ApiException("Failed to communicate with D1 API", ex);
            }
        }

        /// <summary>
        /// Gets information about a specific database.
        /// </summary>
        public async Task<D1Database> GetDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId))
            {
                throw new D1ConfigurationException("AccountId must be configured for database management operations.");
            }

            var url = $"/accounts/{_options.AccountId}/d1/database/{databaseId}";

            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new D1ApiException($"Failed to get database: {response.StatusCode}", statusCode: (int)response.StatusCode);
                }

                var result = JsonSerializer.Deserialize<D1ApiResponse<D1Database>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Success != true || result.Result == null)
                {
                    throw new D1ApiException("Failed to get database", result?.Errors);
                }

                return result.Result;
            }
            catch (HttpRequestException ex)
            {
                throw new D1ApiException("Failed to communicate with D1 API", ex);
            }
        }

        /// <summary>
        /// Creates a new D1 database.
        /// </summary>
        public async Task<D1Database> CreateDatabaseAsync(string name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId))
            {
                throw new D1ConfigurationException("AccountId must be configured for database management operations.");
            }

            var url = $"/accounts/{_options.AccountId}/d1/database";
            var request = new CreateDatabaseRequest { Name = name };
            var json = JsonSerializer.Serialize(request);

            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new D1ApiException($"Failed to create database: {response.StatusCode}", statusCode: (int)response.StatusCode);
                }

                var result = JsonSerializer.Deserialize<D1ApiResponse<D1Database>>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Success != true || result.Result == null)
                {
                    throw new D1ApiException("Failed to create database", result?.Errors);
                }

                return result.Result;
            }
            catch (HttpRequestException ex)
            {
                throw new D1ApiException("Failed to communicate with D1 API", ex);
            }
        }

        /// <summary>
        /// Deletes a D1 database.
        /// </summary>
        public async Task<bool> DeleteDatabaseAsync(string databaseId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId))
            {
                throw new D1ConfigurationException("AccountId must be configured for database management operations.");
            }

            var url = $"/accounts/{_options.AccountId}/d1/database/{databaseId}";

            try
            {
                using var response = await _httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new D1ApiException($"Failed to delete database: {response.StatusCode}", statusCode: (int)response.StatusCode);
                }

                var result = JsonSerializer.Deserialize<D1ApiResponse<object>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result?.Success == true;
            }
            catch (HttpRequestException ex)
            {
                throw new D1ApiException("Failed to communicate with D1 API", ex);
            }
        }

        /// <summary>
        /// Queries the database at a specific point in time (Time Travel).
        /// </summary>
        public async Task<D1QueryResult> QueryAtTimestampAsync(string sql, string timestamp, object? parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId) || string.IsNullOrWhiteSpace(_options.DatabaseId))
            {
                throw new D1ConfigurationException("AccountId and DatabaseId must be configured for time travel queries.");
            }

            var url = $"/accounts/{_options.AccountId}/d1/database/{_options.DatabaseId}/query";
            var statement = new D1Statement { Sql = sql, Params = parameters };
            var payload = new[] { new { sql = statement.Sql, @params = statement.Params } };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("CF-D1-TimeTravel", timestamp);

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new D1ApiException($"Time travel query failed: {response.StatusCode}", statusCode: (int)response.StatusCode);
                }

                var apiResponse = JsonSerializer.Deserialize<D1ApiResponse<D1QueryResult[]>>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse?.Success != true || apiResponse.Result == null || apiResponse.Result.Length == 0)
                {
                    throw new D1ApiException("Time travel query returned unsuccessful response", apiResponse?.Errors);
                }

                return apiResponse.Result[0];
            }
            catch (HttpRequestException ex)
            {
                throw new D1ApiException("Failed to execute time travel query", ex);
            }
        }

        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}
