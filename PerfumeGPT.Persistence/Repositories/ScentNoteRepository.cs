using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Persistence.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.ScentNotes;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ScentNoteRepository : GenericRepository<ScentNote>, IScentNoteRepository
	{
		public ScentNoteRepository(PerfumeDbContext context) : base(context) { }

		public async Task<bool> HasAssociationsAsync(int scentNoteId)
		=> await _context.ProductNoteMaps.AnyAsync(x => x.ScentNoteId == scentNoteId)
				|| await _context.CustomerNotePreferences.AnyAsync(x => x.NoteId == scentNoteId);

		public async Task<List<ScentNoteLookupResponse>> GetScentNoteLookupListAsync()
		=> await _context.ScentNotes
			.Select(x => new ScentNoteLookupResponse
			{
				Id = x.Id,
				Name = x.Name
			})
			.ToListAsync();

		public async Task<List<ScentNoteResponse>> GetAllScentNotesAsync()
	  => await _context.ScentNotes
			.Select(x => new ScentNoteResponse
			{
				Id = x.Id,
				Name = x.Name
			})
			.ToListAsync();

		public async Task<ScentNoteResponse?> GetScentNoteByIdAsync(int id)
	   => await _context.ScentNotes
			.Where(x => x.Id == id)
			.Select(x => new ScentNoteResponse
			{
				Id = x.Id,
				Name = x.Name
			})
			.FirstOrDefaultAsync();
	}
}
