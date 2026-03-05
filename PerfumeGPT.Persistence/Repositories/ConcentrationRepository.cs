using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Concentrations;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ConcentrationRepository : GenericRepository<Concentration>, IConcentrationRepository
	{
		public ConcentrationRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<ConcentrationLookupDto>> GetConcentrationLookupsAsync()
		{
			return await _context.Concentrations.ProjectToType<ConcentrationLookupDto>().ToListAsync();
		}
	}
}
