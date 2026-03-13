using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
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
		{
			if (string.IsNullOrWhiteSpace(phoneNumber))
				return null;
			return await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
		}

		public async Task<User?> FindByPhoneOrEmailAsync(string phoneOrEmail)
		{
			if (string.IsNullOrWhiteSpace(phoneOrEmail))
				return null;
			var normalizedInput = phoneOrEmail.Trim().ToLower();
			return await _userManager.Users.FirstOrDefaultAsync(u =>
				(u.PhoneNumber != null && u.PhoneNumber == normalizedInput) ||
				(u.Email != null && u.Email.ToLower() == normalizedInput));
		}
	}
}
