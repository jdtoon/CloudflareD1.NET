namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Defines the type of set operation to perform between two queries.
    /// </summary>
    public enum SetOperationType
    {
        /// <summary>
        /// UNION - Combines results from two queries, removing duplicates.
        /// </summary>
        Union,

        /// <summary>
        /// UNION ALL - Combines results from two queries, keeping all duplicates.
        /// </summary>
        UnionAll,

        /// <summary>
        /// INTERSECT - Returns only rows that appear in both queries.
        /// </summary>
        Intersect,

        /// <summary>
        /// EXCEPT - Returns rows from the first query that don't appear in the second query.
        /// </summary>
        Except
    }
}
