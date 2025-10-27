using System;
using System.Linq;
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.CodeFirst.Converters;
using CloudflareD1.NET.Configuration;
using CloudflareD1.NET.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudflareD1.NET.CodeFirst.Tests;

public class OneToOneTests
{
    private class Person
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Passport? Passport { get; set; }
    }

    private class Passport
    {
        [Key]
        public int Id { get; set; }
        public int PersonId { get; set; }
        public Person? Person { get; set; }
    }

    private class PeopleContext : D1Context
    {
        public PeopleContext(D1Client client) : base(client) {}
        public D1Set<Person> People { get; set; } = null!;
        public D1Set<Passport> Passports { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var b = modelBuilder.Entity<Passport>();
            b.HasOne(p => p.Person)
             .WithOne(p => p.Passport)
             .HasForeignKey(p => p.PersonId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }

    [Fact]
    public void OneToOne_Adds_Unique_Index_On_FK()
    {
        var tempDb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"o2otest_{Guid.NewGuid():N}.db");
        var options = Options.Create(new D1Options { UseLocalMode = true, LocalDatabasePath = tempDb });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);
        var ctx = new PeopleContext(client);

        var model = ctx.Model;
        var passports = model.GetEntity(typeof(Passport));
        Assert.NotNull(passports);

        // Index exists and is unique on person_id
        Assert.Contains(passports!.Indexes, ix => ix.IsUnique && ix.Properties.Count == 1 && ix.Properties[0].ColumnName == "person_id");

        var schema = ModelSchemaConverter.ToDatabaseSchema(model);
        var table = schema.Tables.First(t => t.Name == "passports");
        Assert.Contains(table.Indexes, i => i.Sql != null && i.Sql.Contains("UNIQUE INDEX") && i.Sql.Contains("person_id"));

        // Scaffolder emits CreateIndex with unique flag true
        var scaffolder = new MigrationScaffolder();
        var code = scaffolder.GenerateMigration(null, schema, "Init");
        Assert.Contains("CreateIndex(\"", code); // basic sanity
        Assert.Contains("passports", code);
        Assert.Contains("person_id", code);
    }
}
