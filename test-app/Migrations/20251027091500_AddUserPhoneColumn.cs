using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: AddUserPhoneColumn
/// Created: 2025-10-27 09:15:00 UTC
/// </summary>
public class Migration20251027091500_AddUserPhoneColumn : Migration
{
    public override string Id => "20251027091500";
    public override string Name => "AddUserPhoneColumn";

    public override void Up(MigrationBuilder builder)
    {
        builder.AddColumn("users", "phone", "TEXT", nullable: true);
        builder.CreateIndex("idx_users_phone", "users", new[] { "phone" });
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropIndex("idx_users_phone");
        builder.DropColumn("users", "phone");
    }
}
