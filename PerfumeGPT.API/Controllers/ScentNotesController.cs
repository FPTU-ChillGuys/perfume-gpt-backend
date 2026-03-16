using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ScentNotes;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ScentNotesController : BaseApiController
	{
		private readonly IScentNoteService _scentNoteService;

		public ScentNotesController(IScentNoteService scentNoteService)
		{
			_scentNoteService = scentNoteService;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<ScentNoteLookupResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<ScentNoteLookupResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<ScentNoteLookupResponse>>>> GetScentNoteLookupList()
		{
			var result = await _scentNoteService.GetScentNoteLookupListAsync();
			return HandleResponse(result);
		}
	}
}
