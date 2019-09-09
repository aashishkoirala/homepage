using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AK.Homepage
{
	public class MainContext : DbContext
	{
		private readonly IConfiguration _configuration;

		public MainContext(IConfiguration configuration) => _configuration = configuration;

		public DbSet<PageAccess> PageAccess { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder options) =>
			options.UseSqlServer(_configuration.GetConnectionString("Main"));
	}
}