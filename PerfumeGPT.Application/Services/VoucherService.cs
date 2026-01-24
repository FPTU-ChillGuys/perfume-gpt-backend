using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class VoucherService : IVoucherService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILoyaltyPointService _loyaltyPointService;
		private readonly IMapper _mapper;
		private readonly IValidator<CreateVoucherRequest> _createValidator;
		private readonly IValidator<UpdateVoucherRequest> _updateValidator;

		public VoucherService(
			IUnitOfWork unitOfWork,
			ILoyaltyPointService loyaltyPointService,
			IMapper mapper,
			IValidator<CreateVoucherRequest> createValidator,
			IValidator<UpdateVoucherRequest> updateValidator)
		{
			_unitOfWork = unitOfWork;
			_loyaltyPointService = loyaltyPointService;
			_mapper = mapper;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		#region Admin Operations

		public async Task<BaseResponse<string>> CreateVoucherAsync(CreateVoucherRequest request)
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

			try
			{
				// Use repository method to check if code exists
				var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code);
				if (codeExists)
				{
					return BaseResponse<string>.Fail(
						"Voucher code already exists",
						ResponseErrorType.Conflict
					);
				}

				// Use mapper to create Voucher entity
				var voucher = _mapper.Map<Voucher>(request);
				voucher.Code = request.Code.ToUpper();

				await _unitOfWork.Vouchers.AddAsync(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucher.Id.ToString(), "Voucher created successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error creating voucher: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<string>> UpdateVoucherAsync(Guid voucherId, UpdateVoucherRequest request)
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

			try
			{
				var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
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
					// Use repository method to check code existence (excluding current voucher)
					var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code, voucherId);
					if (codeExists)
					{
						return BaseResponse<string>.Fail(
							"Voucher code already exists",
							ResponseErrorType.Conflict
						);
					}
				}

				// Use mapper to update entity
				_mapper.Map(request, voucher);

				// Ensure code is uppercase if provided
				if (request.Code != null)
				{
					voucher.Code = request.Code.ToUpper();
				}

				_unitOfWork.Vouchers.Update(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucherId.ToString(), "Voucher updated successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error updating voucher: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId)
		{
			try
			{
				var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
				if (voucher == null || voucher.IsDeleted)
				{
					return BaseResponse<string>.Fail(
						"Voucher not found",
						ResponseErrorType.NotFound
					);
				}

				voucher.IsDeleted = true;
				_unitOfWork.Vouchers.Update(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucherId.ToString(), "Voucher deleted successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error deleting voucher: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<VoucherResponse>> GetVoucherAsync(Guid voucherId)
		{
			var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(
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

			// Use mapper to convert to response DTO
			var response = _mapper.Map<VoucherResponse>(voucher);
			return BaseResponse<VoucherResponse>.Ok(response, "Voucher retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<VoucherResponse>>> GetVouchersAsync(GetPagedVouchersRequest request)
		{
			// Use repository method with filter logic
			var (items, totalCount) = await _unitOfWork.Vouchers.GetPagedVouchersAsync(request);

			// Use mapper to convert to response DTOs
			var voucherList = _mapper.Map<List<VoucherResponse>>(items);

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
			try
			{
				// This NEEDS transaction because it does MULTIPLE operations:
				// 1. Deduct loyalty points
				// 2. Create user voucher
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(
						v => v.Id == request.VoucherId && !v.IsDeleted,
						asNoTracking: true
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

					// Use repository method to check if user already redeemed this voucher
					var hasRedeemed = await _unitOfWork.UserVouchers.HasRedeemedVoucherAsync(userId, request.VoucherId);
					if (hasRedeemed)
					{
						return BaseResponse<string>.Fail(
							"Voucher already redeemed",
							ResponseErrorType.Conflict
						);
					}

					// Use LoyaltyPointService to deduct points
					var remainingPoints = await _loyaltyPointService.RedeemPointAsync(userId, (int)voucher.RequiredPoints);
					if (remainingPoints < 0)
					{
						return BaseResponse<string>.Fail(
							"Insufficient loyalty points",
							ResponseErrorType.BadRequest
						);
					}

					// Create user voucher
					var userVoucher = new UserVoucher
					{
						UserId = userId,
						VoucherId = request.VoucherId,
						IsUsed = false,
						Status = UsageStatus.Available
					};

					await _unitOfWork.UserVouchers.AddAsync(userVoucher);
					// Don't save - let transaction orchestrator handle it

					return BaseResponse<string>.Ok(
						userVoucher.Id.ToString(),
						"Voucher redeemed successfully"
					);
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error redeeming voucher: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(
			Guid userId,
			GetUserVouchersRequest request)
		{
			// Use repository method with includes, sorting, and filtering
			var (items, totalCount) = await _unitOfWork.UserVouchers.GetPagedWithVouchersAsync(
				userId,
				request
			);

			// Use mapper to convert to response DTOs
			var userVoucherList = _mapper.Map<List<UserVoucherResponse>>(items);

			var pagedResult = new PagedResult<UserVoucherResponse>(
				userVoucherList,
				request.PageNumber,
				request.PageSize,
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
			// Use repository method to get voucher by code
			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(request.VoucherId);

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

			// Use repository method to check if user owns this voucher
			var userVoucher = await _unitOfWork.UserVouchers.GetUnusedUserVoucherAsync(userId, voucher.Id);
			if (userVoucher == null)
			{
				return BaseResponse<ApplyVoucherResponse>.Fail(
					"You don't own this voucher or it has already been used",
					ResponseErrorType.BadRequest
				);
			}

			// Check if voucher is in correct status (Available)
			if (userVoucher.Status != UsageStatus.Available)
			{
				return BaseResponse<ApplyVoucherResponse>.Fail(
					"Voucher is not available for use",
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

		public async Task<BaseResponse<bool>> ValidateToApplyVoucherAsync(Guid voucherId, Guid userId)
		{
			// Use repository method to get voucher by code
			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);

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

			// Use repository method to check if user owns unused voucher
			var userVoucher = await _unitOfWork.UserVouchers.GetUnusedUserVoucherAsync(userId, voucher.Id);
			if (userVoucher == null)
			{
				return BaseResponse<bool>.Fail(
					"You don't own this voucher or it has already been used",
					ResponseErrorType.BadRequest
				);
			}

			// Check if voucher is in correct status (Available)
			if (userVoucher.Status != UsageStatus.Available)
			{
				return BaseResponse<bool>.Fail(
					"Voucher is not available for use",
					ResponseErrorType.BadRequest
				);
			}

			return BaseResponse<bool>.Ok(true, "Voucher is valid");
		}

		public async Task<BaseResponse<bool>> MarkVoucherAsReservedAsync(Guid userId, Guid voucherId)
		{
			try
			{
				// This NEEDS transaction because it's a critical state change
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
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

					if (userVoucher.Status != UsageStatus.Available)
					{
						return BaseResponse<bool>.Fail(
							"Voucher is not available to reserve",
							ResponseErrorType.BadRequest
						);
					}

					userVoucher.Status = UsageStatus.Reserved;
					_unitOfWork.UserVouchers.Update(userVoucher);
					// Don't save - let transaction orchestrator handle it

					return BaseResponse<bool>.Ok(true, "Voucher marked as reserved successfully");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail(
					$"Error marking voucher as reserved: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<bool>> MarkVoucherAsUsedAsync(Guid userId, Guid voucherId)
		{
			try
			{
				// This NEEDS transaction because it's a critical state change
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
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

					if (userVoucher.Status != UsageStatus.Reserved)
					{
						return BaseResponse<bool>.Fail(
							"Voucher must be reserved before marking as used",
							ResponseErrorType.BadRequest
						);
					}

					userVoucher.IsUsed = true;
					userVoucher.Status = UsageStatus.Used;
					_unitOfWork.UserVouchers.Update(userVoucher);
					// Don't save - let transaction orchestrator handle it

					return BaseResponse<bool>.Ok(true, "Voucher marked as used successfully");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail(
					$"Error marking voucher as used: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<bool>> ReleaseReservedVoucherAsync(Guid userId, Guid voucherId)
		{
			try
			{
				// This NEEDS transaction because it's a critical state change
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
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

					if (userVoucher.Status == UsageStatus.Reserved)
					{
						userVoucher.Status = UsageStatus.Available;
						_unitOfWork.UserVouchers.Update(userVoucher);
					}
					// Don't save - let transaction orchestrator handle it

					return BaseResponse<bool>.Ok(true, "Voucher released successfully");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail(
					$"Error releasing voucher: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<decimal> CalculateVoucherDiscountAsync(Guid voucherId, decimal totalPrice)
		{
			try
			{
				var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
				if (voucher == null)
				{
					return totalPrice;
				}

				var discountAmount = voucher.DiscountType switch
				{
					DiscountType.Percentage => totalPrice * (voucher.DiscountValue / 100m),
					DiscountType.Fixed => voucher.DiscountValue,
					_ => 0m
				};

				var finalPrice = totalPrice - discountAmount;
				return finalPrice < 0m ? 0m : finalPrice;
			}
			catch
			{
				return totalPrice;
			}
		}

		public async Task<Voucher?> GetVoucherByCodeAsync(string code)
		{
			return await _unitOfWork.Vouchers.FirstOrDefaultAsync(
				v => v.Code.ToUpper() == code.ToUpper() && !v.IsDeleted,
				asNoTracking: true
			);
		}

		#endregion
	}
}
