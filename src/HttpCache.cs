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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AK.Homepage
{
    public class HttpCache
    {
        private static readonly IDictionary<(string, bool), Entry> _cache = new Dictionary<(string, bool), Entry>();
        private static readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private readonly RequestDelegate _next;
        private readonly string[] _pathEqualsList;
        private readonly string[] _pathStartsWithList;
        private readonly ILogger _logger;

        public HttpCache(RequestDelegate next, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _next = next;
            _pathEqualsList = configuration["HttpCachePathEqualsList"].Split(',');
            _pathStartsWithList = configuration["HttpCachePathStartsWithList"].Split(',');
            _logger = loggerFactory.CreateLogger<HttpCache>();
        }

        public static void Clear(Func<string, bool>? pathPredicate = null)
        {
	        using var _ = _cacheLock.LockWrite(true, "Cannot acquire write lock on HttpCache.");
	        if (pathPredicate == null)
	        {
		        _cache.Clear();
		        return;
	        }
	        var keys = _cache.Keys.Where(x => pathPredicate(x.Item1)).ToArray();
	        foreach (var key in keys) _cache.Remove(key);
        }

        public static async Task<(string, bool, string?)[]> Warm(string baseAddress, string[] relativeUrls)
        {
            // "Warming the cache" entails making requests to all provided URLs. The schema for the tuples
            // that are returned: (url, isSuccess, errorMessageIfAny).

            var returnValue = new List<(string, bool, string?)>();
            using (var httpClient = new HttpClient {BaseAddress = new Uri(baseAddress)})
            {
                var tasks = new Dictionary<string, Task>();
                foreach (var url in relativeUrls)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    tasks[url] = httpClient.SendAsync(request);
                }
                foreach (var (key, value) in tasks)
                {
                    try
                    {
                        await value;
                        returnValue.Add((key, true, null));
                    }
                    catch (Exception ex)
                    {
                        returnValue.Add((key, false, ex.Message));
                    }
                }
            }
            return returnValue.ToArray();
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method != "GET" || CanSkip(context.Request.Path))
            {
                await _next.Invoke(context);
                return;
            }

            var path = context.Request.Path.Value;
            if (context.Request.QueryString.HasValue) path += context.Request.QueryString.Value;
            var isGZipCompressed = context.Request.Headers["Accept-Encoding"].Contains("gzip");
			if (context.Request.Cookies.TryGetValue("AK-DarkMode", out var darkMode) && darkMode == "True") path += "|Dark";
            var key = (path, isGZipCompressed);

            _logger.LogTrace("Looking for resource in HttpCache: path = {path}, isGZipCompressed = {isGZipCompressed}",
                path, isGZipCompressed);

            Entry? entry = null;
            using (var locked = _cacheLock.LockRead())
            {
                if (locked != null) _cache.TryGetValue(key, out entry);
                else _logger.LogWarning("Could not acquire read lock on HttpCache while looking up {path}.", path);
            }

            var skipCaching = false;
            if (entry == null)
            {
                _logger.LogTrace(
                    "Did not find in HttpCache, will fetch and write: path = {path}, isGZipCompressed = {isGZipCompressed}",
                    path, isGZipCompressed);

                entry = await CreateEntry(context);

                // If this header is set, the upstream is telling me not to cache probably because it is an error output.
                if (entry.Headers.ContainsKey("X-AK-SkipCache")) skipCaching = true;
                else WriteEntry(key, entry);
            }

            if (!skipCaching &&
                context.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag) &&
                entry.ETag == etag)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.ContentLength = 0;
                return;
            }

            _logger.LogTrace("Serving from HttpCache: path = {path}, isGZipCompressed = {isGZipCompressed}",
                path, isGZipCompressed);

            foreach (var header in entry.Headers) context.Response.Headers[header.Key] = header.Value;
            context.Response.Headers[HeaderNames.ETag] = entry.ETag;
            await context.Response.Body.WriteAsync(entry.Data, 0, entry.Data.Length);
        }

        private bool CanSkip(PathString path)
        {
            if (!path.HasValue) return true;
            var value = path.Value;
            return _pathEqualsList.All(x => x != value) && !_pathStartsWithList.Any(x => value.StartsWith(x));
        }

        private async Task<Entry> CreateEntry(HttpContext context)
        {
            // Creating the entry entails switching out the response stream so that we consume the response instead,
            // then run the pipeline, cache the output, and put the response stream back where it was.

            var entry = new Entry();
            var originalStream = context.Response.Body;
            await using (var memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;
                await _next.Invoke(context);
                entry.Data = memoryStream.ToArray();
            }
            context.Response.Body = originalStream;
            entry.Headers = context.Response.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray());
            using (var sha1 = SHA1.Create())
            {
                var checksum = sha1.ComputeHash(entry.Data);
                entry.ETag = $"\"{WebEncoders.Base64UrlEncode(checksum)}\"";
            }
            return entry;
        }

        private static void WriteEntry((string, bool) key, Entry entry)
        {
	        using var locked = _cacheLock.LockWrite();
	        if (locked != null && !_cache.ContainsKey(key)) _cache[key] = entry;
        }

        private class Entry
        {
            public IDictionary<string, string[]> Headers { get; set; } = new Dictionary<string, string[]>();
            public byte[] Data { get; set; } = new byte[0];
            public string ETag { get; set; } = string.Empty;
        }
    }
}