using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var options = Options.Create(new D1Options
{
    UseLocalMode = true,
    LocalDatabasePath = "blog.db"
});

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<D1Client>();
using var client = new D1Client(options, logger);

// Create tables
await client.ExecuteAsync(@"
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY, 
    username TEXT NOT NULL, 
    email TEXT NOT NULL, 
    full_name TEXT, 
    bio TEXT, 
    is_active INTEGER NOT NULL, 
    created_at TEXT NOT NULL
)");

await client.ExecuteAsync(@"
CREATE TABLE IF NOT EXISTS blog_posts (
    id INTEGER PRIMARY KEY, 
    title TEXT NOT NULL, 
    content TEXT, 
    author_id INTEGER NOT NULL, 
    published_at TEXT, 
    created_at TEXT NOT NULL,
    FOREIGN KEY (author_id) REFERENCES users(id)
)");

await client.ExecuteAsync(@"
CREATE TABLE IF NOT EXISTS comments (
    id INTEGER PRIMARY KEY, 
    content TEXT NOT NULL, 
    post_id INTEGER NOT NULL, 
    author_id INTEGER NOT NULL, 
    created_at TEXT NOT NULL,
    FOREIGN KEY (post_id) REFERENCES blog_posts(id),
    FOREIGN KEY (author_id) REFERENCES users(id)
)");

Console.WriteLine("✓ Tables created successfully!");
