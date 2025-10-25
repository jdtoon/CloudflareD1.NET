using System;
using System.Collections.Generic;
using CloudflareD1.NET.Models;

namespace CloudflareD1.NET.Exceptions
{
    /// <summary>
    /// Base exception for all Cloudflare D1 related errors.
    /// </summary>
    public class D1Exception : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="D1Exception"/> class.
        /// </summary>
        public D1Exception() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="D1Exception"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public D1Exception(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="D1Exception"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public D1Exception(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when a D1 API request fails.
    /// </summary>
    public class D1ApiException : D1Exception
    {
        /// <summary>
        /// Gets the errors returned by the Cloudflare D1 API.
        /// </summary>
        public List<D1Error>? Errors { get; }

        /// <summary>
        /// Gets the HTTP status code of the failed request.
        /// </summary>
        public int? StatusCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="D1ApiException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errors">The errors returned by the API.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public D1ApiException(string message, List<D1Error>? errors = null, int? statusCode = null)
            : base(message)
        {
            Errors = errors;
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="D1ApiException"/> class with an inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="errors">The errors returned by the API.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public D1ApiException(string message, Exception innerException, List<D1Error>? errors = null, int? statusCode = null)
            : base(message, innerException)
        {
            Errors = errors;
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Exception thrown when a SQL query fails to execute.
    /// </summary>
    public class D1QueryException : D1Exception
    {
        /// <summary>
        /// Gets the SQL query that failed.
        /// </summary>
        public string? Sql { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="D1QueryException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="sql">The SQL query that failed.</param>
        public D1QueryException(string message, string? sql = null)
            : base(message)
        {
            Sql = sql;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="D1QueryException"/> class with an inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="sql">The SQL query that failed.</param>
        public D1QueryException(string message, Exception innerException, string? sql = null)
            : base(message, innerException)
        {
            Sql = sql;
        }
    }

    /// <summary>
    /// Exception thrown when D1 configuration is invalid.
    /// </summary>
    public class D1ConfigurationException : D1Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="D1ConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public D1ConfigurationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="D1ConfigurationException"/> class with an inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public D1ConfigurationException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when an operation is not supported in the current mode (local vs remote).
    /// </summary>
    public class D1NotSupportedException : D1Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="D1NotSupportedException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public D1NotSupportedException(string message) : base(message) { }
    }
}
