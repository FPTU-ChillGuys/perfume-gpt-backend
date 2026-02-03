using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Users;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class UserService : IUserService
	{
		private readonly UserManager<User> _userManager;

		public UserService(UserManager<User> userManager)
		{
			_userManager = userManager;
		}

		public async Task<BaseResponse<List<StaffLookupItem>>> GetStaffLookupAsync()
		{
			try
			{
				// Get all users in the "staff" role
				var staffUsers = await _userManager.GetUsersInRoleAsync("staff");

				// Also get users in the "admin" role (admins can also verify)
				var adminUsers = await _userManager.GetUsersInRoleAsync("admin");

				// Combine and remove duplicates (in case a user has both roles)
				var allStaff = staffUsers.Union(adminUsers)
					.Where(u => !u.IsDeleted && u.IsActive)
					.OrderBy(u => u.FullName)
					.Select(u => new StaffLookupItem
					{
						Id = u.Id,
						UserName = u.UserName ?? string.Empty,
						FullName = u.FullName,
						Email = u.Email ?? string.Empty
					})
					.ToList();

				return BaseResponse<List<StaffLookupItem>>.Ok(allStaff, "Staff lookup retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<List<StaffLookupItem>>.Fail($"Error retrieving staff lookup: {ex.Message}", ResponseErrorType.InternalError);
			}
		}
	}
}
