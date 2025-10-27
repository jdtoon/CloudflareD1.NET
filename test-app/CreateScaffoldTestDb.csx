using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

Console.WriteLine("Creating scaffold test database...");

var options = Options.Create(new D1Options
{
    UseLocalMode = true,
    LocalDatabasePath = "scaffold-test.db"
});

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
var logger = loggerFactory.CreateLogger<D1Client>();
var client = new D1Client(options, logger);

// Create customers table
await client.ExecuteAsync(@"
CREATE TABLE customers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    email TEXT UNIQUE,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
)");

await client.ExecuteAsync("CREATE INDEX idx_customers_email ON customers(email)");

// Create orders table
await client.ExecuteAsync(@"
CREATE TABLE orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    customer_id INTEGER NOT NULL,
    total REAL NOT NULL,
    status TEXT DEFAULT 'pending',
    order_date TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(customer_id) REFERENCES customers(id)
)");

await client.ExecuteAsync("CREATE INDEX idx_orders_customer_id ON orders(customer_id)");
await client.ExecuteAsync("CREATE INDEX idx_orders_status ON orders(status)");

Console.WriteLine("✓ Database created with customers and orders tables");
