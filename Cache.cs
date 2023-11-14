﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private Dictionary<TKey, CacheItem<TValue>> _cache = null;
        private static readonly object _cacheLock = new object();
        private int Capacity { get; }
        /// <summary>
        /// Get the Initial capacity of the Cache (Dictionary)
        /// </summary>
        /// <param name="capacity"></param>
        public Cache(int capacity)
        {
            Capacity = capacity;
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
                        }
                        return (TValue)Convert.ChangeType("Value: " + value + " added against key: " + key, typeof(TValue));
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
                    return (TValue)Convert.ChangeType("Value already added against this key!", typeof(TValue));
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
                        increment(key);
                    }
                    return (TValue)Convert.ChangeType("Value: " + value + " updated against key: " + key, typeof(TValue));
                }
                else
                {
                    return (TValue)Convert.ChangeType("No key found!", typeof(TValue));
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
                if (_cache == null) return (TValue)Convert.ChangeType("No Cache Found!", typeof(TValue));
                if (!_cache.ContainsKey(key)) return (TValue)Convert.ChangeType("No Value Found!", typeof(TValue));

                var cached = _cache[key];
                increment(key);
                if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
                {
                    _cache.Remove(key);
                    _keyCounter.Remove(key);
                    return (TValue)Convert.ChangeType("Cache Expired!", typeof(TValue));
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
                return (TValue)Convert.ChangeType("Cache removed against key: " + key, typeof(TValue));
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
                return (TValue)Convert.ChangeType("Cache Cleared!", typeof(TValue));
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
                _cache = null;
                _keyCounter = null;
                return (TValue)Convert.ChangeType("Cache Disposed!", typeof(TValue));
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
                    return (TValue)Convert.ChangeType("Cache Initialized!", typeof(TValue));
                }
                else
                {
                    return (TValue)Convert.ChangeType("Cache already Initialized!", typeof(TValue));
                }
            }
            catch (Exception ex)
            {
                return (TValue)Convert.ChangeType(ex.Message, typeof(TValue));
            }
        }

        public TValue ExecuteEvictionPolicy() 
        {
            if ((_cache.Count >= Capacity)) 
            {
                TKey lfuKey = findLFU();
                if (lfuKey != null)
                {
                    Console.WriteLine("Cache size llimit reached: Removing Key--> " + lfuKey + " using LFU.");
                    lock (_cacheLock)
                    {
                        _cache.Remove(lfuKey);
                        _keyCounter.Remove(lfuKey);
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
        public TKey findLFU()
        {
            TKey lfuKey = default;
            int minFrequency = Int32.MaxValue;
            foreach (KeyValuePair<TKey, Frequency<TKey>> entry in _keyCounter)
            {
                if (entry.Value.frequency < minFrequency)
                {
                    minFrequency = entry.Value.frequency;
                    lfuKey = entry.Key;
                }
            }
            return lfuKey;
        }
        /// <summary>
        /// The incremental counter to update the frequencies.
        /// </summary>
        /// <param name="key"></param>
        public void increment(TKey key)
        {
            if (!_keyCounter.ContainsKey(key))
            {
                return;
            }
            _keyCounter[key].frequency += 1;
        }
    }
}