using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Responses.Users;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IUserRepository : IGenericRepository<User>
	{
		Task<User?> FindByPhoneNumberAsync(string phoneNumber);
		Task<User?> FindByPhoneOrEmailAsync(string phoneOrEmail);
		Task<bool> IsPhoneNumberInUseAsync(string phoneNumber, Guid excludedUserId);
		Task<List<string>> GetActiveAdminEmailsAsync();
		Task<List<StaffManageItem>> GetStaffForManagementAsync();
	}
}
