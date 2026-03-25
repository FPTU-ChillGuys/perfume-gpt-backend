using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.ScentNotes;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IScentNoteService
	{
		Task<BaseResponse<List<ScentNoteLookupResponse>>> GetScentNoteLookupListAsync();
		Task<BaseResponse<List<ScentNoteResponse>>> GetAllScentNotesAsync();
		Task<BaseResponse<ScentNoteResponse>> GetScentNoteByIdAsync(int id);
		Task<BaseResponse<ScentNoteResponse>> CreateScentNoteAsync(CreateScentNoteRequest request);
		Task<BaseResponse<ScentNoteResponse>> UpdateScentNoteAsync(int id, UpdateScentNoteRequest request);
		Task<BaseResponse<bool>> DeleteScentNoteAsync(int id);
	}
}
