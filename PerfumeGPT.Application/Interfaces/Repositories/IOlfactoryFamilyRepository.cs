using PerfumeGPT.Application.DTOs.Responses.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IOlfactoryFamilyRepository : IGenericRepository<OlfactoryFamily>
	{
		Task<List<OlfactoryLookupResponse>> GetOlfactoryFamilyLookupListAsync();
	}
}
