using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ScentNotes;
using PerfumeGPT.Application.Interfaces.Repositories;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public class ScentNoteService : IScentNoteService
	{
		private readonly IScentNoteRepository _scentNoteRepository;

		public ScentNoteService(IScentNoteRepository scentNoteRepository)
		{
			_scentNoteRepository = scentNoteRepository;
		}

		public async Task<BaseResponse<List<ScentNoteLookupResponse>>> GetScentNoteLookupListAsync()
		{
			return BaseResponse<List<ScentNoteLookupResponse>>.Ok(
				await _scentNoteRepository.GetScentNoteLookupListAsync()
			);
		}
	}
}
