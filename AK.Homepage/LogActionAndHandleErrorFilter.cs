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
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using System;

namespace AK.Homepage
{
    public class LogActionAndHandleErrorFilter : IActionFilter, IExceptionFilter
    {
        private readonly ILogger _logger;
        private readonly IModelMetadataProvider _modelMetadataProvider;

        public LogActionAndHandleErrorFilter(ILoggerFactory loggerFactory, IModelMetadataProvider modelMetadataProvider)
        {
            _logger = loggerFactory.CreateLogger<LogActionAndHandleErrorFilter>();
            _modelMetadataProvider = modelMetadataProvider;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var path = context.HttpContext.Request.Path;
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress.ToString();
            _logger.LogTrace("[ENTER] [{ipAddress}] [{path}]", ipAddress, path);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var path = context.HttpContext.Request.Path;
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress.ToString();
            _logger.LogTrace("[EXIT] [{ipAddress}] [{path}]", ipAddress, path);
        }

        public void OnException(ExceptionContext context)
        {
            var errorCode = Guid.NewGuid().ToString();
            _logger.LogError(context.Exception, "[ErrorCode: {errorCode}]", errorCode);

            var path = context.HttpContext.Request.Path;
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress.ToString();
            _logger.LogTrace("[ERROR] [{ipAddress}] [{path}]", ipAddress, path);

            // We repurpose the blog post page as the error page with the friendly error
            // message being the actual "post" that is rendered.
            context.Result = new ViewResult
            {
                ViewName = "Blog",
                ViewData = new ViewDataDictionary(_modelMetadataProvider, context.ModelState)
                {
                    Model = CreateErrorViewModel(errorCode)
                }
            };

            // We set this so that HttpCache knows not to cache the error output.
            context.HttpContext.Response.Headers["X-AK-SkipCache"] = "1";

            context.HttpContext.Response.Headers["X-AK-ErrorCode"] = errorCode;
            context.ExceptionHandled = true;
        }

        private static BlogViewModel CreateErrorViewModel(string errorCode) => new BlogViewModel
        {
            Category = Category.Meta,
            Post = new Post
            {
                Title = "Something went wrong!",
                ContentHtml = "Looks like something went wrong. Apologies for the inconvenience. " +
                              $"Please reach out to me if something is critical.<!-- ErrorCode: {errorCode} -->"
            },
            PreventIndexing = true
        };
    }
}