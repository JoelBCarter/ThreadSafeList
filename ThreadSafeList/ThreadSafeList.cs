using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSafeList
{
    /// <summary>
    /// Abstract base class for all ThreadSafeList&lt;T&gt;'s to inherrit from.  This
    /// allows derived classes to implement their own syncronization mechanism as different
    /// use cases might call for different balances between performance, memory, & safety.  Anyone
    /// wanting to derive from ThreadSafeList&lt;T&gt; need only implement the ThreadSafeWrapper
    /// method that is used to provide safety/synchronization across threads.
    /// </summary>
    /// <typeparam name="T">The type of objects to store in the ThreadSafeList</typeparam>
    public abstract class ThreadSafeList<T> : IList<T>
    {
        /// <summary>
        /// The list of objects that will be exposed in a threadsafe
        /// manner via this class
        /// </summary>
        protected List<T> _internalList;

        /// <summary>
        /// Internal object on which to lock when performing critical
        /// operations
        /// </summary>
        protected object _lock = new object();

        /// <summary>
        /// This is the only method you'll need to implement to derive
        /// from this class.  It is the wrapper that will be used for all
        /// methods invoked on the internal list.  It is used to provide
        /// support for concurrency in whichever fasion you deem appropriate.
        /// </summary>
        /// <param name="operation">The action that will be performed in a
        /// thread-safe manner</param>
        protected abstract void ThreadSafeWrapper(Action operation);

        /// <summary>
        /// The method used to Clone the internal list so it can
        /// be enumerated over.  The default implmentation is a
        /// deep clone and unless you are opening yourself up to
        /// errors if you change it to shallow. Nevertheless, if you
        /// find yourself in need just override this method with either
        /// ShallowCloneInternalList below, or your the implementation
        /// your use case dictates.  Be sure to read all the cautions listed
        /// on the IEnumerator<T> GetEnumerator() of this class before you do
        /// though because I thought this through hard and you'll want to know
        /// why.
        /// </summary>
        /// <returns>A copy of the internal list</returns>
        protected virtual List<T> CloneInternalList()
        {
            return this.DeepCloneInternalList();
        }

        /// <summary>
        /// Provides a deep copy of the internal list
        /// </summary>
        /// <returns>A deep copy of the internal list</returns>
        protected List<T> DeepCloneInternalList()
        {
            // Serialize to memory then back out to create
            // a deep clone
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, _internalList);
                ms.Position = 0;
                return (List<T>)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// Provides a shallow copy of the internal list
        /// </summary>
        /// <returns>A shallow copy of the internal list</returns>
        protected List<T> ShallowCloneInternalList()
        {
            return ((ThreadSafeList<T>)this.MemberwiseClone())._internalList;
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ThreadSafeList&lt;T&gt; class
        /// that is empty and has the default initial capacity.
        /// </summary>
        public ThreadSafeList()
        {
            this._internalList = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the ThreadSafeList&lt;T&gt; class
        /// that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">capacity is less than 0</exception>
        public ThreadSafeList(int capacity)
        {
            this._internalList = new List<T>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the ThreadSafeList&lt;T&gt; class
        /// that contains elements copied from the specified collection and has sufficient
        /// capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <exception cref="System.ArgumentNullException">collection is null</exception>
        public ThreadSafeList(IEnumerable<T> collection)
        {
            this._internalList = new List<T>(collection);
        }

        #endregion Constructors

        #region IList<T>

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// first occurrence within the entire ThreadSafeList&lt;T&gt;.
        /// </summary>
        /// <param name="item">The object to locate in the ThreadSafeList&lt;T&gt;. The value
        /// can be null for reference types.</param>
        /// <returns>The zero-based index of the first occurrence of item within the entire ThreadSafeList&lt;T&gt;,
        /// if found; otherwise, –1.</returns>
        public int IndexOf(T item)
        {
            int tmp = -1;
            ThreadSafeWrapper(() => tmp = _internalList.IndexOf(item));
            return tmp;
        }

        /// <summary>
        /// Inserts an element into the ThreadSafeList&lt;T&gt; at the specified
        /// index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert. The value can be null for reference types.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">index is less than 0.-or-index is greater than ThreadSafeList&lt;T&gt;.Count.</exception>
        public void Insert(int index, T item)
        {
            ThreadSafeWrapper(() => _internalList.Insert(index, item));
        }

        /// <summary>
        /// Removes the element at the specified index of the ThreadSafeList&lt;T&gt;.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">index is less than 0.-or-index is greater than ThreadSafeList&lt;T&gt;.Count.</exception>
        public void RemoveAt(int index)
        {
            ThreadSafeWrapper(() => _internalList.RemoveAt(index));
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index is less than 0.-or-index is greater than ThreadSafeList&lt;T&gt;.Count.</exception>
        public T this[int index]
        {
            get
            {
                T tmp = default(T);
                ThreadSafeWrapper(() => tmp = _internalList[index]);
                return tmp;
            }
            set
            {
                ThreadSafeWrapper(() => _internalList[index] = value);
            }
        }

        /// <summary>
        /// Adds an object to the end of the ThreadSafeList&lt;T&gt;.
        /// </summary>
        /// <param name="item">The object to be added to the end of the ThreadSafeList&lt;T&gt;.
        /// The value can be null for reference types.</param>
        public void Add(T item)
        {
            ThreadSafeWrapper(() => _internalList.Add(item));
        }

        /// <summary>
        /// Removes all elements from the ThreadSafeList&lt;T&gt;.
        /// </summary>
        public void Clear()
        {
            ThreadSafeWrapper(() => _internalList.Clear());
        }

        /// <summary>
        /// Determines whether an element is in the ThreadSafeList&lt;T&gt;.
        /// </summary>
        /// <param name="item">The object to locate in the ThreadSafeList&lt;T&gt;. The value
        /// can be null for reference types.</param>
        /// <returns>true if item is found in the ThreadSafeList&lt;T&gt;; otherwise,
        /// false.</returns>
        public bool Contains(T item)
        {
            bool tmp = false;
            ThreadSafeWrapper(() => tmp = _internalList.Contains(item));
            return tmp;
        }

        /// <summary>
        /// Copies the entire ThreadSafeList&lt;T&gt; to a compatible one-dimensional
        /// array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements
        /// copied from ThreadSafeList&lt;T&gt;. The System.Array must have
        /// zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="System.ArgumentNullException">array is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        /// <exception cref="System.ArgumentException">The number of elements in the source ThreadSafeList&lt;T&gt; is
        /// greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            // Check because we need to use properties on the array below
            if (array == null )
            {
                throw new ArgumentNullException();
            }

            // If the array supports syncronization we'll be polite,
            // otherwise all bets are off
            if (array.IsSynchronized)
            {
                bool lockTaken = false;
                try
                {
                    // Have timeout in case of deadlock
                    Monitor.TryEnter(array.SyncRoot, 5000, ref lockTaken);

                    if (lockTaken)
                    {
                        ThreadSafeWrapper(() => _internalList.CopyTo(array, arrayIndex));   
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(array.SyncRoot);
                    }
                }                
            }
            else
            {
                ThreadSafeWrapper(() => _internalList.CopyTo(array, arrayIndex));
            }
        }

        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        public int Count
        {
            get
            {
                int tmp = 0;
                ThreadSafeWrapper(() => tmp = _internalList.Count());
                return tmp;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ThreadSafeList&lt;T&gt; is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the ThreadSafeList&lt;T&gt;.
        /// </summary>
        /// <param name="item">The object to remove from the ThreadSafeList&lt;T&gt;. The value
        /// can be null for reference types.</param>
        /// <returns>true if item is successfully removed; otherwise, false. This method also
        /// returns false if item was not found in the ThreadSafeList&lt;T&gt;.</returns>
        public bool Remove(T item)
        {
            bool tmp = false;
            ThreadSafeWrapper(() => tmp = _internalList.Remove(item));
            return tmp;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the ThreadSafeList&lt;T&gt;.
        /// <para>
        /// The default implmentation of this method allows the caller to enumerate over
        /// a snapshot of the ThreadSafeList&lt;T&gt; items at the time that the enumerator
        /// was obtained. This is a memory intensive operation so it was left as virtual in
        /// case you want to do a shallow clone. The deciscion to implement it this way was made for
        /// several reasons, namely:
        /// 
        ///   - Though a foreach loop can't modify the collection (a compile time error), it
        ///   can modify properties of the members within the collection and that is definitely not
        ///   thread safe.  So rather than confuse consumers of this class with the word "ThreadSafe"
        ///   we just create a DeepClone of the internal list for consumers to iterate over since they
        ///   shouldn't be modifying the collection anyway.
        ///   
        ///   - It's how Microsoft did it.  So it's probably best to conform to a known API so that consumers
        ///   get the performance they have come to expect from other classes in the Base Class Library. For
        ///   example, if you look ConcurrentDictionary&lt;TKey, TValue>&gt; .Keys and .Values properties it
        ///   appears they are snapshots at the time the Enumerator was obtained.
        ///   
        /// You might think that you could just cast the list to a ReadOnly collection and then grab that enumerator
        /// but you still run into the first issue that people can modify the property values of members within the
        /// collection, thus ruining your thread safety.  The other option would be to just lock the collection until
        /// the Enumerating is complete, but that's an even worse idea (just asking for deadlock) since there is no
        /// guarantee the enumeration will ever occur so this seems to be the lesser of many evils for now.
        /// </para>
        /// <para>
        /// If you want to change the default behaviour to a shallow copy (not recommended for all the
        /// reasons above and more) you can override the CloneInternalList method of this class and
        /// use the ShallowCloneInternalList method instead, roll your own clone method, or just
        /// override this method.
        /// </para>
        /// </summary>
        /// <returns>A ThreadSafeList&lt;T&gt;.Enumerator for the ThreadSafeList&lt;T&gt;.</returns>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return this.CloneInternalList().GetEnumerator();
        }

        /// <summary>
        /// Allows older libraries that you might not have access to which were compiled
        /// against .NET 1.X to iterate over this class.
        /// </summary>
        /// <returns>A System.Collections.IEnumerator for the ThreadSafeList&lt;T&gt;.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion IList<T>
    }

    /// <summary>
    /// Use this collection when you need a ThreadSafeList&lt;T&gt;, don't want to hog memory
    /// to accomplish it, and are ok if invalid operations result in partially/incompletely modified
    /// colleciton members.
    /// <para>
    /// This class exposes all the standard IList&lt;T&gt; operations on the collection, but if an error
    /// is thrown the data "could" potetially be in undefined or corrupt state.  I'm pretty sure that's
    /// the same behaviour as for the .NET implementation of List&lt;T&gt; so don't be too alarmed.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of objects to store in the ThreadSafeList</typeparam>
    public sealed class PerformanceThreadSafeList<T> : ThreadSafeList<T>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the PerformanceThreadSafeList&lt;T&gt; class
        /// that is empty and has the default initial capacity.
        /// </summary>
        public PerformanceThreadSafeList() : base() { }

        /// <summary>
        /// Initializes a new instance of the PerformanceThreadSafeList&lt;T&gt; class
        /// that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">capacity is less than 0</exception>
        public PerformanceThreadSafeList(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes a new instance of the PerformanceThreadSafeList&lt;T&gt; class
        /// that contains elements copied from the specified collection and has sufficient
        /// capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <exception cref="System.ArgumentNullException">collection is null</exception>
        public PerformanceThreadSafeList(IEnumerable<T> collection) : base(collection) { }

        #endregion Constructors

        /// <summary>
        /// Locks via the standard MS pattern so there is no worry of a deadlock here, but if
        /// an operation throws an exception there is the very realy possibility that your data
        /// is left in an undefined or corrupt state.
        /// </summary>
        /// <param name="operation">The action to perform in a threadsafe fashion</param>
        protected override void ThreadSafeWrapper(Action operation)
        {
            // Lock on the lock object
            lock (_lock)
            {
                // Do the operation
                operation();
            }
        }
    }

    /// <summary>
    /// Use this collection when you need a ThreadSafeList&lt;T&gt; AND data
    /// integrity is paramount.
    /// <para>
    /// For standard lock operations the framework ensures that a deadlock will
    /// not occur by making sure you release the lock object, but it could leave
    /// the date in the List in an undefined or corrupt state. This class mitigates
    /// that by creating a (deep) copy of the data held in the list before performing
    /// operations that could potentially throw errors.  If an error is caught, the
    /// collection is reverted to it's original state (the state it was in before the
    /// operation).
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of objects to store in the ThreadSafeList</typeparam>
    public sealed class DataFidelityThreadSafeList<T> : ThreadSafeList<T>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the DataFidelityThreadSafeList&lt;T&gt; class
        /// that is empty and has the default initial capacity.
        /// </summary>
        public DataFidelityThreadSafeList() : base() { }

        /// <summary>
        /// Initializes a new instance of the DataFidelityThreadSafeList&lt;T&gt; class
        /// that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">capacity is less than 0</exception>
        public DataFidelityThreadSafeList(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes a new instance of the DataFidelityThreadSafeList&lt;T&gt; class
        /// that contains elements copied from the specified collection and has sufficient
        /// capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <exception cref="System.ArgumentNullException">collection is null</exception>
        public DataFidelityThreadSafeList(IEnumerable<T> collection) : base(collection) { }

        #endregion Constructors

        /// <summary>
        /// Locks via the standard MS pattern but everytime this method is called a temporary
        /// copy of your list will be created.  It implements a rollback in case of any error
        /// so that your collection isn't left in an undefined or corrupt state, but at the
        /// expense of both processor and memory.
        /// </summary>
        /// <param name="operation">The action to perform in a threadsafe fashion</param>
        protected override void ThreadSafeWrapper(Action operation)
        {
            // Copy collection
            var oldCopy = this.DeepCloneInternalList();

            // Obtain the lock
            Monitor.Enter(_lock);

            try
            {
                // Do the operation
                operation();
            }

            // Pokemon Exception Handler... (gotta catch 'em all!!!)
            catch
            {
                // On any errors we need to rollback the collection to it's
                // original state
                _internalList = oldCopy;

                // Rethrow the original error, preserving the stack trace
                throw;
            }
            finally
            {
                // Release the lock
                Monitor.Exit(_lock);
            }
        }
    }
}
