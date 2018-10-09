/*******************************************************************************************************************************
 * Copyright © 2018 Aashish Koirala <https://www.aashishkoirala.com>
 * 
 * This file is part of Aashish Koirala's Personal Website and Blog (AKPWB).
 *  
 * AKPWB is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Listor is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with AKPWB.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *******************************************************************************************************************************/

using System;
using System.Threading;

namespace AK.Homepage
{
    public static class LockManager
    {
        public static TimeSpan ReadLockTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public static TimeSpan WriteLockTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public static IDisposable LockRead(this ReaderWriterLockSlim lockObject,
            bool throwIfLockNotAcquiredInTime = false, string errorMessageIfThrown = null)
        {
            if (lockObject.TryEnterReadLock(ReadLockTimeout)) return new Lock(lockObject, false);
            HandleLockTimeout(throwIfLockNotAcquiredInTime, errorMessageIfThrown);
            return null;
        }

        public static IDisposable LockWrite(this ReaderWriterLockSlim lockObject,
            bool throwIfLockNotAcquiredInTime = false, string errorMessageIfThrown = null)
        {
            if (lockObject.TryEnterWriteLock(WriteLockTimeout)) return new Lock(lockObject, true);
            HandleLockTimeout(throwIfLockNotAcquiredInTime, errorMessageIfThrown);
            return null;
        }

        private static void HandleLockTimeout(bool throwIfLockNotAcquiredInTime, string errorMessageIfThrown)
        {
            if (!throwIfLockNotAcquiredInTime) return;
            if (string.IsNullOrWhiteSpace(errorMessageIfThrown)) errorMessageIfThrown = "Could not acquire lock.";
            throw new Exception(errorMessageIfThrown);
        }

        private class Lock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;
            private readonly bool _isWrite;
            private bool _isDisposed;

            public Lock(ReaderWriterLockSlim lockObject, bool isWrite)
            {
                _lock = lockObject;
                _isWrite = isWrite;
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                if (_isWrite) _lock.ExitWriteLock();
                else _lock.ExitReadLock();
                _isDisposed = true;
            }
        }
    }
}