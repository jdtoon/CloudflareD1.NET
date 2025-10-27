using System.Linq;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.CodeFirst.Converters;
using CloudflareD1.NET.Migrations;
using Xunit;

namespace CloudflareD1.NET.CodeFirst.Tests;

public class IndexTests
{
    [Fact]
    public void Index_Attribute_Single_Column_CreatesMetadata()
    {
        // Arrange
        var builder = new ModelBuilder();
        var model = builder.Build(typeof(TestContextWithIndexAttribute));

        // Act
        var entity = model.GetEntity(typeof(EntityWithIndexAttribute));

        // Assert
        Assert.NotNull(entity);
        Assert.Single(entity.Indexes);
        var index = entity.Indexes[0];
        Assert.Equal("Email", index.Properties[0].PropertyInfo.Name);
        Assert.False(index.IsUnique);
    }

    [Fact]
    public void Index_Attribute_Unique_CreatesUniqueIndex()
    {
        // Arrange
        var builder = new ModelBuilder();
        var model = builder.Build(typeof(TestContextWithUniqueIndexAttribute));

        // Act
        var entity = model.GetEntity(typeof(EntityWithUniqueIndexAttribute));

        // Assert
        Assert.NotNull(entity);
        Assert.Single(entity.Indexes);
        var index = entity.Indexes[0];
        Assert.True(index.IsUnique);
        Assert.Equal("Email", index.Properties[0].PropertyInfo.Name);
    }

    [Fact]
    public void Index_Attribute_Composite_CreatesCompositeIndex()
    {
        // Arrange
        var builder = new ModelBuilder();
        var model = builder.Build(typeof(TestContextWithCompositeIndexAttribute));

        // Act
        var entity = model.GetEntity(typeof(EntityWithCompositeIndexAttribute));

        // Assert
        Assert.NotNull(entity);
        Assert.Single(entity.Indexes);
        var index = entity.Indexes[0];
        Assert.Equal(2, index.Properties.Count);
        Assert.Equal("FirstName", index.Properties[0].PropertyInfo.Name);
        Assert.Equal("LastName", index.Properties[1].PropertyInfo.Name);
    }

    [Fact]
    public void Index_FluentAPI_Single_Column_CreatesMetadata()
    {
        // Arrange
        var builder = new ModelBuilder();
        builder.Entity<EntityWithIndexFluent>()
            .HasIndex(e => e.Email);
        var model = builder.Build(typeof(TestContextWithIndexFluent));

        // Act
        var entity = model.GetEntity(typeof(EntityWithIndexFluent));

        // Assert
        Assert.NotNull(entity);
        Assert.Single(entity.Indexes);
        var index = entity.Indexes[0];
        Assert.Equal("Email", index.Properties[0].PropertyInfo.Name);
        Assert.False(index.IsUnique);
        Assert.StartsWith("ix_", index.Name);
    }

    [Fact]
    public void Index_FluentAPI_Unique_CreatesUniqueIndex()
    {
        // Arrange
        var builder = new ModelBuilder();
        builder.Entity<EntityWithIndexFluent>()
            .HasIndex(e => e.Email)
            .IsUnique();
        var model = builder.Build(typeof(TestContextWithIndexFluent));

        // Act
        var entity = model.GetEntity(typeof(EntityWithIndexFluent));

        // Assert
        Assert.NotNull(entity);
        Assert.Single(entity.Indexes);
        var index = entity.Indexes[0];
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Index_FluentAPI_HasName_SetsCustomName()
    {
        // Arrange
        var builder = new ModelBuilder();
        builder.Entity<EntityWithIndexFluent>()
            .HasIndex(e => e.Email)
            .HasName("idx_custom_email");
        var model = builder.Build(typeof(TestContextWithIndexFluent));

        // Act
        var entity = model.GetEntity(typeof(EntityWithIndexFluent));

        // Assert
        Assert.NotNull(entity);
        Assert.Single(entity.Indexes);
        var index = entity.Indexes[0];
        Assert.Equal("idx_custom_email", index.Name);
    }

    [Fact]
    public void Index_FluentAPI_Composite_CreatesCompositeIndex()
    {
        // Arrange
        var builder = new ModelBuilder();
        builder.Entity<EntityWithIndexFluent>()
            .HasIndex(e => e.FirstName, e => e.LastName);
        var model = builder.Build(typeof(TestContextWithIndexFluent));

        // Act
        var entity = model.GetEntity(typeof(EntityWithIndexFluent));

        // Assert
        Assert.NotNull(entity);
        Assert.Single(entity.Indexes);
        var index = entity.Indexes[0];
        Assert.Equal(2, index.Properties.Count);
        Assert.Equal("FirstName", index.Properties[0].PropertyInfo.Name);
        Assert.Equal("LastName", index.Properties[1].PropertyInfo.Name);
    }

    [Fact]
    public void Index_Converter_GeneratesCreateIndexSQL()
    {
        // Arrange
        var builder = new ModelBuilder();
        builder.Entity<EntityWithIndexFluent>()
            .HasIndex(e => e.Email)
            .IsUnique()
            .HasName("idx_email");
        var model = builder.Build(typeof(TestContextWithIndexFluent));

        // Act
        var schema = ModelSchemaConverter.ToDatabaseSchema(model);

        // Assert
        var table = schema.Tables.FirstOrDefault();
        Assert.NotNull(table);
        Assert.Single(table.Indexes);
        var index = table.Indexes[0];
        Assert.Equal("idx_email", index.Name);
        Assert.Contains("CREATE UNIQUE INDEX idx_email", index.Sql);
        Assert.Contains("ON entity_with_index_fluents", index.Sql);
        Assert.Contains("(email)", index.Sql);
    }

    [Fact]
    public void Index_Scaffolder_GeneratesCreateIndexStatement()
    {
        // Arrange
        var builder = new ModelBuilder();
        builder.Entity<EntityWithIndexFluent>()
            .ToTable("products")
            .HasIndex(e => e.Email)
            .IsUnique()
            .HasName("idx_product_email");
        var model = builder.Build(typeof(TestContextWithIndexFluent));
        var schema = ModelSchemaConverter.ToDatabaseSchema(model);

        var scaffolder = new MigrationScaffolder();

        // Act
        var code = scaffolder.GenerateMigration(null, schema, "TestMigration");

        // Assert
        Assert.Contains("CreateUniqueIndex(\"products\", \"idx_product_email\"", code);
    }

    // Test entities
    [Index(nameof(Email))]
    public class EntityWithIndexAttribute
    {
        [Key] public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    [Index(nameof(Email), IsUnique = true)]
    public class EntityWithUniqueIndexAttribute
    {
        [Key] public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    [Index(nameof(FirstName), nameof(LastName))]
    public class EntityWithCompositeIndexAttribute
    {
        [Key] public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class EntityWithIndexFluent
    {
        [Key] public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public class TestContextWithIndexAttribute : D1Context
    {
        public TestContextWithIndexAttribute(D1Client client) : base(client) { }
        public D1Set<EntityWithIndexAttribute> Entities { get; set; } = null!;
    }

    public class TestContextWithUniqueIndexAttribute : D1Context
    {
        public TestContextWithUniqueIndexAttribute(D1Client client) : base(client) { }
        public D1Set<EntityWithUniqueIndexAttribute> Entities { get; set; } = null!;
    }

    public class TestContextWithCompositeIndexAttribute : D1Context
    {
        public TestContextWithCompositeIndexAttribute(D1Client client) : base(client) { }
        public D1Set<EntityWithCompositeIndexAttribute> Entities { get; set; } = null!;
    }

    public class TestContextWithIndexFluent : D1Context
    {
        public TestContextWithIndexFluent(D1Client client) : base(client) { }
        public D1Set<EntityWithIndexFluent> Entities { get; set; } = null!;
    }
}
