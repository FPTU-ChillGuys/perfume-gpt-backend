using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IUserVoucherRepository : IGenericRepository<UserVoucher>
	{
		Task<(List<UserVoucher> Items, int TotalCount)> GetPagedWithVouchersAsync(
			Guid userId,
			GetPagedUserVouchersRequest request);
		Task MigrateGuestVouchersAsync(Guid userId, string email, string phoneNumber);
		Task<bool> HasRedeemedVoucherAsync(Guid userId, Guid voucherId, string? guestEmailOrPhone);
		Task<UserVoucher?> GetUnusedUserVoucherAsync(Guid userId, Guid voucherId);
	}
}


