using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IAttributeValueRepository : IGenericRepository<AttributeValue>
	{
		Task<List<AttributeValueLookupItem>> GetLookupListByAttributeIdAsync(int attributeId);
		Task<List<int>> GetExistingIdsAsync(IEnumerable<int> ids);
		Task<bool> IsInUseAsync(int valueId);
	}
}
