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
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AK.Homepage.Blog
{
    public class BlogImageServer
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly BlogContentExtractor _blogContentExtractor;

        public BlogImageServer(RequestDelegate next,
            ILoggerFactory loggerFactory, BlogContentExtractor blogContentExtractor)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<BlogImageServer>();
            _blogContentExtractor = blogContentExtractor;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method != "GET" || !context.Request.Path.HasValue ||
                !context.Request.Path.Value.StartsWith("/blog") ||
                !context.Request.Path.Value.EndsWith(".png"))
            {
                await _next.Invoke(context);
                return;
            }

            var path = context.Request.Path.Value.Replace("/blog/", string.Empty);
            _logger.LogInformation("Fetching blog image from {path}...", path);

            var data = await _blogContentExtractor.ExtractAsset(path);
            await context.Response.Body.WriteAsync(data, 0, data.Length);
        }
    }
}