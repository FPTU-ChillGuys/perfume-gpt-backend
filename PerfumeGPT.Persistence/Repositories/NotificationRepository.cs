using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
	{
		public NotificationRepository(PerfumeDbContext context) : base(context) { }

		public async Task<bool> MarkAllAsReadAsync(Guid userId)
		{
			var updatedRows = await _context.Notifications
				.Where(n => n.UserId == userId && !n.IsRead)
				.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

			return true;
		}
	}
}
