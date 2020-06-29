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
using AK.Homepage.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AK.Homepage
{
	public class Startup
	{
		private readonly bool _runningInContainer;
		private readonly bool _isDevelopment;

		public Startup(IHostEnvironment env)
		{
			_runningInContainer = bool.TryParse(Environment.GetEnvironmentVariable("AK_RUNNING_IN_CONTAINER"), out var b) ? b : false;
			_isDevelopment = env.IsDevelopment();

			Configuration = BuildConfiguration(env.EnvironmentName, env.ContentRootPath);
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddHealthChecks();
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
				.AddSingleton<PageAccessRecorderIgnoredIpAddresses>()
				.AddSingleton<PageAccessRecorder>()
				.AddScoped<LogActionAndHandleErrorFilter>();

			services
				.AddHealthChecks()
				.AddCheck<BlogPostHealthCheck>(nameof(BlogPostHealthCheck))
				.AddCheck<PageAccessDatabaseHealthCheck>(nameof(PageAccessDatabaseHealthCheck));

			services.AddControllersWithViews();
			services.AddResponseCompression(x => x.EnableForHttps = true);
			services.AddLogging(ConfigureLogging);

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

		public void Configure(IApplicationBuilder app, PageAccessRecorder recorder)
		{
			recorder.Start();

			if (_isDevelopment) app.UseDeveloperExceptionPage();
			else if (!_runningInContainer) app.UseRewriter(new RewriteOptions().AddRedirectToHttps());

			// Re-purposing the "blog post not found" page for all general errors, which would mostly be 404s
			// anyway since we handle 500's separately through an action filter.
			const string errorPath = "/blog/an-entry-that-should-never-exist";

			app
				.UseHealthChecks("/health")
				.UseMiddleware<ThemeSwitcherMiddleware>()
				.UseMiddleware<PageAccessRecorderMiddleware>()
				.UseMiddleware<HttpCache>()
				.UseStatusCodePagesWithReExecute(errorPath)
				.UseResponseCompression()
				.UseMiddleware<BlogImageServer>()
				.UseRouting()
				.UseEndpoints(e => e.MapDefaultControllerRoute())
				.UseFileServer();
		}

		private IConfiguration BuildConfiguration(string environmentName, string contentRootPath)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(contentRootPath)
				.AddJsonFile("appsettings.json", true, true)
				.AddJsonFile($"appsettings.{environmentName}.json", true);

			builder = _runningInContainer
				? builder
					.AddKeyPerFile("/config", false)
					.AddKeyPerFile("/secrets", false)
				: builder.AddEnvironmentVariables();

			return builder.Build();
		}

		private static void ConfigureHttpClient(HttpClient client, string baseUrl)
		{
			var baseAddress = new Uri(baseUrl);
			client.BaseAddress = baseAddress;
			client.DefaultRequestHeaders.Host = baseAddress.Host;

			// Need this because GitHub won't let just any willy-nilly user-agent through.
			client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
		}

		private void ConfigureLogging(ILoggingBuilder builder)
		{
			if (_runningInContainer || _isDevelopment)
			{
				builder = builder.AddConsole(o =>
				{
					o.DisableColors = _runningInContainer;
					o.Format = ConsoleLoggerFormat.Systemd;
				});
			}
			else
			{
				var applicationInsightsInstrumentationKey = Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
				if (!string.IsNullOrWhiteSpace(applicationInsightsInstrumentationKey))
				{
					builder = builder.AddApplicationInsights(applicationInsightsInstrumentationKey);
				}
			}

			builder
				.AddFilter("Microsoft", LogLevel.Warning) // This disables verbose logging from the .NET system
				.AddFilter("System", LogLevel.Warning) // from causing noise to my actual logs.
				.SetMinimumLevel(LogLevel.Trace);
		}
	}
}