using System;
using System.Collections.Generic;
using System.Linq;
using CloudflareD1.NET.CodeFirst.Metadata;

namespace CloudflareD1.NET.CodeFirst;

/// <summary>
/// Analyzes foreign key dependencies between entity types to determine
/// the correct order for insert and delete operations.
/// </summary>
public class DependencyAnalyzer
{
    private readonly ModelMetadata _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyAnalyzer"/> class.
    /// </summary>
    /// <param name="model">The model metadata containing all entity types and their relationships</param>
    public DependencyAnalyzer(ModelMetadata model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Orders entity types for INSERT operations based on foreign key dependencies.
    /// Types with no dependencies come first, then types that depend on them, etc.
    /// </summary>
    /// <param name="entityTypes">The entity types to order</param>
    /// <returns>Ordered list of entity types (parents before children)</returns>
    /// <exception cref="InvalidOperationException">Thrown if circular dependencies are detected</exception>
    public List<Type> GetInsertOrder(IEnumerable<Type> entityTypes)
    {
        var types = entityTypes.ToList();
        var graph = BuildDependencyGraph(types);
        return TopologicalSort(graph, types);
    }

    /// <summary>
    /// Orders entity types for DELETE operations based on foreign key dependencies.
    /// Types with dependents come last (children deleted before parents).
    /// </summary>
    /// <param name="entityTypes">The entity types to order</param>
    /// <returns>Ordered list of entity types (children before parents)</returns>
    /// <exception cref="InvalidOperationException">Thrown if circular dependencies are detected</exception>
    public List<Type> GetDeleteOrder(IEnumerable<Type> entityTypes)
    {
        // Delete order is the reverse of insert order
        var insertOrder = GetInsertOrder(entityTypes);
        insertOrder.Reverse();
        return insertOrder;
    }

    /// <summary>
    /// Builds a directed graph where an edge from A to B means A depends on B (A has FK to B).
    /// </summary>
    private Dictionary<Type, List<Type>> BuildDependencyGraph(List<Type> types)
    {
        var graph = new Dictionary<Type, List<Type>>();

        // Initialize graph with all types
        foreach (var type in types)
        {
            graph[type] = new List<Type>();
        }

        // Add edges for foreign key dependencies
        foreach (var type in types)
        {
            var entityMeta = _model.GetEntity(type);
            if (entityMeta == null) continue;

            foreach (var fk in entityMeta.ForeignKeys)
            {
                // Only consider FKs where the principal is also in our set of types
                if (types.Contains(fk.PrincipalType))
                {
                    // Skip self-references for now (handle separately)
                    if (fk.DependentType != fk.PrincipalType)
                    {
                        // Edge: dependent → principal (dependent depends on principal)
                        graph[fk.DependentType].Add(fk.PrincipalType);
                    }
                }
            }
        }

        return graph;
    }

    /// <summary>
    /// Performs topological sort using Kahn's algorithm.
    /// Returns types in dependency order (dependencies first).
    /// </summary>
    private List<Type> TopologicalSort(Dictionary<Type, List<Type>> graph, List<Type> allTypes)
    {
        // Calculate in-degree (number of edges pointing to each type)
        var inDegree = new Dictionary<Type, int>();
        foreach (var type in allTypes)
        {
            inDegree[type] = 0;
        }

        // Count incoming edges: for edge A→B, B has an incoming edge
        foreach (var type in allTypes)
        {
            foreach (var dependency in graph[type])
            {
                // type → dependency: dependency has an incoming edge FROM type
                inDegree[dependency]++;
            }
        }

        // Start with types that have no incoming edges (no one depends on them, or independent)
        var queue = new Queue<Type>(allTypes.Where(t => inDegree[t] == 0));
        var result = new List<Type>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            // For each edge current→dependency, remove it by decrementing dependency's in-degree
            foreach (var dependency in graph[current])
            {
                inDegree[dependency]--;
                if (inDegree[dependency] == 0)
                {
                    queue.Enqueue(dependency);
                }
            }
        }

        // If we didn't process all types, there's a cycle
        if (result.Count != allTypes.Count)
        {
            var unprocessed = allTypes.Except(result).ToList();
            var cycleInfo = string.Join(", ", unprocessed.Select(t => t.Name));
            throw new InvalidOperationException(
                $"Circular foreign key dependency detected among entity types: {cycleInfo}. " +
                "SaveChanges cannot automatically order operations with circular dependencies.");
        }

        // Reverse to get insert order: dependencies must come before dependents
        // TopSort gives us leaves first (Product, City), but we need roots first (Category, Country)
        result.Reverse();
        return result;
    }

    /// <summary>
    /// Checks if an entity type has a self-referencing foreign key.
    /// </summary>
    public bool HasSelfReference(Type entityType)
    {
        var entityMeta = _model.GetEntity(entityType);
        if (entityMeta == null) return false;

        return entityMeta.ForeignKeys.Any(fk => fk.PrincipalType == entityType);
    }
}
