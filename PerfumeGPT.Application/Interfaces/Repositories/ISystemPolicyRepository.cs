using PerfumeGPT.Application.DTOs.Responses.Policies;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ISystemPolicyRepository : IGenericRepository<SystemPolicy>
	{
		Task<SystemPolicyResponse?> GetResponseByPolicyCodeAsync(string policyCode);
		Task<SystemPolicy?> GetByPolicyCodeAsync(string policyCode);
	}
}
