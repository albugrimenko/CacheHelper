using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CacheHelper;
using System.Threading;

namespace CacheHelperTests {
    [TestClass]
    public class CacheDictionaryRemoteTest {

        public delegate void ErrHandler(string msg);
        public static event ErrHandler ErrHappened;
        public static event ErrHandler StatusUpdate;
        public static System.Text.StringBuilder sb = new System.Text.StringBuilder();

        public CacheDictionaryRemoteTest() {
            // Initialize logger
            log4net.Config.XmlConfigurator.Configure();
        }

        [TestMethod]
        public void DictionaryRemote_Add() {
            using (var dictionary = new CacheDictionaryRemote<string, object>()) {
                dictionary.DefaultTimeToLive = TimeSpan.FromMilliseconds(50);
                dictionary.Add("a", "b");
                Thread.Sleep(51);
                Assert.IsTrue(dictionary.ContainsKey("a"));
            }
        }

        [TestMethod]
        public void DictionaryRemote_this() {
            using (var dictionary = new CacheDictionaryRemote<string, object>()) {
                dictionary.DefaultTimeToLive = TimeSpan.FromMilliseconds(50);
                dictionary.Add("a", "b");
                dictionary.Add("c", "d");

                var actual = dictionary["c"];
                Assert.AreEqual("d", actual.ToString());

                dictionary["a"] = "123";
                actual = dictionary["a"];
                Assert.AreEqual("123", actual.ToString());
            }
        }

        [TestMethod]
        public void DictionaryRemoteAutoExpiresItems() {
            using (var dictionary = new CacheDictionaryRemote<string, object>(true, false)) {
                dictionary.DefaultTimeToLive = TimeSpan.FromMilliseconds(50);
                dictionary.AutoClearExpiredItemsFrequency = TimeSpan.FromMilliseconds(150);
                string key = "a";
                object value = "b";
                dictionary[key] = value;

                object sender = null;
                string eventKey = null;
                object eventValue = null;
                dictionary.ItemExpired += (s, e) => {
                                                  sender = s;
                                                  eventKey = e.Key;
                                                  eventValue = e.Value;
                                              };
                Thread.Sleep(351);
                Assert.AreSame(sender, dictionary);
                Assert.AreEqual(eventKey, key);
                Assert.AreEqual(eventValue, value);
            }
        }

        [TestMethod]
        public void DictionaryRemote_MultiThreading() {
            int numberOfThreads = 3;
            sb = new System.Text.StringBuilder();
            ErrHappened += CacheDictionaryRemoteTest_ErrHappened;
            StatusUpdate += CacheDictionaryRemoteTest_StatusUpdate;

            Thread[] thPool = new Thread[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++) {
                thPool[i] = new Thread(new ThreadStart(MT_AddGet));
                thPool[i].Start();
            }
            for (int i = 0; i < numberOfThreads; i++) {
                if (thPool[i] != null)
                    thPool[i].Join();
            }
            ErrHappened -= CacheDictionaryRemoteTest_ErrHappened;
            StatusUpdate -= CacheDictionaryRemoteTest_StatusUpdate;

            Assert.IsTrue(sb.Length == 0);
        }

        void CacheDictionaryRemoteTest_StatusUpdate(string msg) {
            //Console.WriteLine(msg);
        }

        void CacheDictionaryRemoteTest_ErrHappened(string msg) {
            //Console.WriteLine("EERROR: " + msg);
            sb.Append(msg);
        }

        #region -- private ---
        public static void MT_AddGet(){
            int numberOfCycles = 5;
            int numberOfIterations = 100;
            int delay_sec = 1;

            string threadName = "th";

            using (var dictionary = new CacheDictionaryRemote<string, object>(true, true)) {
                dictionary.DefaultTimeToLive = TimeSpan.FromMilliseconds(10000);
                for (int j = 0; j < numberOfCycles; j++) {
                    // write
                    for (int i = 0; i < numberOfIterations; i++) {
                        string key = (i * 333).ToString();
                        dictionary.Add(key, string.Format("Test_{0}_[{1}]", (i * j).ToString(), key));
                    }
                    Thread.Sleep(100); // 0.1 sec
                    // read
                    for (int i = 0; i < numberOfIterations; i++) {
                        string key = (i*333).ToString();
                        if (!dictionary.ContainsKey(key)) {
                            if (ErrHappened != null)
                                ErrHappened(string.Format("{0}:: Key [{1}] is missing.\r\n", threadName, key));
                        }
                        else if (dictionary.IsLocallyCacheable && dictionary[key] == null) {
                            if (ErrHappened != null)
                                ErrHappened(String.Format("{0}:: Object [{1}] is missing.\r\n", threadName, key));
                        }
                        else if (dictionary.IsRemotelyCacheable && dictionary[key] == null) {
                            if (ErrHappened != null)
                                ErrHappened(String.Format("{0}:: Object [{1}] is missing.\r\n", threadName, key));
                        }
                        else { 
                            if (StatusUpdate != null)
                                StatusUpdate(String.Format("{0}:: Object [{1}] retrieved.\r\n", threadName, key));
                        }
                    }
                    if (delay_sec > 0)
                        Thread.Sleep(delay_sec * 1000); // 1 sec = 1000 ms
                } // j
            }
            return;
        }
        #endregion -- private ---
    }
}
