using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class AttributeValueRepository : GenericRepository<AttributeValue>, IAttributeValueRepository
	{
		public AttributeValueRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<int>> GetExistingIdsAsync(IEnumerable<int> ids)
		=> await _context.AttributeValues.Where(v => ids.Contains(v.Id)).Select(v => v.Id).ToListAsync();

		public async Task<Dictionary<int, int>> GetAttributeMapByValueIdsAsync(IEnumerable<int> valueIds)
		=> await _context.AttributeValues
			.Where(v => valueIds.Contains(v.Id))
			.ToDictionaryAsync(v => v.Id, v => v.AttributeId);

		public async Task<List<AttributeValueLookupItem>> GetLookupListByAttributeIdAsync(int attributeId)
		=> await _context.AttributeValues
			.Where(v => v.AttributeId == attributeId)
			.Select(v => new AttributeValueLookupItem
			{
				Id = v.Id,
				Value = v.Value
			})
			.ToListAsync();

		public async Task<bool> IsInUseAsync(int valueId)
		=> await _context.ProductAttributes.AnyAsync(pa => pa.ValueId == valueId)
			|| await _context.CustomerAttributePreferences.AnyAsync(cap => cap.AttributeValueId == valueId);
	}
}
