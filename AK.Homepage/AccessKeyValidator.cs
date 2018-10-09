/*******************************************************************************************************************************
 * Copyright © 2018 Aashish Koirala <https://www.aashishkoirala.com>
 * 
 * This file is part of Aashish Koirala's Personal Website and Blog (AKPWB).
 *  
 * AKPWB is free software: you can redistribute it and/or modify
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
 * along with AKPWB.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *******************************************************************************************************************************/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace AK.Homepage
{
    public class AccessKeyValidator
    {
        private readonly string _accessKey;
        private readonly ILogger _logger;

        public AccessKeyValidator(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _accessKey = configuration["AccessKey"];
            _logger = loggerFactory.CreateLogger<AccessKeyValidator>();
        }

        public void Validate(string accessKey)
        {
            if (string.IsNullOrWhiteSpace(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (accessKey.Equals(_accessKey, StringComparison.Ordinal)) return;

            _logger.LogError("Attempt to perform privileged operation with invalid access key {accessKey}.", accessKey);
            throw new UnauthorizedAccessException();
        }
    }
}