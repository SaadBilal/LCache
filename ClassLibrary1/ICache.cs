
namespace ClassLibrary1
{
    public interface ICache
    {
        /// <summary>
        /// Initializes the cache instance
        /// </summary>
        void Initialize();
        /// <summary>
        /// Adds the item to the cache.
        /// </summary>
        void Add(string key, object value, int? expirationSeconds = null);
        /// <summary>
        /// Updates the item from the cache.
        /// </summary>
        void Update(string key, object value, int? expirationSeconds = null);
        /// <summary>
        /// Removes the item from the cache.
        /// </summary>
        void Remove(string key);
        /// <summary>
        /// Retrieves the item from the cache.
        /// </summary>
        object Get(string key);
        /// <summary>
        /// Clears the contents of the cache.
        /// </summary>
         void Clear();
        /// <summary>
        /// Destroys the cache.
        /// </summary>
         void Dispose();
        /// <summary>
        /// Subscribe to cache updates.
        /// </summary>
        void SubscribeToCacheUpdates(ICacheEvents cacheListner);
        /// <summary>
        /// Unsubscribe from cache updates.
        /// </summary>
        void UnsubscribeFromCacheUpdates(ICacheEvents cacheListner);
    }
}
