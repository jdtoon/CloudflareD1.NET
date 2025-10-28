using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Initialcreate
/// Created: 2025-10-28 08:18:58 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028081858_Initialcreate : Migration
{
    public override string Id => "20251028081858";
    public override string Name => "Initialcreate";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("products", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("name").NotNull();
        });

        builder.CreateTable("order_items", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Integer("product_id").NotNull();
            t.Integer("quantity").NotNull();
        });

    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("products");

        builder.DropTable("order_items");

    }
}
