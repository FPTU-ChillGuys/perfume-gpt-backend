using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IImportTicketService
	{
		Task<BaseResponse<string>> CreateImportTicketAsync(CreateImportTicketRequest request, Guid userId);
		Task<BaseResponse<string>> VerifyImportTicketAsync(Guid ticketId, VerifyImportTicketRequest request, Guid verifiedByUserId);
		Task<BaseResponse<ImportTicketResponse>> GetImportTicketByIdAsync(Guid id);
		Task<BaseResponse<PagedResult<ImportTicketListItem>>> GetPagedImportTicketsAsync(GetPagedImportTicketsRequest request);
		Task<BaseResponse<string>> UpdateImportStatusAsync(Guid id, UpdateImportTicketRequest request);
		Task<BaseResponse<bool>> DeleteImportTicketAsync(Guid id);
	}
}
