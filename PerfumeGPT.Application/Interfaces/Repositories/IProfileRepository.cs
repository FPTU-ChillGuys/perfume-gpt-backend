using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Responses.Profiles;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IProfileRepository : IGenericRepository<CustomerProfile>
	{
		Task<CustomerProfile?> GetByUserIdWithPreferencesAsync(Guid userId);
		Task<ProfileResponse?> GetProfileResponseByUserIdAsync(Guid userId);
		Task<List<int>> GetMissingNoteIdsAsync(IEnumerable<int> noteIds);
		Task<List<int>> GetMissingFamilyIdsAsync(IEnumerable<int> familyIds);
		Task<List<int>> GetMissingAttributeValueIdsAsync(IEnumerable<int> attributeValueIds);
	}
}
