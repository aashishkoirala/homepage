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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AK.Homepage
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", true, true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddSingleton(Configuration)
				.AddSingleton<BlogRepository>()
				.AddSingleton<BlogCache>()
				.AddSingleton<BlogContentExtractor>()
				.AddSingleton<PostInfoUrlManager>()
				.AddSingleton<ProfileRepository>()
				.AddSingleton<AccessKeyValidator>()
				.AddSingleton<MetadataGenerator>()
				.AddSingleton<PageAccessRecorderIgnoredUserAgents>()
				.AddSingleton<PageAccessRecorder>()
				.AddScoped<LogActionAndHandleErrorFilter>();
			
			services.AddMvc();
			services.AddResponseCompression(x => x.EnableForHttps = true);
			services.AddLogging(x => x
				.AddApplicationInsights(Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"])
				.AddFilter("Microsoft", LogLevel.Warning) // This disables verbose logging from the .NET system
				.AddFilter("System", LogLevel.Warning) // from causing noise to my actual logs.
				.SetMinimumLevel(LogLevel.Trace));

			// HTTP client used to get the list of blog entries from GitHub.
			services.AddHttpClient("BlogList", x =>
			{
				ConfigureHttpClient(x, Configuration["BlogListAddress"]);
				x.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			});

			// HTTP client used to get a specific blog post content from GitHub Raw.
			services.AddHttpClient("BlogContent", x =>
			{
				ConfigureHttpClient(x, Configuration["BlogContentBaseAddress"]);
				x.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
			});
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, PageAccessRecorder recorder)
		{
			recorder.Start();

			if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
			else app.UseRewriter(new RewriteOptions().AddRedirectToHttps());

			// Re-purposing the "blog post not found" page for all general errors, which would mostly be 404s
			// anyway since we handle 500's separately through an action filter.
			const string errorPath = "/blog/an-entry-that-should-never-exist";

			app
				.UseMiddleware<PageAccessRecorderMiddleware>()
				.UseMiddleware<HttpCache>()
				.UseStatusCodePagesWithReExecute(errorPath)
				.UseResponseCompression()
				.UseMiddleware<BlogImageServer>()
				.UseMvcWithDefaultRoute()
				.UseFileServer();
		}

		private static void ConfigureHttpClient(HttpClient client, string baseUrl)
		{
			var baseAddress = new Uri(baseUrl);
			client.BaseAddress = baseAddress;
			client.DefaultRequestHeaders.Host = baseAddress.Host;

			// Need this because GitHub won't let just any willy-nilly user-agent through.
			client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
		}
	}
}