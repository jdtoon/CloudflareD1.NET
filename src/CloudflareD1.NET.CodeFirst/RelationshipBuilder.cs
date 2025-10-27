using System;
using System.Linq.Expressions;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Provides a fluent API for configuring relationships
/// </summary>
public class ReferenceCollectionBuilder<TPrincipal, TDependent>
    where TPrincipal : class
    where TDependent : class
{
    private readonly ModelBuilder _modelBuilder;
    private readonly Expression<Func<TPrincipal, System.Collections.Generic.IEnumerable<TDependent>?>> _collectionExpression;

    internal ReferenceCollectionBuilder(
        ModelBuilder modelBuilder,
        Expression<Func<TPrincipal, System.Collections.Generic.IEnumerable<TDependent>?>> collectionExpression)
    {
        _modelBuilder = modelBuilder;
        _collectionExpression = collectionExpression;
    }

    /// <summary>
    /// Configures the inverse navigation property
    /// </summary>
    public ReferenceCollectionBuilder<TPrincipal, TDependent> WithOne(
        Expression<Func<TDependent, TPrincipal?>>? navigationExpression = null)
    {
        // Extract property names
        var collectionProperty = GetPropertyName(_collectionExpression);
        var referenceProperty = navigationExpression != null ? GetPropertyName(navigationExpression) : null;

        // Store relationship configuration
        _modelBuilder.AddRelationship(
            typeof(TPrincipal),
            typeof(TDependent),
            collectionProperty,
            referenceProperty,
            isRequired: false);

        return this;
    }

    /// <summary>
    /// Configures the foreign key property
    /// </summary>
    public ReferenceCollectionBuilder<TPrincipal, TDependent> HasForeignKey(
        Expression<Func<TDependent, object?>> foreignKeyExpression)
    {
        if (foreignKeyExpression == null)
            throw new ArgumentNullException(nameof(foreignKeyExpression));

        var foreignKeyProperty = GetPropertyName(foreignKeyExpression);
        _modelBuilder.SetForeignKeyProperty(typeof(TDependent), typeof(TPrincipal), foreignKeyProperty);

        return this;
    }

    /// <summary>
    /// Configures the principal key property
    /// </summary>
    public ReferenceCollectionBuilder<TPrincipal, TDependent> HasPrincipalKey(
        Expression<Func<TPrincipal, object?>> keyExpression)
    {
        if (keyExpression == null)
            throw new ArgumentNullException(nameof(keyExpression));

        var keyProperty = GetPropertyName(keyExpression);
        _modelBuilder.SetPrincipalKeyProperty(typeof(TDependent), typeof(TPrincipal), keyProperty);

        return this;
    }

    /// <summary>
    /// Configures the delete behavior
    /// </summary>
    public ReferenceCollectionBuilder<TPrincipal, TDependent> OnDelete(DeleteBehavior deleteBehavior)
    {
        _modelBuilder.SetDeleteBehavior(typeof(TDependent), typeof(TPrincipal), deleteBehavior);
        return this;
    }

    private static string GetPropertyName<T>(Expression<T> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression innerMemberExpression)
        {
            return innerMemberExpression.Member.Name;
        }

        throw new ArgumentException($"Expression '{expression}' is not a valid property expression");
    }
}

/// <summary>
/// Provides a fluent API for configuring a one-to-one or many-to-one relationship
/// </summary>
public class ReferenceReferenceBuilder<TPrincipal, TDependent>
    where TPrincipal : class
    where TDependent : class
{
    private readonly ModelBuilder _modelBuilder;
    private readonly Expression<Func<TDependent, TPrincipal?>> _referenceExpression;

    internal ReferenceReferenceBuilder(
        ModelBuilder modelBuilder,
        Expression<Func<TDependent, TPrincipal?>> referenceExpression)
    {
        _modelBuilder = modelBuilder;
        _referenceExpression = referenceExpression;
    }

    /// <summary>
    /// Configures the inverse navigation property for a one-to-one relationship
    /// </summary>
    public ReferenceReferenceBuilder<TPrincipal, TDependent> WithOne(
        Expression<Func<TPrincipal, TDependent?>>? navigationExpression = null)
    {
        var referenceProperty = GetPropertyName(_referenceExpression);
        var inverseProperty = navigationExpression != null ? GetPropertyName(navigationExpression) : null;

        _modelBuilder.AddRelationship(
            typeof(TPrincipal),
            typeof(TDependent),
            inverseProperty,
            referenceProperty,
            isRequired: false);

        return this;
    }

    /// <summary>
    /// Configures the inverse navigation property for a many-to-one relationship
    /// </summary>
    public ReferenceCollectionBuilder<TPrincipal, TDependent> WithMany(
        Expression<Func<TPrincipal, System.Collections.Generic.IEnumerable<TDependent>?>>? navigationExpression = null)
    {
        var referenceProperty = GetPropertyName(_referenceExpression);
        var collectionProperty = navigationExpression != null ? GetPropertyName(navigationExpression) : null;

        _modelBuilder.AddRelationship(
            typeof(TPrincipal),
            typeof(TDependent),
            collectionProperty,
            referenceProperty,
            isRequired: false);

        return new ReferenceCollectionBuilder<TPrincipal, TDependent>(_modelBuilder, navigationExpression!);
    }

    /// <summary>
    /// Configures the foreign key property
    /// </summary>
    public ReferenceReferenceBuilder<TPrincipal, TDependent> HasForeignKey(
        Expression<Func<TDependent, object?>> foreignKeyExpression)
    {
        if (foreignKeyExpression == null)
            throw new ArgumentNullException(nameof(foreignKeyExpression));

        var foreignKeyProperty = GetPropertyName(foreignKeyExpression);
        _modelBuilder.SetForeignKeyProperty(typeof(TDependent), typeof(TPrincipal), foreignKeyProperty);

        return this;
    }

    /// <summary>
    /// Configures the principal key property
    /// </summary>
    public ReferenceReferenceBuilder<TPrincipal, TDependent> HasPrincipalKey(
        Expression<Func<TPrincipal, object?>> keyExpression)
    {
        if (keyExpression == null)
            throw new ArgumentNullException(nameof(keyExpression));

        var keyProperty = GetPropertyName(keyExpression);
        _modelBuilder.SetPrincipalKeyProperty(typeof(TDependent), typeof(TPrincipal), keyProperty);

        return this;
    }

    /// <summary>
    /// Configures whether the relationship is required
    /// </summary>
    public ReferenceReferenceBuilder<TPrincipal, TDependent> IsRequired()
    {
        _modelBuilder.SetRelationshipRequired(typeof(TDependent), typeof(TPrincipal));
        return this;
    }

    /// <summary>
    /// Configures the delete behavior
    /// </summary>
    public ReferenceReferenceBuilder<TPrincipal, TDependent> OnDelete(DeleteBehavior deleteBehavior)
    {
        _modelBuilder.SetDeleteBehavior(typeof(TDependent), typeof(TPrincipal), deleteBehavior);
        return this;
    }

    private static string GetPropertyName<T>(Expression<T> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression innerMemberExpression)
        {
            return innerMemberExpression.Member.Name;
        }

        throw new ArgumentException($"Expression '{expression}' is not a valid property expression");
    }
}

/// <summary>
/// Specifies the delete behavior for a relationship
/// </summary>
public enum DeleteBehavior
{
    /// <summary>
    /// No action on delete (default SQLite behavior)
    /// </summary>
    NoAction,

    /// <summary>
    /// Cascade delete related entities
    /// </summary>
    Cascade,

    /// <summary>
    /// Set foreign key to NULL on delete
    /// </summary>
    SetNull,

    /// <summary>
    /// Restrict deletion if related entities exist
    /// </summary>
    Restrict
}
