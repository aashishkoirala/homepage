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

 namespace AK.Homepage
{
    public class Profile
    {
        public Link[] ContactLinks { get; set; }
        public Link[] ProjectLinks { get; set; }
        public Resume ProfessionalResume { get; set; }

        public class Link
        {
            public string Url { get; set; }
            public string Label { get; set; }
            public string Title { get; set; }
            public string Icon { get; set; }
        }

        public class Resume
        {
            public string Name { get; set; }
            public string Location { get; set; }
            public string Contact { get; set; }
            public string LinkedInUrl { get; set; }
            public string GitHubUrl { get; set; }
            public string WebsiteUrl { get; set; }
            public string[] Summary { get; set; }
            public string[] Technologies { get; set; }
            public ResumeExperience[] Experience { get; set; }
            public ResumeEducation[] Education { get; set; }
        }

        public class ResumeExperience
        {
            public string Organization { get; set; }
            public string Location { get; set; }
            public string Title { get; set; }
            public string Timeframe { get; set; }
            public string Responsibilities { get; set; }
            public ResumeAchievement[] Achievements { get; set; }
        }

        public class ResumeAchievement
        {
            public string Text { get; set; }
            public string[] Children { get; set; }
        }

        public class ResumeEducation
        {
            public string Degree { get; set; }
            public string School { get; set; }
        }
    }
}