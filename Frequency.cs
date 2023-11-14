using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheServerConcole
{
    /// <summary>
    /// Class get Cache frequency
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class Frequency<TKey>
    {
        public TKey key
        {
            get;
            set;
        }
        public int frequency
        {
            get;
            set;
        }

        /// <summary>
        /// Frequency class constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="frequency"></param>
        public Frequency(TKey key, int frequency)
        {
            this.key = key;
            this.frequency = frequency;
        }
    }
}
