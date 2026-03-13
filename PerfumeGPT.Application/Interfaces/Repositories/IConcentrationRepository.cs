using PerfumeGPT.Application.DTOs.Responses.Concentrations;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IConcentrationRepository : IGenericRepository<Concentration>
	{
		Task<List<ConcentrationLookupDto>> GetConcentrationLookupsAsync();
		Task<List<ConcentrationResponse>> GetAllConcentrationsAsync();
		Task<ConcentrationResponse?> GetConcentrationByIdAsync(int id);
	}
}
