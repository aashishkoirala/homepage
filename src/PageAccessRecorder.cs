using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AK.Homepage
{
	public class PageAccess
	{
		public int Id { get; set; }
		public DateTimeOffset TimeStamp { get; set; }
		public string Path { get; set; } = string.Empty;
		public string IpAddress { get; set; } = string.Empty;
		public string UserAgent { get; set; } = string.Empty;
	}

	public class PageAccessRecorder
	{
		private readonly BlockingCollection<PageAccess> _pageAccessList = new BlockingCollection<PageAccess>();
		private readonly IConfiguration _configuration;
		private readonly ILogger<PageAccessRecorder> _logger;
		private readonly PageAccessRecorderIgnoredUserAgents _ignoredUserAgents;
		private readonly PageAccessRecorderIgnoredIpAddresses _ignoredIpAddresses;

		private Task? _messagePumpTask;
		private CancellationTokenSource? _messagePumpTaskCancellationTokenSource;

		public PageAccessRecorder(
			IConfiguration configuration,
			ILogger<PageAccessRecorder> logger,
			PageAccessRecorderIgnoredUserAgents ignoredUserAgents,
			PageAccessRecorderIgnoredIpAddresses ignoredIpAddresses)
		{
			_configuration = configuration;
			_logger = logger;
			_ignoredUserAgents = ignoredUserAgents;
			_ignoredIpAddresses = ignoredIpAddresses;
		}

		public void Record(PageAccess pageAccess)
		{
			try
			{
				_pageAccessList.Add(pageAccess);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error while recording page access: {pageAccess.Path}");
			}
		}

		public void Start()
		{
			_messagePumpTaskCancellationTokenSource = new CancellationTokenSource();
			_messagePumpTask = Task.Run(() => RunMessagePump(_messagePumpTaskCancellationTokenSource.Token));
		}

		public async Task Stop()
		{
			_pageAccessList.CompleteAdding();
			_messagePumpTaskCancellationTokenSource?.Cancel();
			if (_messagePumpTask == null) return;

			try
			{
				await _messagePumpTask;
			}
			catch (OperationCanceledException)
			{
			}

			_messagePumpTaskCancellationTokenSource?.Dispose();
			_pageAccessList.Dispose();
		}

		public async Task<(string Path, int ViewCount)[]> GetStats(CancellationToken cancellationToken)
		{
			await using var dbContext = new MainContext(_configuration);
			var list = await dbContext.PageAccess
				.GroupBy(x => x.Path)
				.Select(x => new { Path = x.Key, ViewCount = x.Count() })
				.ToArrayAsync(cancellationToken);

			return list
				.Select(x => (x.Path, x.ViewCount))
				.OrderByDescending(x => x.ViewCount)
				.ThenBy(x => x.Path)
				.ToArray();
		}

		public async Task<PageAccess[]> GetStatsForPath(string path, CancellationToken cancellationToken)
		{
			await using var dbContext = new MainContext(_configuration);
			var list = await dbContext.PageAccess
				.Where(x => x.Path == path)
				.ToArrayAsync(cancellationToken);

			return list.OrderBy(x => x.TimeStamp).ToArray();
		}

		private async Task RunMessagePump(CancellationToken cancellationToken)
		{
			foreach (var pageAccess in _pageAccessList.GetConsumingEnumerable(cancellationToken))
			{
				if (_ignoredUserAgents.ShouldIgnore(pageAccess.UserAgent)) continue;
				if (_ignoredIpAddresses.ShouldIgnore(pageAccess.IpAddress)) continue;

				try
				{
					await using var dbContext = new MainContext(_configuration);
					dbContext.PageAccess.Add(pageAccess);
					await dbContext.SaveChangesAsync(cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error recording page access from within pump: {pageAccess.Path}");
				}
			}
		}
	}
}