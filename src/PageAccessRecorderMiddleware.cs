using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace AK.Homepage
{
	public class PageAccessRecorderMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly PageAccessRecorder _recorder;

		public PageAccessRecorderMiddleware(RequestDelegate next, PageAccessRecorder recorder)
		{
			_next = next;
			_recorder = recorder;
		}

		public async Task Invoke(HttpContext context)
		{
			await _next(context);

			if (context.Response.StatusCode != (int) HttpStatusCode.OK &&
			    context.Response.StatusCode != (int) HttpStatusCode.NotModified) return;

			if (context.Response.ContentType != null && !context.Response.ContentType.Contains(MediaTypeNames.Text.Html)) return;

			_recorder.Record(new PageAccess
			{
				Path = context.Request.GetEncodedPathAndQuery(),
				UserAgent = context.Request.Headers[HeaderNames.UserAgent].ToString(),
				IpAddress = context.Connection.RemoteIpAddress.ToString()
			});
		}
	}
}