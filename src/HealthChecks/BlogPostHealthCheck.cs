using AK.Homepage.Blog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace AK.Homepage.HealthChecks
{
	public class BlogPostHealthCheck : IHealthCheck
	{
		private readonly BlogCache _blogCache;

		public BlogPostHealthCheck(BlogCache blogCache) => _blogCache = blogCache;

		public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var healthy = await _blogCache.CheckHealth();
			return healthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
		}
	}
}