using Log4NetSample.LogUtility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CacheServerConcole.CacheService;

namespace CacheServerConcole
{
    /// <summary>
    /// Cache manger class to mange all caching operations
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class Cache<TKey, TValue>
    {
        private Dictionary<TKey, Frequency<TKey>> _keyCounter = null;
        SortedDictionary<TKey, int> sortedDict = null;
        private Dictionary<TKey, int> _keyCounterNew = null;
        private Dictionary<TKey, CacheItem<TValue>> _cache = null;
        private static readonly object _cacheLock = new object();
        private List<TKey> cacheKeysToEvict = new List<TKey>();
        private static readonly int DEFAULT_CACHE_CAPACITY = 80;
        Map<string, int> result;
        private int Capacity { get; }
        private Logger Logger { get; }
        /// <summary>
        /// Get the Initial capacity of the Cache (Dictionary)
        /// </summary>
        /// <param name="capacity"></param>
        public Cache(int capacity, Logger logger)
        {
            Capacity = capacity;
            Logger = logger;
        }
        /// <summary>
        /// To Add key value pair to Dictioanry
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresAfter"></param>
        /// <returns></returns>
        public TValue Add(TKey key, TValue value, TimeSpan expiresAfter)
        {
            try
            {
                if (!_cache.ContainsKey(key))
                {
                    /*if (!(_cache.Count == Capacity))
                    {*/
                        lock (_cacheLock)
                        {
                            _cache[key] = new CacheItem<TValue>(value, expiresAfter);
                            _keyCounter.Add(key, new Frequency<TKey>(key, 1));
                            //_keyCounterNew.Add(key, 1);
                            sortedDict.Add(key, 1);

                    }
                       // return (TValue)Convert.ChangeType("Value: " + value + " added against key: " + key, typeof(TValue));
                        return (TValue)Convert.ChangeType(CacheResponseOps.Success, typeof(TValue));
                    //}
                    /*else
                    {
                        TKey lfuKey = findLFU();
                        Console.WriteLine("Cache size llimit reached: Key--> " + lfuKey + " removed using LFU.");
                        lock (_cacheLock)
                        {
                            _cache.Remove(lfuKey);
                            _keyCounter.Remove(lfuKey);
                        }
                        return (TValue)Convert.ChangeType("Cache size limit reached!", typeof(TValue));
                    }*/
                }
                else
                {
                    return (TValue)Convert.ChangeType(CacheResponseOps.DuplicateValue, typeof(TValue));
                }
            }
            catch (Exception ex)
            {
                return (TValue)Convert.ChangeType(ex.Message, typeof(TValue));
            }
        }
        /// <summary>
        /// To updae the value against key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresAfter"></param>
        /// <returns></returns>
        public TValue Update(TKey key, TValue value, TimeSpan expiresAfter)
        {
            try
            {
                if (_cache.ContainsKey(key))
                {
                    lock (_cacheLock)
                    {
                        _cache[key] = new CacheItem<TValue>(value, expiresAfter);
                        IncrementSorted(key);
                    }
                    return (TValue)Convert.ChangeType(CacheResponseOps.Success, typeof(TValue));
                }
                else
                {
                    return (TValue)Convert.ChangeType(CacheResponseOps.NoKeyFound, typeof(TValue));
                }
            }
            catch (Exception ex)
            {
                return (TValue)Convert.ChangeType(ex.Message, typeof(TValue));
            }
        }
        /// <summary>
        /// To get value against key and return value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue Get(TKey key)
        {
            try
            {
                if (_cache == null) return (TValue)Convert.ChangeType(CacheResponseOps.NoCacheFound, typeof(TValue));
                if (!_cache.ContainsKey(key)) return (TValue)Convert.ChangeType(CacheResponseOps.NoValueFound, typeof(TValue));

                var cached = _cache[key];
                IncrementSorted(key);
                if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
                {
                    _cache.Remove(key);
                    _keyCounter.Remove(key);
                    sortedDict.Remove(key);
                    return (TValue)Convert.ChangeType(CacheResponseOps.CacheExpired, typeof(TValue));
                }
                return cached.Value;
            }
            catch (Exception ex)
            {
                return (TValue)Convert.ChangeType(ex.Message, typeof(TValue));
            }
        }
        /// <summary>
        /// To remove any value against key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue Remove(TKey key)
        {
            try
            {
                _cache.Remove(key);
                _keyCounter.Remove(key);
                sortedDict.Remove(key);
                return (TValue)Convert.ChangeType(CacheResponseOps.Success, typeof(TValue));
            }
            catch (Exception ex)
            {
                return (TValue)Convert.ChangeType(ex.Message, typeof(TValue));
            }
        }
        /// <summary>
        /// To clear the cache
        /// </summary>
        /// <returns></returns>
        public TValue Clear()
        {
            try
            {
                _cache.Clear();
                _keyCounter.Clear();
                sortedDict.Clear();
                return (TValue)Convert.ChangeType(CacheResponseOps.Success, typeof(TValue));
            }
            catch (Exception ex)
            {
                return (TValue)Convert.ChangeType(ex.Message, typeof(TValue));
            }
        }
        /// <summary>
        /// To dispose the cache
        /// </summary>
        /// <returns></returns>
        public TValue Dispose()
        {
            try
            {
                _cache.GetEnumerator().Dispose();
                _keyCounter.GetEnumerator().Dispose();
                sortedDict.GetEnumerator().Dispose();
                _cache = null;
                _keyCounter = null;
                sortedDict = null; 
                return (TValue)Convert.ChangeType(CacheResponseOps.Success, typeof(TValue));
            }
            catch (Exception ex)
            {
                return (TValue)Convert.ChangeType(ex.Message, typeof(TValue));
            }
        }
        /// <summary>
        /// To initialize the cache
        /// </summary>
        /// <returns></returns>
        public TValue InitializeCache()
        {
            try
            {
                if (_cache == null)
                {
                    _cache = new Dictionary<TKey, CacheItem<TValue>>(Capacity);
                    _keyCounter = new Dictionary<TKey, Frequency<TKey>>();
                    sortedDict = new SortedDictionary<TKey, int>();
                    return (TValue)Convert.ChangeType(CacheResponseOps.Success, typeof(TValue));
                }
                else
                {
                    return (TValue)Convert.ChangeType(CacheResponseOps.CacheAlreadyInitialized, typeof(TValue));
                }
            }
            catch (Exception ex)
            {
                return (TValue)Convert.ChangeType(ex.Message, typeof(TValue));
            }
        }
        /// <summary>
        /// To evict cache items based on LFU eviction policy
        /// </summary>
        /// <returns></returns>
        public TValue ExecuteEvictionPolicy() 
        {
            if ((_cache.Count >= Capacity)) 
            {
                List<TKey> lfuKey = FindLFU();
                if (lfuKey != null)
                {
                    Logger.Info("Cache is used upto 80%, Executing LFU eviction policy: Removing Key(s) --> " + lfuKey);
                    lock (_cacheLock)
                    {
                        foreach(TKey key in lfuKey) 
                        {
                            _cache.Remove(key);
                            _keyCounter.Remove(key);
                        }
                    }
                }
                return (TValue)Convert.ChangeType("Eviction policy executed!", typeof(TValue));
            }
            
            return default;
            
        }
        /// <summary>
        /// To find least frequently used key value form cache for data eviction based on LFU
        /// </summary>
        /// <returns></returns>
        public List<TKey> FindLFU()
        {
            try 
            {
                TKey lfuKey = default;
                Dictionary<TKey, int> dict =  GetLFEs();
                int n = (int)Math.Round((double)(dict.Count * 20) / 100);
                dict = (Dictionary<TKey, int>)dict.Take(n);
                List<TKey> keyList = new List<TKey>();
           
                int usedCapacity = GetUsedCachePercentage(_cache.Count, Capacity);
                if (usedCapacity >= GetCacheCapacityPercentage())
                {
                    foreach (KeyValuePair<TKey, int> pair in dict)
                    {
                        keyList.Add(pair.Key);
                        Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
                    }
                }
                return keyList;
            } 
            catch (Exception e) 
            {
                Logger.Error("Exception {0}", e.InnerException);
                return default;
            }
        }
        /// <summary>
        /// To calculate cache capacity used
        /// </summary>
        /// <param name="usedCapacity"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static int GetUsedCachePercentage(int usedCapacity, int capacity)
        {
            return (int)Math.Round((double)(100 * usedCapacity) / capacity);
        }
        /// <summary>
        /// The incremental counter to update the frequencies.
        /// </summary>
        /// <param name="key"></param>
        public void Increment(TKey key)
        {
            if (!_keyCounter.ContainsKey(key))
            {
                return;
            }
            _keyCounter[key].frequency += 1;
        }

        public void IncrementSorted(TKey key)
        {
            if (!sortedDict.ContainsKey(key))
            {
                return;
            }
            sortedDict[key] += 1;
        }
        /// <summary>
        /// To read cache capacity percentage from configurations
        /// </summary>
        /// <returns></returns>
        private int GetCacheCapacityPercentage()
        {
            try
            {
                var capacityPercentage = ConfigurationManager.AppSettings["cacheCapacityPercentage"];
                if (!string.IsNullOrEmpty(capacityPercentage))
                {
                    return int.Parse(capacityPercentage);
                }
                else
                {
                    Logger.Info("cache capacity percentage not found in app.config. Using default percentage.");
                    return DEFAULT_CACHE_CAPACITY;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                Logger.Error("Error reading app.config. Using default percentage.", e.InnerException);
                return DEFAULT_CACHE_CAPACITY;
            }
        }
        public Dictionary<TKey,int> GetLFEs()
        {

            var sortedDictCal = sortedDict.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair=> pair.Value);
            Console.WriteLine(sortedDictCal.Values);
          
            foreach (KeyValuePair<TKey, int> pair in sortedDictCal)
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

            return sortedDictCal;
        } 
    }
}
