using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace AK.Homepage
{
	public class PageAccessRecorderIgnoredIpAddresses
	{
		private readonly string[] _ignoredIpAddresses;

		public PageAccessRecorderIgnoredIpAddresses(IConfiguration configuration) => _ignoredIpAddresses =
			(configuration[nameof(PageAccessRecorderIgnoredIpAddresses)] ?? string.Empty).Split(';');

		public bool ShouldIgnore(string ipAddress) => ShouldAlwaysIgnore(ipAddress) || (_ignoredIpAddresses.Any() && _ignoredIpAddresses.Any(x => IsMatch(x, ipAddress)));

		private bool IsMatch(string ignoredIpAddress, string actualIpAddress)
		{
			if (ignoredIpAddress.Equals(actualIpAddress, StringComparison.OrdinalIgnoreCase)) return true;
			if (!IPAddress.TryParse(ignoredIpAddress, out var ignoredIp)) return false;
			if (!IPAddress.TryParse(actualIpAddress, out var actualIp)) return false;

			if (ignoredIp.AddressFamily != actualIp.AddressFamily) return false;
			if (ignoredIp.AddressFamily != AddressFamily.InterNetwork) return false;

			var ignoredParts = ignoredIpAddress.Split('.');
			var actualParts = actualIpAddress.Split('.');
			if (ignoredParts.Length != actualParts.Length) return false;

			for (var i = 0; i < ignoredParts.Length; i++) if (ignoredParts[i] == "*") actualParts[i] = "*";
			return ignoredParts.SequenceEqual(actualParts);
		}

		private bool ShouldAlwaysIgnore(string ipAddress)
		{
			if (ipAddress == "::1") return true;
			var parts = ipAddress.Split('.');
			return parts.Length != 4
				? false
				: parts[0] == "127"
				? true
				: parts[0] == "10"
				? true
				: parts[0] == "192" && parts[1] == "168"
				? true
				: parts[0] != "172" ? false : int.TryParse(parts[1], out var i) && i >= 16 && i <= 31;
		}
	}
}