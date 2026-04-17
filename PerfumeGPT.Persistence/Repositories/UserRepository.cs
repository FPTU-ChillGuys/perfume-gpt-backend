using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Users;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class UserRepository : GenericRepository<User>, IUserRepository
	{
		private readonly UserManager<User> _userManager;

		public UserRepository(PerfumeDbContext context, UserManager<User> userManager) : base(context)
		{
			_userManager = userManager;
		}

		public async Task<User?> FindByPhoneNumberAsync(string phoneNumber)
		=> await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

		public async Task<User?> FindByPhoneOrEmailAsync(string phoneOrEmail)
		{
			var normalizedInput = phoneOrEmail.Trim().ToLower();
			return await _userManager.Users.FirstOrDefaultAsync(u =>
				((u.PhoneNumber != null && u.PhoneNumber == normalizedInput) ||
				(u.Email != null && u.Email.ToLower() == normalizedInput)) && u.IsActive && !u.IsDeleted);
		}

		public async Task<bool> IsPhoneNumberInUseAsync(string phoneNumber, Guid excludedUserId)
		=> await _context.Users.AnyAsync(u => u.Id != excludedUserId
			&& !u.IsDeleted
			&& u.PhoneNumber != null
			&& u.PhoneNumber == phoneNumber);

		public async Task<List<string>> GetActiveAdminEmailsAsync()
		{
			var adminUsers = await _userManager.GetUsersInRoleAsync(UserRole.admin.ToString());

			return [.. adminUsers
				.Where(u => u.IsActive && !u.IsDeleted && !string.IsNullOrWhiteSpace(u.Email))
				.Select(u => u.Email!)];
		}

		public async Task<List<UserManageItem>> GetUsersForManagementAsync()
		{
			var UserIds = _context.UserRoles
				.Join(_context.Roles,
					ur => ur.RoleId,
					r => r.Id,
					(ur, r) => new { ur.UserId, r.Name })
				.Where(x => x.Name == "user")
				.Select(x => x.UserId)
				.Distinct();

			return await _context.Users
				.Where(u => !u.IsDeleted && UserIds.Contains(u.Id))
				.OrderBy(u => u.FullName)
				.Select(u => new UserManageItem
				{
					Id = u.Id,
					UserName = u.UserName ?? string.Empty,
					FullName = u.FullName,
					Email = u.Email ?? string.Empty,
					PhoneNumber = u.PhoneNumber ?? string.Empty,
					IsActive = u.IsActive,
					ProfileImageUrl = u.ProfilePicture != null ? u.ProfilePicture.Url : null,
					DeliveryRefusalCount = u.DeliveryRefusalCount,
					CodBlockedUntil = u.CodBlockedUntil
				})
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<StaffManageItem>> GetStaffForManagementAsync()
		{
			var staffUserIds = _context.UserRoles
				.Join(_context.Roles,
					ur => ur.RoleId,
					r => r.Id,
					(ur, r) => new { ur.UserId, r.Name })
				.Where(x => x.Name == "staff")
				.Select(x => x.UserId)
				.Distinct();

			return await _context.Users
				.Where(u => !u.IsDeleted && staffUserIds.Contains(u.Id))
				.OrderBy(u => u.FullName)
				.Select(u => new StaffManageItem
				{
					Id = u.Id,
					UserName = u.UserName ?? string.Empty,
					FullName = u.FullName,
					Email = u.Email ?? string.Empty,
					PhoneNumber = u.PhoneNumber ?? string.Empty,
					IsActive = u.IsActive,
					ProfileImageUrl = u.ProfilePicture != null ? u.ProfilePicture.Url : null,
				})
				.AsNoTracking()
				.ToListAsync();
		}
	}
}
