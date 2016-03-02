using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CacheHelper {
    #region ----- CacheDictionary -----
    /// <summary>
    /// Defines a caching table whereby items are configured to
    /// expire and thereby be removed automatically. Default "refresh time" is 30 sec.
    /// This collection is thread-safe and uses manual lock for internal Dictionary collection.
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="T">Value</typeparam>
    public class CacheDictionary<K, T> : IDictionary<K, T>, IDisposable {
        public event EventHandler<CacheItemRemovedEventArgs<K, T>> ItemExpired;

        protected internal Dictionary<K, CacheItem<T>> _innerDictionary;
        protected internal object lockObject = new object();

        #region --- Constructors ---
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public CacheDictionary() {
            DefaultTimeToLive = new CacheItem<T>().TimeToLive;
            _innerDictionary = new Dictionary<K, CacheItem<T>>();
            var ts = AutoClearExpiredItemsFrequency;
            this.Timer = new Timer(e => this.ClearExpiredItems(), null, ts, ts);
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="dictionary">A dictionary of values to pre-load into the cache.</param>
        public CacheDictionary(IEnumerable<KeyValuePair<K, T>> dictionary) : this() {
            foreach (var kvp in dictionary) {
                _innerDictionary.Add(kvp.Key, new CacheItem<T>(kvp.Value));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpirableItemDictionary&lt;K, T&gt;"/> class.
        /// </summary>
        /// <param name="dictionary">A dictionary of values to pre-load into the cache.</param>
        /// <param name="comparer">A comparer implementation. For example,
        /// for case-insensitive keys, use <see cref="StringComparer.InvariantCulture"/>.</param>
        public CacheDictionary(IEnumerable<KeyValuePair<K, T>> dictionary, IEqualityComparer<K> comparer) : this(comparer) {
            foreach (var kvp in dictionary) {
                _innerDictionary.Add(kvp.Key, new CacheItem<T>(kvp.Value));
            }
        }

        /// <summary>
        /// Initializes a new instance of this type using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">A comparer implementation. For example,
        /// for case-insensitive keys, use <see cref="StringComparer.InvariantCulture"/>.</param>
        public CacheDictionary(IEqualityComparer<K> comparer) {
            _innerDictionary = new Dictionary<K, CacheItem<T>>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpirableItemDictionary&lt;K, T&gt;"/> class
        /// using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">A comparer implementation. For example,
        /// for case-insensitive keys, use <see cref="StringComparer.InvariantCulture"/>.</param>
        /// <param name="defaultTimeToLive">The default time-to-live.</param>
        public CacheDictionary(IEqualityComparer<K> comparer, TimeSpan defaultTimeToLive) : this(comparer) {
            this.DefaultTimeToLive = defaultTimeToLive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpirableItemDictionary&lt;K, T&gt;"/> class
        /// using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="defaultTimeToLive">The default time-to-live.</param>
        public CacheDictionary(TimeSpan defaultTimeToLive) : this() {
            DefaultTimeToLive = defaultTimeToLive;
        }
        #endregion --- Constructors ---

        #region --- Properties ---
        protected Timer Timer { get; set; }

        private TimeSpan _ts = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the frequency at which expired items are automatically cleared.
        /// </summary>
        /// <value>The auto clear expired items frequency.</value>
        public TimeSpan AutoClearExpiredItemsFrequency {
            get { return _ts; }
            set {
                _ts = value;
                Timer.Change(new TimeSpan(0), value);
            }
        }

        /// <summary>
        /// Gets or sets the default time-to-live.
        /// </summary>
        /// <value>The default time to live.</value>
        public TimeSpan DefaultTimeToLive { get; set; }

        /// <summary>
        /// Sets the <typeparamref name="T"/> value with the specified key and time-to-live.
        /// </summary>
        /// <value></value>
        public T this[K key, TimeSpan timeToLive] {
            set {
                _innerDictionary[key] = new CacheItem<T>(value, timeToLive);
            }
        }

        /// <summary>
        /// Sets the <typeparamref name="T"/> value with the specified key an explicit expiration date/time.
        /// </summary>
        /// <value></value>
        public T this[K key, DateTime expires] {
            set {
                _innerDictionary[key] = new CacheItem<T>(value, expires);
            }
        }
        #endregion --- Properties ---

        #region -- Add --
        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="timeToLive">The time-to-live.</param>
        public void Add(K key, T value, TimeSpan timeToLive) {
            lock (lockObject) {
                if (!ContainsKey(key))
                    _innerDictionary.Add(key, new CacheItem<T>(value, timeToLive));
            } // lock
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires">The explicit date/time to expire the added item.</param>
        public void Add(K key, T value, DateTime expires) {
            lock (lockObject) {
                if (!ContainsKey(key))
                    _innerDictionary.Add(key, new CacheItem<T>(value, expires));
            } // lock
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(KeyValuePair<K, CacheItem<T>> item) {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(K key, CacheItem<T> value) {
            lock (lockObject) {
                if (!ContainsKey(key))
                    _innerDictionary.Add(key, value);
            } // lock
        }
        #endregion -- Add --

        /// <summary>
        /// Manual invocation clears all items that have expired.
        /// This method is not required for expiration but can be
        /// used for tuning application performance and memory,
        /// somewhat similar to GC.Collect(). 
        /// </summary>
        public void ClearExpiredItems() {
            lock (lockObject) {
                List<KeyValuePair<K, CacheItem<T>>> removeList
                    = _innerDictionary.Where(kvp => kvp.Value.HasExpired).ToList();

                removeList.ForEach(kvp => {
                    if (ItemExpired != null)
                        ItemExpired(this, new CacheItemRemovedEventArgs<K, T> {
                            Key = kvp.Key,
                            Value = kvp.Value.Value
                        });

                    _innerDictionary.Remove(kvp.Key);

                });
            }
        }

        /// <summary>
        /// Resets the timestamp for the specified item
        /// if the item exists in the dictionary,
        /// and then cleans out expired items.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="resetTimestamp"></param>
        public void Update(K key, DateTime resetTimestamp) {
            lock (lockObject) {
                if (ContainsKey(key)) 
                    _innerDictionary[key].TimeStamp = resetTimestamp;
                //ClearExpiredItems();
            }
        }

        /// <summary>
        /// Gets the item with the specified key and updates its timestamp
        /// to reset its expiration timespan, and the value will be returned.
        /// If the item does not exist, an instance of <typeparamref name="T"/> 
        /// will be instantiated and added to the dictionary with the specified 
        /// key using the <see cref="DefaultTimeToLive"/>, and that value will 
        /// be returned instead.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public T GetWithUpdateOrCreate(K key) {
            lock (lockObject) {
                T retval;
                if (!ContainsKey(key)) {
                    this[key] = retval = (T)Activator.CreateInstance(typeof(T));
                }
                else 
                    retval = this[key];
                _innerDictionary[key].TimeStamp = DateTime.Now;
                return retval;
            }
        }

        #region --- IDictionary ---
        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(K key, T value) {
            lock (lockObject) {
                if (!ContainsKey(key))
                    _innerDictionary.Add(key, new CacheItem<T>(value, DefaultTimeToLive));
            } // lock
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(KeyValuePair<K, T> item) {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// 	<c>true</c> if the dictionary contains the specified key and the item has not expired; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>This method will auto-clear expired items.</remarks>
        public bool ContainsKey(K key) {
            lock (lockObject) {
                if (_innerDictionary.ContainsKey(key)) {
                    if (_innerDictionary[key].HasExpired) {
                        if (ItemExpired != null)
                            ItemExpired(this, new CacheItemRemovedEventArgs<K, T> {
                                Key = key,
                                Value = _innerDictionary[key].Value
                            });
                        _innerDictionary.Remove(key);
                        return false;
                    }
                    return true;
                }
                return false;
            } // lock
        }

        /// <summary>
        /// Gets the keys of the collection for all items that have not yet expired.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<K> Keys {
            get {
                lock (lockObject) {
                    //ClearExpiredItems();
                    return _innerDictionary.Keys;
                } // lock
            }
        }

        /// <summary>
        /// Removes the item having the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public bool Remove(K key) {
            lock (lockObject) {
                if (ContainsKey(key)) {
                    _innerDictionary.Remove(key);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Tries to the get item having the specified key. Returns <c>true</c> if
        /// the item exists and has not expired.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool TryGetValue(K key, out T value) {
            lock (lockObject) {
                if (ContainsKey(key)) {
                    value = _innerDictionary[key].Value;
                    return true;
                }
                value = default(T);
                return false;
            } // lock
        }

        /// <summary>
        /// Gets all of the values in the dictionary, without any key mappings.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<T> Values {
            get { return this.Cast<T>().ToList(); }
        }

        /// <summary>
        /// Gets or sets the <typeparamref name="T"/> value with the specified key.
        /// </summary>
        /// <value></value>
        public T this[K key] {
            get {
                lock (lockObject) {
                    if (ContainsKey(key)) return _innerDictionary[key].Value;
                    return default(T);
                }
            }
            set {
                lock (lockObject) {
                    _innerDictionary[key] = new CacheItem<T>(value, DefaultTimeToLive);
                }
            }
        }

        /// <summary>
        /// Removes all items from the internal dictionary.
        /// </summary>
        public void Clear() {
            lock (lockObject) {
                _innerDictionary.Clear();
            }
        }

        bool ICollection<KeyValuePair<K, T>>.Contains(KeyValuePair<K, T> item) {
            lock (lockObject) {
                return ContainsKey(item.Key) &&
                       (object)_innerDictionary[item.Key].Value == (object)item.Value;
            }
        }

        public void CopyTo(KeyValuePair<K, T>[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the number of non-expired cache items.
        /// </summary>
        /// <value>The count.</value>
        public int Count {
            get {
                lock (lockObject) {
                    //ClearExpiredItems();
                    return _innerDictionary.Count;
                }
            }
        }

        bool ICollection<KeyValuePair<K, T>>.IsReadOnly {
            get { return false; }
        }

        bool ICollection<KeyValuePair<K, T>>.Remove(KeyValuePair<K, T> item) {
            lock (lockObject) {
                if (ContainsKey(item.Key)) {
                    _innerDictionary.Remove(item.Key);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns a *cloned* dictionary 
        /// (will not throw an exception on MoveNext if an item expires).
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<K, T>> GetEnumerator() {
            lock (lockObject) {
                //ClearExpiredItems();
                var ret = _innerDictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
                return ret.GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        #endregion --- IDictionary ---

        #region --- IDisposable ---
        public void Dispose() {
            Timer.Dispose();
        }
        #endregion --- IDisposable ---

    }
    #endregion ----- CacheDictionary -----
}
