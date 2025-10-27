using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Initialfrommodel
/// Created: 2025-10-27 20:00:14 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251027200014_Initialfrommodel : Migration
{
    public override string Id => "20251027200014";
    public override string Name => "Initialfrommodel";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("name");
        });

    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("users");

    }
}
