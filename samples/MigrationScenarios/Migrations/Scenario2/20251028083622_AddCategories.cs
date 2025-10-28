using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Addcategories
/// Created: 2025-10-28 08:36:22 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028083622_Addcategories : Migration
{
    public override string Id => "20251028083622";
    public override string Name => "Addcategories";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("categories", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("name").NotNull();
        });

    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("categories");

    }
}
