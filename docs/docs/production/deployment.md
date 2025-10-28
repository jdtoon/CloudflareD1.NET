---
sidebar_position: 1
---

# Production Deployment Guide

This guide covers best practices for deploying CloudflareD1.NET applications to production environments.

## Environment Configuration

### Local vs Remote Mode

CloudflareD1.NET supports two modes:

**Local Mode (Development)**
```json
{
  "CloudflareD1": {
    "UseLocalMode": true,
    "LocalDatabasePath": "myapp.db"
  }
}
```

**Remote Mode (Production)**
```json
{
  "CloudflareD1": {
    "UseLocalMode": false,
    "AccountId": "your-account-id",
    "DatabaseId": "your-database-id",
    "ApiToken": "your-api-token"
  }
}
```

### Environment Variables

Use environment variables for production credentials:

**Docker/Kubernetes**
```bash
CloudflareD1__UseLocalMode=false
CloudflareD1__AccountId=xxx
CloudflareD1__DatabaseId=xxx
CloudflareD1__ApiToken=xxx
```

**Azure App Service**
```bash
CloudflareD1:UseLocalMode=false
CloudflareD1:AccountId=xxx
CloudflareD1:DatabaseId=xxx
CloudflareD1:ApiToken=xxx
```

**AWS Lambda/ECS**
```bash
CloudflareD1__UseLocalMode=false
CloudflareD1__AccountId=xxx
CloudflareD1__DatabaseId=xxx
CloudflareD1__ApiToken=xxx
```

## Security Best Practices

### API Token Management

**1. Use Scoped Tokens**
- Create API tokens with minimum required permissions
- Scope: `Account → D1 → Edit` (or Read if read-only)
- Never use Global API Keys in production

**2. Rotate Tokens Regularly**
```bash
# Create new token
# Update secrets management system
# Deploy with new token
# Revoke old token after verification
```

**3. Use Secrets Management**

**Kubernetes Secrets**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: cloudflare-d1-secrets
type: Opaque
stringData:
  api-token: your-token-here
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: cloudflare-d1-config
data:
  account-id: "your-account-id"
  database-id: "your-database-id"
```

**Azure Key Vault**
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-vault.vault.azure.net/"),
    new DefaultAzureCredential());
```

**AWS Secrets Manager**
```csharp
builder.Configuration.AddSecretsManager(
    configurator: options =>
    {
        options.SecretFilter = entry => entry.Name.StartsWith("CloudflareD1");
    });
```

**HashiCorp Vault**
```csharp
builder.Configuration.AddVault(options =>
{
    options.ConfigureVaultClientSettings(settings =>
    {
        settings.VaultServerUriWithPort = "https://vault.example.com:8200";
    });
});
```

### Network Security

**1. Use HTTPS Only**
```csharp
services.AddCloudflareD1(options =>
{
    options.ApiBaseUrl = "https://api.cloudflare.com/client/v4/"; // Always HTTPS
});
```

**2. Firewall Rules**
- Allow outbound HTTPS (443) to `api.cloudflare.com`
- No inbound rules needed (client-initiated only)

**3. Private Networks**
```csharp
// For internal APIs, consider API Gateway or reverse proxy
services.AddHttpClient<D1Client>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = true,
        Proxy = new WebProxy("http://your-proxy:8080")
    });
```

## Health Check Integration

### ASP.NET Core Health Checks

```csharp
// Startup.cs / Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("d1-database", async () =>
    {
        var d1Client = services.GetRequiredService<ID1Client>();
        var health = await d1Client.CheckHealthAsync();
        
        return health.IsHealthy
            ? HealthCheckResult.Healthy($"D1 connection healthy (Latency: {health.LatencyMs}ms)")
            : HealthCheckResult.Unhealthy($"D1 connection failed: {health.ErrorMessage}");
    });

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
```

### Kubernetes Probes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  template:
    spec:
      containers:
      - name: myapp
        image: myapp:latest
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
```

### Docker Health Check

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

## Monitoring & Observability

### Structured Logging

```csharp
builder.Logging.AddConsole();
builder.Logging.AddApplicationInsights(); // Azure
// or
builder.Logging.AddAWSProvider(); // AWS CloudWatch

builder.Services.AddCloudflareD1(options =>
{
    // Logging is automatic - queries log at Information level
    // Enable Debug for detailed request/response logging
});
```

**Example Log Output:**
```
info: CloudflareD1.NET.D1Client[0]
      D1 query executed successfully, returned 42 result(s) (Duration: 123ms)
      
warn: CloudflareD1.NET.Providers.CloudflareD1Provider[0]
      D1 API request failed (Attempt 2/3): Rate limit exceeded. Retrying in 200ms...
      
info: CloudflareD1.NET.Providers.CloudflareD1Provider[0]
      Health check completed: Healthy (Latency: 87.45ms)
```

### Metrics Collection

```csharp
// Custom metrics with Application Insights
public class D1MetricsCollector
{
    private readonly TelemetryClient _telemetry;
    
    public async Task<T> TrackD1Operation<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            _telemetry.TrackMetric($"D1.{operationName}.Duration", sw.ElapsedMilliseconds);
            _telemetry.TrackMetric($"D1.{operationName}.Success", 1);
            return result;
        }
        catch (Exception ex)
        {
            _telemetry.TrackMetric($"D1.{operationName}.Error", 1);
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
```

### Distributed Tracing

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter(); // Or Jaeger, Zipkin, etc.
    });
```

## Performance Configuration

### Retry Policy

```csharp
services.AddCloudflareD1(options =>
{
    options.EnableRetry = true;          // Default: true
    options.MaxRetries = 3;               // Default: 3
    options.InitialRetryDelayMs = 100;    // Default: 100ms (exponential backoff)
});
```

**Retry Behavior:**
- Retries: 429 (rate limit), 503 (service unavailable), network errors, timeouts
- Exponential backoff: 100ms → 200ms → 400ms → 800ms
- Total max delay: ~1.5 seconds for 3 retries

### Timeout Configuration

```csharp
services.AddCloudflareD1(options =>
{
    options.TimeoutSeconds = 30; // Default: 30 seconds
    // Increase for long-running queries
    // Decrease for strict latency requirements
});
```

### Batch Operations

```csharp
// Efficient bulk inserts
var statements = users.Select(u => new D1Statement
{
    Sql = "INSERT INTO users (name, email) VALUES (?, ?)",
    Params = new object[] { u.Name, u.Email }
}).ToList();

await client.BatchAsync(statements);
```

### Connection Pooling (Local Mode)

```csharp
// Local mode uses a single connection with locking
// For high concurrency, consider:
services.AddSingleton<ID1Client>(sp =>
{
    var options = Options.Create(new D1Options
    {
        UseLocalMode = true,
        LocalDatabasePath = "myapp.db"
    });
    return new D1Client(options, sp.GetRequiredService<ILogger<D1Client>>());
});
```

## Deployment Checklist

### Pre-Deployment

- [ ] All tests passing
- [ ] Migration scripts tested
- [ ] API tokens created with correct permissions
- [ ] Secrets stored in secrets management system
- [ ] Health check endpoints tested
- [ ] Logging configured
- [ ] Monitoring/alerting set up
- [ ] Load testing completed

### Deployment

- [ ] Deploy with environment variables
- [ ] Verify health check responds
- [ ] Run smoke tests
- [ ] Check logs for errors
- [ ] Monitor initial traffic

### Post-Deployment

- [ ] Verify metrics collection
- [ ] Test rollback procedure
- [ ] Document deployment notes
- [ ] Update runbook

## High Availability Patterns

### Multi-Region Deployment

```csharp
// Use Cloudflare's global network
// D1 automatically replicates to edge locations
services.AddCloudflareD1(options =>
{
    options.AccountId = "your-account";
    options.DatabaseId = "your-database"; // Same database, global reach
});
```

### Graceful Degradation

```csharp
public class ResilientDataService
{
    private readonly ID1Client _d1Client;
    private readonly IMemoryCache _cache;
    
    public async Task<User> GetUserAsync(int id)
    {
        // Try cache first
        if (_cache.TryGetValue($"user:{id}", out User cachedUser))
            return cachedUser;
        
        try
        {
            // Try D1
            var user = await _d1Client.Query<User>("users")
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();
            
            // Cache on success
            _cache.Set($"user:{id}", user, TimeSpan.FromMinutes(5));
            return user;
        }
        catch (D1Exception ex)
        {
            _logger.LogError(ex, "D1 query failed, using fallback");
            // Fallback logic here
            throw;
        }
    }
}
```

### Circuit Breaker Pattern

```csharp
// Using Polly
services.AddHttpClient<D1Client>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30)
        ));
```

## Scaling Considerations

### Read Replicas
- Cloudflare D1 automatically replicates globally
- Reads are served from nearest edge location
- No additional configuration needed

### Write Scaling
- D1 uses SQLite, optimized for read-heavy workloads
- Consider caching for write-heavy scenarios
- Use batch operations for bulk writes

### Rate Limits
- Monitor rate limit headers (if exposed by API)
- Implement client-side throttling if needed
- Use exponential backoff (enabled by default)

## Container Deployment

### Docker Compose

```yaml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "8080:8080"
    environment:
      - CloudflareD1__UseLocalMode=false
      - CloudflareD1__AccountId=${CLOUDFLARE_ACCOUNT_ID}
      - CloudflareD1__DatabaseId=${CLOUDFLARE_DATABASE_ID}
      - CloudflareD1__ApiToken=${CLOUDFLARE_API_TOKEN}
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 5s
      retries: 3
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
    spec:
      containers:
      - name: myapp
        image: myapp:latest
        ports:
        - containerPort: 8080
        env:
        - name: CloudflareD1__UseLocalMode
          value: "false"
        - name: CloudflareD1__AccountId
          valueFrom:
            configMapKeyRef:
              name: cloudflare-d1-config
              key: account-id
        - name: CloudflareD1__DatabaseId
          valueFrom:
            configMapKeyRef:
              name: cloudflare-d1-config
              key: database-id
        - name: CloudflareD1__ApiToken
          valueFrom:
            secretKeyRef:
              name: cloudflare-d1-secrets
              key: api-token
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

## Best Practices Summary

✅ **Use environment variables** for configuration  
✅ **Enable retry policy** for resilience  
✅ **Implement health checks** for monitoring  
✅ **Use structured logging** for observability  
✅ **Store secrets securely** (never in code)  
✅ **Test deployment process** before production  
✅ **Monitor query performance** and latency  
✅ **Use batch operations** for efficiency  
✅ **Implement graceful degradation** for HA  
✅ **Document runbooks** for operations team  

## Next Steps

- [Troubleshooting Guide](./troubleshooting.md) - Common errors and solutions
- [Performance Tuning Guide](./performance.md) - Optimization techniques
