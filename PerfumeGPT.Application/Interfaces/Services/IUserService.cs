using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Users;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IUserService
	{
		Task<BaseResponse<List<StaffLookupItem>>> GetStaffLookupAsync();
	}
}
