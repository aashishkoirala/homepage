using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AK.Homepage.HealthChecks
{
	public class PageAccessDatabaseHealthCheck : IHealthCheck
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<PageAccessDatabaseHealthCheck> _logger;

		public PageAccessDatabaseHealthCheck(
			IConfiguration configuration,
			ILogger<PageAccessDatabaseHealthCheck> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				await using var dbContext = new MainContext(_configuration);
				await dbContext.PageAccess.Where(x => x.Id == 0).Select(x => 1).Take(1).FirstOrDefaultAsync(cancellationToken);
				return HealthCheckResult.Healthy();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
				return HealthCheckResult.Unhealthy();
			}
		}
	}
}