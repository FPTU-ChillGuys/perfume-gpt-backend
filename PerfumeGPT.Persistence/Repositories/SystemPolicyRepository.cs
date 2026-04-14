using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Policies;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class SystemPolicyRepository : GenericRepository<SystemPolicy>, ISystemPolicyRepository
	{
		public SystemPolicyRepository(PerfumeDbContext context) : base(context) { }

		public async Task<SystemPolicyResponse?> GetResponseByPolicyCodeAsync(string policyCode)
		{
			var normalizedPolicyCode = policyCode.Trim().ToUpperInvariant();

			return await _context.SystemPolicies
				.AsNoTracking()
				.Where(sp => sp.Id == normalizedPolicyCode)
				.Select(sp => new SystemPolicyResponse
				{
					PolicyCode = sp.Id,
					Title = sp.Title,
					HtmlContent = sp.HtmlContent,
					LastUpdated = sp.UpdatedAt ?? sp.CreatedAt
				})
				.FirstOrDefaultAsync();
		}

		public async Task<SystemPolicy?> GetByPolicyCodeAsync(string policyCode)
		{
			var normalizedPolicyCode = policyCode.Trim().ToUpperInvariant();
			return await _context.SystemPolicies.FirstOrDefaultAsync(sp => sp.Id == normalizedPolicyCode);
		}
	}
}
