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

using System.Linq;

namespace AK.Homepage.Blog
{
	public class BlogViewModel
	{
		public Category Category { get; set; }
		public Post? Post { get; set; }
		public PostLink[] SideLinks { get; set; } = new PostLink[0];
		public PostLink[] MainLinks { get; set; } = new PostLink[0];
		public bool PreventIndexing { get; set; }

		public string Description
		{
			get
			{
				var description = "Aashish Koirala: ";
				if (Category != Category.Meta && Post != null)
				{
					description += Post.Title + ". ";
					if (Post.Tags != null && Post.Tags.Any()) description += " " + string.Join(", ", Post.Tags);
					description += " " + Post.Blurb;
				}
				else if (Category == Category.Meta && Post != null) description += Post.Title;
				else description += "Software Architect and Developer Personal Website and Blog";

				return description;
			}
		}

		public string Title => Post != null
			? $"{Post.Title} | Aashish Koirala"
			: Category != Category.Meta
				? $"Blog: {(Category == Category.Tech ? "Software & Tech" : "Non - Tech")} | Aashish Koirala"
				: "Aashish Koirala";
	}
}