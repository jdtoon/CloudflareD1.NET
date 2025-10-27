using CloudflareD1.NET.Migrations;
using Xunit;

namespace CloudflareD1.NET.Migrations.Tests;

public class MigrationTests
{
    [Fact]
    public void GetFullName_ReturnsCorrectFormat()
    {
        // Arrange
        var migration = new TestMigrationForNaming();

        // Act
        var fullName = migration.GetFullName();

        // Assert
        Assert.Equal("20250127120000_CreateUsersTable", fullName);
    }

    [Fact]
    public void Migration_HasCorrectIdAndName()
    {
        // Arrange
        var migration = new TestMigrationForNaming();

        // Act & Assert
        Assert.Equal("20250127120000", migration.Id);
        Assert.Equal("CreateUsersTable", migration.Name);
    }

    private class TestMigrationForNaming : Migration
    {
        public override string Id => "20250127120000";
        public override string Name => "CreateUsersTable";

        public override void Up(MigrationBuilder builder)
        {
            builder.CreateTable("users", t => t.Integer("id").PrimaryKey());
        }

        public override void Down(MigrationBuilder builder)
        {
            builder.DropTable("users");
        }
    }
}
