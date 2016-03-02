using System;
using System.Collections.Specialized;
using System.Runtime.Caching;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CacheHelperTests {
    [TestClass]
    public class MemoryCacheTest {
        [TestMethod]
        public void MemoryCacheTest_Basic() {
            var config = new NameValueCollection();
            var cache = new MemoryCache("myMemCache", config);
            cache.Add(new CacheItem("a", "b"),
                      new CacheItemPolicy {
                          Priority = CacheItemPriority.NotRemovable,
                          SlidingExpiration = TimeSpan.FromMilliseconds(50)
                      });
            Assert.IsTrue(cache.Contains("a"));
            Assert.AreEqual("b", cache["a"]);
        }
    }
}
