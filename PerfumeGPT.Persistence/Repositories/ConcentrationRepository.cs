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
	  => await _context.Concentrations
			.AsNoTracking()
			.Select(c => new ConcentrationLookupDto
			{
				Id = c.Id,
				Name = c.Name
			})
			.ToListAsync();

		public async Task<List<ConcentrationResponse>> GetAllConcentrationsAsync()
		=> await _context.Concentrations
			.AsNoTracking()
		 .Select(c => new ConcentrationResponse
		 {
			 Id = c.Id,
			 Name = c.Name
		 })
			.ToListAsync();

		public async Task<ConcentrationResponse?> GetConcentrationByIdAsync(int id)
		=> await _context.Concentrations
			.Where(c => c.Id == id)
		 .Select(c => new ConcentrationResponse
		 {
			 Id = c.Id,
			 Name = c.Name
		 })
			.FirstOrDefaultAsync();

		public async Task<bool> HasVariantsAsync(int concentrationId)
		=> await _context.ProductVariants.AnyAsync(v => v.ConcentrationId == concentrationId);
	}
}
