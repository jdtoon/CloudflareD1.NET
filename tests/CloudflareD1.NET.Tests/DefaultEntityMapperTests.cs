using System;
using System.Collections.Generic;
using System.Text.Json;
using CloudflareD1.NET.Linq.Mapping;
using FluentAssertions;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    public class DefaultEntityMapperTests
    {
        private readonly DefaultEntityMapper _mapper;

        public DefaultEntityMapperTests()
        {
            _mapper = new DefaultEntityMapper();
        }

        // Test entity classes
        public class SimpleEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        public class SnakeCaseEntity
        {
            public int UserId { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class TypeConversionEntity
        {
            public int IntValue { get; set; }
            public long LongValue { get; set; }
            public double DoubleValue { get; set; }
            public decimal DecimalValue { get; set; }
            public bool BoolValue { get; set; }
            public DateTime DateTimeValue { get; set; }
            public Guid GuidValue { get; set; }
        }

        public class NullableEntity
        {
            public int? NullableInt { get; set; }
            public double? NullableDouble { get; set; }
            public DateTime? NullableDateTime { get; set; }
            public string? NullableString { get; set; }
        }

        public enum Status
        {
            Active,
            Inactive,
            Pending
        }

        public class EnumEntity
        {
            public int Id { get; set; }
            public Status Status { get; set; }
        }

        [Fact]
        public void Map_SimpleEntity_MapsCorrectly()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "id", 1L },
                { "name", "Test" }
            };

            // Act
            var entity = _mapper.Map<SimpleEntity>(row);

            // Assert
            entity.Should().NotBeNull();
            entity.Id.Should().Be(1);
            entity.Name.Should().Be("Test");
        }

        [Fact]
        public void Map_SnakeCaseColumns_MapsToPascalCaseProperties()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "user_id", 42L },
                { "first_name", "John" },
                { "last_name", "Doe" },
                { "created_at", "2024-01-15T10:30:00Z" }
            };

            // Act
            var entity = _mapper.Map<SnakeCaseEntity>(row);

            // Assert
            entity.Should().NotBeNull();
            entity.UserId.Should().Be(42);
            entity.FirstName.Should().Be("John");
            entity.LastName.Should().Be("Doe");
            entity.CreatedAt.Should().BeCloseTo(DateTime.Parse("2024-01-15T10:30:00Z"), TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Map_TypeConversions_ConvertsCorrectly()
        {
            // Arrange
            var guidString = Guid.NewGuid().ToString();
            var row = new Dictionary<string, object?>
            {
                { "IntValue", 123L },
                { "LongValue", 9876543210L },
                { "DoubleValue", 123.45 },
                { "DecimalValue", 678.90 },
                { "BoolValue", 1L }, // SQLite stores bool as 0/1
                { "DateTimeValue", "2024-01-15T12:00:00Z" },
                { "GuidValue", guidString }
            };

            // Act
            var entity = _mapper.Map<TypeConversionEntity>(row);

            // Assert
            entity.IntValue.Should().Be(123);
            entity.LongValue.Should().Be(9876543210L);
            entity.DoubleValue.Should().BeApproximately(123.45, 0.01);
            entity.DecimalValue.Should().Be(678.90m);
            entity.BoolValue.Should().BeTrue();
            entity.DateTimeValue.Should().BeCloseTo(DateTime.Parse("2024-01-15T12:00:00Z"), TimeSpan.FromSeconds(1));
            entity.GuidValue.Should().Be(Guid.Parse(guidString));
        }

        [Fact]
        public void Map_BooleanFromZero_MapsFalse()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "IntValue", 0L },
                { "LongValue", 0L },
                { "DoubleValue", 0.0 },
                { "DecimalValue", 0.0 },
                { "BoolValue", 0L },
                { "DateTimeValue", "2024-01-15T12:00:00Z" },
                { "GuidValue", Guid.Empty.ToString() }
            };

            // Act
            var entity = _mapper.Map<TypeConversionEntity>(row);

            // Assert
            entity.BoolValue.Should().BeFalse();
        }

        [Fact]
        public void Map_NullableProperties_HandlesNullCorrectly()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "NullableInt", null },
                { "NullableDouble", null },
                { "NullableDateTime", null },
                { "NullableString", null }
            };

            // Act
            var entity = _mapper.Map<NullableEntity>(row);

            // Assert
            entity.Should().NotBeNull();
            entity.NullableInt.Should().BeNull();
            entity.NullableDouble.Should().BeNull();
            entity.NullableDateTime.Should().BeNull();
            entity.NullableString.Should().BeNull();
        }

        [Fact]
        public void Map_NullableProperties_HandlesDBNullCorrectly()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "NullableInt", DBNull.Value },
                { "NullableDouble", DBNull.Value },
                { "NullableDateTime", DBNull.Value },
                { "NullableString", DBNull.Value }
            };

            // Act
            var entity = _mapper.Map<NullableEntity>(row);

            // Assert
            entity.Should().NotBeNull();
            entity.NullableInt.Should().BeNull();
            entity.NullableDouble.Should().BeNull();
            entity.NullableDateTime.Should().BeNull();
            entity.NullableString.Should().BeNull();
        }

        [Fact]
        public void Map_NullablePropertiesWithValues_MapsCorrectly()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "NullableInt", 42L },
                { "NullableDouble", 3.14 },
                { "NullableDateTime", "2024-01-15T12:00:00Z" },
                { "NullableString", "test" }
            };

            // Act
            var entity = _mapper.Map<NullableEntity>(row);

            // Assert
            entity.Should().NotBeNull();
            entity.NullableInt.Should().Be(42);
            entity.NullableDouble.Should().BeApproximately(3.14, 0.01);
            entity.NullableDateTime.Should().NotBeNull();
            entity.NullableString.Should().Be("test");
        }

        [Fact]
        public void Map_EnumFromString_MapsCorrectly()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "Id", 1L },
                { "Status", "Active" }
            };

            // Act
            var entity = _mapper.Map<EnumEntity>(row);

            // Assert
            entity.Id.Should().Be(1);
            entity.Status.Should().Be(Status.Active);
        }

        [Fact]
        public void Map_CaseInsensitiveEnumFromString_MapsCorrectly()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "Id", 1L },
                { "Status", "inactive" }
            };

            // Act
            var entity = _mapper.Map<EnumEntity>(row);

            // Assert
            entity.Status.Should().Be(Status.Inactive);
        }

        [Fact]
        public void Map_JsonElement_String_MapsCorrectly()
        {
            // Arrange
            var jsonDoc = JsonDocument.Parse("\"test value\"");
            var row = new Dictionary<string, object?>
            {
                { "id", 1L },
                { "name", jsonDoc.RootElement }
            };

            // Act
            var entity = _mapper.Map<SimpleEntity>(row);

            // Assert
            entity.Name.Should().Be("test value");
        }

        [Fact]
        public void Map_JsonElement_Number_MapsCorrectly()
        {
            // Arrange
            var jsonDoc = JsonDocument.Parse("123");
            var row = new Dictionary<string, object?>
            {
                { "id", jsonDoc.RootElement },
                { "name", "test" }
            };

            // Act
            var entity = _mapper.Map<SimpleEntity>(row);

            // Assert
            entity.Id.Should().Be(123);
        }

        [Fact]
        public void Map_JsonElement_Boolean_MapsCorrectly()
        {
            // Arrange
            var jsonDocTrue = JsonDocument.Parse("true");
            var jsonDocFalse = JsonDocument.Parse("false");

            var row1 = new Dictionary<string, object?>
            {
                { "IntValue", 0L },
                { "LongValue", 0L },
                { "DoubleValue", 0.0 },
                { "DecimalValue", 0.0 },
                { "BoolValue", jsonDocTrue.RootElement },
                { "DateTimeValue", "2024-01-15T12:00:00Z" },
                { "GuidValue", Guid.Empty.ToString() }
            };

            var row2 = new Dictionary<string, object?>
            {
                { "IntValue", 0L },
                { "LongValue", 0L },
                { "DoubleValue", 0.0 },
                { "DecimalValue", 0.0 },
                { "BoolValue", jsonDocFalse.RootElement },
                { "DateTimeValue", "2024-01-15T12:00:00Z" },
                { "GuidValue", Guid.Empty.ToString() }
            };

            // Act
            var entity1 = _mapper.Map<TypeConversionEntity>(row1);
            var entity2 = _mapper.Map<TypeConversionEntity>(row2);

            // Assert
            entity1.BoolValue.Should().BeTrue();
            entity2.BoolValue.Should().BeFalse();
        }

        [Fact]
        public void Map_ExtraColumnsInRow_IgnoresUnmappedColumns()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "id", 1L },
                { "name", "Test" },
                { "extra_column", "ignored" },
                { "another_unmapped", 123 }
            };

            // Act
            var entity = _mapper.Map<SimpleEntity>(row);

            // Assert
            entity.Id.Should().Be(1);
            entity.Name.Should().Be("Test");
        }

        [Fact]
        public void Map_MissingColumnsInRow_LeavesDefaultValues()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "id", 1L }
                // name is missing
            };

            // Act
            var entity = _mapper.Map<SimpleEntity>(row);

            // Assert
            entity.Id.Should().Be(1);
            entity.Name.Should().BeNull();
        }

        [Fact]
        public void MapMany_MultipleRows_MapsAllCorrectly()
        {
            // Arrange
            var rows = new List<Dictionary<string, object?>>
            {
                new Dictionary<string, object?> { { "id", 1L }, { "name", "First" } },
                new Dictionary<string, object?> { { "id", 2L }, { "name", "Second" } },
                new Dictionary<string, object?> { { "id", 3L }, { "name", "Third" } }
            };

            // Act
            var entities = _mapper.MapMany<SimpleEntity>(rows);

            // Assert
            var entitiesList = new List<SimpleEntity>(entities);
            entitiesList.Should().HaveCount(3);
            entitiesList[0].Id.Should().Be(1);
            entitiesList[0].Name.Should().Be("First");
            entitiesList[1].Id.Should().Be(2);
            entitiesList[1].Name.Should().Be("Second");
            entitiesList[2].Id.Should().Be(3);
            entitiesList[2].Name.Should().Be("Third");
        }

        [Fact]
        public void MapMany_EmptyRows_ReturnsEmptyCollection()
        {
            // Arrange
            var rows = new List<Dictionary<string, object?>>();

            // Act
            var entities = _mapper.MapMany<SimpleEntity>(rows);

            // Assert
            entities.Should().BeEmpty();
        }

        [Fact]
        public void Map_InvalidTypeConversion_ThrowsInvalidOperationException()
        {
            // Arrange
            var row = new Dictionary<string, object?>
            {
                { "id", "not_a_number" }, // Invalid conversion
                { "name", "Test" }
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _mapper.Map<SimpleEntity>(row));
        }
    }
}
