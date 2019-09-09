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
using System.Globalization;
using System.Linq;
using System.Web;

namespace AK.Homepage.Blog
{
    public class PostInfoUrlManager
    {
        private readonly string[] _slugifyRemoveCharacters;
        private readonly IDictionary<string, string> _slugifyReplacementMap;
        private readonly ILogger _logger;

        public PostInfoUrlManager(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _slugifyRemoveCharacters = configuration["SlugifyRemovalList"]
                .ToCharArray()
                .Select(x => x.ToString())
                .ToArray();

            _slugifyReplacementMap = configuration["SlugifyReplacementMap"]
                .Split('|')
                .Select(x => x.Split("->"))
                .ToDictionary(x => x[0], x => x[1]);

            _logger = loggerFactory.CreateLogger<PostInfoUrlManager>();
        }

        public PostInfo UrlToPostInfo(string relativeMarkdownUrl)
        {
            _logger.LogTrace("Converting path {relativeMarkdownUrl} to PostInfo...", relativeMarkdownUrl);

            if (!relativeMarkdownUrl.EndsWith(".md", StringComparison.CurrentCultureIgnoreCase)) return null;
            relativeMarkdownUrl = HttpUtility.UrlDecode(relativeMarkdownUrl);

            var parts = relativeMarkdownUrl.Substring(0, relativeMarkdownUrl.Length - 3).Split('/');
            if (parts.Length != 2) return null;

            if (!Enum.TryParse(parts[0], true, out Category category)) return null;

            var firstHyphen = parts[1].IndexOf('-');
            if (firstHyphen <= 0) return null;

            var publishedDatePart = parts[1].Substring(0, firstHyphen);
            if (!DateTimeOffset.TryParseExact(publishedDatePart, "yyyyMMdd",
                new DateTimeFormatInfo(), DateTimeStyles.AssumeUniversal, out DateTimeOffset publishedDate))
                return null;

            var restOfUrl = parts[1].Substring(firstHyphen + 1);
            parts = restOfUrl.Split('@');
            var title = parts[0];
            var tags = parts.Length > 1 ? parts[1].Split(',') : new string[0];
            title = title.Replace('_', ' ');
            var slug = Slugify(title);

            var postInfo = new PostInfo
            {
                Category = category,
                PublishedDate = publishedDate,
                Slug = slug,
                Title = title,
                Tags = tags
            };

            _logger.LogTrace("Successfully converted path {relativeMarkdownUrl} to PostInfo.", relativeMarkdownUrl);
            return postInfo;
        }

        public string PostInfoToUrl(PostInfo postInfo)
        {
            var tags = (postInfo.Tags?.Any() ?? false) ? $"@{string.Join(",", postInfo.Tags)}" : string.Empty;
            return
                HttpUtility.UrlEncode(
                    $"{postInfo.Category.ToString().ToLower()}/{postInfo.PublishedDate:yyyyMMdd}-{postInfo.Title.Replace(' ', '_')}{tags}.md");
        }

        private string Slugify(string title)
        {
            var slug = title;
            foreach (var pair in _slugifyReplacementMap) slug = slug.Replace(pair.Key, pair.Value);
            foreach (var s in _slugifyRemoveCharacters) slug = slug.Replace(s, string.Empty);
            slug = slug.Replace(' ', '-').Replace("--", "-");
            slug = slug.ToLower();

            _logger.LogTrace("Slugified title {title} as slug {slug}.", title, slug);
            return slug;
        }
    }
}