using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: CreatePostsTable
/// Created: 2025-10-27 09:14:00 UTC
/// </summary>
public class Migration20251027091400_CreatePostsTable : Migration
{
    public override string Id => "20251027091400";
    public override string Name => "CreatePostsTable";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("posts", t =>
        {
            t.Integer("id").PrimaryKey().AutoIncrement();
            t.Text("title").NotNull();
            t.Text("content").NotNull();
            t.Integer("user_id").NotNull();
            t.Text("status").Default("draft");
            t.Integer("view_count").Default(0);
            t.Text("created_at").Default("CURRENT_TIMESTAMP");
            t.ForeignKey("user_id", "users", "id", onDelete: "CASCADE");
        });

        builder.CreateIndex("idx_posts_user_id", "posts", new[] { "user_id" });
        builder.CreateIndex("idx_posts_status", "posts", new[] { "status" });
    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropIndex("idx_posts_status");
        builder.DropIndex("idx_posts_user_id");
        builder.DropTable("posts");
    }
}
