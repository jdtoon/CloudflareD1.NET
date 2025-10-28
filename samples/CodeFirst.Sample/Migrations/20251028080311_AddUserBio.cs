using CloudflareD1.NET.Migrations;

namespace YourApp.Migrations;

/// <summary>
/// Migration: Adduserbio
/// Created: 2025-10-28 08:03:11 UTC
/// Scaffolded from database schema
/// </summary>
public class Migration20251028080311_Adduserbio : Migration
{
    public override string Id => "20251028080311";
    public override string Name => "Adduserbio";

    public override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("users", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("username").NotNull();
            t.Text("email").NotNull();
            t.Text("full_name");
            t.Text("bio");
            t.Integer("is_active").NotNull();
            t.Text("created_at").NotNull();
            t.Text("blog_posts");
        });

        builder.CreateTable("blog_posts", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("title").NotNull();
            t.Text("content");
            t.Integer("author_id").NotNull();
            t.Text("published_at");
            t.Text("created_at").NotNull();
            t.Text("author");
            t.Text("comments");
            t.ForeignKey("author_id", "users", "id");
        });

        builder.CreateTable("comments", t =>
        {
            t.Integer("id").PrimaryKey();
            t.Text("content").NotNull();
            t.Integer("post_id").NotNull();
            t.Integer("author_id").NotNull();
            t.Text("created_at").NotNull();
            t.Text("blog_post");
            t.Text("author");
            t.ForeignKey("post_id", "blog_posts", "id");
            t.ForeignKey("author_id", "users", "id");
        });

    }

    public override void Down(MigrationBuilder builder)
    {
        builder.DropTable("users");

        builder.DropTable("blog_posts");

        builder.DropTable("comments");

    }
}
