using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ScentNotes;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IScentNoteService
	{
		Task<BaseResponse<List<ScentNoteLookupResponse>>> GetScentNoteLookupListAsync();
	}
}
