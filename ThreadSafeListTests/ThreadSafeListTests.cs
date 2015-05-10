using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreadSafeList;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
namespace ThreadSafeList.Tests
{
    [TestClass()]
    public class PerformanceThreadSafeListTests
    {
        /// <summary>
        /// Random number generator for obtaining random numbers
        /// </summary>
        private static Random rand = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// The number of threads to instantiate (assuming HyperThreading too)
        /// </summary>
        private static readonly int threadCount = Environment.ProcessorCount * 2;

        /// <summary>
        /// The size of the collections to instantiate
        /// </summary>
        private static readonly int collectionSize = 1000;

        // Data to test against, freshly prepopulated before each test
        List<int> valueTypes;
        List<string> referenceTypes;
        SnapshotThreadSafeList<int> valueTypesThreadSafeList;
        SnapshotThreadSafeList<string> referenceTypesThreadSafeList;

        [TestInitialize()]
        public void BeforeEachTest()
        {
            // Initialize value types
            valueTypes = Enumerable.Range(0, collectionSize).ToList();

            // Initialize reference types with junk strings
            byte[] buff = new byte[20];
            string[] tmp = new string[collectionSize];
            for (int i = 0; i < collectionSize; i++)
            {
                rand.NextBytes(buff);
                tmp[i] = BitConverter.ToString(buff);
            }
            referenceTypes = tmp.ToList();

            valueTypesThreadSafeList = new SnapshotThreadSafeList<int>(valueTypes);
            referenceTypesThreadSafeList = new SnapshotThreadSafeList<string>(referenceTypes);
        }

        //[TestCleanup()]
        //public void Cleanup()
        //{

        //}

        #region MultiThreaded Tests

        /// <summary>
        /// Deadlocks happen when two seperate threads are both waiting
        /// to obtain a lock that the other has to proceed.  Since the list
        /// class only uses a single lock object it should not be an issue.
        /// However, the CopyTo method could introduce another lock object
        /// while waiting to obtain the lock on the array
        /// </summary>
        [TestMethod()]
        public void TryAndForceDeadlock()
        {
            // Try and obtain lock on list.CopyTo???
        }

        /// <summary>
        /// Assuming all threads ran to completion, if
        /// we perform symmetric operations like Insert()
        /// then Remove() we should see that the size of the
        /// list at the beginning of the test is the same as
        /// the size at the end of the test
        /// </summary>
        [TestMethod()]
        public void VerifyAllOperationsSucceeded()
        {
            #region Value Types

            // Get size before parallel operations
            int beforeSize = valueTypesThreadSafeList.Count;

            // Parallel Insert/Remove
            for (int outer = 0; outer < 100; outer++)
            {
                // Create a list of threads
                List<Thread> threads = new List<Thread>(threadCount);

                // Populate the list
                for (int threadIndex = 0; threadIndex < threadCount - 1; threadIndex++)
                {
                    // Add threads to the list with threadstart
                    threads.Add(new Thread(() =>
                    {
                        for (int loopIndex = 0; loopIndex < collectionSize; loopIndex++)
                        {
                            try
                            {
                                // Insert a new item into the list
                                valueTypesThreadSafeList.Insert(loopIndex, loopIndex);

                                // Remove one from the list
                                valueTypesThreadSafeList.RemoveAt(loopIndex);
                            }
                            catch
                            {
                                Assert.Fail();
                            }
                        }
                    }));

                    threads[threadIndex].Name = threadIndex.ToString();
                }

                // Start all threads
                threads.ForEach(t => t.Start());

                // And wait for them to complete
                threads.ForEach(t => t.Join());
            }

            // Get size after parallel operations
            int afterSize = valueTypesThreadSafeList.Count;

            // Make sure they add up
            Assert.AreEqual(beforeSize, afterSize);

            #endregion Value Types

            #region Reference Types

            // Get size before parallel operations
            beforeSize = referenceTypesThreadSafeList.Count;

            // Parallel Insert/Remove
            for (int outer = 0; outer < 100; outer++)
            {
                // Create a list of threads
                List<Thread> threads = new List<Thread>(threadCount);

                // Populate the list
                for (int threadIndex = 0; threadIndex < threadCount - 1; threadIndex++)
                {
                    // Add threads to the list with threadstart
                    threads.Add(new Thread(() =>
                    {
                        for (int loopIndex = 0; loopIndex < collectionSize; loopIndex++)
                        {
                            // Generate a string to insert in the list
                            string tmp = threadIndex.ToString() + " " + loopIndex.ToString();

                            try
                            {
                                // Insert a new item into the list
                                referenceTypesThreadSafeList.Insert(loopIndex, tmp);

                                // Remove one from the list
                                referenceTypesThreadSafeList.RemoveAt(loopIndex);
                            }
                            catch
                            {
                                Assert.Fail();
                            }
                        }
                    }));

                    threads[threadIndex].Name = threadIndex.ToString();
                }

                // Start all threads
                threads.ForEach(t => t.Start());

                // And wait for them to complete
                threads.ForEach(t => t.Join());
            }

            // Get size after parallel operations
            afterSize = referenceTypesThreadSafeList.Count;

            // Make sure they add up
            Assert.AreEqual(beforeSize, afterSize);

            #endregion Reference Types
        }

        /// <summary>
        /// In this test we modify the list while enumerating through
        /// it to make sure foreach/GetEnumerator work on a list that
        /// is being changed across multiple threads
        /// </summary>
        [TestMethod()]
        public void ModifyingWhileEnumeratingTest()
        {
            #region Value Types

            // Parallel Insert/Remove
            for (int outer = 0; outer < 100; outer++)
            {
                // Create a list of threads
                List<Thread> threads = new List<Thread>(threadCount);

                // Populate the list
                for (int threadIndex = 0; threadIndex < threadCount - 1; threadIndex++)
                {
                    // All even number threads
                    if (threadIndex % 2 == 0)
                    {
                        // Add thread to the list with threadstart
                        threads.Add(new Thread(() =>
                        {
                            for (int loopIndex = 0; loopIndex < collectionSize; loopIndex++)
                            {
                                try
                                {
                                    // Insert a new item into the list
                                    valueTypesThreadSafeList.Insert(loopIndex, loopIndex);

                                    // Remove one from the list
                                    valueTypesThreadSafeList.RemoveAt(loopIndex);
                                }
                                catch
                                {
                                    Assert.Fail();
                                }
                            }
                        }));
                    }

                    // All odd number threads
                    else
                    {
                        // Add thread to the list with threadstart
                        threads.Add(new Thread(() =>
                        {
                            // Just iterate through the collection
                            foreach (int val in valueTypesThreadSafeList)
                            {
                                try
                                {
                                    // Do anything with the variable to make
                                    // sure we can obtain it
                                    string tmp = val.ToString();
                                }
                                catch
                                {
                                    Assert.Fail();
                                }
                            }
                        }));
                    }

                    threads[threadIndex].Name = threadIndex.ToString();
                }

                // Start all threads
                threads.ForEach(t => t.Start());

                // And wait for them to complete
                threads.ForEach(t => t.Join());
            }

            #endregion Value Types

            #region Reference Types

            // Parallel Insert/Remove
            for (int outer = 0; outer < 100; outer++)
            {
                // Create a list of threads
                List<Thread> threads = new List<Thread>(threadCount);

                // Populate the list
                for (int threadIndex = 0; threadIndex < threadCount - 1; threadIndex++)
                {
                    // All even number threads
                    if (threadIndex % 2 == 0)
                    {
                        // Add thread to the list with threadstart
                        threads.Add(new Thread(() =>
                        {
                            for (int loopIndex = 0; loopIndex < collectionSize; loopIndex++)
                            {
                                // Generate a string to insert in the list
                                string tmp = threadIndex.ToString() + " " + loopIndex.ToString();

                                try
                                {
                                    // Insert a new item into the list
                                    referenceTypesThreadSafeList.Insert(loopIndex, tmp);

                                    // Remove one from the list
                                    referenceTypesThreadSafeList.RemoveAt(loopIndex);
                                }
                                catch
                                {
                                    Assert.Fail();
                                }
                            }
                        }));
                    }

                    // All odd number threads
                    else
                    {
                        // Add thread to the list with threadstart
                        threads.Add(new Thread(() =>
                        {
                            // Just iterate through the collection
                            foreach (string s in referenceTypesThreadSafeList)
                            {
                                try
                                {
                                    // Do anything with the variable to make
                                    // sure we can obtain it
                                    string tmp = s + "foo";
                                }
                                catch
                                {
                                    Assert.Fail();
                                }
                            }
                        }));
                    }

                    threads[threadIndex].Name = threadIndex.ToString();
                }

                // Start all threads
                threads.ForEach(t => t.Start());

                // And wait for them to complete
                threads.ForEach(t => t.Join());
            }

            #endregion Reference Types
        }

        #endregion MultiThreaded Tests

        #region Constructor Tests

        [TestMethod()]
        public void PerformanceThreadSafeListTest()
        {
            Assert.IsNotNull(new SnapshotThreadSafeList<int>());
            Assert.IsNotNull(new SnapshotThreadSafeList<object>());
        }

        [TestMethod()]
        public void PerformanceThreadSafeListTest1()
        {
            Assert.IsNotNull(new SnapshotThreadSafeList<int>(20));
            Assert.IsNotNull(new SnapshotThreadSafeList<object>(20));
        }

        [TestMethod()]
        public void PerformanceThreadSafeListTest2()
        {
            Assert.IsNotNull(valueTypesThreadSafeList);
            Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
            Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

            Assert.IsNotNull(referenceTypesThreadSafeList);
            Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
            Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
        }

        #endregion Constructor Tests

        #region IList<T> Member Tests

        [TestMethod()]
        public void IndexOfTest()
        {
            for (int i = 0; i < valueTypes.Count(); i++)
            {
                Assert.AreEqual(valueTypesThreadSafeList.IndexOf(valueTypes.ElementAt(i)), i);
                Assert.AreEqual(referenceTypesThreadSafeList.IndexOf(referenceTypes.ElementAt(i)), i);
            }
        }

        [TestMethod()]
        public void InsertTest()
        {
            // Insert at end, middle, and beginning
            int[] positions = new int[] { valueTypes.Count, (int)(valueTypes.Count / 2), 0 };

            foreach (int pos in positions)
            {
                // Insert the values in the collections
                valueTypes.Insert(pos, 3);
                valueTypesThreadSafeList.Insert(pos, 3);

                referenceTypes.Insert(pos, "foo");
                referenceTypesThreadSafeList.Insert(pos, "foo");

                Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
                Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

                Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
                Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
            }
        }

        [TestMethod()]
        public void RemoveAtTest()
        {
            // Insert at end, middle, and beginning
            int[] positions = new int[] { valueTypes.Count - 1, (int)(valueTypes.Count / 2), 0 };

            foreach (int pos in positions)
            {
                // Insert the values in the collections
                valueTypes.RemoveAt(pos);
                valueTypesThreadSafeList.RemoveAt(pos);

                referenceTypes.RemoveAt(pos);
                referenceTypesThreadSafeList.RemoveAt(pos);

                Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
                Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

                Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
                Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
            }
        }

        [TestMethod()]
        public void AddTest()
        {
            for (int i = 0; i < 10; i++)
            {
                // Insert the values in the collections
                valueTypes.Add(i);
                valueTypesThreadSafeList.Add(i);

                string s = new Guid().ToString();
                referenceTypes.Add(s);
                referenceTypesThreadSafeList.Add(s);

                Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
                Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

                Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
                Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
            }
        }

        [TestMethod()]
        public void ClearTest()
        {
            valueTypesThreadSafeList.Clear();
            Assert.AreEqual(valueTypesThreadSafeList.Count, 0);
            referenceTypesThreadSafeList.Clear();
            Assert.AreEqual(referenceTypesThreadSafeList.Count, 0);
        }

        [TestMethod()]
        public void ContainsTest()
        {
            foreach (var item in valueTypes)
            {
                Assert.IsTrue(valueTypesThreadSafeList.Contains(item));
            }

            foreach (var item in referenceTypes)
            {
                Assert.IsTrue(referenceTypesThreadSafeList.Contains(item));
            }
        }

        [TestMethod()]
        public void CopyToTest()
        {
            int offset = 10;
            int[] vals = new int[valueTypes.Count + offset];
            string[] refs = new string[referenceTypes.Count + offset];

            valueTypesThreadSafeList.CopyTo(vals, offset);
            referenceTypesThreadSafeList.CopyTo(refs, offset);

            for (int i = 0; i < valueTypesThreadSafeList.Count; i++)
            {
                Assert.AreEqual(vals[i + offset], valueTypesThreadSafeList[i]);
                Assert.AreEqual(refs[i + offset], referenceTypesThreadSafeList[i]);
            }
        }

        [TestMethod()]
        public void RemoveTest()
        {
            valueTypesThreadSafeList.Remove(valueTypes[(int)(valueTypes.Count / 2)]);
            valueTypes.Remove(valueTypes[(int)(valueTypes.Count / 2)]);
            referenceTypesThreadSafeList.Remove(referenceTypes[(int)(referenceTypes.Count / 2)]);
            referenceTypes.Remove(referenceTypes[(int)(referenceTypes.Count / 2)]);

            Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
            Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

            Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
            Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
        }

        [TestMethod()]
        public void GetEnumeratorTest()
        {
            // Test we get a new one each time
            IEnumerator<int> v1 = valueTypesThreadSafeList.GetEnumerator();
            IEnumerator<int> v2 = valueTypesThreadSafeList.GetEnumerator();
            Assert.AreNotEqual(v1, v2);

            IEnumerator<object> r1 = referenceTypesThreadSafeList.GetEnumerator();
            IEnumerator<object> r2 = referenceTypesThreadSafeList.GetEnumerator();
            Assert.AreNotEqual(r1, r2);
        }

        #endregion IList<T> Member Tests
    }

    [TestClass()]
    public class DataFidelityThreadSafeListTests
    {
        /// <summary>
        /// Random number generator for obtaining random numbers
        /// </summary>
        private static Random rand = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// The number of threads to instantiate (assuming HyperThreading too)
        /// </summary>
        private static readonly int threadCount = Environment.ProcessorCount * 2;

        /// <summary>
        /// The size of the collections to instantiate.  I did 100 because this thing
        /// is a hog.
        /// </summary>
        private static readonly int collectionSize = 100;

        // Data to test against, freshly prepopulated before each test
        List<int> valueTypes;
        List<string> referenceTypes;
        DataFidelityThreadSafeList<int> valueTypesThreadSafeList;
        DataFidelityThreadSafeList<string> referenceTypesThreadSafeList;

        [TestInitialize()]
        public void BeforeEachTest()
        {
            // Initialize value types
            valueTypes = Enumerable.Range(0, collectionSize).ToList();

            // Initialize reference types with junk strings
            byte[] buff = new byte[20];
            string[] tmp = new string[collectionSize];
            for (int i = 0; i < collectionSize; i++)
            {
                rand.NextBytes(buff);
                tmp[i] = BitConverter.ToString(buff);
            }
            referenceTypes = tmp.ToList();

            valueTypesThreadSafeList = new DataFidelityThreadSafeList<int>(valueTypes);
            referenceTypesThreadSafeList = new DataFidelityThreadSafeList<string>(referenceTypes);
        }

        //[TestCleanup()]
        //public void Cleanup()
        //{

        //}

        #region MultiThreaded Tests

        /// <summary>
        /// Deadlocks happen when two seperate threads are both waiting
        /// to obtain a lock that the other has to proceed.  Since the list
        /// class only uses a single lock object it should not be an issue.
        /// However, the CopyTo method could introduce another lock object
        /// while waiting to obtain the lock on the array
        /// </summary>
        [TestMethod()]
        public void TryAndForceDeadlock()
        {
            // Try and obtain lock on list.CopyTo???
        }

        /// <summary>
        /// Assuming all threads ran to completion, if
        /// we perform symmetric operations like Insert()
        /// then Remove() we should see that the size of the
        /// list at the beginning of the test is the same as
        /// the size at the end of the test
        /// </summary>
        [TestMethod()]
        public void VerifyAllOperationsSucceeded()
        {
            #region Value Types

            // Get size before parallel operations
            int beforeSize = valueTypesThreadSafeList.Count;

            // Parallel Insert/Remove
            for (int outer = 0; outer < 100; outer++)
            {
                // Create a list of threads
                List<Thread> threads = new List<Thread>(threadCount);

                // Populate the list
                for (int threadIndex = 0; threadIndex < threadCount - 1; threadIndex++)
                {
                    // Add threads to the list with threadstart
                    threads.Add(new Thread(() =>
                    {
                        for (int loopIndex = 0; loopIndex < collectionSize; loopIndex++)
                        {
                            try
                            {
                                // Insert a new item into the list
                                valueTypesThreadSafeList.Insert(loopIndex, loopIndex);

                                // Remove one from the list
                                valueTypesThreadSafeList.RemoveAt(loopIndex);
                            }
                            catch
                            {
                                Assert.Fail();
                            }
                        }
                    }));

                    threads[threadIndex].Name = threadIndex.ToString();
                }

                // Start all threads
                threads.ForEach(t => t.Start());

                // And wait for them to complete
                threads.ForEach(t => t.Join());
            }

            // Get size after parallel operations
            int afterSize = valueTypesThreadSafeList.Count;

            // Make sure they add up
            Assert.AreEqual(beforeSize, afterSize);

            #endregion Value Types

            #region Reference Types

            // Get size before parallel operations
            beforeSize = referenceTypesThreadSafeList.Count;

            // Parallel Insert/Remove
            for (int outer = 0; outer < 100; outer++)
            {
                // Create a list of threads
                List<Thread> threads = new List<Thread>(threadCount);

                // Populate the list
                for (int threadIndex = 0; threadIndex < threadCount - 1; threadIndex++)
                {
                    // Add threads to the list with threadstart
                    threads.Add(new Thread(() =>
                    {
                        for (int loopIndex = 0; loopIndex < collectionSize; loopIndex++)
                        {
                            // Generate a string to insert in the list
                            string tmp = threadIndex.ToString() + " " + loopIndex.ToString();

                            try
                            {
                                // Insert a new item into the list
                                referenceTypesThreadSafeList.Insert(loopIndex, tmp);

                                // Remove one from the list
                                referenceTypesThreadSafeList.RemoveAt(loopIndex);
                            }
                            catch
                            {
                                Assert.Fail();
                            }
                        }
                    }));

                    threads[threadIndex].Name = threadIndex.ToString();
                }

                // Start all threads
                threads.ForEach(t => t.Start());

                // And wait for them to complete
                threads.ForEach(t => t.Join());
            }

            // Get size after parallel operations
            afterSize = referenceTypesThreadSafeList.Count;

            // Make sure they add up
            Assert.AreEqual(beforeSize, afterSize);

            #endregion Reference Types
        }

        /// <summary>
        /// In this test we modify the list while enumerating through
        /// it to make sure foreach/GetEnumerator work on a list that
        /// is being changed across multiple threads
        /// </summary>
        [TestMethod()]
        public void ModifyingWhileEnumeratingTest()
        {
            #region Value Types

            // Parallel Insert/Remove
            for (int outer = 0; outer < 100; outer++)
            {
                // Create a list of threads
                List<Thread> threads = new List<Thread>(threadCount);

                // Populate the list
                for (int threadIndex = 0; threadIndex < threadCount - 1; threadIndex++)
                {
                    // All even number threads
                    if (threadIndex % 2 == 0)
                    {
                        // Add thread to the list with threadstart
                        threads.Add(new Thread(() =>
                        {
                            for (int loopIndex = 0; loopIndex < collectionSize; loopIndex++)
                            {
                                try
                                {
                                    // Insert a new item into the list
                                    valueTypesThreadSafeList.Insert(loopIndex, loopIndex);

                                    // Remove one from the list
                                    valueTypesThreadSafeList.RemoveAt(loopIndex);
                                }
                                catch
                                {
                                    Assert.Fail();
                                }
                            }
                        }));
                    }

                    // All odd number threads
                    else
                    {
                        // Add thread to the list with threadstart
                        threads.Add(new Thread(() =>
                        {
                            // Just iterate through the collection
                            foreach (int val in valueTypesThreadSafeList)
                            {
                                try
                                {
                                    // Do anything with the variable to make
                                    // sure we can obtain it
                                    string tmp = val.ToString();
                                }
                                catch
                                {
                                    Assert.Fail();
                                }
                            }
                        }));
                    }

                    threads[threadIndex].Name = threadIndex.ToString();
                }

                // Start all threads
                threads.ForEach(t => t.Start());

                // And wait for them to complete
                threads.ForEach(t => t.Join());
            }

            #endregion Value Types

            #region Reference Types

            // Parallel Insert/Remove
            for (int outer = 0; outer < 100; outer++)
            {
                // Create a list of threads
                List<Thread> threads = new List<Thread>(threadCount);

                // Populate the list
                for (int threadIndex = 0; threadIndex < threadCount - 1; threadIndex++)
                {
                    // All even number threads
                    if (threadIndex % 2 == 0)
                    {
                        // Add thread to the list with threadstart
                        threads.Add(new Thread(() =>
                        {
                            for (int loopIndex = 0; loopIndex < collectionSize; loopIndex++)
                            {
                                // Generate a string to insert in the list
                                string tmp = threadIndex.ToString() + " " + loopIndex.ToString();

                                try
                                {
                                    // Insert a new item into the list
                                    referenceTypesThreadSafeList.Insert(loopIndex, tmp);

                                    // Remove one from the list
                                    referenceTypesThreadSafeList.RemoveAt(loopIndex);
                                }
                                catch
                                {
                                    Assert.Fail();
                                }
                            }
                        }));
                    }

                    // All odd number threads
                    else
                    {
                        // Add thread to the list with threadstart
                        threads.Add(new Thread(() =>
                        {
                            // Just iterate through the collection
                            foreach (string s in referenceTypesThreadSafeList)
                            {
                                try
                                {
                                    // Do anything with the variable to make
                                    // sure we can obtain it
                                    string tmp = s + "foo";
                                }
                                catch
                                {
                                    Assert.Fail();
                                }
                            }
                        }));
                    }

                    threads[threadIndex].Name = threadIndex.ToString();
                }

                // Start all threads
                threads.ForEach(t => t.Start());

                // And wait for them to complete
                threads.ForEach(t => t.Join());
            }

            #endregion Reference Types
        }

        #endregion MultiThreaded Tests

        #region Constructor Tests

        [TestMethod()]
        public void DataFidelityThreadSafeListTest()
        {
            Assert.IsNotNull(new DataFidelityThreadSafeList<int>());
            Assert.IsNotNull(new DataFidelityThreadSafeList<object>());
        }

        [TestMethod()]
        public void DataFidelityThreadSafeListTest1()
        {
            Assert.IsNotNull(new DataFidelityThreadSafeList<int>(20));
            Assert.IsNotNull(new DataFidelityThreadSafeList<object>(20));
        }

        [TestMethod()]
        public void DataFidelityThreadSafeListTest2()
        {
            Assert.IsNotNull(valueTypesThreadSafeList);
            Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
            Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

            Assert.IsNotNull(referenceTypesThreadSafeList);
            Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
            Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
        }

        #endregion Constructor Tests

        #region IList<T> Member Tests

        [TestMethod()]
        public void IndexOfTest()
        {
            for (int i = 0; i < valueTypes.Count(); i++)
            {
                Assert.AreEqual(valueTypesThreadSafeList.IndexOf(valueTypes.ElementAt(i)), i);
                Assert.AreEqual(referenceTypesThreadSafeList.IndexOf(referenceTypes.ElementAt(i)), i);
            }
        }

        [TestMethod()]
        public void InsertTest()
        {
            // Insert at end, middle, and beginning
            int[] positions = new int[] { valueTypes.Count, (int)(valueTypes.Count / 2), 0 };

            foreach (int pos in positions)
            {
                // Insert the values in the collections
                valueTypes.Insert(pos, 3);
                valueTypesThreadSafeList.Insert(pos, 3);

                referenceTypes.Insert(pos, "foo");
                referenceTypesThreadSafeList.Insert(pos, "foo");

                Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
                Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

                Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
                Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
            }
        }

        [TestMethod()]
        public void RemoveAtTest()
        {
            // Insert at end, middle, and beginning
            int[] positions = new int[] { valueTypes.Count - 1, (int)(valueTypes.Count / 2), 0 };

            foreach (int pos in positions)
            {
                // Insert the values in the collections
                valueTypes.RemoveAt(pos);
                valueTypesThreadSafeList.RemoveAt(pos);

                referenceTypes.RemoveAt(pos);
                referenceTypesThreadSafeList.RemoveAt(pos);

                Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
                Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

                Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
                Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
            }
        }

        [TestMethod()]
        public void AddTest()
        {
            for (int i = 0; i < 10; i++)
            {
                // Insert the values in the collections
                valueTypes.Add(i);
                valueTypesThreadSafeList.Add(i);

                string s = new Guid().ToString();
                referenceTypes.Add(s);
                referenceTypesThreadSafeList.Add(s);

                Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
                Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

                Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
                Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
            }
        }

        [TestMethod()]
        public void ClearTest()
        {
            valueTypesThreadSafeList.Clear();
            Assert.AreEqual(valueTypesThreadSafeList.Count, 0);
            referenceTypesThreadSafeList.Clear();
            Assert.AreEqual(referenceTypesThreadSafeList.Count, 0);
        }

        [TestMethod()]
        public void ContainsTest()
        {
            foreach (var item in valueTypes)
            {
                Assert.IsTrue(valueTypesThreadSafeList.Contains(item));
            }

            foreach (var item in referenceTypes)
            {
                Assert.IsTrue(referenceTypesThreadSafeList.Contains(item));
            }
        }

        [TestMethod()]
        public void CopyToTest()
        {
            int offset = 10;
            int[] vals = new int[valueTypes.Count + offset];
            string[] refs = new string[referenceTypes.Count + offset];

            valueTypesThreadSafeList.CopyTo(vals, offset);
            referenceTypesThreadSafeList.CopyTo(refs, offset);

            for (int i = 0; i < valueTypesThreadSafeList.Count; i++)
            {
                Assert.AreEqual(vals[i + offset], valueTypesThreadSafeList[i]);
                Assert.AreEqual(refs[i + offset], referenceTypesThreadSafeList[i]);
            }
        }

        [TestMethod()]
        public void RemoveTest()
        {
            valueTypesThreadSafeList.Remove(valueTypes[(int)(valueTypes.Count / 2)]);
            valueTypes.Remove(valueTypes[(int)(valueTypes.Count / 2)]);
            referenceTypesThreadSafeList.Remove(referenceTypes[(int)(referenceTypes.Count / 2)]);
            referenceTypes.Remove(referenceTypes[(int)(referenceTypes.Count / 2)]);

            Assert.IsTrue(valueTypesThreadSafeList.SequenceEqual(valueTypes));
            Assert.AreEqual(valueTypesThreadSafeList.Count, valueTypes.Count());

            Assert.IsTrue(referenceTypesThreadSafeList.SequenceEqual(referenceTypes));
            Assert.AreEqual(referenceTypesThreadSafeList.Count, referenceTypes.Count());
        }

        [TestMethod()]
        public void GetEnumeratorTest()
        {
            // Test we get a new one each time
            IEnumerator<int> v1 = valueTypesThreadSafeList.GetEnumerator();
            IEnumerator<int> v2 = valueTypesThreadSafeList.GetEnumerator();
            Assert.AreNotEqual(v1, v2);

            IEnumerator<object> r1 = referenceTypesThreadSafeList.GetEnumerator();
            IEnumerator<object> r2 = referenceTypesThreadSafeList.GetEnumerator();
            Assert.AreNotEqual(r1, r2);
        }

        #endregion IList<T> Member Tests
    }
}
