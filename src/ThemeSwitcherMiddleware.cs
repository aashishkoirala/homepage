using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AK.Homepage
{
	public class ThemeSwitcherMiddleware
	{
		private readonly RequestDelegate _next;

		public ThemeSwitcherMiddleware(RequestDelegate next) => _next = next;

		public async Task Invoke(HttpContext context)
		{
			if (context.Request.Query.TryGetValue("theme", out var themeValues))
			{
				var theme = themeValues.ToString();
				context.Request.Query = new QueryCollection(context.Request.Query.Where(x => x.Key != "theme").ToDictionary(x => x.Key, x => x.Value));
				if (theme == "dark") context.Response.Cookies.Append("AK-DarkMode", "True", new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(365) });
				else context.Response.Cookies.Delete("AK-DarkMode");
				context.Response.Redirect(context.Request.GetEncodedUrl());
				return;
			}

			await _next(context);
		}
	}
}