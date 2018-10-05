/*******************************************************************************************************************************
 * AK.Homepage.Program
 * Copyright © 2018 Aashish Koirala <https://www.aashishkoirala.com>
 * 
 * This file is part of Aashish Koirala's Homepage.
 *  
 * Listor is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Listor is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Listor.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *******************************************************************************************************************************/

using System;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace AK.Homepage
{
    public static class Program
    {
        public static void Main(string[] args) => BuildWebHost(args).Run();

        public static IWebHost BuildWebHost(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args);
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                builder = builder.UseKestrel(o => o.Listen(IPAddress.Any, 5858));
            return builder.UseStartup<Startup>().Build();
        }
    }
}