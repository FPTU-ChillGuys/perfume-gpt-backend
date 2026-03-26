using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Concentrations;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ConcentrationRepository : GenericRepository<Concentration>, IConcentrationRepository
	{
		public ConcentrationRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<ConcentrationLookupDto>> GetConcentrationLookupsAsync()
			=> await _context.Concentrations.AsNoTracking().ProjectToType<ConcentrationLookupDto>().ToListAsync();

		public async Task<List<ConcentrationResponse>> GetAllConcentrationsAsync()
			=> await _context.Concentrations
				.AsNoTracking()
				.ProjectToType<ConcentrationResponse>()
				.ToListAsync();

		public async Task<ConcentrationResponse?> GetConcentrationByIdAsync(int id)
			=> await _context.Concentrations
				.Where(c => c.Id == id)
				.ProjectToType<ConcentrationResponse>()
				.FirstOrDefaultAsync();

		public async Task<bool> HasVariantsAsync(int concentrationId)
			=> await _context.ProductVariants.AnyAsync(v => v.ConcentrationId == concentrationId);
	}
}
