using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IImportTicketService
	{
		Task<BaseResponse<string>> CreateImportTicketAsync(CreateImportTicketRequest request, Guid userId);
		Task<BaseResponse<string>> CreateImportTicketFromExcelAsync(CreateImportTicketFromExcelRequest request, Guid userId);
		Task<BaseResponse<ExcelTemplateResponse>> GenerateImportTemplateAsync();
		Task<BaseResponse<string>> VerifyImportTicketAsync(Guid ticketId, VerifyImportTicketRequest request, Guid verifiedByUserId);
		Task<BaseResponse<ImportTicketResponse>> GetImportTicketByIdAsync(Guid id);
		Task<BaseResponse<PagedResult<ImportTicketListItem>>> GetImportTicketsAsync(GetPagedImportTicketsRequest request);
		Task<BaseResponse<string>> UpdateImportStatusAsync(Guid id, UpdateImportStatusRequest request);
		Task<BaseResponse<string>> UpdateImportTicketAsync(Guid id, UpdateImportRequest request);
		Task<BaseResponse<bool>> DeleteImportTicketAsync(Guid id);
	}
}
