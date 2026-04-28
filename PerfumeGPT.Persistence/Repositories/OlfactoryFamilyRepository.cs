using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OlfactoryFamilyRepository : GenericRepository<OlfactoryFamily>, IOlfactoryFamilyRepository
	{
		public OlfactoryFamilyRepository(PerfumeDbContext context) : base(context) { }

		public async Task<bool> HasAssociationsAsync(int olfactoryFamilyId)
		=> await _context.ProductFamilyMaps.AnyAsync(x => x.OlfactoryFamilyId == olfactoryFamilyId)
			|| await _context.CustomerFamilyPreferences.AnyAsync(x => x.FamilyId == olfactoryFamilyId);

		public async Task<List<OlfactoryLookupResponse>> GetOlfactoryFamilyLookupListAsync()
		 => await _context.OlfactoryFamilies
				.Select(x => new OlfactoryLookupResponse
				{
					Id = x.Id,
					Name = x.Name
				})
			.ToListAsync();

		public async Task<List<OlfactoryFamilyResponse>> GetAllOlfactoryFamiliesAsync()
		 => await _context.OlfactoryFamilies
				.Select(x => new OlfactoryFamilyResponse
				{
					Id = x.Id,
					Name = x.Name
				})
				.ToListAsync();

		public async Task<OlfactoryFamilyResponse?> GetOlfactoryFamilyByIdAsync(int id)
		=> await _context.OlfactoryFamilies
			.Where(x => x.Id == id)
			.Select(x => new OlfactoryFamilyResponse
			{
				Id = x.Id,
				Name = x.Name
			})
			.FirstOrDefaultAsync();
	}
}
