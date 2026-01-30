using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IMediaRepository : IGenericRepository<Media>
	{
		Task<List<Media>> GetMediaByEntityAsync(EntityType entityType, Guid entityId);
		Task<Media?> GetPrimaryMediaAsync(EntityType entityType, Guid entityId);
		Task<int> DeleteAllMediaByEntityAsync(EntityType entityType, Guid entityId);
	}
}
