using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheHelper {
    #region ----- CacheDictionaryConcurRemote -----
    /// <summary>
    /// Defines a caching table whereby items are configured to
    /// expire and thereby be removed automatically. Default "refresh time" is 30 sec.
    /// This collection is thread-safe and uses ConcurrentDictionary and its implementation of lock mechanism.
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="T">Value</typeparam>
    public class CacheDictionaryConcurRemote<K, T> : CacheDictionaryConcur<K, T> {
        private bool _IsRemotelyCacheable = true;
        private bool _IsLocallyCacheable = true;

        #region --- Constructors ---
        public CacheDictionaryConcurRemote() : base() { }

        public CacheDictionaryConcurRemote(bool isLocallyCacheable = true, bool isRemotelyCacheable = true) : base() {
            _IsRemotelyCacheable = isRemotelyCacheable;
            _IsLocallyCacheable = isLocallyCacheable;
        }
        
        public CacheDictionaryConcurRemote(TimeSpan defaultTimeToLive, bool isLocallyCacheable = true, bool isRemotelyCacheable = true)
            : base(defaultTimeToLive) {
            _IsRemotelyCacheable = isRemotelyCacheable;
            _IsLocallyCacheable = isLocallyCacheable;
        }
        #endregion --- Constructors ---

        #region --- Properties ---
        /// <summary>
        /// Gets or sets value if collection can be cached remotly.
        /// </summary>
        public bool IsRemotelyCacheable {
            get { return _IsRemotelyCacheable; }
            set { _IsRemotelyCacheable = value; }
        }

        /// <summary>
        /// Gets or sets value if collection can be cached locally.
        /// </summary>
        public bool IsLocallyCacheable {
            get { return _IsLocallyCacheable; }
            set { _IsLocallyCacheable = value; }
        }
        #endregion --- Properties ---

        #region -- Add --
        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="timeToLive">The time-to-live.</param>
        public new void Add(K key, T value, TimeSpan timeToLive) {
            if (_IsLocallyCacheable)
                base.Add(key, value, timeToLive);
            if (_IsRemotelyCacheable)
                Helpers.SQLHelper.ObjectPut(typeof(T).ToString(), key.ToString(), value);
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires">The explicit date/time to expire the added item.</param>
        public new void Add(K key, T value, DateTime expires) {
            if (_IsLocallyCacheable)
                base.Add(key, value, expires);
            if (_IsRemotelyCacheable)
                Helpers.SQLHelper.ObjectPut(typeof(T).ToString(), key.ToString(), value);
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        public new void Add(KeyValuePair<K, CacheItem<T>> item) {
            if (_IsLocallyCacheable)
                base.Add(item);
            if (_IsRemotelyCacheable)
                Helpers.SQLHelper.ObjectPut(typeof(T).ToString(), item.Key.ToString(), item.Value.Value);
        }

        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public new void Add(K key, CacheItem<T> value) {
            if (_IsLocallyCacheable)
                base.Add(key, value);
            if (_IsRemotelyCacheable)
                Helpers.SQLHelper.ObjectPut(typeof(T).ToString(), key.ToString(), value.Value);
        }
        #endregion -- Add --

        #region --- IDictionary ---
        /// <summary>
        /// Adds a new expirable item to the collection.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public new void Add(K key, T value) {
            if (_IsLocallyCacheable)
                base.Add(key, value);
            if (_IsRemotelyCacheable)
                Helpers.SQLHelper.ObjectPut(typeof(T).ToString(), key.ToString(), value);
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// It automatically discover objects in the remote cache if applicable,
        /// therefore no other actions required if Get operation used.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// 	<c>true</c> if the dictionary contains the specified key and the item has not expired; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>This method will auto-clear expired items.</remarks>
        public new bool ContainsKey(K key) {
            if (_IsLocallyCacheable && base.ContainsKey(key))
                return true;
            if (_IsRemotelyCacheable) {
                object o = Helpers.SQLHelper.ObjectGet(typeof(T).ToString(), key.ToString());
                if (o != null && _IsLocallyCacheable)
                    base.Add(key, (T)o);
                if (o != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to the get item having the specified key. Returns <c>true</c> if
        /// the item exists and has not expired.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public new bool TryGetValue(K key, out T value) {
            if (_IsLocallyCacheable && ContainsKey(key))
                return base.TryGetValue(key, out value);
            if (_IsRemotelyCacheable) {
                object o = Helpers.SQLHelper.ObjectGet(typeof(T).ToString(), key.ToString());
                if (o != null && _IsLocallyCacheable)
                    base.Add(key, (T)o);
                if (o != null) {
                    value = (T)o;
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// Gets or sets the <typeparamref name="T"/> value with the specified key.
        /// </summary>
        /// <value></value>
        public new T this[K key] {
            get {
                if (_IsLocallyCacheable) {
                    CacheItem<T> temp;
                    if (ContainsKey(key) && _ItemList.TryGetValue(key, out temp)) {
                        return temp.Value;
                    }
                    else {
                        return default(T);
                    }
                }
                if (_IsRemotelyCacheable) {
                    object o = Helpers.SQLHelper.ObjectGet(typeof(T).ToString(), key.ToString());
                    return (o != null) ? (T)o : default(T);
                }
                return default(T);
            }
            set {
                CacheItem<T> temp = null;
                if (ContainsKey(key) && _ItemList.TryGetValue(key, out temp)) {
                    _ItemList.TryUpdate(key, new CacheItem<T>(value, DefaultTimeToLive), temp);
                    if (_IsRemotelyCacheable)
                        Helpers.SQLHelper.ObjectPut(typeof(T).ToString(), key.ToString(), value);
                }
                else {
                    Add(key, value);
                }
            }
        }
        #endregion --- IDictionary ---
    }
    #endregion ----- CacheDictionaryConcurRemote -----
}
