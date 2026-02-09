using Mapster;
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
		public AttributeValueRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<int>> GetExistingIdsAsync(IEnumerable<int> ids)
		{
			return await _context.AttributeValues.Where(v => ids.Contains(v.Id)).Select(v => v.Id).ToListAsync();
		}

		public async Task<AttributeValue?> GetByIdAsync(int id)
		{
			return await _context.AttributeValues.FirstOrDefaultAsync(v => v.Id == id);
		}

		public async Task<List<AttributeValueLookupItem>> GetLookupListByAttributeIdAsync(int attributeId)
		{
			return await _context.AttributeValues.Where(v => v.AttributeId == attributeId).ProjectToType<AttributeValueLookupItem>().ToListAsync();
		}
	}
}
