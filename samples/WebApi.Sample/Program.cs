using CloudflareD1.NET;
using CloudflareD1.NET.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add CloudflareD1.NET in local mode
builder.Services.AddCloudflareD1Local("todos.db");

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var d1 = scope.ServiceProvider.GetRequiredService<ID1Client>();
    await d1.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS todos (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            description TEXT,
            completed INTEGER DEFAULT 0,
            created_at TEXT DEFAULT CURRENT_TIMESTAMP
        )
    ");
}

app.UseHttpsRedirection();

// GET /todos - Get all todos
app.MapGet("/todos", async (ID1Client d1) =>
{
    var result = await d1.QueryAsync("SELECT * FROM todos ORDER BY created_at DESC");
    return Results.Ok(result.Results);
})
.WithName("GetTodos")
.WithDescription("Get all todos");

// GET /todos/{id} - Get a specific todo
app.MapGet("/todos/{id}", async (ID1Client d1, int id) =>
{
    var result = await d1.QueryAsync(
        "SELECT * FROM todos WHERE id = @id",
        new { id });

    if (result.Results?.Count == 0)
        return Results.NotFound();

    return Results.Ok(result.Results![0]);
})
.WithName("GetTodo")
.WithDescription("Get a specific todo by ID");

// POST /todos - Create a new todo
app.MapPost("/todos", async (ID1Client d1, [FromBody] CreateTodoRequest request) =>
{
    var result = await d1.ExecuteAsync(
        "INSERT INTO todos (title, description) VALUES (@title, @description)",
        new { title = request.Title, description = request.Description });

    var newId = result.Meta?.LastRowId;

    return Results.Created($"/todos/{newId}", new { id = newId });
})
.WithName("CreateTodo")
.WithDescription("Create a new todo");

// PUT /todos/{id} - Update a todo
app.MapPut("/todos/{id}", async (ID1Client d1, int id, [FromBody] UpdateTodoRequest request) =>
{
    var result = await d1.ExecuteAsync(
        @"UPDATE todos
          SET title = @title,
              description = @description,
              completed = @completed
          WHERE id = @id",
        new { id, title = request.Title, description = request.Description, completed = request.Completed ? 1 : 0 });

    if (result.Meta?.Changes == 0)
        return Results.NotFound();

    return Results.NoContent();
})
.WithName("UpdateTodo")
.WithDescription("Update an existing todo");

// DELETE /todos/{id} - Delete a todo
app.MapDelete("/todos/{id}", async (ID1Client d1, int id) =>
{
    var result = await d1.ExecuteAsync(
        "DELETE FROM todos WHERE id = @id",
        new { id });

    if (result.Meta?.Changes == 0)
        return Results.NotFound();

    return Results.NoContent();
})
.WithName("DeleteTodo")
.WithDescription("Delete a todo");

// GET /todos/stats - Get statistics
app.MapGet("/todos/stats", async (ID1Client d1) =>
{
    var result = await d1.QueryAsync(@"
        SELECT
            COUNT(*) as total,
            SUM(CASE WHEN completed = 1 THEN 1 ELSE 0 END) as completed,
            SUM(CASE WHEN completed = 0 THEN 1 ELSE 0 END) as pending
        FROM todos
    ");

    return Results.Ok(result.Results![0]);
})
.WithName("GetTodoStats")
.WithDescription("Get todo statistics");

app.Run();

record CreateTodoRequest(string Title, string? Description);
record UpdateTodoRequest(string Title, string? Description, bool Completed);

