using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AK.Homepage
{
	public class PageAccess
	{
		public int Id { get; set; }
		public string Path { get; set; }
		public string IpAddress { get; set; }
		public string UserAgent { get; set; }
	}

	public class PageAccessRecorder
	{
		private readonly BlockingCollection<PageAccess> _pageAccessList = new BlockingCollection<PageAccess>();
		private readonly IConfiguration _configuration;
		private readonly ILogger<PageAccessRecorder> _logger;
		private readonly PageAccessRecorderIgnoredUserAgents _ignoredUserAgents;

		private Task _messagePumpTask;
		private CancellationTokenSource _messagePumpTaskCancellationTokenSource;

		public PageAccessRecorder(
			IConfiguration configuration,
			ILogger<PageAccessRecorder> logger,
			PageAccessRecorderIgnoredUserAgents ignoredUserAgents)
		{
			_configuration = configuration;
			_logger = logger;
			_ignoredUserAgents = ignoredUserAgents;
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
			_messagePumpTaskCancellationTokenSource.Cancel();
			try
			{
				await _messagePumpTask;
			}
			catch (OperationCanceledException)
			{
			}

			_messagePumpTaskCancellationTokenSource.Dispose();
			_pageAccessList.Dispose();
		}

		private async Task RunMessagePump(CancellationToken cancellationToken)
		{
			foreach (var pageAccess in _pageAccessList.GetConsumingEnumerable(cancellationToken))
			{
				if (_ignoredUserAgents.ShouldIgnore(pageAccess.UserAgent)) continue;

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