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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AK.Homepage
{
	public class ProfileRepository
    {
        private readonly Task<Profile> _loadProfileTask;

        public ProfileRepository(ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<ProfileRepository>();
            _loadProfileTask = LoadProfile(logger);
        }

        public async Task<Profile> GetProfile() => await _loadProfileTask;

        private static async Task<Profile> LoadProfile(ILogger logger)
        {
            logger.LogTrace("Loading profile database...");

            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AK.Homepage.Profile.json") ??
                                     Assembly.GetExecutingAssembly().GetManifestResourceStream("AK.Homepage.Profile");
            using var streamReader = new StreamReader(stream ?? throw new ArgumentNullException(nameof(stream)));
            var json = await streamReader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<Profile>(json);
        }
    }
}