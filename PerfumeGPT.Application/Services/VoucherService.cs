using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class VoucherService : IVoucherService
	{
		#region Dependencies

		private readonly IUnitOfWork _unitOfWork;
		private readonly ICampaignRepository _campaignRepository;
		private readonly IPromotionItemRepository _promotionItemRepository;
		private readonly IUserService _userService;
		private readonly ILoyaltyTransactionService _loyaltyTransactionService;
		private readonly IMapper _mapper;
		private readonly IValidator<CreateVoucherRequest> _createValidator;
		private readonly IValidator<CreateCampaignVoucherRequest> _createCampaignValidator;
		private readonly IValidator<UpdateVoucherRequest> _updateValidator;
		private readonly IValidator<UpdateCampaignVoucherRequest> _updateCampaignValidator;

		public VoucherService(
			IUnitOfWork unitOfWork,
			ICampaignRepository campaignRepository,
		 IPromotionItemRepository promotionItemRepository,
			IMapper mapper,
			IValidator<CreateVoucherRequest> createValidator,
			IValidator<CreateCampaignVoucherRequest> createCampaignValidator,
			IValidator<UpdateVoucherRequest> updateValidator,
			IUserService userService,
			ILoyaltyTransactionService loyaltyTransactionService,
			IValidator<UpdateCampaignVoucherRequest> updateCampaignValidator)
		{
			_unitOfWork = unitOfWork;
			_campaignRepository = campaignRepository;
			_promotionItemRepository = promotionItemRepository;
			_mapper = mapper;
			_createValidator = createValidator;
			_createCampaignValidator = createCampaignValidator;
			_updateValidator = updateValidator;
			_userService = userService;
			_loyaltyTransactionService = loyaltyTransactionService;
			_updateCampaignValidator = updateCampaignValidator;
		}

		#endregion Dependencies

		#region Admin Operations

		public async Task<BaseResponse<string>> CreateRegularVoucherAsync(CreateVoucherRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
				);
			}

			try
			{
				var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code);
				if (codeExists)
				{
					return BaseResponse<string>.Fail(
						"Voucher code already exists",
						ResponseErrorType.Conflict
					);
				}

				var voucher = _mapper.Map<Voucher>(request);

				await _unitOfWork.Vouchers.AddAsync(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucher.Id.ToString(), "Regular voucher created successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error creating regular voucher: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<string>> CreateCampaignVoucherAsync(Guid campaignId, CreateCampaignVoucherRequest request)
		{
			var validationResult = await _createCampaignValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
				);
			}

			try
			{
				var campaign = await _campaignRepository.GetByIdAsync(campaignId);
				if (campaign == null)
				{
					return BaseResponse<string>.Fail("Campaign not found", ResponseErrorType.NotFound);
				}

				var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code);
				if (codeExists)
				{
					return BaseResponse<string>.Fail("Voucher code already exists", ResponseErrorType.Conflict);
				}

				var voucher = _mapper.Map<Voucher>(request);
				voucher.CampaignId = campaignId;
				voucher.ExpiryDate = campaign.EndDate;

				await _unitOfWork.Vouchers.AddAsync(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucher.Id.ToString(), "Campaign voucher created successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error creating campaign voucher: {ex.Message}",
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
					return BaseResponse<string>.Fail("Voucher not found", ResponseErrorType.NotFound);
				}

				if (voucher.CampaignId.HasValue)
				{
					return BaseResponse<string>.Fail("Use campaign voucher endpoint for campaign voucher updates", ResponseErrorType.BadRequest);
				}

				if (request.Code != voucher.Code)
				{
					var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code, voucherId);
					if (codeExists)
					{
						return BaseResponse<string>.Fail("Voucher code already exists", ResponseErrorType.Conflict);
					}
				}

				_mapper.Map(request, voucher);
				_unitOfWork.Vouchers.Update(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucherId.ToString(), "Voucher updated successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error updating voucher: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> UpdateCampaignVoucherAsync(Guid campaignId, Guid voucherId, UpdateCampaignVoucherRequest request)
		{
			var validationResult = await _updateCampaignValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, validationResult.Errors.Select(e => e.ErrorMessage).ToList());
			}

			try
			{
				var campaign = await _campaignRepository.GetByIdAsync(campaignId);
				if (campaign == null)
				{
					return BaseResponse<string>.Fail("Campaign not found", ResponseErrorType.NotFound);
				}

				var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId && v.CampaignId == campaignId && !v.IsDeleted);
				if (voucher == null)
				{
					return BaseResponse<string>.Fail("Campaign voucher not found", ResponseErrorType.NotFound);
				}

				if (request.Code != voucher.Code)
				{
					var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code, voucherId);
					if (codeExists)
					{
						return BaseResponse<string>.Fail("Voucher code already exists", ResponseErrorType.Conflict);
					}
				}

				_mapper.Map(request, voucher);
				voucher.CampaignId = campaignId;
				_unitOfWork.Vouchers.Update(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucherId.ToString(), "Campaign voucher updated successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error updating campaign voucher: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId)
		{
			try
			{
				var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
				if (voucher == null || voucher.IsDeleted)
				{
					return BaseResponse<string>.Fail("Voucher not found", ResponseErrorType.NotFound);
				}

				if (voucher.CampaignId.HasValue)
				{
					return BaseResponse<string>.Fail("Use campaign voucher endpoint for campaign voucher deletion", ResponseErrorType.BadRequest);
				}

				if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == voucherId && !uv.IsUsed))
				{
					return BaseResponse<string>.Fail("Cannot delete voucher that has been redeemed by users", ResponseErrorType.BadRequest);
				}

				_unitOfWork.Vouchers.Remove(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucherId.ToString(), "Voucher deleted successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error deleting voucher: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> DeleteCampaignVoucherAsync(Guid campaignId, Guid voucherId)
		{
			try
			{
				var campaign = await _campaignRepository.GetByIdAsync(campaignId);
				if (campaign == null)
				{
					return BaseResponse<string>.Fail("Campaign not found", ResponseErrorType.NotFound);
				}

				var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId && v.CampaignId == campaignId && !v.IsDeleted);
				if (voucher == null)
				{
					return BaseResponse<string>.Fail("Campaign voucher not found", ResponseErrorType.NotFound);
				}

				if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == voucherId && !uv.IsUsed))
				{
					return BaseResponse<string>.Fail("Cannot delete voucher that has been redeemed by users", ResponseErrorType.BadRequest);
				}

				_unitOfWork.Vouchers.Remove(voucher);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(voucherId.ToString(), "Campaign voucher deleted successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error deleting campaign voucher: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<VoucherResponse>> GetVoucherByIdAsync(Guid voucherId)
		{
			var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(
		   v => v.Id == voucherId && !v.IsDeleted && !v.CampaignId.HasValue,
				asNoTracking: true
			);

			if (voucher == null)
			{
				return BaseResponse<VoucherResponse>.Fail(
					"Voucher not found",
					ResponseErrorType.NotFound
				);
			}

			var response = _mapper.Map<VoucherResponse>(voucher);
			return BaseResponse<VoucherResponse>.Ok(response, "Voucher retrieved successfully");
		}

		public async Task<BaseResponse<VoucherResponse>> GetCampaignVoucherByIdAsync(Guid campaignId, Guid voucherId)
		{
			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<VoucherResponse>.Fail("Campaign not found", ResponseErrorType.NotFound);
			}

			var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(
				v => v.Id == voucherId && !v.IsDeleted && v.CampaignId == campaignId,
				asNoTracking: true
			);

			if (voucher == null)
			{
				return BaseResponse<VoucherResponse>.Fail("Campaign voucher not found", ResponseErrorType.NotFound);
			}

			var response = _mapper.Map<VoucherResponse>(voucher);
			return BaseResponse<VoucherResponse>.Ok(response, "Campaign voucher retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<VoucherResponse>>> GetPagedVouchersAsync(GetPagedVouchersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Vouchers.GetPagedVouchersAsync(request);

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

		public async Task<BaseResponse<PagedResult<RedeemableVoucherResponse>>> GetRedeemableVouchersAsync(GetPagedRedeemableVouchersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Vouchers.GetPagedRedeemableVouchersAsync(request);
			var voucherList = _mapper.Map<List<RedeemableVoucherResponse>>(items);
			var pagedResult = new PagedResult<RedeemableVoucherResponse>(
				voucherList,
				request.PageNumber,
				request.PageSize,
				totalCount
			);
			return BaseResponse<PagedResult<RedeemableVoucherResponse>>.Ok(
				pagedResult,
				"Redeemable vouchers retrieved successfully"
			);
		}

		public async Task<BaseResponse<string>> RedeemVoucherAsync(Guid userId, RedeemVoucherRequest request)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(
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

					if (voucher.RemainingQuantity <= 0)
					{
						return BaseResponse<string>.Fail(
							"Voucher is out of stock",
							ResponseErrorType.BadRequest
						);
					}

					// Use repository method to check if user already redeemed this voucher
					var hasRedeemed = await _unitOfWork.UserVouchers.HasRedeemedVoucherAsync(userId, request.VoucherId, request.ReceiverEmailOrPhone);
					if (hasRedeemed)
					{
						return BaseResponse<string>.Fail(
							"Voucher already redeemed",
							ResponseErrorType.Conflict
						);
					}

					// Use LoyaltyPointService to deduct points
					var remainingPoints = await _loyaltyTransactionService.RedeemPointAsync(userId, voucher.RequiredPoints, voucher.Id, null, false);
					if (!remainingPoints)
					{
						return BaseResponse<string>.Fail(
							"Insufficient loyalty points",
							ResponseErrorType.BadRequest
						);
					}

					Guid? finalOwnerId = userId;
					if (!string.IsNullOrEmpty(request.ReceiverEmailOrPhone))
					{
						var receiver = await _userService.GetByPhoneOrEmailAsync(request.ReceiverEmailOrPhone);
						if (receiver != null) finalOwnerId = receiver.Id;
						else
						{
							// If receiver not found, treat as guest redemption (voucher will be associated with email/phone instead of userId)
							finalOwnerId = null;
						}
					}

					var userVoucher = new UserVoucher
					{
						UserId = finalOwnerId,
						VoucherId = request.VoucherId,
						IsUsed = false,
						GuestEmailOrPhone = request.ReceiverEmailOrPhone ?? null,
						Status = UsageStatus.Available
					};

					// Decrement voucher remaining quantity
					voucher.RemainingQuantity -= 1;
					if (voucher.RemainingQuantity < 0) voucher.RemainingQuantity = 0;
					_unitOfWork.Vouchers.Update(voucher);

					await _unitOfWork.UserVouchers.AddAsync(userVoucher);

					// Notification logic can be added here if needed

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

		public async Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(Guid userId, GetPagedUserVouchersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.UserVouchers.GetPagedWithVouchersAsync(userId, request);

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

		public async Task<BaseResponse<PagedResult<AvailableVoucherResponse>>> GetAvailableVouchersAsync(Guid userId, GetPagedAvailableVouchersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.UserVouchers.GetPagedAvailableVouchersAsync(userId, request);
			var pagedResult = new PagedResult<AvailableVoucherResponse>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<AvailableVoucherResponse>>.Ok(
				pagedResult,
				"Available vouchers retrieved successfully"
			);
		}

		public async Task<BaseResponse<bool>> CanUserApplyVoucherAsync(string voucherCode, Guid? userId, decimal orderAmount, string? emailOrPhone = null, IEnumerable<Guid>? cartVariantIds = null)
		{
			// 1. Validate voucher existence
			var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode);
			if (voucher == null)
			{
				return BaseResponse<bool>.Fail(
					"Invalid voucher code",
					ResponseErrorType.NotFound
				);
			}

			// 2. Check expiry
			if (voucher.ExpiryDate < DateTime.UtcNow)
			{
				return BaseResponse<bool>.Fail(
					"Voucher has expired",
					ResponseErrorType.BadRequest
				);
			}

			// 2.1 If voucher belongs to a campaign, validate campaign/item scope
			var campaignScopeValidation = await ValidateCampaignVoucherScopeAsync(voucher, cartVariantIds);
			if (campaignScopeValidation != null)
			{
				return campaignScopeValidation;
			}

			// 3. Check minimum order value
			var minOrderValue = voucher.MinOrderValue ?? 0m;
			if (orderAmount < minOrderValue)
			{
				return BaseResponse<bool>.Fail(
				 $"Order amount must be at least {minOrderValue:C} to use this voucher",
					ResponseErrorType.BadRequest
				);
			}

			// 4. Determine user type and effective userId
			var (effectiveUserId, isGuest, isAnonymousGuest) = await ResolveEffectiveUserAsync(userId, emailOrPhone);

			// 5. Check ownership and usage strategy
			var requiredPoints = voucher.RequiredPoints ?? 0;
			bool requiresPriorOwnership = !voucher.IsPublic || requiredPoints > 0;

			if (requiresPriorOwnership)
			{
				if (isAnonymousGuest)
				{
					return BaseResponse<bool>.Fail(
						"Login required to use this reward voucher",
						ResponseErrorType.Unauthorized
					);
				}

				var ownedVoucher = await FindUserVoucherAsync(
					voucher.Id,
					effectiveUserId,
					emailOrPhone,
					isGuest,
					UsageStatus.Available);

				if (ownedVoucher == null)
				{
					return BaseResponse<bool>.Fail(
					  requiredPoints > 0
							? "You must redeem this voucher with points before using it"
							: "You do not own this private voucher",
						ResponseErrorType.Forbidden
					);
				}
			}
			else
			{
				bool alreadyUsed = await _unitOfWork.UserVouchers.AnyAsync(uv =>
					uv.VoucherId == voucher.Id &&
					(
						uv.IsUsed ||
						(uv.Status == UsageStatus.Reserved && uv.Order != null && uv.Order.PaymentExpiresAt > DateTime.UtcNow)
					) &&
					(
						(effectiveUserId.HasValue && uv.UserId == effectiveUserId.Value) ||
						(!string.IsNullOrEmpty(emailOrPhone) && uv.GuestEmailOrPhone == emailOrPhone)
					)
				);

				if (alreadyUsed)
				{
					return BaseResponse<bool>.Fail(
						"You have already used this promotion",
						ResponseErrorType.BadRequest
					);
				}

				if (voucher.RemainingQuantity.HasValue && voucher.RemainingQuantity.Value <= 0)
				{
					return BaseResponse<bool>.Fail(
						"Voucher is out of stock",
						ResponseErrorType.BadRequest
					);
				}
			}

			return BaseResponse<bool>.Ok(true, "Voucher can be applied");
		}

		private async Task<BaseResponse<bool>?> ValidateCampaignVoucherScopeAsync(VoucherResponse voucher, IEnumerable<Guid>? cartVariantIds)
		{
			if (!voucher.CampaignId.HasValue)
			{
				return null;
			}

			var campaign = await _campaignRepository.FirstOrDefaultAsync(
				c => c.Id == voucher.CampaignId.Value && !c.IsDeleted,
				asNoTracking: true);

			if (campaign == null)
			{
				return BaseResponse<bool>.Fail("Campaign not found for this voucher", ResponseErrorType.NotFound);
			}

			var now = DateTime.UtcNow;
			if (campaign.Status != CampaignStatus.Active || now < campaign.StartDate || now > campaign.EndDate)
			{
				return BaseResponse<bool>.Fail("Campaign is not active for this voucher", ResponseErrorType.BadRequest);
			}

			if (campaign.Status == CampaignStatus.Paused)
			{
				return BaseResponse<bool>.Fail("Campaign is currently paused, voucher cannot be applied", ResponseErrorType.BadRequest);
			}

			var cartVariantSet = cartVariantIds?
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToHashSet();

			if (voucher.ApplyType == VoucherType.Product && (cartVariantSet == null || cartVariantSet.Count == 0))
			{
				return BaseResponse<bool>.Fail("Cart items are required for product-level voucher validation", ResponseErrorType.BadRequest);
			}

			var hasMatchingActiveItem = await _promotionItemRepository.AnyAsync(
				i => i.CampaignId == campaign.Id
					&& !i.IsDeleted
					&& i.ItemType == voucher.TargetItemType
					&& (!i.StartDate.HasValue || i.StartDate.Value <= now)
					&& (!i.EndDate.HasValue || i.EndDate.Value >= now)
					&& (voucher.ApplyType != VoucherType.Product
						|| (cartVariantSet != null && cartVariantSet.Contains(i.ProductVariantId))));

			if (!hasMatchingActiveItem)
			{
				var message = voucher.ApplyType == VoucherType.Product
					? "No active campaign product item matches this voucher"
					: "No active campaign order item matches this voucher";

				return BaseResponse<bool>.Fail(message, ResponseErrorType.BadRequest);
			}

			return null;
		}

		public async Task<BaseResponse<UserVoucher>> MarkVoucherAsReservedAsync(Guid? userId, string? emailOrPhone, Guid voucherId, Guid orderId)
		{
			try
			{
				var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
				if (voucher == null || voucher.IsDeleted)
				{
					return BaseResponse<UserVoucher>.Fail(
						"Voucher not found",
						ResponseErrorType.NotFound
					);
				}

				var (effectiveUserId, isGuest, isAnonymousGuest) = await ResolveEffectiveUserAsync(userId, emailOrPhone);
				UserVoucher actionedUserVoucher = null!;

				bool requiresPriorOwnership = !voucher.IsPublic || voucher.RequiredPoints > 0;

				if (requiresPriorOwnership)
				{
					if (isAnonymousGuest)
					{
						return BaseResponse<UserVoucher>.Fail(
							"Login required to use this reward voucher",
							ResponseErrorType.Unauthorized
						);
					}

					var userVoucher = await FindUserVoucherAsync(
						voucherId,
						effectiveUserId,
						emailOrPhone,
						isGuest,
						UsageStatus.Available);

					if (userVoucher == null)
					{
						return BaseResponse<UserVoucher>.Fail(
							voucher.RequiredPoints > 0
								? "You must redeem this voucher with points before using it"
								: "You do not own this private voucher",
							ResponseErrorType.Forbidden
						);
					}

					// Mark as reserved
					userVoucher.Status = UsageStatus.Reserved;
					userVoucher.OrderId = orderId;
					_unitOfWork.UserVouchers.Update(userVoucher);

					actionedUserVoucher = userVoucher;
				}
				else
				{
					// Check stock
					if (voucher.RemainingQuantity <= 0)
					{
						return BaseResponse<UserVoucher>.Fail(
							"Voucher is out of stock",
							ResponseErrorType.BadRequest
						);
					}

					// Check duplicate usage/reservation for identified users
					if (!isAnonymousGuest)
					{
						bool alreadyExists = await _unitOfWork.UserVouchers.AnyAsync(uv =>
							uv.VoucherId == voucherId &&
							(
								(effectiveUserId.HasValue && uv.UserId == effectiveUserId.Value) ||
								(!string.IsNullOrEmpty(emailOrPhone) && uv.GuestEmailOrPhone == emailOrPhone)
							)
						);

						if (alreadyExists)
						{
							return BaseResponse<UserVoucher>.Fail(
								"You have already reserved or used this promotion",
								ResponseErrorType.BadRequest
							);
						}
					}

					// Create new UserVoucher and reserve
					var publicUserVoucher = new UserVoucher
					{
						UserId = effectiveUserId,
						VoucherId = voucherId,
						OrderId = orderId,
						GuestEmailOrPhone = isAnonymousGuest ? null : emailOrPhone,
						IsUsed = false,
						Status = UsageStatus.Reserved
					};

					await _unitOfWork.UserVouchers.AddAsync(publicUserVoucher);

					// Decrement quantity
					voucher.RemainingQuantity -= 1;
					if (voucher.RemainingQuantity < 0) voucher.RemainingQuantity = 0;
					_unitOfWork.Vouchers.Update(voucher);

					actionedUserVoucher = publicUserVoucher;
				}

				return BaseResponse<UserVoucher>.Ok(actionedUserVoucher, "Voucher marked as reserved successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<UserVoucher>.Fail(
					$"Error marking voucher as reserved: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<bool>> MarkVoucherAsUsedAsync(Guid orderId)
		{
			try
			{
				var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
					uv => uv.OrderId == orderId && !uv.IsUsed,
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

				userVoucher.OrderId = orderId;
				userVoucher.IsUsed = true;
				userVoucher.Status = UsageStatus.Used;
				_unitOfWork.UserVouchers.Update(userVoucher);

				return BaseResponse<bool>.Ok(true, "Voucher marked as used successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail(
					$"Error marking voucher as used: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<bool>> ReleaseReservedVoucherAsync(Guid orderId)
		{
			try
			{
				var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
					uv => uv.OrderId == orderId && !uv.IsUsed,
					asNoTracking: false
				);

				if (userVoucher == null)
				{
					return BaseResponse<bool>.Fail(
						"Voucher not reserved or already used",
						ResponseErrorType.NotFound
					);
				}

				if (userVoucher.Status != UsageStatus.Reserved)
				{
					return BaseResponse<bool>.Ok(true, "Voucher was not reserved, no action needed");
				}

				var voucher = await _unitOfWork.Vouchers.GetByIdAsync(userVoucher.VoucherId);
				if (voucher == null)
				{
					return BaseResponse<bool>.Fail(
						"Voucher not found",
						ResponseErrorType.NotFound
					);
				}

				if (voucher.IsPublic)
				{
					// Public voucher: remove the UserVoucher record and restore quantity
					_unitOfWork.UserVouchers.Remove(userVoucher);
					voucher.RemainingQuantity += 1;
					_unitOfWork.Vouchers.Update(voucher);
				}
				else
				{
					// Private voucher: reset status to Available so user can reuse
					userVoucher.Status = UsageStatus.Available;
					userVoucher.OrderId = null;
					_unitOfWork.UserVouchers.Update(userVoucher);
				}

				return BaseResponse<bool>.Ok(true, "Voucher released successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail(
					$"Error releasing voucher: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<decimal> CalculateVoucherDiscountAsync(string voucherCode, decimal totalPrice)
		{
			try
			{
				var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode);
				if (voucher == null)
				{
					return totalPrice;
				}

				var discountAmount = voucher.DiscountType switch
				{
					DiscountType.Percentage => totalPrice * (voucher.DiscountValue / 100m),
					DiscountType.FixedAmount => voucher.DiscountValue,
					_ => 0m
				};

				var finalPrice = Math.Round(totalPrice - discountAmount, 0, MidpointRounding.AwayFromZero);
				return finalPrice < 0m ? 0m : finalPrice;
			}
			catch
			{
				return totalPrice;
			}
		}

		public async Task<VoucherResponse?> GetVoucherByCodeAsync(string code)
		{
			return await _unitOfWork.Vouchers.GetByCodeAsync(code);
		}

		#endregion Apply Voucher Logic

		#region Private Helper Methods

		private async Task<(Guid? EffectiveUserId, bool IsGuest, bool IsAnonymousGuest)> ResolveEffectiveUserAsync(Guid? userId, string? emailOrPhone)
		{
			bool isGuest = !userId.HasValue;
			Guid? effectiveUserId = userId;
			bool isAnonymousGuest = false;

			if (isGuest && !string.IsNullOrEmpty(emailOrPhone))
			{
				var user = await _userService.GetByPhoneOrEmailAsync(emailOrPhone);
				if (user != null)
				{
					effectiveUserId = user.Id;
					isGuest = false;
				}
			}
			else if (isGuest && string.IsNullOrEmpty(emailOrPhone))
			{
				isAnonymousGuest = true;
			}

			return (effectiveUserId, isGuest, isAnonymousGuest);
		}

		private async Task<UserVoucher?> FindUserVoucherAsync(Guid voucherId, Guid? effectiveUserId, string? emailOrPhone, bool isGuest, UsageStatus? requiredStatus = null)
		{
			if (isGuest)
			{
				return await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
					uv => uv.VoucherId == voucherId &&
						  uv.UserId == null &&
						  uv.GuestEmailOrPhone == emailOrPhone &&
						  !uv.IsUsed &&
						  (requiredStatus == null || uv.Status == requiredStatus),
					asNoTracking: false
				);
			}
			else
			{
				return await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
					uv => uv.VoucherId == voucherId &&
						  uv.UserId == effectiveUserId!.Value &&
						  !uv.IsUsed &&
						  (requiredStatus == null || uv.Status == requiredStatus),
					asNoTracking: false
				);
			}
		}

		#endregion Private Helper Methods
	}
}
