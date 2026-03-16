using PerfumeGPT.Application.DTOs.Responses.ScentNotes;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IScentNoteRepository : IGenericRepository<ScentNote>
	{
		Task<List<ScentNoteLookupResponse>> GetScentNoteLookupListAsync();
	}
}
