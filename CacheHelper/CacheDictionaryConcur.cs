using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CacheHelper {
    #region ----- CacheDictionary -----
    /// <summary>
    /// Defines a caching table whereby items are configured to
    /// expire and thereby be removed automatically. Default "refresh time" is 30 sec.
    /// This collection is thread-safe and uses ConcurrentDictionary and its implementation of lock mechanism.
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="T">Value</typeparam>
    public class CacheDictionaryConcur<K, T> : IDictionary<K, T>, IDisposable {
        protected internal ConcurrentDictionary<K, CacheItem<T>> _ItemList;
        public event EventHandler<CacheItemRemovedEventArgs<K, T>> ItemExpired;

        #region --- Constructors ---
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public CacheDictionaryConcur() {
            DefaultTimeToLive = new CacheItem<T>().TimeToLive;
            _ItemList = new ConcurrentDictionary<K, CacheItem<T>>();
            var ts = AutoClearExpiredItemsFrequency;
            this._Timer = new Timer(e => this.ClearExpiredItems(), null, ts, ts);
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="dictionary">A dictionary of values to pre-load into the cache.</param>
        public CacheDictionaryConcur(IEnumerable<KeyValuePair<K, T>> dictionary) : this() {
            foreach (var kvp in dictionary) {
                CacheItem<T> ci = new CacheItem<T>(kvp.Value);
                _ItemList.AddOrUpdate(kvp.Key, ci,
                    (key, existingVal) => {
                        // If this delegate is invoked, then the key already exists.
                        return ci; // full replace
                    });
            }
        }

        /// <summary>
        /// Initializes a new instance of this type using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">A comparer implementation. For example,
        /// for case-insensitive keys, use <see cref="StringComparer.InvariantCulture"/>.</param>
        public CacheDictionaryConcur(IEqualityComparer<K> comparer) {
            _ItemList = new ConcurrentDictionary<K, CacheItem<T>>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpirableItemDictionary&lt;K, T&gt;"/> class.
        /// </summary>
        /// <param name="dictionary">A dictionary of values to pre-load into the cache.</param>
        /// <param name="comparer">A comparer implementation. For example,
        /// for case-insensitive keys, use <see cref="StringComparer.InvariantCulture"/>.</param>
        public CacheDictionaryConcur(IEnumerable<KeyValuePair<K, T>> dictionary, IEqualityComparer<K> comparer) : this(comparer) {
            foreach (var kvp in dictionary) {
                CacheItem<T> ci = new CacheItem<T>(kvp.Value, DefaultTimeToLive);
                _ItemList.AddOrUpdate(kvp.Key, ci,
                    (key, existingVal) => {
                        // If this delegate is invoked, then the key already exists.
                        return ci; // full replace
                    });
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpirableItemDictionary&lt;K, T&gt;"/> class
        /// using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">A comparer implementation. For example,
        /// for case-insensitive keys, use <see cref="StringComparer.InvariantCulture"/>.</param>
        /// <param name="defaultTimeToLive">The default time-to-live.</param>
        public CacheDictionaryConcur(IEqualityComparer<K> comparer, TimeSpan defaultTimeToLive) : this(comparer) {
            this.DefaultTimeToLive = defaultTimeToLive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpirableItemDictionary&lt;K, T&gt;"/> class
        /// using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="defaultTimeToLive">The default time-to-live.</param>
        public CacheDictionaryConcur(TimeSpan defaultTimeToLive) : this() {
            DefaultTimeToLive = defaultTimeToLive;
        }
        #endregion --- Constructors ---

        #region --- Properties ---
        protected Timer _Timer { get; set; }

        private TimeSpan _ts = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the frequency at which expired items are automatically cleared.
        /// </summary>
        /// <value>The auto clear expired items frequency.</value>
        public TimeSpan AutoClearExpiredItemsFrequency {
            get { return _ts; }
            set {
                _ts = value;
                _Timer.Change(new TimeSpan(0), value);
            }
        }

        /// <summary>
        /// Gets or sets the default time-to-live.
        /// </summary>
        /// <value>The default time to live.</value>
        public TimeSpan DefaultTimeToLive { get; set; }

        /// <summary>
        /// Sets the value with the specified key and time-to-live.
        /// </summary>
        /// <value></value>
        public T this[K key, TimeSpan timeToLive] {
            set {
                CacheItem<T> temp;
                if (_ItemList.TryGetValue(key, out temp))
                    _ItemList.TryUpdate(key, new CacheItem<T>(value, timeToLive), temp);
            }
        }

        /// <summary>
        /// Sets the <typeparamref name="T"/> value with the specified key an explicit expiration date/time.
        /// </summary>
        /// <value></value>
        public T this[K key, DateTime expires] {
            set {
                CacheItem<T> temp;
                if (_ItemList.TryGetValue(key, out temp))
                    _ItemList.TryUpdate(key, new CacheItem<T>(value, expires), temp);
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
            _ItemList.TryAdd(key, new CacheItem<T>(value, timeToLive));
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires">The explicit date/time to expire the added item.</param>
        public void Add(K key, T value, DateTime expires) {
            _ItemList.TryAdd(key, new CacheItem<T>(value, expires));
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(KeyValuePair<K, CacheItem<T>> item) {
            _ItemList.TryAdd(item.Key, item.Value);
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(K key, CacheItem<T> value) {
            _ItemList.TryAdd(key, value);
        }
        #endregion -- Add --

        /// <summary>
        /// Manual invocation clears all items that have expired.
        /// This method is not required for expiration but can be
        /// used for tuning application performance and memory,
        /// somewhat similar to GC.Collect(). 
        /// </summary>
        public void ClearExpiredItems() {
            List<KeyValuePair<K, CacheItem<T>>> removeList = _ItemList.Where(kvp => kvp.Value.HasExpired).ToList();

            CacheItem<T> temp;
            removeList.ForEach(kvp => {
                if (_ItemList.TryRemove(kvp.Key, out temp)) {
                    if (ItemExpired != null)
                        ItemExpired(this, new CacheItemRemovedEventArgs<K, T> {
                            Key = kvp.Key,
                            Value = temp.Value
                        });
                }
            });
        }

        /// <summary>
        /// Resets the timestamp for the specified item
        /// if the item exists in the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="resetTimestamp"></param>
        public void Update(K key, DateTime resetTimestamp) {
            CacheItem<T> temp;
            if (_ItemList.TryGetValue(key, out temp)) {
                _ItemList.TryUpdate(key, new CacheItem<T>(temp.Value, resetTimestamp, temp.TimeToLive), temp);
            }
        }

        #region --- IDictionary ---
        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(K key, T value) {
            _ItemList.TryAdd(key, new CacheItem<T>(value, DefaultTimeToLive));
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(KeyValuePair<K, T> item) {
            _ItemList.TryAdd(item.Key, new CacheItem<T>(item.Value, DefaultTimeToLive));
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
            CacheItem<T> temp;
            if (_ItemList.TryGetValue(key, out temp)) {
                if (temp.HasExpired) {
                    if (ItemExpired != null)
                        ItemExpired(this, new CacheItemRemovedEventArgs<K, T> {
                            Key = key,
                            Value = temp.Value
                        });
                    if (_ItemList.TryRemove(key, out temp))
                        return false;
                    else
                        return true;
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Gets the keys of the collection for all items that have not yet expired.
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<K> Keys {
            get { return _ItemList.Keys; }
        }

        /// <summary>
        /// Removes the item having the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public bool Remove(K key) {
            CacheItem<T> temp;
            bool res = _ItemList.TryRemove(key, out temp);
            return res;
            //return _ItemList.TryRemove(key, out temp);
        }

        /// <summary>
        /// Tries to the get item having the specified key. Returns <c>true</c> if
        /// the item exists and has not expired.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool TryGetValue(K key, out T value) {
            CacheItem<T> temp;
            if (_ItemList.TryGetValue(key, out temp)) {
                value = temp.Value;
                return true;
            }
            else {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Gets all of the values in the dictionary, without any key mappings.
        /// </summary>
        /// <value>The values.</value>
        public ICollection<T> Values {
            get { return this.Cast<T>().ToList(); }
        }

        /// <summary>
        /// Gets or sets the T value with the specified key.
        /// </summary>
        /// <value></value>
        public T this[K key] {
            get {
                CacheItem<T> temp;
                if (_ItemList.TryGetValue(key, out temp)) {
                    return temp.Value;
                }
                else {
                    return default(T);
                }
            }
            set {
                CacheItem<T> temp;
                if (_ItemList.TryGetValue(key, out temp))
                    _ItemList.TryUpdate(key, new CacheItem<T>(value, DefaultTimeToLive), temp);
            }
        }

        /// <summary>
        /// Removes all items from the internal dictionary.
        /// </summary>
        public void Clear() {
            _ItemList.Clear();
        }

        bool ICollection<KeyValuePair<K, T>>.Contains(KeyValuePair<K, T> item) {
            return ContainsKey(item.Key) &&
                    (object)_ItemList[item.Key].Value == (object)item.Value;
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
                return _ItemList.Count;
            }
        }

        bool ICollection<KeyValuePair<K, T>>.IsReadOnly {
            get { return false; }
        }

        bool ICollection<KeyValuePair<K, T>>.Remove(KeyValuePair<K, T> item) {
            CacheItem<T> temp;
            return _ItemList.TryRemove(item.Key, out temp);
        }

        /// <summary>
        /// Returns a *cloned* dictionary 
        /// (will not throw an exception on MoveNext if an item expires).
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<K, T>> GetEnumerator() {
            var ret = _ItemList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
            return ret.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        #endregion --- IDictionary ---

        #region --- IDisposable ---
        public void Dispose() {
            _Timer.Dispose();
        }
        #endregion --- IDisposable ---

    }
    #endregion ----- CacheDictionaryConcur -----

}
