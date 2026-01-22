using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class VoucherService : IVoucherService
	{
		private readonly IVoucherRepository _voucherRepository;
		private readonly IUserVoucherRepository _userVoucherRepository;
		private readonly ILoyaltyPointRepository _loyaltyPointRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<CreateVoucherRequest>? _createValidator;
		private readonly IValidator<UpdateVoucherRequest>? _updateValidator;

		public VoucherService(
			IVoucherRepository voucherRepository,
			IUserVoucherRepository userVoucherRepository,
			ILoyaltyPointRepository loyaltyPointRepository,
			IUnitOfWork unitOfWork,
			IValidator<CreateVoucherRequest>? createValidator = null,
			IValidator<UpdateVoucherRequest>? updateValidator = null)
		{
			_voucherRepository = voucherRepository;
			_userVoucherRepository = userVoucherRepository;
			_loyaltyPointRepository = loyaltyPointRepository;
			_unitOfWork = unitOfWork;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		#region Admin Operations

		public async Task<BaseResponse<string>> CreateVoucherAsync(CreateVoucherRequest request)
		{
			if (_createValidator != null)
			{
				var validationResult = await _createValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return BaseResponse<string>.Fail(
						"Validation failed",
						ResponseErrorType.BadRequest,
						validationResult.Errors.Select(e => e.ErrorMessage).ToList()
					);
				}
			}

			// Check if voucher code already exists
			var existingVoucher = await _voucherRepository.FirstOrDefaultAsync(
				v => v.Code == request.Code && !v.IsDeleted,
				asNoTracking: true
			);

			if (existingVoucher != null)
			{
				return BaseResponse<string>.Fail(
					"Voucher code already exists",
					ResponseErrorType.Conflict
				);
			}

			var voucher = new Voucher
			{
				Code = request.Code.ToUpper(),
				DiscountValue = request.DiscountValue,
				DiscountType = request.DiscountType,
				RequiredPoints = request.RequiredPoints,
				MinOrderValue = request.MinOrderValue,
				ExpiryDate = request.ExpiryDate
			};

			await _voucherRepository.AddAsync(voucher);
			var saved = await _voucherRepository.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail(
					"Failed to create voucher",
					ResponseErrorType.InternalError
				);
			}

			return BaseResponse<string>.Ok(voucher.Id.ToString(), "Voucher created successfully");
		}

		public async Task<BaseResponse<string>> UpdateVoucherAsync(Guid voucherId, UpdateVoucherRequest request)
		{
			if (_updateValidator != null)
			{
				var validationResult = await _updateValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					return BaseResponse<string>.Fail(
						"Validation failed",
						ResponseErrorType.BadRequest,
						validationResult.Errors.Select(e => e.ErrorMessage).ToList()
					);
				}
			}

			var voucher = await _voucherRepository.GetByIdAsync(voucherId);
			if (voucher == null || voucher.IsDeleted)
			{
				return BaseResponse<string>.Fail(
					"Voucher not found",
					ResponseErrorType.NotFound
				);
			}

			// Check if code is being updated and already exists
			if (request.Code != null && request.Code != voucher.Code)
			{
				var existingVoucher = await _voucherRepository.FirstOrDefaultAsync(
					v => v.Code == request.Code && v.Id != voucherId && !v.IsDeleted,
					asNoTracking: true
				);

				if (existingVoucher != null)
				{
					return BaseResponse<string>.Fail(
						"Voucher code already exists",
						ResponseErrorType.Conflict
					);
				}

				voucher.Code = request.Code.ToUpper();
			}

			if (request.DiscountValue.HasValue)
				voucher.DiscountValue = request.DiscountValue.Value;

			if (request.DiscountType.HasValue)
				voucher.DiscountType = request.DiscountType.Value;

			if (request.RequiredPoints.HasValue)
				voucher.RequiredPoints = request.RequiredPoints.Value;

			if (request.MinOrderValue.HasValue)
				voucher.MinOrderValue = request.MinOrderValue.Value;

			if (request.ExpiryDate.HasValue)
				voucher.ExpiryDate = request.ExpiryDate.Value;

			_voucherRepository.Update(voucher);
			var saved = await _voucherRepository.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail(
					"Failed to update voucher",
					ResponseErrorType.InternalError
				);
			}

			return BaseResponse<string>.Ok(voucherId.ToString(), "Voucher updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId)
		{
			var voucher = await _voucherRepository.GetByIdAsync(voucherId);
			if (voucher == null || voucher.IsDeleted)
			{
				return BaseResponse<string>.Fail(
					"Voucher not found",
					ResponseErrorType.NotFound
				);
			}

			_voucherRepository.Update(voucher);
			var saved = await _voucherRepository.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail(
					"Failed to delete voucher",
					ResponseErrorType.InternalError
				);
			}

			return BaseResponse<string>.Ok(voucherId.ToString(), "Voucher deleted successfully");
		}

		public async Task<BaseResponse<VoucherResponse>> GetVoucherAsync(Guid voucherId)
		{
			var voucher = await _voucherRepository.FirstOrDefaultAsync(
				v => v.Id == voucherId && !v.IsDeleted,
				asNoTracking: true
			);

			if (voucher == null)
			{
				return BaseResponse<VoucherResponse>.Fail(
					"Voucher not found",
					ResponseErrorType.NotFound
				);
			}

			var response = MapToVoucherResponse(voucher);
			return BaseResponse<VoucherResponse>.Ok(response, "Voucher retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<VoucherResponse>>> GetVouchersAsync(GetPagedVouchersRequest request)
		{
			var now = DateTime.UtcNow;

			var (items, totalCount) = await _voucherRepository.GetPagedAsync(
				filter: v => !v.IsDeleted &&
					(!request.IsExpired.HasValue ||
						(request.IsExpired.Value && v.ExpiryDate < now) ||
						(!request.IsExpired.Value && v.ExpiryDate >= now)) &&
					(string.IsNullOrEmpty(request.Code) || v.Code.Contains(request.Code)),
				orderBy: q => q.OrderByDescending(v => v.CreatedAt),
				pageNumber: request.PageNumber,
				pageSize: request.PageSize,
				asNoTracking: true
			);

			var voucherList = items.Select(MapToVoucherResponse).ToList();
			var pagedResult = new PagedResult<VoucherResponse>(
				voucherList,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<VoucherResponse>>.Ok(
				pagedResult,
				"Vouchers retrieved successfully"
			);
		}

		#endregion

		#region User Operations

		public async Task<BaseResponse<string>> RedeemVoucherAsync(Guid userId, RedeemVoucherRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var voucher = await _voucherRepository.FirstOrDefaultAsync(
					v => v.Id == request.VoucherId && !v.IsDeleted,
					asNoTracking: false
				);

				if (voucher == null)
				{
					return BaseResponse<string>.Fail(
						"Voucher not found",
						ResponseErrorType.NotFound
					);
				}

				if (voucher.ExpiryDate < DateTime.UtcNow)
				{
					return BaseResponse<string>.Fail(
						"Voucher has expired",
						ResponseErrorType.BadRequest
					);
				}

				// Check if user already redeemed this voucher
				var existingUserVoucher = await _userVoucherRepository.FirstOrDefaultAsync(
					uv => uv.UserId == userId && uv.VoucherId == request.VoucherId,
					asNoTracking: true
				);

				if (existingUserVoucher != null)
				{
					return BaseResponse<string>.Fail(
						"Voucher already redeemed",
						ResponseErrorType.Conflict
					);
				}

				// Check loyalty points
				var loyaltyPoint = await _loyaltyPointRepository.FirstOrDefaultAsync(
					lp => lp.UserId == userId,
					asNoTracking: false
				);

				if (loyaltyPoint == null || loyaltyPoint.PointBalance < voucher.RequiredPoints)
				{
					return BaseResponse<string>.Fail(
						"Insufficient loyalty points",
						ResponseErrorType.BadRequest
					);
				}

				// Deduct points
				loyaltyPoint.PointBalance -= (int)voucher.RequiredPoints;
				_loyaltyPointRepository.Update(loyaltyPoint);

				// Create user voucher
				var userVoucher = new UserVoucher
				{
					UserId = userId,
					VoucherId = request.VoucherId,
					IsUsed = false
				};

				await _userVoucherRepository.AddAsync(userVoucher);

				// No need to call SaveChangesAsync here - ExecuteInTransactionAsync will handle it automatically
				return BaseResponse<string>.Ok(
					userVoucher.Id.ToString(),
					"Voucher redeemed successfully"
				);
			});
		}

		public async Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(
			Guid userId,
			int pageNumber = 1,
			int pageSize = 10)
		{
			var (items, totalCount) = await _userVoucherRepository.GetPagedAsync(
				filter: uv => uv.UserId == userId,
				include: q => q.Include(uv => uv.Voucher),
				orderBy: q => q.OrderByDescending(uv => uv.CreatedAt),
				pageNumber: pageNumber,
				pageSize: pageSize,
				asNoTracking: true
			);

			var now = DateTime.UtcNow;
			var userVoucherList = items.Select(uv => new UserVoucherResponse
			{
				Id = uv.Id,
				VoucherId = uv.VoucherId,
				Code = uv.Voucher.Code,
				DiscountValue = uv.Voucher.DiscountValue,
				DiscountType = uv.Voucher.DiscountType.ToString(),
				MinOrderValue = uv.Voucher.MinOrderValue,
				ExpiryDate = uv.Voucher.ExpiryDate,
				IsUsed = uv.IsUsed,
				IsExpired = uv.Voucher.ExpiryDate < now,
				RedeemedAt = uv.CreatedAt
			}).ToList();

			var pagedResult = new PagedResult<UserVoucherResponse>(
				userVoucherList,
				pageNumber,
				pageSize,
				totalCount
			);

			return BaseResponse<PagedResult<UserVoucherResponse>>.Ok(
				pagedResult,
				"User vouchers retrieved successfully"
			);
		}

		#endregion

		#region Apply Voucher Logic

		public async Task<BaseResponse<ApplyVoucherResponse>> ApplyVoucherToOrderAsync(
			Guid userId,
			ApplyVoucherRequest request)
		{
			var voucher = await _voucherRepository.FirstOrDefaultAsync(
				v => v.Code == request.VoucherCode.ToUpper() && !v.IsDeleted,
				asNoTracking: true
			);

			if (voucher == null)
			{
				return BaseResponse<ApplyVoucherResponse>.Fail(
					"Invalid voucher code",
					ResponseErrorType.NotFound
				);
			}

			if (voucher.ExpiryDate < DateTime.UtcNow)
			{
				return BaseResponse<ApplyVoucherResponse>.Fail(
					"Voucher has expired",
					ResponseErrorType.BadRequest
				);
			}

			if (request.OrderAmount < voucher.MinOrderValue)
			{
				return BaseResponse<ApplyVoucherResponse>.Fail(
					$"Minimum order value is {voucher.MinOrderValue:C}",
					ResponseErrorType.BadRequest
				);
			}

			// Check if user owns this voucher
			var userVoucher = await _userVoucherRepository.FirstOrDefaultAsync(
				uv => uv.UserId == userId && uv.VoucherId == voucher.Id && !uv.IsUsed,
				asNoTracking: true
			);

			if (userVoucher == null)
			{
				return BaseResponse<ApplyVoucherResponse>.Fail(
					"You don't own this voucher or it has already been used",
					ResponseErrorType.BadRequest
				);
			}

			// Calculate discount
			decimal discountAmount = voucher.DiscountType == Domain.Enums.DiscountType.Percentage
				? request.OrderAmount * (voucher.DiscountValue / 100m)
				: voucher.DiscountValue;

			// Ensure discount doesn't exceed order amount
			discountAmount = Math.Min(discountAmount, request.OrderAmount);
			var finalAmount = request.OrderAmount - discountAmount;

			var response = new ApplyVoucherResponse
			{
				VoucherId = voucher.Id,
				Code = voucher.Code,
				DiscountAmount = discountAmount,
				FinalAmount = finalAmount,
				DiscountType = voucher.DiscountType.ToString()
			};

			return BaseResponse<ApplyVoucherResponse>.Ok(
				response,
				"Voucher applied successfully"
			);
		}

		public async Task<BaseResponse<bool>> ValidateToApplyVoucherAsync(string voucherCode, Guid userId)
		{
			var voucher = await _voucherRepository.FirstOrDefaultAsync(
				v => v.Code == voucherCode.ToUpper() && !v.IsDeleted,
				asNoTracking: true
			);

			if (voucher == null)
			{
				return BaseResponse<bool>.Fail(
					"Invalid voucher code",
					ResponseErrorType.NotFound
				);
			}

			if (voucher.ExpiryDate < DateTime.UtcNow)
			{
				return BaseResponse<bool>.Fail(
					"Voucher has expired",
					ResponseErrorType.BadRequest
				);
			}

			var userVoucher = await _userVoucherRepository.FirstOrDefaultAsync(
				uv => uv.UserId == userId && uv.VoucherId == voucher.Id && !uv.IsUsed,
				asNoTracking: true
			);


			if (userVoucher == null)
			{
				return BaseResponse<bool>.Fail(
					"You don't own this voucher or it has already been used",
					ResponseErrorType.BadRequest
				);
			}

			return BaseResponse<bool>.Ok(true, "Voucher is valid");
		}

		public async Task<BaseResponse<bool>> MarkVoucherAsUsedAsync(Guid userId, Guid voucherId)
		{
			var userVoucher = await _userVoucherRepository.FirstOrDefaultAsync(
				uv => uv.UserId == userId && uv.VoucherId == voucherId && !uv.IsUsed,
				asNoTracking: false
			);

			if (userVoucher == null)
			{
				return BaseResponse<bool>.Fail(
					"User voucher not found or already used",
					ResponseErrorType.NotFound
				);
			}

			userVoucher.IsUsed = true;
			_userVoucherRepository.Update(userVoucher);
			var saved = await _userVoucherRepository.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<bool>.Fail(
					"Failed to mark voucher as used",
					ResponseErrorType.InternalError
				);
			}

			return BaseResponse<bool>.Ok(true, "Voucher marked as used successfully");
		}

		#endregion

		#region Helper Methods

		private static VoucherResponse MapToVoucherResponse(Voucher voucher)
		{
			return new VoucherResponse
			{
				Id = voucher.Id,
				Code = voucher.Code,
				DiscountValue = voucher.DiscountValue,
				DiscountType = voucher.DiscountType.ToString(),
				RequiredPoints = voucher.RequiredPoints,
				MinOrderValue = voucher.MinOrderValue,
				ExpiryDate = voucher.ExpiryDate,
				IsExpired = voucher.ExpiryDate < DateTime.UtcNow,
				CreatedAt = voucher.CreatedAt
			};
		}

		#endregion
	}
}
