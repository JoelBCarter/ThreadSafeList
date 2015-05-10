using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSafeList
{
    public class ThreadSafeEnumerator<T> : IEnumerator<T>
    {
        IEnumerator<T> _enumerator;
        ReaderWriterLockSlim _readLockObj;

        public ThreadSafeEnumerator(IEnumerator<T> enumerator, ReaderWriterLockSlim readLockObj)
        {
            _enumerator = enumerator;
            _readLockObj = readLockObj;
            _readLockObj.EnterReadLock();
        }

        #region IDisposable

        public void Dispose()
        {
            _readLockObj.ExitReadLock();
        }

        #endregion IDisposable

        #region IEnumerable<T>

        public T Current
        {
            get { return _enumerator.Current; }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return this.Current; }
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        #endregion IEnumerable<T>
    }
}