using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ITemporaryMediaRepository : IGenericRepository<TemporaryMedia>
	{
		Task<List<TemporaryMedia>> GetExpiredMediaAsync();
		Task<int> DeleteExpiredMediaAsync();
	}
}
