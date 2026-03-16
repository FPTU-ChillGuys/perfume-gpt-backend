using Mapster;
using PerfumeGPT.Application.DTOs.Responses.ScentNotes;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ScentNoteRepository : GenericRepository<ScentNote>, IScentNoteRepository
	{
		public ScentNoteRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<ScentNoteLookupResponse>> GetScentNoteLookupListAsync()
		{
			var scentNotes = await _context.ScentNotes.ProjectToType<ScentNoteLookupResponse>().ToListAsync();
			return scentNotes;
		}
	}
}
