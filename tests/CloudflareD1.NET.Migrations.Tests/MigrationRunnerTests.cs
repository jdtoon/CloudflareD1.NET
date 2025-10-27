using CloudflareD1.NET;
using CloudflareD1.NET.Migrations;
using CloudflareD1.NET.Models;
using Moq;
using Xunit;

namespace CloudflareD1.NET.Migrations.Tests;

public class MigrationRunnerTests
{
    [Fact]
    public async Task MigrateAsync_AppliesPendingMigrations()
    {
        // Arrange
        var mockClient = new Mock<ID1Client>();
        mockClient.Setup(c => c.QueryAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(new D1QueryResult { Success = true, Results = new List<Dictionary<string, object>>() });
        mockClient.Setup(c => c.ExecuteAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(new D1QueryResult { Success = true });

        var migrations = new Migration[]
        {
            new TestMigration1(),
            new TestMigration2()
        };

        var runner = new MigrationRunner(mockClient.Object, migrations);

        // Act
        var applied = await runner.MigrateAsync();

        // Assert
        Assert.Equal(2, applied.Count);
        Assert.Contains("20240101120000", applied);
        Assert.Contains("20240102120000", applied);
    }

    [Fact]
    public async Task MigrateAsync_SkipsAlreadyAppliedMigrations()
    {
        // Arrange
        var mockClient = new Mock<ID1Client>();

        // Mock that migration 1 is already applied
        mockClient.Setup(c => c.QueryAsync(It.Is<string>(s => s.Contains("SELECT migration_id FROM __migrations")), null, default))
            .ReturnsAsync(new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object>>
                {
                    new() { ["migration_id"] = "20240101120000" }
                }
            });

        mockClient.Setup(c => c.QueryAsync(It.Is<string>(s => !s.Contains("SELECT migration_id")), null, default))
            .ReturnsAsync(new D1QueryResult { Success = true, Results = new List<Dictionary<string, object>>() });

        mockClient.Setup(c => c.ExecuteAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(new D1QueryResult { Success = true });

        var migrations = new Migration[]
        {
            new TestMigration1(),
            new TestMigration2()
        };

        var runner = new MigrationRunner(mockClient.Object, migrations);

        // Act
        var applied = await runner.MigrateAsync();

        // Assert - Only migration 2 should be applied
        Assert.Single(applied);
        Assert.Contains("20240102120000", applied);
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_ReturnsAppliedMigrationIds()
    {
        // Arrange
        var mockClient = new Mock<ID1Client>();
        mockClient.Setup(c => c.QueryAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object>>
                {
                    new() { ["migration_id"] = "20240101120000" },
                    new() { ["migration_id"] = "20240102120000" }
                }
            });

        var migrations = new Migration[] { new TestMigration1() };
        var runner = new MigrationRunner(mockClient.Object, migrations);

        // Act
        var applied = await runner.GetAppliedMigrationsAsync();

        // Assert
        Assert.Equal(2, applied.Count);
        Assert.Contains("20240101120000", applied);
        Assert.Contains("20240102120000", applied);
    }

    [Fact]
    public async Task GetPendingMigrationsAsync_ReturnsCorrectPendingMigrations()
    {
        // Arrange
        var mockClient = new Mock<ID1Client>();

        // Mock that migration 1 is already applied
        mockClient.Setup(c => c.QueryAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(new D1QueryResult
            {
                Success = true,
                Results = new List<Dictionary<string, object>>
                {
                    new() { ["migration_id"] = "20240101120000" }
                }
            });

        var migrations = new Migration[]
        {
            new TestMigration1(),
            new TestMigration2(),
            new TestMigration3()
        };

        var runner = new MigrationRunner(mockClient.Object, migrations);

        // Act
        var pending = await runner.GetPendingMigrationsAsync();

        // Assert
        Assert.Equal(2, pending.Count);
        Assert.Contains("20240102120000", pending);
        Assert.Contains("20240103120000", pending);
    }

    [Fact]
    public void MigrationRunner_WithNullClient_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MigrationRunner(null!, new Migration[] { new TestMigration1() }));
    }

    [Fact]
    public void MigrationRunner_WithNullMigrations_ThrowsException()
    {
        // Arrange
        var mockClient = new Mock<ID1Client>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MigrationRunner(mockClient.Object, null!));
    }

    // Test migration classes
    private class TestMigration1 : Migration
    {
        public override string Id => "20240101120000";
        public override string Name => "CreateUsersTable";

        public override void Up(MigrationBuilder builder)
        {
            builder.CreateTable("users", t =>
            {
                t.Integer("id").PrimaryKey().AutoIncrement();
                t.Text("name").NotNull();
                t.Text("email").NotNull();
            });
        }

        public override void Down(MigrationBuilder builder)
        {
            builder.DropTable("users");
        }
    }

    private class TestMigration2 : Migration
    {
        public override string Id => "20240102120000";
        public override string Name => "CreatePostsTable";

        public override void Up(MigrationBuilder builder)
        {
            builder.CreateTable("posts", t =>
            {
                t.Integer("id").PrimaryKey().AutoIncrement();
                t.Text("title").NotNull();
                t.Text("content");
            });
        }

        public override void Down(MigrationBuilder builder)
        {
            builder.DropTable("posts");
        }
    }

    private class TestMigration3 : Migration
    {
        public override string Id => "20240103120000";
        public override string Name => "CreateCommentsTable";

        public override void Up(MigrationBuilder builder)
        {
            builder.CreateTable("comments", t =>
            {
                t.Integer("id").PrimaryKey().AutoIncrement();
                t.Integer("post_id").NotNull();
                t.Text("content").NotNull();
            });
        }

        public override void Down(MigrationBuilder builder)
        {
            builder.DropTable("comments");
        }
    }
}
