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

 using AK.Homepage.Blog;

namespace AK.Homepage
{
    public class HomeViewModel
    {
        public PostLink[] TechPostLinks { get; set; } = new PostLink[0];
        public PostLink[] NonTechPostLinks { get; set; } = new PostLink[0];
        public Profile.Link[] ContactLinks { get; set; } = new Profile.Link[0];
        public Profile.Link[] ProjectLinks { get; set; } = new Profile.Link[0];
    }
}