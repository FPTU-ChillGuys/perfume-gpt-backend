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

		public async Task<List<ConcentrationResponse>> GetAllConcentrationsAsync()
		{
			return await _context.Concentrations
				.AsNoTracking()
				.ProjectToType<ConcentrationResponse>()
				.ToListAsync();
		}

		public async Task<ConcentrationResponse?> GetConcentrationByIdAsync(int id)
		{
			return await _context.Concentrations
				.Where(c => c.Id == id)
				.ProjectToType<ConcentrationResponse>()
				.FirstOrDefaultAsync();
		}
	}
}
