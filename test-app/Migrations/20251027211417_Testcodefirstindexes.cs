using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Testcodefirstindexes
/// Created: 2025-10-27 21:14:17 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251027211417_Testcodefirstindexes : Migration
{
    public override string Id => "20251027211417";
    public override string Name => "Testcodefirstindexes";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("orders", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Integer("customer_id").NotNull();
            t.Real("total").NotNull();
            t.Text("status");
            t.Text("order_date");
            t.Text("customer");
            t.ForeignKey("customer_id", "customers", "id", "CASCADE");
        });

        builder.CreateIndex("orders", "ix_orders_customer_id", "customer_id");

        builder.CreateIndex("orders", "ix_orders_status", "status");

        builder.CreateTable("customers", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("first_name").NotNull();
            t.Text("last_name").NotNull();
            t.Text("email").NotNull();
            t.Text("created_at");
        });

        builder.CreateUniqueIndex("customers", "ix_customers_email", "email");

        builder.CreateIndex("customers", "ix_customers_first_name_last_name", "first_name");

    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("orders");

        builder.DropTable("customers");

    }
}
