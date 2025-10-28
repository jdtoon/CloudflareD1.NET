using System;
using System.Collections.Generic;
using System.Linq;
using CloudflareD1.NET;
using CloudflareD1.NET.CodeFirst;
using CloudflareD1.NET.CodeFirst.Attributes;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudflareD1.NET.CodeFirst.Tests;

public class DependencyAnalyzerTests
{
    // Simple parent-child models
    private class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }
    }

    // Multi-level hierarchy: Country → State → City
    private class Country
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class State
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [ForeignKey(nameof(Country))]
        public int CountryId { get; set; }

        public Country? Country { get; set; }
    }

    private class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [ForeignKey(nameof(State))]
        public int StateId { get; set; }

        public State? State { get; set; }
    }

    // Self-referencing model
    private class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [ForeignKey(nameof(Manager))]
        public int? ManagerId { get; set; }

        public Employee? Manager { get; set; }
    }

    // Circular dependency models
    private class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [ForeignKey(nameof(FavoriteBook))]
        public int? FavoriteBookId { get; set; }

        public Book? FavoriteBook { get; set; }
    }

    private class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        [ForeignKey(nameof(Author))]
        public int AuthorId { get; set; }

        public Author? Author { get; set; }
    }

    // Independent entities (no FKs)
    private class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Setting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    // Context for simple parent-child
    private class SimpleContext : D1Context
    {
        public SimpleContext(D1Client client) : base(client) { }
        public D1Set<Category> Categories { get; set; } = null!;
        public D1Set<Product> Products { get; set; } = null!;
    }

    // Context for multi-level hierarchy
    private class HierarchyContext : D1Context
    {
        public HierarchyContext(D1Client client) : base(client) { }
        public D1Set<Country> Countries { get; set; } = null!;
        public D1Set<State> States { get; set; } = null!;
        public D1Set<City> Cities { get; set; } = null!;
    }

    // Context for self-referencing
    private class SelfRefContext : D1Context
    {
        public SelfRefContext(D1Client client) : base(client) { }
        public D1Set<Employee> Employees { get; set; } = null!;
    }

    // Context for circular dependency
    private class CircularContext : D1Context
    {
        public CircularContext(D1Client client) : base(client) { }
        public D1Set<Author> Authors { get; set; } = null!;
        public D1Set<Book> Books { get; set; } = null!;
    }

    // Context for independent entities
    private class IndependentContext : D1Context
    {
        public IndependentContext(D1Client client) : base(client) { }
        public D1Set<Tag> Tags { get; set; } = null!;
        public D1Set<Setting> Settings { get; set; } = null!;
    }

    private D1Client CreateTestClient()
    {
        var options = Options.Create(new D1Options { UseLocalMode = true, LocalDatabasePath = ":memory:" });
        return new D1Client(options, NullLogger<D1Client>.Instance);
    }

    [Fact]
    public void GetInsertOrder_SimpleParentChild_OrdersParentFirst()
    {
        var client = CreateTestClient();
        var context = new SimpleContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        var types = new[] { typeof(Product), typeof(Category) };
        var orderedTypes = analyzer.GetInsertOrder(types);

        Assert.Equal(2, orderedTypes.Count);
        Assert.Equal(typeof(Category), orderedTypes[0]); // Parent first
        Assert.Equal(typeof(Product), orderedTypes[1]);  // Child second
    }

    [Fact]
    public void GetDeleteOrder_SimpleParentChild_OrdersChildFirst()
    {
        var client = CreateTestClient();
        var context = new SimpleContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        var types = new[] { typeof(Product), typeof(Category) };
        var orderedTypes = analyzer.GetDeleteOrder(types);

        Assert.Equal(2, orderedTypes.Count);
        Assert.Equal(typeof(Product), orderedTypes[0]);  // Child first
        Assert.Equal(typeof(Category), orderedTypes[1]); // Parent second
    }

    [Fact]
    public void GetInsertOrder_MultiLevelHierarchy_OrdersCorrectly()
    {
        var client = CreateTestClient();
        var context = new HierarchyContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        var types = new[] { typeof(City), typeof(State), typeof(Country) };
        var orderedTypes = analyzer.GetInsertOrder(types);

        Assert.Equal(3, orderedTypes.Count);
        Assert.Equal(typeof(Country), orderedTypes[0]); // Top level
        Assert.Equal(typeof(State), orderedTypes[1]);   // Middle
        Assert.Equal(typeof(City), orderedTypes[2]);    // Bottom
    }

    [Fact]
    public void GetDeleteOrder_MultiLevelHierarchy_OrdersCorrectly()
    {
        var client = CreateTestClient();
        var context = new HierarchyContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        var types = new[] { typeof(City), typeof(State), typeof(Country) };
        var orderedTypes = analyzer.GetDeleteOrder(types);

        Assert.Equal(3, orderedTypes.Count);
        Assert.Equal(typeof(City), orderedTypes[0]);    // Bottom first
        Assert.Equal(typeof(State), orderedTypes[1]);   // Middle
        Assert.Equal(typeof(Country), orderedTypes[2]); // Top last
    }

    [Fact]
    public void GetInsertOrder_IndependentEntities_AnyOrderIsValid()
    {
        var client = CreateTestClient();
        var context = new IndependentContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        var types = new[] { typeof(Tag), typeof(Setting) };
        var orderedTypes = analyzer.GetInsertOrder(types);

        // Both types should be included
        Assert.Equal(2, orderedTypes.Count);
        Assert.Contains(typeof(Tag), orderedTypes);
        Assert.Contains(typeof(Setting), orderedTypes);
        // Order doesn't matter since no dependencies, just verify no exception
    }

    [Fact]
    public void HasSelfReference_SelfReferencingEntity_ReturnsTrue()
    {
        var client = CreateTestClient();
        var context = new SelfRefContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        var hasSelfRef = analyzer.HasSelfReference(typeof(Employee));

        Assert.True(hasSelfRef);
    }

    [Fact]
    public void HasSelfReference_NonSelfReferencingEntity_ReturnsFalse()
    {
        var client = CreateTestClient();
        var context = new SimpleContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        var hasSelfRef = analyzer.HasSelfReference(typeof(Product));

        Assert.False(hasSelfRef);
    }

    [Fact]
    public void GetInsertOrder_CircularDependency_ThrowsException()
    {
        var client = CreateTestClient();
        var context = new CircularContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        var types = new[] { typeof(Author), typeof(Book) };

        var ex = Assert.Throws<InvalidOperationException>(() => analyzer.GetInsertOrder(types));
        Assert.Contains("Circular foreign key dependency", ex.Message);
        Assert.Contains("Author, Book", ex.Message);
    }

    [Fact]
    public void GetInsertOrder_MixedDependentAndIndependent_OrdersCorrectly()
    {
        var client = CreateTestClient();
        var context = new SimpleContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        // Mix: Category (independent), Product (depends on Category)
        var types = new[] { typeof(Product), typeof(Category) };
        var orderedTypes = analyzer.GetInsertOrder(types);

        Assert.Equal(typeof(Category), orderedTypes[0]);
        Assert.Equal(typeof(Product), orderedTypes[1]);
    }

    [Fact]
    public void GetInsertOrder_AlreadyInCorrectOrder_PreservesOrder()
    {
        var client = CreateTestClient();
        var context = new HierarchyContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        // Already in correct dependency order
        var types = new[] { typeof(Country), typeof(State), typeof(City) };
        var orderedTypes = analyzer.GetInsertOrder(types);

        Assert.Equal(typeof(Country), orderedTypes[0]);
        Assert.Equal(typeof(State), orderedTypes[1]);
        Assert.Equal(typeof(City), orderedTypes[2]);
    }

    [Fact]
    public void GetInsertOrder_ReverseOrder_CorrectlyReorders()
    {
        var client = CreateTestClient();
        var context = new HierarchyContext(client);
        var analyzer = new DependencyAnalyzer(context.Model);

        // In reverse order (should be reordered)
        var types = new[] { typeof(City), typeof(State), typeof(Country) };
        var orderedTypes = analyzer.GetInsertOrder(types);

        Assert.Equal(typeof(Country), orderedTypes[0]);
        Assert.Equal(typeof(State), orderedTypes[1]);
        Assert.Equal(typeof(City), orderedTypes[2]);
    }
}
