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
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AK.Homepage.Blog
{
    public class BlogRepository
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly HttpClient _blogListHttpClient;
        private readonly string _blogContentBaseAddress;
        private readonly PostInfoUrlManager _postInfoUrlManager;
        private readonly BlogContentExtractor _blogContentExtractor;

        public BlogRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory,
            PostInfoUrlManager postInfoUrlManager,
            BlogContentExtractor blogContentExtractor)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<BlogRepository>();
            _blogContentBaseAddress = configuration["BlogContentBaseAddress"];
            _blogListHttpClient = httpClientFactory.CreateClient("BlogList");
            _postInfoUrlManager = postInfoUrlManager;
            _blogContentExtractor = blogContentExtractor;
        }

        public async Task<PostInfo[]> GetAllPostInfos()
        {
            _logger.LogTrace("Getting PostInfo list for all blog posts...");

            var response = await _blogListHttpClient.GetStringAsync(string.Empty);
            _logger.LogDebug("Response from GitHub: {response}", response);

            var directories = JsonConvert.DeserializeObject<GithubItem[]>(response).Where(x => x.Type == "dir");
            var postInfos = new List<PostInfo>();
            foreach (var directory in directories)
            {
                response = await _blogListHttpClient.GetStringAsync(directory.Url);

#pragma warning disable CS8620
				postInfos.AddRange(JsonConvert.DeserializeObject<GithubItem[]>(response)
                    .Where(x => x.Type == "file")
                    .Where(x => x.DownloadUrl.StartsWith(_blogContentBaseAddress))
                    .Select(x => x.DownloadUrl.Substring(_blogContentBaseAddress.Length /* + 1*/))
                    .Select(_postInfoUrlManager.UrlToPostInfo)
                    .Where(x => x != null));
#pragma warning restore CS8620
			}

            for (var i = 0; i < postInfos.Count; i++)
            {
                if (i > 0 && postInfos[i - 1].Category == postInfos[i].Category)
                    postInfos[i].Previous = new PostLink {Slug = postInfos[i - 1].Slug, Title = postInfos[i - 1].Title};
                if (i < postInfos.Count - 1 && postInfos[i + 1].Category == postInfos[i].Category)
                    postInfos[i].Next = new PostLink {Slug = postInfos[i + 1].Slug, Title = postInfos[i + 1].Title};
            }

            return postInfos.OrderBy(x => x.Category).ThenByDescending(x => x.PublishedDate).ToArray();
        }

        public async Task<Post> GetPost(PostInfo postInfo, PostInfo[] allPostInfos)
        {
            var slug = postInfo.Slug;
            _logger.LogTrace("Getting post with slug {slug}...", slug);

            var post = new Post
            {
                Category = postInfo.Category,
                Next = postInfo.Next,
                Previous = postInfo.Previous,
                PublishedDate = postInfo.PublishedDate,
                Slug = postInfo.Slug,
                Tags = postInfo.Tags,
                Title = postInfo.Title
            };
            var url = _postInfoUrlManager.PostInfoToUrl(post);
            var (contentHtml, blurb) = await _blogContentExtractor.ExtractContentHtmlAndBlurb(url);

            post.ContentHtml = contentHtml;
            post.Blurb = blurb;

            for (var i = 0; i < allPostInfos.Length; i++)
            {
                if (post.Slug != allPostInfos[i].Slug) continue;
                if (i > 0 && allPostInfos[i - 1].Category == post.Category)
                {
                    post.Previous = new PostLink
                    {
                        Slug = allPostInfos[i - 1].Slug,
                        Title = allPostInfos[i - 1].Title
                    };
                }
                if (i < allPostInfos.Length - 1 && allPostInfos[i + 1].Category == post.Category)
                {
                    post.Next = new PostLink
                    {
                        Slug = allPostInfos[i + 1].Slug,
                        Title = allPostInfos[i + 1].Title
                    };
                }
                break;
            }

            return post;
        }

        public IDictionary<string, PostCache> GetAllPostsKeyedBySlug(PostInfo[] postInfos)
        {
            _logger.LogTrace("Getting all posts cached and keyed by slugs...");

            var sortedPostInfos = postInfos.OrderBy(x => x.Category).ThenBy(x => x.PublishedDate).ToArray();
            return postInfos.ToDictionary(x => x.Slug,
                x => new PostCache(() => GetPost(x, sortedPostInfos), _loggerFactory));
        }

        private class GithubItem
        {
	        [JsonProperty("url")] public string Url { get; set; } = string.Empty;
	        [JsonProperty("type")] public string Type { get; set; } = string.Empty;
	        [JsonProperty("download_url")] public string DownloadUrl { get; set; } = string.Empty;
        }
    }
}