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

using System.Collections.Generic;

namespace AK.Homepage
{
    public class Profile
    {
        public Link[] ContactLinks { get; set; } = new Link[0];
        public Link[] ProjectLinks { get; set; } = new Link[0];
        public Resume? ProfessionalResume { get; set; }

        public class Link
        {
	        public string? Url { get; set; }
            public string Label { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
			public string? Icon { get; set; }
        }

        public class Resume
        {
	        public string Name { get; set; } = string.Empty;
			public string Subtitle { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
			public string WorkStatus { get; set; } = string.Empty;
            public string Contact { get; set; } = string.Empty;
            public string LinkedInUrl { get; set; } = string.Empty;
            public string GitHubUrl { get; set; } = string.Empty;
            public string WebsiteUrl { get; set; } = string.Empty;
            public string[] Summary { get; set; } = new string[0];
			public Dictionary<string, string[]> Technologies { get; set; } = new Dictionary<string, string[]>();
            public ResumeExperience[] Experience { get; set; } = new ResumeExperience[0];
            public ResumeEducation[] Education { get; set; } = new ResumeEducation[0];
        }

        public class ResumeExperience
        {
			public string Organization { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
			public Dictionary<string, string> Titles { get; set; } = new Dictionary<string, string>();
			public string[] Achievements { get; set; } = new string[0];
        }

        public class ResumeEducation
        {
	        public string Degree { get; set; } = string.Empty;
            public string School { get; set; } = string.Empty;
			public string Year { get; set; } = string.Empty;
        }
    }
}