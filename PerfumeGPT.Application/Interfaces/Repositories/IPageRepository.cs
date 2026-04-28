using PerfumeGPT.Application.DTOs.Requests.Pages;
using PerfumeGPT.Application.DTOs.Responses.Pages;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IPageRepository : IGenericRepository<SystemPage>
	{
		Task<SystemPage?> GetBySlugAsync(string slug, bool asNoTracking = false);
		Task<SystemPage?> GetPublishedBySlugAsync(string slug);
		Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
		Task<(List<PageResponse> Items, int TotalCount)> GetPagedPagesAsync(GetPagedPageRequest request);
	}
}
