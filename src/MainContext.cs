using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AK.Homepage
{
	public class MainContext : DbContext
	{
		private readonly IConfiguration _configuration;

#pragma warning disable CS8618
		public MainContext(IConfiguration configuration) => _configuration = configuration;
#pragma warning restore CS8618

		public DbSet<PageAccess> PageAccess { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder options) =>
			options.UseSqlServer(GetConnectionString());

		private string GetConnectionString()
		{
			var cs = _configuration.GetConnectionString("Main");
			return !string.IsNullOrWhiteSpace(cs) ? cs : _configuration["MainConnectionString"];
		}
	}
}