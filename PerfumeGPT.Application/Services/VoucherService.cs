using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using System.Globalization;
using static PerfumeGPT.Domain.Entities.Voucher;

namespace PerfumeGPT.Application.Services
{
	public class VoucherService : IVoucherService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserService _userService;
		private readonly ILoyaltyTransactionService _loyaltyTransactionService;

		public VoucherService(
			IUnitOfWork unitOfWork,
			IUserService userService,
			ILoyaltyTransactionService loyaltyTransactionService)
		{
			_unitOfWork = unitOfWork;
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
				throw AppException.Conflict("Mã giảm giá đã tồn tại");
			}

			// ĐÃ FIX: Map vào VoucherRegularCreationFactor
			var voucher = Voucher.CreateRegular(new VoucherRegularCreationFactor
			{
				Code = request.Code,
				DiscountValue = request.DiscountValue,
				DiscountType = request.DiscountType,
				ApplyType = request.ApplyType,
				RequiredPoints = request.RequiredPoints,
				MaxDiscountAmount = request.MaxDiscountAmount,
				MinOrderValue = request.MinOrderValue,
				ExpiryDate = request.ExpiryDate,
				TotalQuantity = request.TotalQuantity,
				MaxUsagePerUser = request.MaxUsagePerUser,
				IsPublic = request.IsPublic,
				IsMemberOnly = request.IsMemberOnly
			});

			await _unitOfWork.Vouchers.AddAsync(voucher);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Tạo mã giảm giá thất bại");

			return BaseResponse<string>.Ok(voucher.Id.ToString(), "Tạo mã giảm giá thường thành công");
		}

		public async Task<BaseResponse<string>> UpdateVoucherAsync(Guid voucherId, UpdateVoucherRequest request)
		{
			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
			if (voucher == null || voucher.IsDeleted)
			{
				throw AppException.NotFound("Không tìm thấy mã giảm giá");
			}

			if (request.Code != voucher.Code)
			{
				var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code, voucherId);
				if (codeExists)
				{
					throw AppException.Conflict("Mã giảm giá đã tồn tại");
				}
			}

			// ĐÃ FIX: Map vào VoucherRegularUpdateFactor
			voucher.UpdateRegular(new VoucherRegularUpdateFactor
			{
				Code = request.Code,
				DiscountValue = request.DiscountValue,
				DiscountType = request.DiscountType,
				ApplyType = request.ApplyType,
				RequiredPoints = request.RequiredPoints,
				MaxDiscountAmount = request.MaxDiscountAmount,
				MinOrderValue = request.MinOrderValue,
				ExpiryDate = request.ExpiryDate,
				TotalQuantity = request.TotalQuantity,
				RemainingQuantity = request.RemainingQuantity,
				MaxUsagePerUser = request.MaxUsagePerUser,
				IsPublic = request.IsPublic,
				IsMemberOnly = request.IsMemberOnly
			});

			_unitOfWork.Vouchers.Update(voucher);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật mã giảm giá thất bại");

			return BaseResponse<string>.Ok(voucherId.ToString(), "Cập nhật mã giảm giá thành công");
		}

		public async Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId)
		{
			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
			if (voucher == null || voucher.IsDeleted)
			{
				throw AppException.NotFound("Không tìm thấy mã giảm giá");
			}

			if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == voucherId && uv.Status == UsageStatus.Used))
			{
				throw AppException.BadRequest("Không thể xóa mã giảm giá đã được người dùng đổi");
			}

			_unitOfWork.Vouchers.Remove(voucher);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xóa mã giảm giá thất bại");

			return BaseResponse<string>.Ok(voucherId.ToString(), "Xóa mã giảm giá thành công");
		}

		public async Task<BaseResponse<VoucherResponse>> GetVoucherByIdAsync(Guid voucherId)
		{
			var voucher = await _unitOfWork.Vouchers.GetByIdResponseAsync(voucherId)
				?? throw AppException.NotFound("Không tìm thấy mã giảm giá");

			return BaseResponse<VoucherResponse>.Ok(voucher, "Lấy thông tin mã giảm giá thành công");
		}

		public async Task<BaseResponse<PagedResult<VoucherResponse>>> GetPagedVouchersAsync(GetPagedVouchersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Vouchers.GetPagedVouchersAsync(request);

			var pagedResult = new PagedResult<VoucherResponse>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<VoucherResponse>>.Ok(pagedResult, "Lấy danh sách mã giảm giá thành công");
		}
		#endregion Admin Operations



		#region User Operations
		public async Task<BaseResponse<PagedResult<RedeemableVoucherResponse>>> GetRedeemableVouchersAsync(GetPagedRedeemableVouchersRequest request, Guid? userId = null)
		{
			var (items, totalCount) = await _unitOfWork.Vouchers.GetPagedRedeemableVouchersAsync(request, userId);
			var pagedResult = new PagedResult<RedeemableVoucherResponse>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount
			);
			return BaseResponse<PagedResult<RedeemableVoucherResponse>>.Ok(pagedResult, "Lấy danh sách mã có thể đổi thành công");
		}

		public async Task<BaseResponse<string>> RedeemVoucherAsync(Guid userId, RedeemVoucherRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(
					v => v.Id == request.VoucherId && !v.IsDeleted,
					asNoTracking: false
			  ) ?? throw AppException.NotFound("Không tìm thấy mã giảm giá");

				voucher.EnsureNotExpired(DateTime.UtcNow);

				var usageCount = await _unitOfWork.UserVouchers.GetUserVoucherUsageCountAsync(userId, request.VoucherId, request.ReceiverEmailOrPhone);
				// Use repository method to check if user already redeemed this voucher
				if (voucher.MaxUsagePerUser.HasValue && usageCount >= voucher.MaxUsagePerUser.Value)
				{
					throw AppException.Conflict($"Bạn đã đạt giới hạn sử dụng tối đa ({voucher.MaxUsagePerUser.Value}) cho mã này.");
				}

				// Use LoyaltyPointService to deduct points
				if (voucher.RequiredPoints > 0)
				{
					await _loyaltyTransactionService.RedeemPointAsync(userId, voucher.RequiredPoints, voucher.Id, null, false);
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

				var userVoucher = UserVoucher.CreateAvailable(
					 finalOwnerId,
					 request.VoucherId,
					 finalOwnerId.HasValue ? null : request.ReceiverEmailOrPhone);

				// Decrement voucher remaining quantity
				voucher.DecreaseRemainingQuantity();
				_unitOfWork.Vouchers.Update(voucher);

				await _unitOfWork.UserVouchers.AddAsync(userVoucher);

				// Notification logic can be added here if needed

				return BaseResponse<string>.Ok(userVoucher.Id.ToString(), "Đổi mã giảm giá thành công");
			});
		}

		public async Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(Guid userId, GetPagedUserVouchersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.UserVouchers.GetPagedWithVouchersAsync(userId, request);

			var pagedResult = new PagedResult<UserVoucherResponse>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<UserVoucherResponse>>.Ok(pagedResult, "Lấy danh sách mã giảm giá của người dùng thành công");
		}
		#endregion User Operations



		#region Apply Voucher Logic
		public async Task<BaseResponse<List<ApplicableVoucherResponse>>> GetApplicableVouchersAsync(GetApplicableVouchersRequest request)
		{
			var cartItems = NormalizeCartItems(request.CartItems);

			// Dù giỏ hàng trống, VẪN CHO ĐI TIẾP để xem các voucher Applicable (nhưng chúng sẽ bị đánh dấu là Ineligible vì chưa mua đủ tiền)

			var aggregatedVouchers = await AggregateVouchersForApplicabilityAsync(request.CustomerId);
			if (aggregatedVouchers.Count == 0)
			{
				return BaseResponse<List<ApplicableVoucherResponse>>.Ok([], "Không có mã giảm giá.");
			}

			// Chuẩn bị dữ liệu Campaign cho Evaluator
			var campaignIds = aggregatedVouchers.Where(v => v.CampaignId.HasValue).Select(v => v.CampaignId!.Value).Distinct().ToList();

			var campaigns = await _unitOfWork.Campaigns.GetAllAsync(c => campaignIds.Contains(c.Id));
			var campaignsById = campaigns.ToDictionary(c => c.Id, c => c);

			var promotionItems = await _unitOfWork.PromotionItems.GetActiveByCampaignIdsAsync(campaignIds);
			var promotionItemsByCampaign = promotionItems.GroupBy(x => x.CampaignId).ToDictionary(g => g.Key, g => g.ToList());

			var (effectiveUserId, isGuest, isAnonymousGuest) = await ResolveEffectiveUserAsync(request.CustomerId, null);
			var totalCartValue = cartItems.Sum(x => x.Price * x.Quantity);

			var evaluations = new List<VoucherApplicabilityEvaluation>();
			foreach (var voucher in aggregatedVouchers)
			{
				evaluations.Add(await EvaluateSingleVoucherAsync(
					voucher, totalCartValue, cartItems, promotionItemsByCampaign, campaignsById, effectiveUserId, isGuest, isAnonymousGuest, null));
			}

			// LỌC BỎ CÁC MÃ HIDDEN, CHỈ TRẢ VỀ CÁC MÃ APPLICABLE HOẶC INELIGIBLE
			var payload = evaluations
				.Where(x => !x.IsHidden)
				.OrderByDescending(x => x.IsApplicable)
				.ThenBy(x => x.Voucher.Code)
				.Select(x => new ApplicableVoucherResponse
				{
					VoucherId = x.Voucher.Id,
					Code = x.Voucher.Code,
					DiscountValue = x.Voucher.DiscountValue,
					DiscountType = x.Voucher.DiscountType,
					IsApplicable = x.IsApplicable,
					IneligibleReason = x.IneligibleReason
				})
				.ToList();

			return BaseResponse<List<ApplicableVoucherResponse>>.Ok(payload, "Đánh giá mã giảm giá áp dụng thành công");
		}

		public async Task EnsureVoucherApplicableAsync(string voucherCode, Guid? userId, IEnumerable<ApplicableVoucherCartItemRequest> cartItems, string? emailOrPhone = null)
		{
			if (string.IsNullOrWhiteSpace(voucherCode))
			{
				return;
			}

			var normalizedCartItems = NormalizeCartItems(cartItems);
			if (normalizedCartItems.Count == 0)
			{
				throw AppException.BadRequest("Giỏ hàng không có sản phẩm hợp lệ để áp dụng mã giảm giá.");
			}

			var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode)
				?? throw AppException.NotFound("Không tìm thấy mã giảm giá");

			var evaluation = (await EvaluateVoucherApplicabilityAsync([voucher], userId, normalizedCartItems, emailOrPhone)).First();
			if (!evaluation.IsApplicable)
			{
				throw AppException.BadRequest(evaluation.IneligibleReason ?? "Mã giảm giá không áp dụng được cho giỏ hàng này.");
			}
		}

		public async Task<bool> CanUserApplyVoucherAsync(string voucherCode, Guid? userId, decimal orderAmount, string? emailOrPhone = null, IEnumerable<Guid>? cartVariantIds = null)
		{
			// 1. Validate voucher existence
			var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode) ?? throw AppException.NotFound("Không tìm thấy mã giảm giá");

			// 2. Check expiry
			if (voucher.ExpiryDate < DateTime.UtcNow)
				throw AppException.NotFound("Không tìm thấy mã giảm giá hoặc mã đã hết hạn");

			// 2.1 If voucher belongs to a campaign, validate campaign/item scope
			await ValidateCampaignVoucherScopeAsync(voucher, cartVariantIds);

			// 3. Check minimum order value
			var minOrderValue = voucher.MinOrderValue ?? 0m;
			if (orderAmount < minOrderValue)
			{
				throw AppException.BadRequest($"Giá trị đơn hàng tối thiểu để dùng mã này là {FormatVietnameseCurrency(minOrderValue)}");
			}

			// 4. Determine user type and effective userId
			var (effectiveUserId, isGuest, isAnonymousGuest) = await ResolveEffectiveUserAsync(userId, emailOrPhone);

			if (voucher.IsMemberOnly && !userId.HasValue)
			{
				throw AppException.BadRequest("Mã giảm giá này chỉ dành cho Khách hàng Thành viên. Vui lòng đăng ký tài khoản.");
			}

			// 5. Check ownership and usage strategy
			var requiredPoints = voucher.RequiredPoints ?? 0;
			bool requiresPriorOwnership = !voucher.IsPublic || requiredPoints > 0;

			if (requiresPriorOwnership)
			{
				if (isAnonymousGuest)
				{
					throw AppException.Unauthorized("Vui lòng đăng nhập để sử dụng mã đổi điểm này");
				}

				var ownedVoucher = await FindUserVoucherAsync(voucher.Id, effectiveUserId, emailOrPhone, isGuest, UsageStatus.Available)
					?? throw AppException.Forbidden(
						   requiredPoints > 0
							? "Bạn cần đổi mã này bằng điểm trước khi sử dụng"
							   : "Bạn không sở hữu mã giảm giá riêng tư này");
			}
			else if (voucher.MaxUsagePerUser.HasValue) // Chỉ cần có MaxUsage là phải đếm
			{
				int currentUsageCount = 0;

				if (effectiveUserId.HasValue)
				{
					currentUsageCount = await _unitOfWork.UserVouchers.CountAsync(uv =>
						uv.VoucherId == voucher.Id &&
						uv.UserId == effectiveUserId.Value &&
						(uv.Status == UsageStatus.Used || (uv.Status == UsageStatus.Reserved && uv.Order != null && uv.Order.PaymentExpiresAt > DateTime.UtcNow))
					);
				}
				else if (!isAnonymousGuest) // Đếm cho khách vãng lai có để lại Email/Phone
				{
					currentUsageCount = await _unitOfWork.UserVouchers.CountAsync(uv =>
						uv.VoucherId == voucher.Id &&
						uv.GuestIdentifier == emailOrPhone &&
						(uv.Status == UsageStatus.Used || (uv.Status == UsageStatus.Reserved && uv.Order != null && uv.Order.PaymentExpiresAt > DateTime.UtcNow))
					);
				}

				if (currentUsageCount >= voucher.MaxUsagePerUser.Value)
				{
					throw AppException.BadRequest($"Bạn đã đạt giới hạn số lần sử dụng ({voucher.MaxUsagePerUser.Value}) cho mã này.");
				}
			}

			return true;
		}

		private async Task<List<VoucherResponse>> AggregateVouchersForApplicabilityAsync(Guid? customerId)
		{
			var publicVouchers = await _unitOfWork.Vouchers.GetPublicVouchersForApplicabilityAsync();
			if (!customerId.HasValue || customerId.Value == Guid.Empty)
			{
				return publicVouchers
					.GroupBy(v => v.Id)
					.Select(g => g.First())
					.ToList();
			}

			var ownedVouchers = await _unitOfWork.UserVouchers.GetAvailableVoucherDetailsByUserIdAsync(customerId.Value);

			return ownedVouchers
				.Concat(publicVouchers)
				.GroupBy(v => v.Id)
				.Select(g => g.First())
				.ToList();
		}

		private async Task<List<VoucherApplicabilityEvaluation>> EvaluateVoucherApplicabilityAsync(
	IEnumerable<VoucherResponse> vouchers,
	Guid? userId,
	List<ApplicableVoucherCartItemRequest> cartItems,
	string? emailOrPhone)
		{
			var voucherList = vouchers
				.Where(v => v.Id != Guid.Empty)
				.GroupBy(v => v.Id)
				.Select(g => g.First())
				.ToList();

			var totalCartValue = cartItems.Sum(x => x.Price * x.Quantity);
			var campaignIds = voucherList
				.Where(v => v.CampaignId.HasValue)
				.Select(v => v.CampaignId!.Value)
				.Distinct()
				.ToList();

			// 💡 BỔ SUNG: Truy vấn Campaigns tại đây
			var campaigns = await _unitOfWork.Campaigns.GetAllAsync(c => campaignIds.Contains(c.Id));
			var campaignsById = campaigns.ToDictionary(c => c.Id, c => c);

			var promotionItems = await _unitOfWork.PromotionItems.GetActiveByCampaignIdsAsync(campaignIds);
			var promotionItemsByCampaign = promotionItems
				.GroupBy(x => x.CampaignId)
				.ToDictionary(g => g.Key, g => g.ToList());

			var (effectiveUserId, isGuest, isAnonymousGuest) = await ResolveEffectiveUserAsync(userId, emailOrPhone);

			var evaluations = new List<VoucherApplicabilityEvaluation>(voucherList.Count);
			foreach (var voucher in voucherList)
			{
				evaluations.Add(await EvaluateSingleVoucherAsync(
					voucher,
					totalCartValue,
					cartItems,
					promotionItemsByCampaign,
					campaignsById, // 💡 Truyền dữ liệu thật vào đây
					effectiveUserId,
					isGuest,
					isAnonymousGuest,
					emailOrPhone));
			}

			return evaluations;
		}

		private async Task<VoucherApplicabilityEvaluation> EvaluateSingleVoucherAsync(
			VoucherResponse voucher,
			decimal totalCartValue,
			List<ApplicableVoucherCartItemRequest> cartItems,
			Dictionary<Guid, List<PromotionItem>> promotionItemsByCampaign,
			Dictionary<Guid, Campaign> campaignsById,
			Guid? effectiveUserId,
			bool isGuest,
			bool isAnonymousGuest,
			string? emailOrPhone)
		{
			var now = DateTime.UtcNow;

			// ==========================================
			// NHÓM 1: CÁC TRƯỜNG HỢP HIDDEN (ẨN HOÀN TOÀN KHỎI UI)
			// ==========================================

			// 1. Mã đã hết hạn thời gian tuyệt đối
			if (voucher.ExpiryDate < now)
				return new VoucherApplicabilityEvaluation(voucher, false, "Đã hết hạn", IsHidden: true);

			Campaign? campaign = null;
			if (voucher.CampaignId.HasValue && campaignsById.TryGetValue(voucher.CampaignId.Value, out campaign))
			{
				// 2. Campaign đã bị Hủy hoặc Đã kết thúc
				if (campaign.Status == CampaignStatus.Cancelled || campaign.EndDate < now)
					return new VoucherApplicabilityEvaluation(voucher, false, "Chương trình đã kết thúc", IsHidden: true);
			}

			// 3. Giỏ hàng trống nhưng đây là mã yêu cầu Sản phẩm cụ thể (Ẩn cho đỡ rác UI)
			if (cartItems.Count == 0 && voucher.ApplyType == VoucherType.Product)
				return new VoucherApplicabilityEvaluation(voucher, false, "Cần có sản phẩm trong giỏ", IsHidden: true);


			// ==========================================
			// NHÓM 2: CÁC TRƯỜNG HỢP INELIGIBLE (HIỆN MỜ + LÝ DO)
			// ==========================================

			// 4. Campaign chưa bắt đầu hoặc đang tạm dừng
			if (campaign != null)
			{
				if (campaign.StartDate > now)
					return new VoucherApplicabilityEvaluation(voucher, false, $"Sắp diễn ra vào {campaign.StartDate:dd/MM/yyyy HH:mm}");

				if (campaign.Status == CampaignStatus.Paused)
					return new VoucherApplicabilityEvaluation(voucher, false, "Chương trình đang tạm dừng");
			}

			// 5. Hết số lượng phát hành
			if (voucher.RemainingQuantity.HasValue && voucher.RemainingQuantity.Value <= 0)
				return new VoucherApplicabilityEvaluation(voucher, false, "Đã hết lượt sử dụng của hệ thống");

			// 6. Yêu cầu thành viên
			if (voucher.IsMemberOnly && !effectiveUserId.HasValue)
				return new VoucherApplicabilityEvaluation(voucher, false, "Chỉ dành cho Thành viên. Vui lòng đăng nhập.");

			// 7. Yêu cầu sở hữu riêng (Đổi điểm / Tặng riêng)
			var requiredPoints = voucher.RequiredPoints ?? 0;
			bool requiresPriorOwnership = !voucher.IsPublic || requiredPoints > 0;

			if (requiresPriorOwnership)
			{
				if (isAnonymousGuest)
					return new VoucherApplicabilityEvaluation(voucher, false, "Vui lòng đăng nhập để dùng mã cá nhân.");

				var ownedVoucher = await FindUserVoucherAsync(voucher.Id, effectiveUserId, emailOrPhone, isGuest, UsageStatus.Available);
				if (ownedVoucher == null)
				{
					// Không sở hữu thì ẩn luôn cũng được, hoặc báo mờ
					return new VoucherApplicabilityEvaluation(voucher, false, "Bạn chưa sở hữu hoặc chưa đổi mã này.", IsHidden: true);
				}
			}
			// 8. Giới hạn sử dụng cá nhân (Max Usage)
			else if (voucher.MaxUsagePerUser.HasValue)
			{
				int currentUsageCount = 0;
				if (effectiveUserId.HasValue)
				{
					currentUsageCount = await _unitOfWork.UserVouchers.CountAsync(uv =>
						uv.VoucherId == voucher.Id && uv.UserId == effectiveUserId.Value &&
						(uv.Status == UsageStatus.Used || (uv.Status == UsageStatus.Reserved && uv.Order != null && uv.Order.PaymentExpiresAt > now)));
				}
				else if (!isAnonymousGuest)
				{
					currentUsageCount = await _unitOfWork.UserVouchers.CountAsync(uv =>
						uv.VoucherId == voucher.Id && uv.GuestIdentifier == emailOrPhone &&
						(uv.Status == UsageStatus.Used || (uv.Status == UsageStatus.Reserved && uv.Order != null && uv.Order.PaymentExpiresAt > now)));
				}

				if (currentUsageCount >= voucher.MaxUsagePerUser.Value)
					return new VoucherApplicabilityEvaluation(voucher, false, "Bạn đã dùng hết số lượt cho phép.");
			}

			// ==========================================
			// NHÓM 3: XÉT ĐIỀU KIỆN TIỀN & SẢN PHẨM TRONG GIỎ HÀNG
			// ==========================================

			// Tránh tính toán nếu giỏ hàng rỗng (Chỉ xem danh sách)
			if (cartItems.Count == 0)
				return new VoucherApplicabilityEvaluation(voucher, false, "Vui lòng thêm sản phẩm vào giỏ hàng.");

			if (voucher.ApplyType == VoucherType.Order)
			{
				var minOrderValue = voucher.MinOrderValue ?? 0m;
				if (totalCartValue < minOrderValue)
				{
					var needMore = minOrderValue - totalCartValue;
					return new VoucherApplicabilityEvaluation(voucher, false, $"Mua thêm {FormatVietnameseCurrency(needMore)} để sử dụng mã này.");
				}

				return new VoucherApplicabilityEvaluation(voucher, true, null); // THÀNH CÔNG
			}

			// Xử lý Voucher theo Sản phẩm
			if (!voucher.CampaignId.HasValue)
				return new VoucherApplicabilityEvaluation(voucher, false, "Mã giảm giá bị lỗi cấu hình chiến dịch.", IsHidden: true);

			if (!promotionItemsByCampaign.TryGetValue(voucher.CampaignId.Value, out var campaignPromotionItems))
				return new VoucherApplicabilityEvaluation(voucher, false, "Giỏ hàng không có sản phẩm thuộc chương trình.");

			var eligibleVariantIds = campaignPromotionItems
				.Where(x => x.ItemType == voucher.TargetItemType)
				.Select(x => x.TargetProductVariantId)
				.ToHashSet();

			var campaignSubTotal = cartItems
				.Where(x => eligibleVariantIds.Contains(x.VariantId))
				.Sum(x => x.Price * x.Quantity);

			if (campaignSubTotal <= 0)
				return new VoucherApplicabilityEvaluation(voucher, false, "Giỏ hàng không có sản phẩm thuộc chương trình.");

			var minCampaignValue = voucher.MinOrderValue ?? 0m;
			if (campaignSubTotal < minCampaignValue)
			{
				var needMore = minCampaignValue - campaignSubTotal;
				return new VoucherApplicabilityEvaluation(voucher, false, $"Mua thêm {FormatVietnameseCurrency(needMore)} sản phẩm cùng chương trình để sử dụng.");
			}

			return new VoucherApplicabilityEvaluation(voucher, true, null); // THÀNH CÔNG
		}

		private static List<ApplicableVoucherCartItemRequest> NormalizeCartItems(IEnumerable<ApplicableVoucherCartItemRequest>? cartItems)
		{
			if (cartItems == null)
				return [];

			return cartItems
				.Where(x => x.VariantId != Guid.Empty && x.Quantity > 0 && x.Price > 0)
				.GroupBy(x => new { x.VariantId, x.Price })
				.Select(g => new ApplicableVoucherCartItemRequest
				{
					VariantId = g.Key.VariantId,
					Price = g.Key.Price,
					Quantity = g.Sum(x => x.Quantity)
				})
				.ToList();
		}

		private static string FormatVietnameseCurrency(decimal amount)
		{
			var roundedAmount = Math.Round(Math.Max(0m, amount), 0, MidpointRounding.AwayFromZero);
			return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}đ", roundedAmount);
		}

		public async Task<bool> RefundVoucherForCancelledOrderAsync(Guid orderId)
		{
			var oldUserVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(uv => uv.OrderId == orderId, asNoTracking: false);
			if (oldUserVoucher == null) return true;

			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(oldUserVoucher.VoucherId)
				?? throw AppException.NotFound("Không tìm thấy mã giảm giá");

			bool requiresPriorOwnership = !voucher.IsPublic || voucher.RequiredPoints > 0;

			if (!requiresPriorOwnership)
			{
				_unitOfWork.UserVouchers.Remove(oldUserVoucher);
				voucher.IncreaseRemainingQuantity();
				_unitOfWork.Vouchers.Update(voucher);
			}
			else
			{
				if (voucher.ExpiryDate > DateTime.UtcNow)
				{
					if (oldUserVoucher.Status == UsageStatus.Used)
						oldUserVoucher.RevertUsed();
					else
						oldUserVoucher.ReleaseReservation();

					_unitOfWork.UserVouchers.Update(oldUserVoucher);
				}
			}

			return true;
		}

		private async Task ValidateCampaignVoucherScopeAsync(VoucherResponse voucher, IEnumerable<Guid>? cartVariantIds)
		{
			if (!voucher.CampaignId.HasValue)
			{
				return;
			}

			var campaign = await _unitOfWork.Campaigns.FirstOrDefaultAsync(
				c => c.Id == voucher.CampaignId.Value && !c.IsDeleted,
			  asNoTracking: true) ?? throw AppException.NotFound("Không tìm thấy chiến dịch cho mã giảm giá này");

			var now = DateTime.UtcNow;
			if (campaign.Status != CampaignStatus.Active || now < campaign.StartDate || now > campaign.EndDate)
			{
				throw AppException.BadRequest("Chiến dịch của mã giảm giá này hiện không hoạt động");
			}

			if (campaign.Status == CampaignStatus.Paused)
			{
				throw AppException.BadRequest("Chiến dịch đang tạm dừng, không thể áp dụng mã giảm giá");
			}

			var cartVariantSet = cartVariantIds?
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToHashSet();

			if (voucher.ApplyType == VoucherType.Product && (cartVariantSet == null || cartVariantSet.Count == 0))
			{
				throw AppException.BadRequest("Cần có sản phẩm giỏ hàng để kiểm tra mã giảm giá theo sản phẩm");
			}

			var hasMatchingActiveItem = await _unitOfWork.PromotionItems.AnyAsync(
				i => i.CampaignId == campaign.Id
					&& !i.IsDeleted
					&& i.ItemType == voucher.TargetItemType
					&& (i.IsActive)
					&& (voucher.ApplyType != VoucherType.Product
						|| (cartVariantSet != null && cartVariantSet.Contains(i.TargetProductVariantId))));

			if (!hasMatchingActiveItem)
			{
				var message = voucher.ApplyType == VoucherType.Product
					? "Không có sản phẩm khuyến mãi đang hoạt động phù hợp với mã này"
					: "Không có khuyến mãi đơn hàng đang hoạt động phù hợp với mã này";
				throw AppException.BadRequest(message);
			}
		}

		public async Task<UserVoucher> MarkVoucherAsReservedAsync(Guid? userId, string? emailOrPhone, Guid voucherId, Guid orderId)
		{
			var voucher = await _unitOfWork.Vouchers.GetByIdAsync(voucherId);
			if (voucher == null || voucher.IsDeleted)
			{
				throw AppException.NotFound("Không tìm thấy mã giảm giá");
			}

			voucher.EnsureMemberEligible(userId);

			var (effectiveUserId, isGuest, isAnonymousGuest) = await ResolveEffectiveUserAsync(userId, emailOrPhone);
			UserVoucher actionedUserVoucher = null!;

			bool requiresPriorOwnership = !voucher.IsPublic || voucher.RequiredPoints > 0;

			if (requiresPriorOwnership)
			{
				if (isAnonymousGuest)
				{
					throw AppException.Unauthorized("Vui lòng đăng nhập để sử dụng mã đổi điểm này");
				}

				var userVoucher = await FindUserVoucherAsync(voucherId, effectiveUserId, emailOrPhone, isGuest, UsageStatus.Available)
					?? throw AppException.Forbidden(
						  voucher.RequiredPoints > 0
							? "Bạn cần đổi mã này bằng điểm trước khi sử dụng"
							  : "Bạn không sở hữu mã giảm giá riêng tư này");

				// Mark as reserved
				userVoucher.Reserve(orderId);
				_unitOfWork.UserVouchers.Update(userVoucher);

				actionedUserVoucher = userVoucher;
			}
			else
			{
				if (voucher.IsMemberOnly)
				{
					if (!effectiveUserId.HasValue)
					{
						throw AppException.BadRequest("Mã giảm giá này chỉ dành cho Khách hàng Thành viên. Vui lòng đăng ký tài khoản.");
					}

					int currentUsageCount = await _unitOfWork.UserVouchers.CountAsync(uv =>
						uv.VoucherId == voucherId &&
						(
							uv.Status == UsageStatus.Used ||
							(uv.Status == UsageStatus.Reserved && uv.Order != null && uv.Order.PaymentExpiresAt > DateTime.UtcNow)
						) &&
					   effectiveUserId.HasValue &&
						uv.UserId == effectiveUserId.Value
					);

					var allowedUsage = voucher.MaxUsagePerUser ?? 1;

					if (currentUsageCount >= allowedUsage)
					{
						throw AppException.BadRequest($"Bạn đã đạt giới hạn sử dụng tối đa ({allowedUsage}) cho khuyến mãi này");
					}
				}

				// Create new UserVoucher and reserve
				var publicUserVoucher = UserVoucher.CreateReserved(
					   effectiveUserId,
					   voucherId,
					   orderId,
					voucher.IsMemberOnly ? null : (isAnonymousGuest ? null : emailOrPhone));

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
			var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(uv => uv.OrderId == orderId);

			if (userVoucher == null)
				return true;

			if (userVoucher.Status == UsageStatus.Used)
				return true;

			userVoucher.MarkUsed();
			_unitOfWork.UserVouchers.Update(userVoucher);

			return true;
		}

		public async Task<decimal> CalculateVoucherDiscountAsync(string voucherCode, decimal totalPrice)
		{
			// 1. Lấy dữ liệu (BỎ try-catch để nếu DB lỗi thì Controller còn biết mà văng HTTP 500)
			var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode);
			if (voucher == null)
			{
				return totalPrice;
			}

			// 2. Tính toán số tiền giảm thô (Raw Discount)
			var rawDiscountAmount = voucher.DiscountType switch
			{
				DiscountType.Percentage => Math.Round(totalPrice * (voucher.DiscountValue / 100m), 0, MidpointRounding.AwayFromZero),
				DiscountType.FixedAmount => voucher.DiscountValue,
				_ => 0m
			};

			// 3. RÀO CHẮN 1: Áp dụng Trần giảm giá tối đa (Max Discount)
			if (voucher.MaxDiscountAmount.HasValue && voucher.MaxDiscountAmount.Value > 0)
			{
				rawDiscountAmount = Math.Min(rawDiscountAmount, voucher.MaxDiscountAmount.Value);
			}

			// 4. RÀO CHẮN 2: Không bao giờ cho phép giảm lố giá trị đơn hàng (Ví dụ: Đơn 50k mà voucher giảm 100k)
			var safeDiscountAmount = Math.Min(rawDiscountAmount, totalPrice);

			// 5. Tính giá cuối và làm tròn ĐỒNG NHẤT với Pipeline (2 chữ số thập phân)
			// Nếu tiền của bạn là VNĐ không có số lẻ, bạn có thể đổi số 2 thành số 0 ở ĐỒNG LOẠT tất cả các hàm.
			var finalPrice = Math.Round(totalPrice - safeDiscountAmount, 0, MidpointRounding.AwayFromZero);

			return finalPrice;
		}

		public async Task<VoucherResponse?> GetVoucherByCodeAsync(string code)
		{
			return await _unitOfWork.Vouchers.GetByCodeAsync(code);
		}
		#endregion Apply Voucher Logic



		#region Private Helper Methods
		private sealed record VoucherApplicabilityEvaluation(VoucherResponse Voucher, bool IsApplicable, string? IneligibleReason, bool IsHidden = false);

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
						  uv.GuestIdentifier == emailOrPhone &&
					   uv.Status != UsageStatus.Used &&
						  (requiredStatus == null || uv.Status == requiredStatus),
					asNoTracking: false
				);
			}
			else
			{
				return await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
					uv => uv.VoucherId == voucherId &&
						  uv.UserId == effectiveUserId!.Value &&
					   uv.Status != UsageStatus.Used &&
						  (requiredStatus == null || uv.Status == requiredStatus),
					asNoTracking: false
				);
			}
		}
		#endregion Private Helper Methods
	}
}
