using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface INotificationRepository : IGenericRepository<Notification>
	{
		Task<bool> MarkAllAsReadAsync(Guid userId);
	}
}
