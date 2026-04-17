using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IVoucherRepository : IGenericRepository<Voucher>
	{
		Task<bool> CodeExistsAsync(string code, Guid? excludeVoucherId = null);
		Task<VoucherResponse?> GetByCodeAsync(string code);
		Task<VoucherResponse?> GetByIdResponseAsync(Guid voucherId);
		Task<List<VoucherResponse>> GetByCampaignIdAsync(Guid campaignId);
		Task<List<VoucherResponse>> GetPublicVouchersForApplicabilityAsync();
		Task<(List<VoucherResponse> Items, int TotalCount)> GetPagedVouchersAsync(GetPagedVouchersRequest request);
		Task<(List<RedeemableVoucherResponse> Items, int TotalCount)> GetPagedRedeemableVouchersAsync(GetPagedRedeemableVouchersRequest request, Guid? userId = null);
	}
}


