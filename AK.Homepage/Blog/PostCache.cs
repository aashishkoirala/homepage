/*******************************************************************************************************************************
 * Copyright © 2018-2019 Aashish Koirala <https://www.aashishkoirala.com>
 * 
 * This file is part of Aashish Koirala's Personal Website and Blog (AKPWB).
 *  
 * AKPWB is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * AKPWB is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with AKPWB.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *******************************************************************************************************************************/

using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AK.Homepage.Blog
{
    public class PostCache : IDisposable
    {
        private readonly Func<Task<Post>> _postTaskFunc;
        private readonly ReaderWriterLockSlim _taskLock = new ReaderWriterLockSlim();
        private Task<Post> _postTask;
        private readonly ILogger _logger;

        public PostCache(Func<Task<Post>> postTaskFunc, ILoggerFactory loggerFactory)
        {
            _postTaskFunc = postTaskFunc;
            _postTask = postTaskFunc();
            _logger = loggerFactory.CreateLogger<PostCache>();
        }

        public void Dispose() => _taskLock.Dispose();

        private Task<Post> PostTask
        {
            get
            {
                using (_taskLock.LockRead(true, "Cannot acquire read lock on task."))
                {
                    return _postTask;
                }
            }
            set
            {
                using (_taskLock.LockWrite(true, "Cannot acquire write lock on task."))
                {
                    _postTask = value;
                }
            }
        }

        private async Task<Post> Retrieve()
        {
            try
            {
                return await PostTask;
            }
            catch (Exception ex)
            {
                PostTask = _postTaskFunc();
                _logger.LogError(ex, "Error while fetching post.");
                throw;
            }
        }

        public void Reset() => PostTask = _postTaskFunc();

        public TaskAwaiter<Post> GetAwaiter() => Retrieve().GetAwaiter();
    }
}