using Hangfire.Dashboard;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	/// <summary>
	/// Authorization filter for Hangfire Dashboard - restricts access to admins only
	/// </summary>
	public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
	{
		public bool Authorize(DashboardContext context)
		{
			var httpContext = context.GetHttpContext();

			// Allow access only to authenticated users with Admin role
			return httpContext.User.Identity?.IsAuthenticated == true
				&& httpContext.User.IsInRole("Admin");
		}
	}
}
