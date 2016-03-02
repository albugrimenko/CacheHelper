using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CacheHelper;
using System.Threading;

namespace CacheHelperTests {
    [TestClass]
    public class CacheDicConcurTest {
        [TestMethod]
        public void DicConcurExpiresStaleItems() {
            using (var dictionary = new CacheDictionaryConcur<string, object>()) {
                dictionary.DefaultTimeToLive = TimeSpan.FromMilliseconds(50);
                dictionary.Add("a", "b");
                Thread.Sleep(51);
                dictionary.ClearExpiredItems(); // otherwise it will be cleared in 30 seconds
                Assert.IsFalse(dictionary.ContainsKey("a"));
            }
        }

        [TestMethod]
        public void DicConcurDoesNotExpiredNonStaleItems() {
            using (var dictionary = new CacheDictionaryConcur<string, object>()) {
                dictionary.DefaultTimeToLive = TimeSpan.FromMilliseconds(50);
                dictionary.Add("a", "b");
                Assert.IsTrue(dictionary.ContainsKey("a"));
            }
        }

        [TestMethod]
        public void DicConcurRaisesExpirationEvent() {
            using (var dictionary = new CacheDictionaryConcur<string, object>()) {
                dictionary.DefaultTimeToLive = TimeSpan.FromMilliseconds(50);
                string key = "a";
                object value = "b";
                dictionary.Add(key, value);

                object sender = null;
                string eventKey = null;
                object eventValue = null;
                dictionary.ItemExpired += (s, e) => {
                                                  sender = s;
                                                  eventKey = e.Key;
                                                  eventValue = e.Value;
                                              };
                Thread.Sleep(50);
                dictionary.ClearExpiredItems();
                Assert.AreSame(sender, dictionary);
                Assert.AreEqual(eventKey, key);
                Assert.AreEqual(eventValue, value);
            }
        }

        [TestMethod]
        public void DicConcurAutoExpiresItems() {
            using (var dictionary = new CacheDictionaryConcur<string, object>()) {
                dictionary.DefaultTimeToLive = TimeSpan.FromMilliseconds(30);
                object sender = null;
                string eventKey = null;
                object eventValue = null;
                dictionary.ItemExpired += (s, e) => {
                                                  sender = s;
                                                  eventKey = e.Key;
                                                  eventValue = e.Value;
                                              };

                string key = "a";
                object value = "b";
                dictionary.Add(key, value);
                dictionary.AutoClearExpiredItemsFrequency = TimeSpan.FromMilliseconds(150);

                Thread.Sleep(200);
                Assert.AreSame(sender, dictionary);
                Assert.AreEqual(eventKey, key);
                Assert.AreEqual(eventValue, value);
            }
        }
    }
}
