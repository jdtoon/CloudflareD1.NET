using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Initialcreate
/// Created: 2025-10-28 08:36:22 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028083622_Initialcreate : Migration
{
    public override string Id => "20251028083622";
    public override string Name => "Initialcreate";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("products", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("name").NotNull();
            t.Real("price").NotNull();
        });

    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("products");

    }
}
