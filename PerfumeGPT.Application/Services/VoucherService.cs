using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Exceptions;
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

		public VoucherService(
			IUnitOfWork unitOfWork,
			ICampaignRepository campaignRepository,
			IPromotionItemRepository promotionItemRepository,
			IMapper mapper,
			IUserService userService,
			ILoyaltyTransactionService loyaltyTransactionService)
		{
			_unitOfWork = unitOfWork;
			_campaignRepository = campaignRepository;
			_promotionItemRepository = promotionItemRepository;
			_mapper = mapper;
			_userService = userService;
			_loyaltyTransactionService = loyaltyTransactionService;
		}
		#endregion Dependencies


		#region Admin Operations
		public async Task<BaseResponse<string>> CreateRegularVoucherAsync(CreateVoucherRequest request)
		{
			var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code);
			if (codeExists)
			{
				throw AppException.Conflict("Voucher code already exists");
			}

			var voucher = Voucher.CreateRegular(
				request.Code,
				request.DiscountValue,
				request.DiscountType,
				request.ApplyType,
				request.RequiredPoints,
				request.MinOrderValue,
				request.ExpiryDate,
				request.TotalQuantity,
				request.IsPublic);

			await _unitOfWork.Vouchers.AddAsync(voucher);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create voucher");

			return BaseResponse<string>.Ok(voucher.Id.ToString(), "Regular voucher created successfully");
		}

		public async Task<BaseResponse<string>> UpdateVoucherAsync(Guid voucherId, UpdateVoucherRequest request)
		{
			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
			if (voucher == null || voucher.IsDeleted)
			{
				throw AppException.NotFound("Voucher not found");
			}

			if (request.Code != voucher.Code)
			{
				var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code, voucherId);
				if (codeExists)
				{
					throw AppException.Conflict("Voucher code already exists");
				}
			}

			voucher.UpdateRegular(
				  request.Code,
				  request.DiscountValue,
				  request.DiscountType,
				  request.ApplyType,
				  request.RequiredPoints,
				  request.MinOrderValue,
				  request.ExpiryDate,
				  request.TotalQuantity,
				  request.RemainingQuantity,
				  request.IsPublic);
			_unitOfWork.Vouchers.Update(voucher);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update voucher");

			return BaseResponse<string>.Ok(voucherId.ToString(), "Voucher updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId)
		{
			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
			if (voucher == null || voucher.IsDeleted)
			{
				throw AppException.NotFound("Voucher not found");
			}

			if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == voucherId && !uv.IsUsed))
			{
				throw AppException.BadRequest("Cannot delete voucher that has been redeemed by users");
			}

			_unitOfWork.Vouchers.Remove(voucher);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete voucher");

			return BaseResponse<string>.Ok(voucherId.ToString(), "Voucher deleted successfully");
		}

		public async Task<BaseResponse<VoucherResponse>> GetVoucherByIdAsync(Guid voucherId)
		{
			var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId && !v.IsDeleted, asNoTracking: true)
				?? throw AppException.NotFound("Voucher not found");

			var response = _mapper.Map<VoucherResponse>(voucher);
			return BaseResponse<VoucherResponse>.Ok(response, "Voucher retrieved successfully");
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

			return BaseResponse<PagedResult<VoucherResponse>>.Ok(pagedResult, "Vouchers retrieved successfully");
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
			return BaseResponse<PagedResult<RedeemableVoucherResponse>>.Ok(pagedResult, "Redeemable vouchers retrieved successfully");
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
					) ?? throw AppException.NotFound("Voucher not found");

					voucher.EnsureNotExpired(DateTime.UtcNow);
					voucher.EnsureInStock();

					// Use repository method to check if user already redeemed this voucher
					var hasRedeemed = await _unitOfWork.UserVouchers.HasRedeemedVoucherAsync(userId, request.VoucherId, request.ReceiverEmailOrPhone);
					if (hasRedeemed)
					{
						throw AppException.Conflict("Voucher already redeemed");
					}

					// Use LoyaltyPointService to deduct points
					await _loyaltyTransactionService.RedeemPointAsync(userId, voucher.RequiredPoints, voucher.Id, null, false);

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

					var userVoucher = UserVoucher.CreateAvailable(
						 finalOwnerId,
						 request.VoucherId,
						 request.ReceiverEmailOrPhone ?? null);

					// Decrement voucher remaining quantity
					voucher.DecreaseRemainingQuantity();
					_unitOfWork.Vouchers.Update(voucher);

					await _unitOfWork.UserVouchers.AddAsync(userVoucher);

					// Notification logic can be added here if needed

					return BaseResponse<string>.Ok(userVoucher.Id.ToString(), "Voucher redeemed successfully");
				});
			}
			catch (AppException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw AppException.Internal($"Error redeeming voucher: {ex.Message}");
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

			return BaseResponse<PagedResult<UserVoucherResponse>>.Ok(pagedResult, "User vouchers retrieved successfully");
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

		public async Task<bool> CanUserApplyVoucherAsync(string voucherCode, Guid? userId, decimal orderAmount, string? emailOrPhone = null, IEnumerable<Guid>? cartVariantIds = null)
		{
			// 1. Validate voucher existence
			var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode) ?? throw AppException.NotFound("Voucher not found");

			// 2. Check expiry
			if (voucher.ExpiryDate < DateTime.UtcNow)
				throw AppException.NotFound("Voucher not found or has expired");

			// 2.1 If voucher belongs to a campaign, validate campaign/item scope
			await ValidateCampaignVoucherScopeAsync(voucher, cartVariantIds);

			// 3. Check minimum order value
			var minOrderValue = voucher.MinOrderValue ?? 0m;
			if (orderAmount < minOrderValue)
			{
				throw AppException.BadRequest($"Order amount must be at least {minOrderValue:C} to use this voucher");
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
					throw AppException.Unauthorized("Login required to use this reward voucher");
				}

				var ownedVoucher = await FindUserVoucherAsync(voucher.Id, effectiveUserId, emailOrPhone, isGuest, UsageStatus.Available)
					?? throw AppException.Forbidden(
						   requiredPoints > 0
							   ? "You must redeem this voucher with points before using it"
							   : "You do not own this private voucher");
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
					throw AppException.BadRequest("You have already used this promotion");
				}

				if (voucher.RemainingQuantity.HasValue && voucher.RemainingQuantity.Value <= 0)
				{
					throw AppException.BadRequest("Voucher is out of stock");
				}
			}

			return true;
		}

		public async Task<bool> RefundVoucherForCancelledOrderAsync(Guid orderId)
		{
			var oldUserVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(uv => uv.OrderId == orderId, asNoTracking: false);
			if (oldUserVoucher == null) return true;

			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(oldUserVoucher.VoucherId)
				?? throw AppException.NotFound("Voucher not found");

			if (voucher.IsPublic)
			{
				voucher.IncreaseRemainingQuantity();
				_unitOfWork.Vouchers.Update(voucher);
			}

			if (!oldUserVoucher.IsUsed && oldUserVoucher.Status == UsageStatus.Reserved)
			{
				oldUserVoucher.ReleaseReservation();
			}
			else
			{
				oldUserVoucher.MarkAsRefunded();
				if (voucher.ExpiryDate > DateTime.UtcNow)
				{
					var refundedUserVoucher = oldUserVoucher.CreateReplacement();
					await _unitOfWork.UserVouchers.AddAsync(refundedUserVoucher);
				}
			}

			_unitOfWork.UserVouchers.Update(oldUserVoucher);
			return true;
		}

		private async Task ValidateCampaignVoucherScopeAsync(VoucherResponse voucher, IEnumerable<Guid>? cartVariantIds)
		{
			if (!voucher.CampaignId.HasValue)
			{
				return;
			}

			var campaign = await _campaignRepository.FirstOrDefaultAsync(
				c => c.Id == voucher.CampaignId.Value && !c.IsDeleted,
				asNoTracking: true) ?? throw AppException.NotFound("Campaign not found for this voucher");

			var now = DateTime.UtcNow;
			if (campaign.Status != CampaignStatus.Active || now < campaign.StartDate || now > campaign.EndDate)
			{
				throw AppException.BadRequest("Campaign is not active for this voucher");
			}

			if (campaign.Status == CampaignStatus.Paused)
			{
				throw AppException.BadRequest("Campaign is currently paused, voucher cannot be applied");
			}

			var cartVariantSet = cartVariantIds?
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToHashSet();

			if (voucher.ApplyType == VoucherType.Product && (cartVariantSet == null || cartVariantSet.Count == 0))
			{
				throw AppException.BadRequest("Cart items are required for product-level voucher validation");
			}

			var hasMatchingActiveItem = await _promotionItemRepository.AnyAsync(
				i => i.CampaignId == campaign.Id
					&& !i.IsDeleted
					&& i.ItemType == voucher.TargetItemType
					&& (i.IsActive)
					&& (voucher.ApplyType != VoucherType.Product
						|| (cartVariantSet != null && cartVariantSet.Contains(i.ProductVariantId))));

			if (!hasMatchingActiveItem)
			{
				var message = voucher.ApplyType == VoucherType.Product
					? "No active campaign product item matches this voucher"
					: "No active campaign order item matches this voucher";
				throw AppException.BadRequest(message);
			}
		}

		public async Task<UserVoucher> MarkVoucherAsReservedAsync(Guid? userId, string? emailOrPhone, Guid voucherId, Guid orderId)
		{
			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
			if (voucher == null || voucher.IsDeleted)
			{
				throw AppException.NotFound("Voucher not found");
			}

			var (effectiveUserId, isGuest, isAnonymousGuest) = await ResolveEffectiveUserAsync(userId, emailOrPhone);
			UserVoucher actionedUserVoucher = null!;

			bool requiresPriorOwnership = !voucher.IsPublic || voucher.RequiredPoints > 0;

			if (requiresPriorOwnership)
			{
				if (isAnonymousGuest)
				{
					throw AppException.Unauthorized("Login required to use this reward voucher");
				}

				var userVoucher = await FindUserVoucherAsync(voucherId, effectiveUserId, emailOrPhone, isGuest, UsageStatus.Available)
					?? throw AppException.Forbidden(
						  voucher.RequiredPoints > 0
							  ? "You must redeem this voucher with points before using it"
							  : "You do not own this private voucher");

				// Mark as reserved
				userVoucher.Reserve(orderId);
				_unitOfWork.UserVouchers.Update(userVoucher);

				actionedUserVoucher = userVoucher;
			}
			else
			{
				// Check stock
				if (voucher.RemainingQuantity <= 0)
				{
					throw AppException.BadRequest("Voucher is out of stock");
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
						throw AppException.BadRequest("You have already reserved or used this promotion");
					}
				}

				// Create new UserVoucher and reserve
				var publicUserVoucher = UserVoucher.CreateReserved(
					   effectiveUserId,
					   voucherId,
					   orderId,
					   isAnonymousGuest ? null : emailOrPhone);

				await _unitOfWork.UserVouchers.AddAsync(publicUserVoucher);

				// Decrement quantity
				voucher.DecreaseRemainingQuantity();
				_unitOfWork.Vouchers.Update(voucher);

				actionedUserVoucher = publicUserVoucher;
			}

			return actionedUserVoucher;
		}

		public async Task<bool> MarkVoucherAsUsedAsync(Guid orderId)
		{
			var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(uv => uv.OrderId == orderId && !uv.IsUsed)
				?? throw AppException.NotFound("User voucher not found or already used");
			if (userVoucher.Status != UsageStatus.Reserved)
			{
				throw AppException.BadRequest("Voucher must be reserved before marking as used");
			}

			userVoucher.MarkUsed(orderId);
			_unitOfWork.UserVouchers.Update(userVoucher);

			return true;
		}

		public async Task<bool> ReleaseReservedVoucherAsync(Guid orderId)
		{
			var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(uv => uv.OrderId == orderId && !uv.IsUsed)
								?? throw AppException.NotFound("Voucher not reserved or already used");

			if (userVoucher.Status != UsageStatus.Reserved)
			{
				return true;
			}

			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(userVoucher.VoucherId)
				?? throw AppException.NotFound("Voucher not found");

			if (voucher.IsPublic)
			{
				// Public voucher: remove the UserVoucher record and restore quantity
				_unitOfWork.UserVouchers.Remove(userVoucher);
				voucher.IncreaseRemainingQuantity();
				_unitOfWork.Vouchers.Update(voucher);
			}
			else
			{
				// Private voucher: reset status to Available so user can reuse
				userVoucher.ReleaseReservation();
				_unitOfWork.UserVouchers.Update(userVoucher);
			}

			return true;
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
