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

using AK.Homepage.Blog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace AK.Homepage
{
    public class MetadataGenerator
    {
        private readonly ILogger _logger;
        private readonly int _rssItemCount;
        private readonly BlogCache _blogCache;

        public MetadataGenerator(ILoggerFactory loggerFactory, IConfiguration config, BlogCache blogCache)
        {
            _logger = loggerFactory.CreateLogger<MetadataGenerator>();
            _rssItemCount = int.TryParse(config["RssItemCount"], out var c) ? c : 15;
            _blogCache = blogCache;
        }

        public async Task<string[]> GetAllUrls()
        {
            var urls = new List<string> {"/", "/about", "/blog?type=tech", "/blog?type=nontech", "/resume"};
            var techLinks = await _blogCache.GetAllLinks(Category.Tech);
            var nonTechLinks = await _blogCache.GetAllLinks(Category.NonTech);
            urls.AddRange(techLinks.Concat(nonTechLinks).Select(x => $"/blog/{x.Slug}"));
            return urls.ToArray();
        }

        public async Task<string> GetSiteMapXml(string baseAddress)
        {
            _logger.LogTrace("Generating sitemap XML...");

            var urls = await GetAllUrls();

            var xmlDocument = new XmlDocument();
            xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

            var root = CreateElement(xmlDocument, xmlDocument, "urlset", null,
                "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (var url in urls)
            {
                var urlNode = CreateElement(xmlDocument, root, "url");
                CreateElement(xmlDocument, urlNode, "loc", $"{baseAddress}{url}");
            }

            return xmlDocument.OuterXml;
        }

        public async Task<string> GetRssXml(Category category, string baseAddress)
        {
            _logger.LogTrace("Generating RSS XML for category {category}...", category);

            var xmlDocument = new XmlDocument();
            xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));
            var root = CreateElement(xmlDocument, xmlDocument, "rss");
            root.SetAttribute("version", "2.0");
            var channel = CreateElement(xmlDocument, root, "channel");


            CreateElement(xmlDocument, channel, "title",
                $"Aashish Koirala's {(category == Category.Tech ? "Software & Tech" : "Non-Tech")} Blog");
            CreateElement(xmlDocument, channel, "link", baseAddress);
            CreateElement(xmlDocument, channel, "description", category == Category.Tech
                ? "Posts about software and software tech, primary around .NET, C#, JS and architecture."
                : "General non-technical posts about this and that.");
            CreateElement(xmlDocument, channel, "category",
                category == Category.Tech ? "Software Development" : "Musings");
            CreateElement(xmlDocument, channel, "copyright", $"Copyright (C) {DateTime.Now.Year} Aashish Koirala");
            CreateElement(xmlDocument, channel, "language", "en-us");
            var image = CreateElement(xmlDocument, channel, "image");

            CreateElement(xmlDocument, image, "url", $"{baseAddress}/images/favicons/mstile-150x150.png");
            CreateElement(xmlDocument, image, "title",
                $"Aashish Koirala's {(category == Category.Tech ? "Software & Tech" : "Non-Tech")} Blog");
            CreateElement(xmlDocument, image, "link", baseAddress);

            var links = await _blogCache.GetAllLinks(category, null, _rssItemCount);
            foreach (var slug in links.Select(x => x.Slug))
            {
                var post = await _blogCache.GetPost(slug);
                var item = CreateElement(xmlDocument, channel, "item");
                CreateElement(xmlDocument, item, "title", post.Title);
                CreateElement(xmlDocument, item, "link", $"{baseAddress}/blog/{slug}");
                CreateElement(xmlDocument, item, "description", post.Blurb);
            }

            return xmlDocument.OuterXml;
        }

        private static XmlElement CreateElement(XmlDocument xmlDocument, XmlNode parentNode,
            string elementName, string innerTextIfAny = null, string namespaceUri = null)
        {
            var element = (XmlElement)
                parentNode.AppendChild(namespaceUri != null
                    ? xmlDocument.CreateElement(elementName, namespaceUri)
                    : xmlDocument.CreateElement(elementName));
            if (innerTextIfAny != null) element.InnerText = innerTextIfAny;
            return element;
        }
    }
}