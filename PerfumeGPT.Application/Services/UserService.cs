using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using PerfumeGPT.Application.DTOs.Requests.Users;
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
				return BaseResponse<CustomerForPosResponse>.Fail("Bắt buộc nhập số điện thoại hoặc email.", ResponseErrorType.BadRequest);
			}

			var user = await _userRepository.FindByPhoneOrEmailAsync(phoneOrEmail);
			if (user == null || user.IsDeleted || !user.IsActive)
			{
				return BaseResponse<CustomerForPosResponse>.Fail("Không tìm thấy khách hàng.", ResponseErrorType.NotFound);
			}

			var response = new CustomerForPosResponse
			{
				Id = user.Id,
				FullName = user.FullName,
				PhoneNumber = user.PhoneNumber ?? string.Empty,
				Email = user.Email ?? string.Empty,
				LoyaltyPoint = user.PointBalance
			};

			return BaseResponse<CustomerForPosResponse>.Ok(response, "Lấy thông tin khách hàng thành công.");
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

				return BaseResponse<List<StaffLookupItem>>.Ok(allStaff, "Lấy danh sách tra cứu nhân viên thành công.");
			}
			catch (Exception ex)
			{
				return BaseResponse<List<StaffLookupItem>>.Fail($"Lỗi khi lấy danh sách tra cứu nhân viên: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<List<StaffManageItem>>> GetStaffForManagementAsync()
		{
			var staffs = await _userRepository.GetStaffForManagementAsync();
			return BaseResponse<List<StaffManageItem>>.Ok(staffs, "Lấy danh sách nhân viên thành công.");
		}

		public async Task<BaseResponse<List<UserManageItem>>> GetUsersForManagementAsync()
		{
			var users = await _userRepository.GetUsersForManagementAsync();
			return BaseResponse<List<UserManageItem>>.Ok(users, "Lấy danh sách người dùng thành công.");
		}

		public async Task<BaseResponse<string>> UpdateUserBasicInfoAsync(Guid userId, UpdateUserBasicInfoRequest request)
		{
			var user = await _userRepository.GetByIdAsync(userId);
			if (user == null || user.IsDeleted)
			{
				return BaseResponse<string>.Fail("Không tìm thấy người dùng.", ResponseErrorType.NotFound);
			}

			var phoneNumber = request.PhoneNumber.Trim();
			var hasDuplicatePhone = await _userRepository.IsPhoneNumberInUseAsync(phoneNumber, userId);

			if (hasDuplicatePhone)
			{
				return BaseResponse<string>.Fail("Số điện thoại đã được sử dụng.", ResponseErrorType.BadRequest);
			}

			user.UpdateBasicInfo(request.FullName, phoneNumber);

			var result = await _userManager.UpdateAsync(user);
			if (!result.Succeeded)
			{
				var error = string.Join("; ", result.Errors.Select(e => e.Description));
				return BaseResponse<string>.Fail($"Cập nhật thông tin người dùng thất bại: {error}", ResponseErrorType.BadRequest);
			}

			return BaseResponse<string>.Ok(userId.ToString(), "Cập nhật thông tin người dùng thành công.");
		}

		public async Task<BaseResponse<string>> InactiveUserAsync(Guid userId)
		{
			var user = await _userRepository.GetByIdAsync(userId);
			if (user == null || user.IsDeleted)
			{
				return BaseResponse<string>.Fail("Không tìm thấy người dùng.", ResponseErrorType.NotFound);
			}

			var isAdmin = await _userManager.IsInRoleAsync(user, "amin");
			if (isAdmin)
			{
				return BaseResponse<string>.Fail("Người dùng là admin không thể bị vô hiệu hóa.", ResponseErrorType.BadRequest);
			}

			if (!user.IsActive)
			{
				return BaseResponse<string>.Fail("Người dùng đã ở trạng thái không hoạt động.", ResponseErrorType.BadRequest);
			}

			user.Deactivate();
			var updateResult = await _userManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				var error = string.Join("; ", updateResult.Errors.Select(e => e.Description));
				return BaseResponse<string>.Fail($"Vô hiệu hóa người dùng thất bại: {error}", ResponseErrorType.BadRequest);
			}

			return BaseResponse<string>.Ok(userId.ToString(), "Vô hiệu hóa người dùng thành công.");
		}

		public async Task<BaseResponse<string>> GetEmailByIdAsync(Guid userId)
		{
			try
			{
				var user = await _userRepository.GetByIdAsync(userId);
				if (user == null)
				{
					return BaseResponse<string>.Fail("Không tìm thấy người dùng.", ResponseErrorType.NotFound);
				}
				var email = user.Email;
				return BaseResponse<string>.Ok(email ?? string.Empty, "Lấy email thành công.");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Lỗi khi lấy email: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<UserCredentialsResponse>> GetUserCredentialsAsync(Guid userId)
		{
			var user = await _userRepository.GetByIdAsync(userId);
			if (user == null)
			{
				return BaseResponse<UserCredentialsResponse>.Fail("Không tìm thấy người dùng.", ResponseErrorType.NotFound);
			}
			var userCredentials = _mapper.Map<UserCredentialsResponse>(user);
			return BaseResponse<UserCredentialsResponse>.Ok(userCredentials, "Lấy thông tin đăng nhập người dùng thành công.");
		}
	}
}
