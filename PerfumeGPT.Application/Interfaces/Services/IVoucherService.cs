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
		Task<BaseResponse<VoucherResponse>> GetVoucherByIdAsync(Guid voucherId);
		Task<BaseResponse<PagedResult<VoucherResponse>>> GetPagedVouchersAsync(GetPagedVouchersRequest request);

		// User operations
		Task<VoucherResponse?> GetVoucherByCodeAsync(string code);
		Task<BaseResponse<string>> RedeemVoucherAsync(Guid userId, RedeemVoucherRequest request);
		Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(Guid userId, GetPagedUserVouchersRequest request);

		// Apply voucher logic
		Task<BaseResponse<ApplyVoucherResponse>> ApplyVoucherToOrderAsync(Guid userId, ApplyVoucherRequest request);
		Task<BaseResponse<bool>> CanUserApplyVoucherAsync(string voucherCode, Guid userId);

		// Voucher status management
		Task<BaseResponse<bool>> MarkVoucherAsReservedAsync(Guid userId, Guid voucherId);
		Task<BaseResponse<bool>> MarkVoucherAsUsedAsync(Guid userId, Guid voucherId);
		Task<BaseResponse<bool>> ReleaseReservedVoucherAsync(Guid userId, Guid voucherId);

		Task<decimal> CalculateVoucherDiscountAsync(string voucherCode, decimal totalPrice);
	}
}

