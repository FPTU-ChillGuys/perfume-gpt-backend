using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ITemporaryMediaRepository : IGenericRepository<TemporaryMedia>
	{
		/// <summary>
		/// Get all expired temporary media records
		/// </summary>
		Task<List<TemporaryMedia>> GetExpiredMediaAsync();

		/// <summary>
		/// Get temporary media by user ID
		/// </summary>
		Task<List<TemporaryMedia>> GetByUserIdAsync(Guid userId);

		/// <summary>
		/// Delete expired temporary media and return count
		/// </summary>
		Task<int> DeleteExpiredMediaAsync();
	}
}
