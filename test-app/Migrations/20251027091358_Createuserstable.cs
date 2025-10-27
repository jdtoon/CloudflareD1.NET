using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Createuserstable
/// Created: 2025-10-27 09:13:58 UTC
/// </summary>
public class Migration20251027091358_Createuserstable : Migration
{
    public override string Id => "20251027091358";
    public override string Name => "Createuserstable";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey().AutoIncrement();
            t.Text("name").NotNull();
            t.Text("email").NotNull().Unique();
            t.Integer("age");
            t.Text("created_at").Default("CURRENT_TIMESTAMP");
        });

        builder.CreateIndex("idx_users_email", "users", new[] { "email" }, unique: true);
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropIndex("idx_users_email");
        builder.DropTable("users");
    }
}
