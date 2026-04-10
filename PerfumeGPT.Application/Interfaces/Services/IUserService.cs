using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Requests.Users;
using PerfumeGPT.Application.DTOs.Responses.Users;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IUserService
	{
		Task<BaseResponse<List<StaffLookupItem>>> GetStaffLookupAsync();
		Task<BaseResponse<List<StaffManageItem>>> GetStaffForManagementAsync();
		Task<BaseResponse<string>> InactiveStaffAsync(Guid staffId);
		Task<BaseResponse<CustomerForPosResponse>> GetCustomerForPosAsync(string phoneOrEmail);
		Task<BaseResponse<string>> GetEmailByIdAsync(Guid userId);
		Task<BaseResponse<UserCredentialsResponse>> GetUserCredentialsAsync(Guid userId);
		Task<BaseResponse<string>> UpdateUserBasicInfoAsync(Guid userId, UpdateUserBasicInfoRequest request);
		Task<User?> GetByPhoneOrEmailAsync(string phoneOrEmail);
	}
}
