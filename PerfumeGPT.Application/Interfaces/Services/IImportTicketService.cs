using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IImportTicketService
	{
		Task<BaseResponse<ImportTicketResponse>> CreateImportTicketAsync(CreateImportTicketRequest request, Guid userId);
		Task<BaseResponse<ImportTicketResponse>> VerifyImportTicketAsync(VerifyImportTicketRequest request);
		Task<BaseResponse<ImportTicketResponse>> GetImportTicketByIdAsync(Guid id);
		Task<BaseResponse<PagedResult<ImportTicketListItem>>> GetPagedImportTicketsAsync(GetPagedImportTicketsRequest request);
		Task<BaseResponse<ImportTicketResponse>> UpdateImportStatusAsync(Guid id, UpdateImportTicketRequest request);
		Task<BaseResponse<bool>> DeleteImportTicketAsync(Guid id);
	}
}
