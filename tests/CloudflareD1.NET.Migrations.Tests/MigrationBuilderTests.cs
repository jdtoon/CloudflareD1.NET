using CloudflareD1.NET.Migrations;
using Xunit;

namespace CloudflareD1.NET.Migrations.Tests;

public class MigrationBuilderTests
{
    [Fact]
    public void CreateTable_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey().AutoIncrement();
            t.Text("name").NotNull();
            t.Text("email").NotNull().Unique();
            t.Integer("age");
        });

        // Assert
        Assert.Single(builder.Statements);
        var sql = builder.Statements[0];
        Assert.Contains("CREATE TABLE users", sql);
        Assert.Contains("id INTEGER PRIMARY KEY AUTOINCREMENT", sql);
        Assert.Contains("name TEXT NOT NULL", sql);
        Assert.Contains("email TEXT NOT NULL UNIQUE", sql);
        Assert.Contains("age INTEGER", sql);
    }

    [Fact]
    public void CreateTable_WithDefaultValue_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.CreateTable("posts", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("title").NotNull();
            t.Text("status").Default("draft");
            t.Integer("view_count").Default(0);
        });

        // Assert
        var sql = builder.Statements[0];
        Assert.Contains("status TEXT DEFAULT 'draft'", sql);
        Assert.Contains("view_count INTEGER DEFAULT 0", sql);
    }

    [Fact]
    public void CreateTable_WithConstraints_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.CreateTable("order_items", t =>
        {
            t.Integer("order_id").NotNull();
            t.Integer("product_id").NotNull();
            t.Integer("quantity").NotNull();
            t.PrimaryKey("order_id", "product_id");
            t.ForeignKey("order_id", "orders", "id", onDelete: "CASCADE");
        });

        // Assert
        var sql = builder.Statements[0];
        Assert.Contains("PRIMARY KEY (order_id, product_id)", sql);
        Assert.Contains("FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE", sql);
    }

    [Fact]
    public void DropTable_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.DropTable("users");

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("DROP TABLE IF EXISTS users", builder.Statements[0]);
    }

    [Fact]
    public void DropTable_WithIfExistsFalse_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.DropTable("users", ifExists: false);

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("DROP TABLE users", builder.Statements[0]);
    }

    [Fact]
    public void DropTable_WithIfExistsTrue_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.DropTable("users", ifExists: true);

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("DROP TABLE IF EXISTS users", builder.Statements[0]);
    }

    [Fact]
    public void AddColumn_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.AddColumn("users", "phone", "TEXT", nullable: true);

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("ALTER TABLE users ADD COLUMN phone TEXT", builder.Statements[0]);
    }

    [Fact]
    public void AddColumn_NotNull_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.AddColumn("users", "email", "TEXT", nullable: false);

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("ALTER TABLE users ADD COLUMN email TEXT NOT NULL", builder.Statements[0]);
    }

    [Fact]
    public void AddColumn_WithDefault_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.AddColumn("users", "status", "TEXT", nullable: false, defaultValue: "'active'");

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("ALTER TABLE users ADD COLUMN status TEXT NOT NULL DEFAULT 'active'", builder.Statements[0]);
    }

    [Fact]
    public void DropColumn_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.DropColumn("users", "phone");

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("ALTER TABLE users DROP COLUMN phone", builder.Statements[0]);
    }

    [Fact]
    public void RenameColumn_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.RenameColumn("users", "fullname", "full_name");

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("ALTER TABLE users RENAME COLUMN fullname TO full_name", builder.Statements[0]);
    }

    [Fact]
    public void RenameTable_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.RenameTable("user", "users");

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("ALTER TABLE user RENAME TO users", builder.Statements[0]);
    }

    [Fact]
    public void CreateIndex_SingleColumn_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.CreateIndex("idx_email", "users", new[] { "email" });

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("CREATE INDEX idx_email ON users (email)", builder.Statements[0]);
    }

    [Fact]
    public void CreateIndex_MultipleColumns_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.CreateIndex("idx_name_email", "users", new[] { "first_name", "last_name", "email" });

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("CREATE INDEX idx_name_email ON users (first_name, last_name, email)", builder.Statements[0]);
    }

    [Fact]
    public void CreateIndex_Unique_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.CreateIndex("idx_unique_email", "users", new[] { "email" }, unique: true);

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("CREATE UNIQUE INDEX idx_unique_email ON users (email)", builder.Statements[0]);
    }

    [Fact]
    public void DropIndex_GeneratesCorrectSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.DropIndex("idx_email");

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("DROP INDEX IF EXISTS idx_email", builder.Statements[0]);
    }

    [Fact]
    public void Sql_AddsRawSQL()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act
        builder.Sql("INSERT INTO users (name) VALUES ('Admin')");

        // Assert
        Assert.Single(builder.Statements);
        Assert.Equal("INSERT INTO users (name) VALUES ('Admin')", builder.Statements[0]);
    }

    [Fact]
    public void CreateTable_WithNullTableName_ThrowsException()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.CreateTable(null!, t => { }));
    }

    [Fact]
    public void DropTable_WithNullTableName_ThrowsException()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.DropTable(null!));
    }

    [Fact]
    public void AddColumn_WithNullTableName_ThrowsException()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddColumn(null!, "col", "TEXT"));
    }

    [Fact]
    public void CreateIndex_WithNullIndexName_ThrowsException()
    {
        // Arrange
        var builder = new MigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.CreateIndex(null!, "users", new[] { "col" }));
    }
}
