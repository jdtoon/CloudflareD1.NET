using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CodeFirstExample;

// Define entities
[Table("users")]
public class User
{
	[Key]
	[Column("id")]
	public int Id { get; set; }

	[Required]
	[Column("username")]
	public string Username { get; set; } = string.Empty;

	[Column("email")]
	public string? Email { get; set; }

	[Column("created_at")]
	public DateTime CreatedAt { get; set; }
}

[Table("orders")]
public class Order
{
	[Key]
	[Column("id")]
	public int Id { get; set; }

	[Required]
	[Column("order_number")]
	public string OrderNumber { get; set; } = string.Empty;

	[Column("user_id")]
	[ForeignKey("User")]
	public int UserId { get; set; }

	[Column("total")]
	public decimal Total { get; set; }

	[Column("created_at")]
	public DateTime CreatedAt { get; set; }
}

// Define DbContext
public class AppDbContext : D1Context
{
	public AppDbContext(D1Client client) : base(client)
	{
	}

	public D1Set<User> Users { get; set; } = null!;
	public D1Set<Order> Orders { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Configure using fluent API
		modelBuilder.Entity<User>()
			.ToTable("users");

		modelBuilder.Entity<Order>()
			.ToTable("orders");
	}
}

class Program
{
	static async Task Main(string[] args)
	{
		Console.WriteLine("CloudflareD1.NET Code-First Example");
		Console.WriteLine("====================================\n");

		// Note: This example shows the API usage
		// You'll need to replace these with actual credentials
	var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
	var logger = loggerFactory.CreateLogger<D1Client>();

	var d1Options = Options.Create(new D1Options
		{
			AccountId = "your-account-id",
			DatabaseId = "your-database-id",
			ApiToken = "your-api-token"
	});

	var client = new D1Client(d1Options, logger);
		var context = new AppDbContext(client);

		Console.WriteLine("Context created successfully!");
		Console.WriteLine($"- Users table: {context.Users.TableName}");
		Console.WriteLine($"- Orders table: {context.Orders.TableName}");

		Console.WriteLine("\nModel metadata:");
		var userMetadata = context.Model.GetEntity<User>();
		if (userMetadata != null)
		{
			Console.WriteLine($"- User entity: {userMetadata.TableName}");
			Console.WriteLine($"  - Properties: {userMetadata.Properties.Count}");
			Console.WriteLine($"  - Primary keys: {userMetadata.PrimaryKey.Count}");
			foreach (var pk in userMetadata.PrimaryKey)
			{
				Console.WriteLine($"    - {pk.PropertyInfo.Name} -> {pk.ColumnName}");
			}
		}

		var orderMetadata = context.Model.GetEntity<Order>();
		if (orderMetadata != null)
		{
			Console.WriteLine($"- Order entity: {orderMetadata.TableName}");
			Console.WriteLine($"  - Properties: {orderMetadata.Properties.Count}");
			Console.WriteLine($"  - Foreign keys: {orderMetadata.ForeignKeys.Count}");
		}

		Console.WriteLine("\n✓ Code-First setup complete!");
		Console.WriteLine("\nNext steps:");
		Console.WriteLine("1. Replace credentials with your Cloudflare D1 details");
		Console.WriteLine("2. Create migrations: dotnet d1 add InitialCreate");
		Console.WriteLine("3. Apply migrations: await context.MigrateAsync()");
		Console.WriteLine("4. Query data: var users = await context.Users.ToListAsync()");
	}
}
