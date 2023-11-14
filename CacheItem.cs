using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheServerConcole
{
    /// <summary>
    /// Cache Item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheItem<T>
    {
        /// <summary>
        /// Cache Item generic parametrized constructor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="expiresAfter"></param>
        public CacheItem(T value, TimeSpan expiresAfter)
        {
            Value = value;
            ExpiresAfter = expiresAfter;           
        }
        
        public T Value { get; }
        internal DateTimeOffset Created { get; } = DateTimeOffset.Now;
        internal TimeSpan ExpiresAfter { get; }
    }
}
