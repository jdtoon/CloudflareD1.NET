using System;
using System.Linq.Expressions;
using CloudflareD1.NET.Linq.Mapping;
using CloudflareD1.NET.Linq.Query;
using FluentAssertions;
using Xunit;

namespace CloudflareD1.NET.Tests
{
    public class SqlExpressionVisitorTests
    {
        private readonly SqlExpressionVisitor _visitor;

        public SqlExpressionVisitorTests()
        {
            _visitor = new SqlExpressionVisitor(new DefaultEntityMapper());
        }

        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public decimal Balance { get; set; }
        }

        [Fact]
        public void Translate_SimpleEquality_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Age == 25;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(age = ?)");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be(25);
        }

        [Fact]
        public void Translate_GreaterThan_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Age > 18;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(age > ?)");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be(18);
        }

        [Fact]
        public void Translate_LessThanOrEqual_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Age <= 65;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(age <= ?)");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be(65);
        }

        [Fact]
        public void Translate_NotEqual_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Id != 0;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(id != ?)");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be(0);
        }

        [Fact]
        public void Translate_AndAlso_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Age > 18 && x.Age < 65;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("((age > ?) AND (age < ?))");
            parameters.Should().HaveCount(2);
            parameters[0].Should().Be(18);
            parameters[1].Should().Be(65);
        }

        [Fact]
        public void Translate_OrElse_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Age < 18 || x.Age > 65;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("((age < ?) OR (age > ?))");
            parameters.Should().HaveCount(2);
            parameters[0].Should().Be(18);
            parameters[1].Should().Be(65);
        }

        [Fact]
        public void Translate_ComplexLogic_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate =
                x => (x.Age > 18 && x.Age < 65) || x.IsActive;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(((age > ?) AND (age < ?)) OR is_active)");
            parameters.Should().HaveCount(2);
            parameters[0].Should().Be(18);
            parameters[1].Should().Be(65);
        }

        [Fact]
        public void Translate_StringContains_GeneratesLikeSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Name.Contains("John");

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("name LIKE ?");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be("%John%");
        }

        [Fact]
        public void Translate_StringStartsWith_GeneratesLikeSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Name.StartsWith("J");

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("name LIKE ?");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be("J%");
        }

        [Fact]
        public void Translate_StringEndsWith_GeneratesLikeSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Name.EndsWith("son");

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("name LIKE ?");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be("%son");
        }

        [Fact]
        public void Translate_StringToLower_GeneratesLowerSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Name.ToLower() == "john";

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(LOWER(name) = ?)");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be("john");
        }

        [Fact]
        public void Translate_Not_GeneratesNotSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => !x.IsActive;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("NOT is_active");
            parameters.Should().BeEmpty();
        }

        [Fact]
        public void Translate_CapturedVariable_UsesParameter()
        {
            // Arrange
            int minAge = 21;
            Expression<Func<TestEntity, bool>> predicate = x => x.Age >= minAge;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(age >= ?)");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be(21);
        }

        [Fact]
        public void Translate_DecimalComparison_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Balance > 1000.50m;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(balance > ?)");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be(1000.50m);
        }

        [Fact]
        public void Translate_MathOperations_GeneratesCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.Age + 5 > 30;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("((age + ?) > ?)");
            parameters.Should().HaveCount(2);
            parameters[0].Should().Be(5);
            parameters[1].Should().Be(30);
        }

        [Fact]
        public void Translate_PropertyToSnakeCase_ConvertsCorrectly()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> predicate = x => x.IsActive == true;

            // Act
            var sql = _visitor.Translate(predicate.Body);
            var parameters = _visitor.GetParameters();

            // Assert
            sql.Should().Be("(is_active = ?)");
            parameters.Should().HaveCount(1);
            parameters[0].Should().Be(true);
        }
    }
}
