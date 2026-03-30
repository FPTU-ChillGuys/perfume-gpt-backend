using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PerfumeGPT.Domain.Enums;
using System.Security.Claims;

namespace PerfumeGPT.Infrastructure.Hubs
{
	[Authorize]
	public class NotificationHub : Hub
	{
		public override async Task OnConnectedAsync()
		{
			string? role = Context.User?.FindFirst("role")?.Value
				?? Context.User?.FindFirst(ClaimTypes.Role)?.Value;

			if (string.Equals(role, UserRole.staff.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				await Groups.AddToGroupAsync(Context.ConnectionId, "StaffGroup");
			}

			if (string.Equals(role, UserRole.admin.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			await base.OnDisconnectedAsync(exception);
		}
	}
}
