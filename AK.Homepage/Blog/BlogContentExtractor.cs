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

using CommonMark;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AK.Homepage.Blog
{
    public class BlogContentExtractor
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public BlogContentExtractor(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _logger = loggerFactory.CreateLogger<BlogContentExtractor>();
            _httpClient = httpClientFactory.CreateClient("BlogContent");
        }

        public async Task<(string, string)> ExtractContentHtmlAndBlurb(string relativeMarkdownUrl)
        {
            _logger.LogTrace("Extracting content for path {relativeMarkdownUrl}", relativeMarkdownUrl);

            var markdown = await _httpClient.GetStringAsync($"{relativeMarkdownUrl}?v={DateTime.UtcNow.Ticks}");
            var contentHtml = CommonMarkConverter.Convert(markdown);
            var blurb = ConvertMarkdownToPlainText(markdown);
            blurb = blurb.Substring(0, Math.Min(500, blurb.Length));
            var lastIndexOfSpace = blurb.LastIndexOf(' ');
            if (lastIndexOfSpace >= 0) blurb = blurb.Substring(0, lastIndexOfSpace) + "...";
            return (contentHtml, blurb);
        }

        public async Task<byte[]> ExtractAsset(string relativeUrl)
        {
            _logger.LogTrace("Extracting asset at {relativeUrl}", relativeUrl);

            return await _httpClient.GetByteArrayAsync($"{relativeUrl}?v={DateTime.UtcNow.Ticks}");
        }

        private static string ConvertMarkdownToPlainText(string markdown)
        {
            // This is a port+simplification of https://github.com/stiang/remove-markdown.

            var output = Regex.Replace(markdown, "^(-\\s*?|\\*\\s*?|_\\s*?){3,}\\s*$m", "");
            output = Regex.Replace(output, "\\n={2,}", "\\n");
            output = Regex.Replace(output, "~{3}.*\\n", "");
            output = Regex.Replace(output, "~~", "");
            output = Regex.Replace(output, "`{3}.*\\n", "");
            output = Regex.Replace(output, "<[^>]*>", "");
            output = Regex.Replace(output, "^[=\\-]{2,}\\s*$", "");
            output = Regex.Replace(output, "\\[\\^.+?\\](\\: .*?$)?", "");
            output = Regex.Replace(output, "\\s{0,2}\\[.*?\\]: .*?$", "");
            output = Regex.Replace(output, "\\[(.*?)\\][\\[\\(].*?[\\]\\)]", "$1");
            output = Regex.Replace(output, "^\\s{0,3}>\\s?", "");
            output = Regex.Replace(output, "^\\s{1,2}\\[(.*?)\\]: (\\S+)( \".*? \")?\\s*$", "");
            output = Regex.Replace(output, "^(\\n)?\\s{0,}#{1,6}\\s+| {0,}(\\n)?\\s{0,}#{0,} {0,}(\\n)?\\s{0,}$",
                "$1$2$3");
            output = Regex.Replace(output, "([\\*_]{1,3})(\\S.*?\\S{0,1})\\1", "$2");
            output = Regex.Replace(output, "([\\*_]{1,3})(\\S.*?\\S{0,1})\\1", "$2");
            output = Regex.Replace(output, "(`{3,})(.*?)\\1", "$2");
            output = Regex.Replace(output, "`(.+?)`", "$1");
            return output;
        }
    }
}