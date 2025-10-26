namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Specifies the type of SQL JOIN operation.
    /// </summary>
    internal enum JoinType
    {
        /// <summary>
        /// INNER JOIN - returns only matching rows from both tables.
        /// </summary>
        Inner,

        /// <summary>
        /// LEFT JOIN (LEFT OUTER JOIN) - returns all rows from the left table and matching rows from the right table.
        /// </summary>
        Left,

        /// <summary>
        /// RIGHT JOIN (RIGHT OUTER JOIN) - returns all rows from the right table and matching rows from the left table.
        /// </summary>
        Right
    }
}
