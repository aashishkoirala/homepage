using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace AK.Homepage
{
	public enum UserAgentIgnoreType
	{
		Contains,
		StartsWith,
		EndsWith,
		Equals
	}

	public class IgnoredUserAgent
	{
		public UserAgentIgnoreType Type { get; set; }
		public string UserAgent { get; set; }
	}

	public class PageAccessRecorderIgnoredUserAgents
	{
		private readonly IgnoredUserAgent[] _ignoredUserAgents;

		public PageAccessRecorderIgnoredUserAgents(IConfiguration configuration) => _ignoredUserAgents =
			BuildIgnoredUserAgents(configuration[nameof(PageAccessRecorderIgnoredUserAgents)]);

		public bool ShouldIgnore(string userAgent) => _ignoredUserAgents.Any(x => ShouldIgnore(x, userAgent));

		private static bool ShouldIgnore(IgnoredUserAgent ignoredUserAgent, string userAgent)
		{
			if (string.IsNullOrWhiteSpace(userAgent)) return true;
			switch (ignoredUserAgent.Type)
			{
				case UserAgentIgnoreType.Equals:
					return userAgent.Equals(ignoredUserAgent.UserAgent, StringComparison.OrdinalIgnoreCase);
				case UserAgentIgnoreType.Contains:
					return userAgent.Contains(ignoredUserAgent.UserAgent, StringComparison.OrdinalIgnoreCase);
				case UserAgentIgnoreType.StartsWith:
					return userAgent.StartsWith(ignoredUserAgent.UserAgent, StringComparison.OrdinalIgnoreCase);
				case UserAgentIgnoreType.EndsWith:
					return userAgent.EndsWith(ignoredUserAgent.UserAgent, StringComparison.OrdinalIgnoreCase);
				default:
					return false;
			}
		}

		private static IgnoredUserAgent[] BuildIgnoredUserAgents(string configuredString) =>
			string.IsNullOrWhiteSpace(configuredString)
				? new IgnoredUserAgent[0]
				: configuredString.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(CreateIgnoredUserAgent).Where(x => x != null)
					.ToArray();

		private static IgnoredUserAgent CreateIgnoredUserAgent(string configuredString)
		{
			var parts = configuredString.Split(':');
			if (parts.Length == 0) return null;
			var prefix = parts[0];
			var remainder = parts[1];
			if (parts.Length > 2) remainder = string.Join(':', parts.Skip(1));
			UserAgentIgnoreType type;
			switch (prefix)
			{
				case "has":
					type = UserAgentIgnoreType.Contains;
					break;
				case "is":
					type = UserAgentIgnoreType.Equals;
					break;
				case "sw":
					type = UserAgentIgnoreType.StartsWith;
					break;
				case "ew":
					type = UserAgentIgnoreType.EndsWith;
					break;
				default:
					return null;
			}

			return new IgnoredUserAgent {Type = type, UserAgent = remainder};
		}
	}
}