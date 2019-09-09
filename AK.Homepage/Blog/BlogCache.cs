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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AK.Homepage.Blog
{
    public class BlogCache
    {
        private static Task<Cache> _cacheTask;
        private static readonly ReaderWriterLockSlim _cacheTaskLock = new ReaderWriterLockSlim();
        private readonly int _homeLinkCount;
        private readonly ILogger _logger;
        private readonly BlogRepository _blogRepository;
        private readonly AccessKeyValidator _accessKeyValidator;

        public BlogCache(IConfiguration config, ILoggerFactory loggerFactory,
            BlogRepository blogRepository, AccessKeyValidator accessKeyValidator)
        {
            _homeLinkCount = int.TryParse(config["HomeLinkCount"], out int c) ? c : 10;
            _logger = loggerFactory.CreateLogger<BlogCache>();
            _blogRepository = blogRepository;
            _accessKeyValidator = accessKeyValidator;
        }

        public async Task<(PostLink[], PostLink[])> GetHomePostLinks()
        {
            _logger.LogTrace("Getting blog post links for homepage...");

            var cache = await InitializeCacheIfNeeded();
            return cache == null
                ? (new PostLink[0], new PostLink[0])
                : (cache.HomeTechPostLinks, cache.HomeNonTechPostLinks);
        }

        public async Task<PostLink[]> GetAllLinks(Category category, string slugToExclude = null, int top = 0)
        {
            _logger.LogTrace(
                "Getting top {top} (0 = all) blog post links for category {category} excluding {slugToExclude}...", top,
                category, slugToExclude);

            var cache = await InitializeCacheIfNeeded();
            if (cache == null) return new PostLink[0];

            if (!cache.PostLinksByCategory.TryGetValue(category, out PostLink[] links)) links = new PostLink[0];
            if (slugToExclude != null) links = links.Where(x => x.Slug != slugToExclude).ToArray();
            if (top > 0) links = links.Take(top).ToArray();

            return links;
        }

        public async Task<Post> GetPost(string slug)
        {
            _logger.LogTrace("Getting blog post with slug {slug}...", slug);

            var cache = await InitializeCacheIfNeeded();
            var postCache = cache == null ? null : (cache.PostsBySlug.TryGetValue(slug, out PostCache p) ? p : null);
            if (postCache == null) return null;
            return await postCache;
        }

        public async Task Reload(string accessKey)
        {
            _accessKeyValidator.Validate(accessKey);

            _logger.LogTrace("Reloading all blog posts...");

            var cacheTask = CacheTask;
            Cache cache = null;
            try
            {
                cache = await cacheTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving current cache to clear.");
            }
            var postCachesToDispose = cache?.PostsBySlug.Values.ToArray() ?? new PostCache[0];

            cacheTask = InitializeCache();
            HttpCache.Clear();
            CacheTask = cacheTask;
            try
            {
                await cacheTask;
            }
            catch (Exception ex)
            {
                CacheTask = null;
                _logger.LogError(ex, "Error while reloading all blog posts");
                throw;
            }
            finally
            {
                foreach (var postCache in postCachesToDispose) postCache.Dispose();
            }
        }

        public async Task Reload(string accessKey, string slug)
        {
            _accessKeyValidator.Validate(accessKey);

            _logger.LogTrace("Reloading blog post with slug {slug}...", slug);

            var cache = await InitializeCacheIfNeeded();
            var postCache = cache == null ? null : (cache.PostsBySlug.TryGetValue(slug, out PostCache p) ? p : null);
            if (postCache == null)
            {
                _logger.LogWarning("Could not find post with slug {slug} to reload.", slug);
                return;
            }
            HttpCache.Clear(x =>
                x == "/" || x == "/sitemap" || x.StartsWith("/rss") ||
                x.StartsWith("/blog") && !x.EndsWith(".png") ||
                x.StartsWith("/blog/") && x.Contains($"{slug}"));
            postCache.Reset();
        }

        private async Task<Cache> InitializeCacheIfNeeded()
        {
            var cacheTask = CacheTask;
            try
            {
                if (cacheTask != null) return await cacheTask;
            }
            catch (Exception ex)
            {
                CacheTask = null;
                _logger.LogError(ex, "Error while initializing cache.");
                throw;
            }

            cacheTask = InitializeCache();
            CacheTask = cacheTask;
            try
            {
                return await cacheTask;
            }
            catch (Exception ex)
            {
                CacheTask = null;
                _logger.LogError(ex, "Error while initializing cache.");
                throw;
            }
        }

        private async Task<Cache> InitializeCache()
        {
            _logger.LogTrace("Initializing cache...");

            var postInfos = await _blogRepository.GetAllPostInfos();
            var postsBySlug = _blogRepository.GetAllPostsKeyedBySlug(postInfos);

            var cache = new Cache
            {
                PostsBySlug = postsBySlug,
                PostLinksByCategory = postInfos
                    .GroupBy(x => x.Category)
                    .ToDictionary(x => x.Key, x => x.Cast<PostLink>().ToArray())
            };
            cache.HomeTechPostLinks = GetHomePostLinks(cache, Category.Tech);
            cache.HomeNonTechPostLinks = GetHomePostLinks(cache, Category.NonTech);

            return cache;
        }

        private PostLink[] GetHomePostLinks(Cache cache, Category category)
        {
            var links = cache.PostLinksByCategory.TryGetValue(category, out PostLink[] l)
                ? l.Take(_homeLinkCount).ToArray()
                : new PostLink[0];

            if (links.Length < _homeLinkCount)
            {
                links = links
                    .Concat(Enumerable.Range(0, _homeLinkCount - links.Length).Select(x => new PostLink()))
                    .ToArray();
            }

            return links;
        }

        private static Task<Cache> CacheTask
        {
            get
            {
                using (_cacheTaskLock.LockRead(true, "Cannot get read lock on CacheTaskLock."))
                {
                    return _cacheTask;
                }
            }
            set
            {
                using (_cacheTaskLock.LockWrite(true, "Cannot get write lock on CacheTaskLock."))
                {
                    _cacheTask = value;
                }
            }
        }

        private class Cache
        {
            public IDictionary<string, PostCache> PostsBySlug { get; set; }
            public IDictionary<Category, PostLink[]> PostLinksByCategory { get; set; }
            public PostLink[] HomeTechPostLinks { get; set; }
            public PostLink[] HomeNonTechPostLinks { get; set; }
        }
    }
}