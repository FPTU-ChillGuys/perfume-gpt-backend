using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Persistence.Repositories
{
	public class UserRepository : IUserRepository
	{
		private readonly UserManager<User> _userManager;

		public UserRepository(UserManager<User> userManager)
		{
			_userManager = userManager;
		}

		public async Task<User?> FindByPhoneNumberAsync(string phoneNumber)
		{
			if (string.IsNullOrWhiteSpace(phoneNumber))
				return null;
			return await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
		}
	}
}
