using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public interface ICacheEvents
    {
        /// <summary>
        /// Item added to cache
        /// </summary>
        void OnItemAdded(string key);
        /// <summary>
        /// Item updated in cache
        /// </summary>
        void OnItemUpdated(string key);
        /// <summary>
        /// Item removed from cache
        /// </summary>
        void OnItemRemoved(string key);
    }
}
