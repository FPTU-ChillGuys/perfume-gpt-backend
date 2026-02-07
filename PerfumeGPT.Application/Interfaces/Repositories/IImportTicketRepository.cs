using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IImportTicketRepository : IGenericRepository<ImportTicket>
	{
		Task<ImportTicket?> GetByIdWithDetailsAsync(Guid id);
		Task<ImportTicketResponse?> GetResponseByIdAsync(Guid id);
		Task<ImportTicket?> GetByIdWithDetailsAndBatchesAsync(Guid id);
		Task<(List<ImportTicketListItem> Items, int TotalCount)> GetPagedAsync(GetPagedImportTicketsRequest request);
	}
}
