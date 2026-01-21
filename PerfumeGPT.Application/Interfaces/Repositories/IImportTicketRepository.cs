using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IImportTicketRepository : IGenericRepository<ImportTicket>
	{
		Task<ImportTicket?> GetByIdWithDetailsAsync(Guid id);
		Task<ImportTicket?> GetByIdWithDetailsForDeleteAsync(Guid id);
		Task<(IEnumerable<ImportTicket> Items, int TotalCount)> GetPagedWithDetailsAsync(GetPagedImportTicketsRequest request);
	}
}
