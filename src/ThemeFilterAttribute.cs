using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AK.Homepage
{
	public class ThemeFilterAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			if (context.HttpContext.Request.Cookies.TryGetValue("AK-DarkMode", out var cookieDark) && cookieDark == "True")
			{
				if (context.Controller is Controller c) c.TempData["dark"] = true;
			}
		}
	}
}