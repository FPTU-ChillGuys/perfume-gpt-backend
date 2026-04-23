using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using Attribute = PerfumeGPT.Domain.Entities.Attribute;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IAttributeRepository : IGenericRepository<Attribute>
	{
		Task<List<int>> GetExistingIdsAsync(IEnumerable<int> ids);
		Task<List<AttributeLookupItem>> GetLookupListAsync(bool isVariantLevel);
		Task<List<Attribute>> GetByIdsAsync(IEnumerable<int> ids);
		Task<bool> IsInUseAsync(int attributeId);
	}
}
