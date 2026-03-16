using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OlfactoryFamilyRepository : GenericRepository<OlfactoryFamily>, IOlfactoryFamilyRepository
	{
		public OlfactoryFamilyRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<OlfactoryLookupResponse>> GetOlfactoryFamilyLookupListAsync()
		{
			var olfactoryFamilies = await _context.OlfactoryFamilies.ProjectToType<OlfactoryLookupResponse>().ToListAsync();

			return olfactoryFamilies;
		}
	}
}
