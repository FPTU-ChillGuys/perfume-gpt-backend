using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Users;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class UserService : IUserService
	{
		private readonly UserManager<User> _userManager;
		private readonly IUserRepository _userRepository;
		private readonly IMapper _mapper;

		public UserService(UserManager<User> userManager, IUserRepository userRepository, IMapper mapper)
		{
			_userManager = userManager;
			_userRepository = userRepository;
			_mapper = mapper;
		}

		public async Task<User?> GetByPhoneOrEmailAsync(string phoneOrEmail)
		{
			var user = await _userRepository.FindByPhoneOrEmailAsync(phoneOrEmail);
			if (user == null)
			{
				return null;
			}
			return user;
		}

		public async Task<BaseResponse<CustomerForPosResponse>> GetCustomerForPosAsync(string phoneOrEmail)
		{
			if (string.IsNullOrWhiteSpace(phoneOrEmail))
			{
				return BaseResponse<CustomerForPosResponse>.Fail("Phone number or email is required.", ResponseErrorType.BadRequest);
			}

			var user = await _userRepository.FindByPhoneOrEmailAsync(phoneOrEmail);
			if (user == null || user.IsDeleted || !user.IsActive)
			{
				return BaseResponse<CustomerForPosResponse>.Fail("Customer not found.", ResponseErrorType.NotFound);
			}

			var response = new CustomerForPosResponse
			{
				Id = user.Id,
				FullName = user.FullName,
				PhoneNumber = user.PhoneNumber ?? string.Empty,
				Email = user.Email ?? string.Empty,
				LoyaltyPoint = user.PointBalance
			};

			return BaseResponse<CustomerForPosResponse>.Ok(response, "Customer retrieved successfully.");
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

		public async Task<BaseResponse<string>> GetEmailByIdAsync(Guid userId)
		{
			try
			{
				var user = await _userRepository.GetByIdAsync(userId);
				if (user == null)
				{
					return BaseResponse<string>.Fail("User not found.", ResponseErrorType.NotFound);
				}
				var email = user.Email;
				return BaseResponse<string>.Ok(email ?? string.Empty, "Email retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error retrieving email: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<UserCredentialsResponse>> GetUserCredentialsAsync(Guid userId)
		{
			var user = await _userRepository.GetByIdAsync(userId);
			if (user == null)
			{
				return BaseResponse<UserCredentialsResponse>.Fail("User not found.", ResponseErrorType.NotFound);
			}
			var userCredentials = _mapper.Map<UserCredentialsResponse>(user);
			return BaseResponse<UserCredentialsResponse>.Ok(userCredentials, "User credentials retrieved successfully.");
		}
	}
}
