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

using AK.Homepage.Blog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AK.Homepage
{
    [ServiceFilter(typeof(LogActionAndHandleErrorFilter))]
    public class HomeController : Controller
    {
        private static readonly Post NotFoundPost = CreateNotFoundPost();
        private readonly BlogCache _blogCache;
        private readonly ProfileRepository _profileRepository;
        private readonly AccessKeyValidator _accessKeyValidator;
        private readonly MetadataGenerator _metadataGenerator;

        public HomeController(BlogCache blogCache, ProfileRepository profileRepository,
            AccessKeyValidator accessKeyValidator, MetadataGenerator metadataGenerator)
        {
            _blogCache = blogCache;
            _profileRepository = profileRepository;
            _accessKeyValidator = accessKeyValidator;
            _metadataGenerator = metadataGenerator;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (techPostLinks, nonTechPostLinks) = await _blogCache.GetHomePostLinks();
            var profile = await _profileRepository.GetProfile();

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return View(new HomeViewModel
            {
                TechPostLinks = techPostLinks,
                NonTechPostLinks = nonTechPostLinks,
                ContactLinks = profile.ContactLinks,
                ProjectLinks = profile.ProjectLinks
            });
        }

        [HttpGet("blog/{slug}")]
        public async Task<IActionResult> Blog(string slug)
        {
            var post = await _blogCache.GetPost(slug);
            if (post == null) return BlogNotFound();

            var sideLinks = await _blogCache.GetAllLinks(post.Category, post.Slug, 15);
            var model = new BlogViewModel {Category = post.Category, SideLinks = sideLinks, Post = post};

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return View(model);
        }

        [HttpGet("blog")]
        public async Task<IActionResult> BlogList(string type)
        {
            Category category;
            if (type.Equals("tech", StringComparison.CurrentCultureIgnoreCase)) category = Category.Tech;
            else if (type.Equals("nontech", StringComparison.CurrentCultureIgnoreCase)) category = Category.NonTech;
            else return BlogNotFound();
            var mainLinks = await _blogCache.GetAllLinks(category);

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return View("Blog", new BlogViewModel {Category = category, MainLinks = mainLinks});
        }

        [HttpGet("about")]
        public async Task<IActionResult> About()
        {
            var post = await _blogCache.GetPost("about-me");

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return post == null
                ? BlogNotFound()
                : View("Blog", new BlogViewModel {Category = post.Category, Post = post});
        }

        [HttpGet("resume")]
        public async Task<IActionResult> Resume()
        {
            var resume = (await _profileRepository.GetProfile()).ProfessionalResume;

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return View(resume);
        }

        [HttpGet("reload/{accessKey}/{slug}")]
        public async Task<IActionResult> Reload(string accessKey, string slug)
        {
            var task = slug == "ALL" ? _blogCache.Reload(accessKey) : _blogCache.Reload(accessKey, slug);
            await task;

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return slug == "ALL" ? RedirectToAction("Index") : RedirectToAction("Blog", new {slug});
        }

        [HttpGet("sitemap")]
        public async Task<IActionResult> Sitemap()
        {
            var siteMapXml = await _metadataGenerator.GetSiteMapXml($"{Request.Scheme}://{Request.Host}");

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return Content(siteMapXml, "text/xml", Encoding.UTF8);
        }

        [HttpGet("warmcache/{accessKey}")]
        public async Task<IActionResult> WarmCache(string accessKey)
        {
            _accessKeyValidator.Validate(accessKey);

            var urls = await _metadataGenerator.GetAllUrls();
            var result = await HttpCache.Warm($"{Request.Scheme}://{Request.Host}", urls);

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return Json(result.Select(x => new {Path = x.Item1, Success = x.Item2, Error = x.Item3}).ToArray());
        }

        [HttpGet("rss/{type}")]
        public async Task<IActionResult> Rss(string type)
        {
            Category category;
            if (type.Equals("tech", StringComparison.CurrentCultureIgnoreCase)) category = Category.Tech;
            else if (type.Equals("nontech", StringComparison.CurrentCultureIgnoreCase)) category = Category.NonTech;
            else return BlogNotFound();

            var rssXml = await _metadataGenerator.GetRssXml(category, $"{Request.Scheme}://{Request.Host}");

            Response.Headers[HeaderNames.CacheControl] = "no-cache";
            return Content(rssXml, "text/xml", Encoding.UTF8);
        }

        private IActionResult BlogNotFound()
        {
            // If a blog post is not found, we render a blog post page with the "not found" message
            // rendered as the post.

            var post = NotFoundPost;
            Response.Headers["X-AK-SkipCache"] = "1";
            return View("Blog", new BlogViewModel {Category = post.Category, Post = post, PreventIndexing = true});
        }

        private static Post CreateNotFoundPost() => new Post
        {
            Category = Category.Meta,
            Title = "Can't find that entry!",
            ContentHtml = "The entry you are looking for does not seem to exist. Maybe you mistyped it, or " +
                          "maybe I moved it. If you feel this is a real problem, please be sure to let me know."
        };
    }
}