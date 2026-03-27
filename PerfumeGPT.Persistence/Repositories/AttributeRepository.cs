using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using Attribute = PerfumeGPT.Domain.Entities.Attribute;

namespace PerfumeGPT.Persistence.Repositories
{
	public class AttributeRepository : GenericRepository<Attribute>, IAttributeRepository
	{
		public AttributeRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<int>> GetExistingIdsAsync(IEnumerable<int> ids)
			=> await _context.Attributes.Where(a => ids.Contains(a.Id)).Select(a => a.Id).ToListAsync();

		public async Task<List<Attribute>> GetByIdsAsync(IEnumerable<int> ids)
			=> await _context.Attributes.Where(a => ids.Contains(a.Id)).ToListAsync();

		public async Task<bool> IsInUseAsync(int attributeId)
			=> await _context.ProductAttributes.AnyAsync(pa => pa.AttributeId == attributeId);

		public async Task<List<AttributeLookupItem>> GetLookupListAsync(bool? isVariantLevel = null)
		{
			var query = _context.Attributes.AsQueryable();
			if (isVariantLevel.HasValue)
			{
				query = query.Where(a => a.IsVariantLevel == isVariantLevel.Value);
			}

			return await query.ProjectToType<AttributeLookupItem>().ToListAsync();
		}
	}
}
