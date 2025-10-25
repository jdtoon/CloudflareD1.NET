# CloudflareD1.NET Web API Sample

This sample demonstrates how to build a REST API using ASP.NET Core and CloudflareD1.NET.

## Features

- ✅ Complete CRUD operations for a todo list
- ✅ RESTful API design with proper HTTP verbs
- ✅ Dependency injection with D1Client
- ✅ Database initialization on startup
- ✅ Parameterized queries for security
- ✅ Statistics endpoint
- ✅ OpenAPI/Swagger support

## Endpoints

### Get all todos
```
GET /todos
```

### Get a specific todo
```
GET /todos/{id}
```

### Create a new todo
```
POST /todos
Content-Type: application/json

{
  "title": "Buy groceries",
  "description": "Milk, eggs, bread"
}
```

### Update a todo
```
PUT /todos/{id}
Content-Type: application/json

{
  "title": "Buy groceries",
  "description": "Milk, eggs, bread, butter",
  "completed": true
}
```

### Delete a todo
```
DELETE /todos/{id}
```

### Get statistics
```
GET /todos/stats
```

Returns:
```json
{
  "total": 10,
  "completed": 7,
  "pending": 3
}
```

## Running the Sample

1. Navigate to the sample directory:
```bash
cd samples/WebApi.Sample
```

2. Run the application:
```bash
dotnet run
```

3. Open your browser to the displayed URL (typically `https://localhost:7xxx` or `http://localhost:5xxx`)

4. Access the OpenAPI UI at `/openapi/v1.json` or use a tool like `curl` or Postman to test the endpoints.

## Testing with curl

```bash
# Create a todo
curl -X POST https://localhost:7xxx/todos \
  -H "Content-Type: application/json" \
  -d '{"title":"Learn CloudflareD1.NET","description":"Build awesome apps"}'

# Get all todos
curl https://localhost:7xxx/todos

# Get stats
curl https://localhost:7xxx/todos/stats

# Update a todo
curl -X PUT https://localhost:7xxx/todos/1 \
  -H "Content-Type: application/json" \
  -d '{"title":"Learn CloudflareD1.NET","description":"Build awesome apps with D1","completed":true}'

# Delete a todo
curl -X DELETE https://localhost:7xxx/todos/1
```

## Configuration

The sample uses local SQLite mode by default. To switch to Cloudflare D1:

Edit `Program.cs` and replace:
```csharp
builder.Services.AddCloudflareD1Local("todos.db");
```

With:
```csharp
builder.Services.AddCloudflareD1(options =>
{
    options.Mode = D1Mode.Remote;
    options.AccountId = "your-account-id";
    options.DatabaseId = "your-database-id";
    options.ApiToken = "your-api-token";
});
```

Or use configuration in `appsettings.json`:
```json
{
  "CloudflareD1": {
    "Mode": "Remote",
    "AccountId": "your-account-id",
    "DatabaseId": "your-database-id",
    "ApiToken": "your-api-token"
  }
}
```

## Code Highlights

### Dependency Injection
```csharp
builder.Services.AddCloudflareD1Local("todos.db");
```

### Database Initialization
```csharp
using (var scope = app.Services.CreateScope())
{
    var d1 = scope.ServiceProvider.GetRequiredService<ID1Client>();
    await d1.ExecuteAsync("CREATE TABLE IF NOT EXISTS todos (...)");
}
```

### Parameterized Queries
```csharp
app.MapGet("/todos/{id}", async (ID1Client d1, int id) =>
{
    var result = await d1.QueryAsync(
        "SELECT * FROM todos WHERE id = @id",
        new { id });
    // ...
});
```

## Next Steps

- Add authentication and authorization
- Implement pagination for large result sets
- Add validation using FluentValidation
- Add caching with IMemoryCache
- Deploy to Azure, AWS, or Cloudflare Workers
