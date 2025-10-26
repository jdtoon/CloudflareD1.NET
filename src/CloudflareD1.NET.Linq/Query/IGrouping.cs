using System.Collections.Generic;

namespace CloudflareD1.NET.Linq.Query
{
    /// <summary>
    /// Represents a collection of objects that have a common key.
    /// This is D1's implementation of System.Linq.IGrouping for GroupBy() support.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the group.</typeparam>
    public interface ID1Grouping<out TKey, out TElement> : IEnumerable<TElement>
    {
        /// <summary>
        /// Gets the key of the IGrouping.
        /// </summary>
        TKey Key { get; }
    }

    /// <summary>
    /// Internal implementation of ID1Grouping.
    /// </summary>
    internal class D1Grouping<TKey, TElement> : ID1Grouping<TKey, TElement>
    {
        private readonly List<TElement> _elements;

        public D1Grouping(TKey key, IEnumerable<TElement> elements)
        {
            Key = key;
            _elements = new List<TElement>(elements);
        }

        public TKey Key { get; }

        public IEnumerator<TElement> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
