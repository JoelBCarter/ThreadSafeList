using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSafeList
{
    /// <summary>
    /// An enumerator to compliment a threadsafe collection.  It locks
    /// the collection in readonly mode as it iterates through the members.
    /// </summary>
    /// <typeparam name="T">The type of objects to enumerate.This type parameter
    /// is covariant. That is, you can use either the type you specified or any
    /// type that is more derived. For more information about covariance and
    /// contravariance, see Covariance and Contravariance in Generics.</typeparam>
    public class ThreadSafeEnumerator<T> : IEnumerator<T>
    {
        /// <summary>
        /// The underlying enumerator for the collection
        /// </summary>
        IEnumerator<T> _enumerator;

        /// <summary>
        /// The threadsafe read object we'll use to control
        /// concurrent access to the collection
        /// </summary>
        ReaderWriterLockSlim _readLockObj;

        /// <summary>
        /// The constructor for the ThreadSafeEnumerator.
        /// </summary>
        /// <param name="enumerator">A (potentially non-threadsafe) enumerator
        /// for a collection.</param>
        /// <param name="readLockObj">The object to obtain a read lock from when
        /// iterating over the collection.</param>
        public ThreadSafeEnumerator(IEnumerator<T> enumerator, ReaderWriterLockSlim readLockObj)
        {
            _enumerator = enumerator;
            _readLockObj = readLockObj;

            // Don't take out read lock yet because we haven't tried
            // anything on the collection yet.  This limits our time spent
            // locking and minimizes our exposure to deadlock.
        }

        #region IDisposable

        /// <summary>
        ///  Performs application-defined tasks associated with freeing, releasing, or
        ///  resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // If we have a read lock taken out
            if (_readLockObj.IsReadLockHeld)
            {
                // Return it
                _readLockObj.ExitReadLock();
            }
        }

        #endregion IDisposable

        #region IEnumerator<T>

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public T Current
        {
            get
            {
                // If we don't have a read lock taken out
                if (!_readLockObj.IsReadLockHeld)
                {
                    // Take it out now
                    _readLockObj.EnterReadLock();
                }

                return _enumerator.Current;
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        object System.Collections.IEnumerator.Current
        {
            get { return this.Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the
        /// next element; false if the enumerator has passed the end of the
        /// collection.</returns>
        public bool MoveNext()
        {
            // If we don't have a read lock taken out
            if (!_readLockObj.IsReadLockHeld)
            {
                // Take it out now
                _readLockObj.EnterReadLock();
            }

            return _enumerator.MoveNext();
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before
        /// the first element in the collection.
        /// </summary>
        public void Reset()
        {
            // If we have a read lock taken out
            if (_readLockObj.IsReadLockHeld)
            {
                // Return it
                _readLockObj.ExitReadLock();
            }

            _enumerator.Reset();
        }

        #endregion IEnumerator<T>
    }
}