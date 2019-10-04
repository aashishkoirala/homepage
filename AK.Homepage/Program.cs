/*******************************************************************************************************************************
 * Copyright © 2018-2019 Aashish Koirala <https://www.aashishkoirala.com>
 * 
 * This file is part of Aashish Koirala's Personal Website and Blog (AKPWB).
 *  
 * AKPWB is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * AKPWB is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with AKPWB.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *******************************************************************************************************************************/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AK.Homepage
{
	public static class Program
	{
		public static Task Main(string[] args) => BuildHost(args).RunAsync();

		public static IHost BuildHost(string[] args)
		{
			var builder = Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(w =>
			{
				w.UseStartup<Startup>();
				if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
					w.UseKestrel(o => o.Listen(IPAddress.Any, 5858));
			});
			return builder.Build();
		}
	}
}