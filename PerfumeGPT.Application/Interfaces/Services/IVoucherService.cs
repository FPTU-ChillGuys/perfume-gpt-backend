using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IVoucherService
	{
		// Admin operations
		Task<BaseResponse<string>> CreateVoucherAsync(CreateVoucherRequest request);
		Task<BaseResponse<string>> UpdateVoucherAsync(Guid voucherId, UpdateVoucherRequest request);
		Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId);
		Task<BaseResponse<VoucherResponse>> GetVoucherAsync(Guid voucherId);
		Task<BaseResponse<PagedResult<VoucherResponse>>> GetVouchersAsync(GetPagedVouchersRequest request);

		// User operations
		Task<BaseResponse<string>> RedeemVoucherAsync(Guid userId, RedeemVoucherRequest request);
		Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(Guid userId, int pageNumber = 1, int pageSize = 10);

		// Apply voucher logic
		Task<BaseResponse<ApplyVoucherResponse>> ApplyVoucherToOrderAsync(Guid userId, ApplyVoucherRequest request);
		Task<BaseResponse<bool>> ValidateToApplyVoucherAsync(string voucherCode, Guid userId);

		// Mark voucher as used (called when order is completed)
		Task<BaseResponse<bool>> MarkVoucherAsUsedAsync(Guid userId, Guid voucherId);
	}
}

