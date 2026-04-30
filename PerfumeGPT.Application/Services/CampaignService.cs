using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace PerfumeGPT.Application.Services
{
	public class CampaignService : ICampaignService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<CampaignService> _logger;

		public CampaignService(
			IUnitOfWork unitOfWork,
			IMapper mapper,
			IBackgroundJobService backgroundJobService,
			ILogger<CampaignService> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
		}

		#region Campaign Management
		public async Task<BaseResponse<List<CampaignResponse>>> GetHomeCampaignsAsync()
		{
			var campaigns = await _unitOfWork.Campaigns.GetHomeCampaignsAsync();
			return BaseResponse<List<CampaignResponse>>.Ok(campaigns, "Lấy danh sách chiến dịch trang chủ thành công.");
		}

		public async Task<BaseResponse<List<CampaignLookupItem>>> GetActiveCampaignLookupListAsync()
		{
			var campaigns = await _unitOfWork.Campaigns.GetActiveCampaignLookupListAsync();
			return BaseResponse<List<CampaignLookupItem>>.Ok(campaigns, "Lấy danh sách tra cứu chiến dịch đang hoạt động thành công.");
		}

		public async Task<BaseResponse<PagedResult<CampaignResponse>>> GetPagedCampaignsAsync(GetPagedCampaignsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Campaigns.GetPagedCampaignsAsync(request);

			var pagedResult = new PagedResult<CampaignResponse>(items, request.PageNumber, request.PageSize, totalCount);

			return BaseResponse<PagedResult<CampaignResponse>>.Ok(pagedResult, "Lấy danh sách chiến dịch thành công.");
		}

		public async Task<BaseResponse<CampaignResponse>> GetCampaignByIdAsync(Guid campaignId)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignByIdDtoAsync(campaignId)
				  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			return BaseResponse<CampaignResponse>.Ok(campaign, "Lấy thông tin chiến dịch thành công.");
		}

		public async Task<BaseResponse<List<CampaignPromotionItemResponse>>> GetCampaignItemsByCampaignIdAsync(Guid campaignId)
		{
			_ = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
			  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			var items = await _unitOfWork.Campaigns.GetCampaignItemsAsync(campaignId, asNoTracking: true);

			return BaseResponse<List<CampaignPromotionItemResponse>>.Ok(items, "Lấy danh sách mục chiến dịch thành công.");
		}

		public async Task<BaseResponse<CampaignPromotionItemResponse>> GetCampaignItemByIdAsync(Guid campaignId, Guid itemId)
		{
			_ = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
			  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			var item = await _unitOfWork.Campaigns.GetCampaignItemByIdAsync(campaignId, itemId, asNoTracking: true)
			 ?? throw AppException.NotFound("Không tìm thấy mục chiến dịch.");

			return BaseResponse<CampaignPromotionItemResponse>.Ok(item, "Lấy thông tin mục chiến dịch thành công.");
		}

		public async Task<BaseResponse<string>> CreateCampaignAsync(CreateCampaignRequest request)
		{
			await ValidateCampaignItemsAsync(request.Items);

			// Gom tất cả Code cần kiểm tra
			var inputCodes = request.Vouchers.Select(v => v.Code.Trim().ToUpperInvariant()).ToList();

			// Lấy những Voucher trong DB có trùng Code
			var existingVouchers = await _unitOfWork.Vouchers.GetAllAsync(
				v => inputCodes.Contains(v.Code) && !v.IsDeleted,
				asNoTracking: true);

			foreach (var voucherRequest in request.Vouchers)
			{
				var existing = existingVouchers.FirstOrDefault(v => v.Code == voucherRequest.Code.Trim().ToUpperInvariant());

				if (existing != null)
				{
					throw AppException.Conflict($"Mã giảm giá '{voucherRequest.Code}' đã tồn tại hệ thống.");
				}
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var campaign = Campaign.Create(_mapper.Map<Campaign.CampaignCreationFactor>(request));

				foreach (var requestItem in request.Items)
				{
					campaign.AddPromotionItem(_mapper.Map<Campaign.PromotionItemConfigFactor>(requestItem));
				}

				foreach (var voucherRequest in request.Vouchers)
				{
					campaign.AddVoucher(_mapper.Map<Campaign.VoucherConfigFactor>(voucherRequest));
				}

				await _unitOfWork.Campaigns.AddAsync(campaign);

				_backgroundJobService.ScheduleCampaignStart(_logger, campaign.Id, campaign.StartDate);
				_backgroundJobService.ScheduleCampaignEnd(_logger, campaign.Id, campaign.EndDate);

				return BaseResponse<string>.Ok(campaign.Id.ToString(), "Tạo chiến dịch thành công.");
			});
		}

		public async Task<BaseResponse<string>> UpdateCampaignStatusAsync(Guid campaignId, UpdateCampaignStatusRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var isItemActive = request.Status == CampaignStatus.Active;

				campaign.UpdateStatus(request.Status, DateTime.UtcNow);

				foreach (var item in campaign.Items)
				{
					item.SetActive(isItemActive);
				}

				await _unitOfWork.SaveChangesAsync();
				return BaseResponse<string>.Ok(campaignId.ToString(), "Cập nhật trạng thái chiến dịch thành công.");
			});
		}

		public async Task<BaseResponse<string>> UpdateCampaignAsync(Guid campaignId, UpdateCampaignRequest request)
		{
			// 1. Load Campaign + Items + Vouchers in a single query (requires .Include in repo)
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			campaign.EnsureIsNotActive("update");

			// 2. Business validations (DB calls)
			await ValidateCampaignItemsAsync(request.Items);

			// Gom tất cả Code cần kiểm tra
			var inputCodes = request.Vouchers.Select(v => v.Code.Trim().ToUpperInvariant()).ToList();

			// Lấy những Voucher trong DB có trùng Code
			var existingVouchers = await _unitOfWork.Vouchers.GetAllAsync(
				v => inputCodes.Contains(v.Code) && !v.IsDeleted,
				asNoTracking: true);

			foreach (var voucherRequest in request.Vouchers)
			{
				var existing = existingVouchers.FirstOrDefault(v => v.Code == voucherRequest.Code.Trim().ToUpperInvariant());

				// Đối với Update, bỏ qua nếu trùng với chính Voucher đang được sửa
				var existingVoucherId = (voucherRequest as UpdateCampaignVoucherRequest)?.Id;
				if (existing != null && (existingVoucherId == null || existing.Id != existingVoucherId))
				{
					throw AppException.Conflict($"Mã giảm giá '{voucherRequest.Code}' đã tồn tại hệ thống.");
				}
			}

			// Guard: do not allow removing a voucher that has already been redeemed
			var requestVoucherIds = request.Vouchers
				.Where(v => v.Id.HasValue && v.Id != Guid.Empty)
				.Select(v => v.Id!.Value)
				.ToHashSet();

			var vouchersToRemove = campaign.Vouchers
				.Where(v => !requestVoucherIds.Contains(v.Id))
				.ToList();

			var vouchersToRemoveIds = vouchersToRemove.Select(v => v.Id).ToList();

			if (vouchersToRemoveIds.Count != 0)
			{
				// Kéo 1 lần xem có Voucher nào trong danh sách bị xóa ĐÃ ĐƯỢC DÙNG hay chưa
				var usedVoucherIds = await _unitOfWork.UserVouchers
					.GetAllAsync(uv => vouchersToRemoveIds.Contains(uv.VoucherId) && uv.Status == UsageStatus.Used)
					.ContinueWith(t => t.Result.Select(uv => uv.VoucherId).ToHashSet());

				var invalidVoucher = vouchersToRemove.FirstOrDefault(v => usedVoucherIds.Contains(v.Id));
				if (invalidVoucher != null)
				{
					throw AppException.BadRequest($"Không thể gỡ mã giảm giá '{invalidVoucher.Code}' vì đã được đổi bởi người dùng.");
				}
			}

			// 3. Execute in transaction
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var itemFactors = request.Items
				   .Select(x => _mapper.Map<Campaign.PromotionItemSyncFactor>(x))
					.ToList();

				var voucherFactors = request.Vouchers
				 .Select(x => _mapper.Map<Campaign.VoucherSyncFactor>(x))
					.ToList();

				campaign.UpdateInfo(_mapper.Map<Campaign.CampaignUpdateInfoFactor>(request));

				campaign.SyncPromotionItems(itemFactors, campaign.Status == CampaignStatus.Active);
				campaign.SyncVouchers(voucherFactors);

				return BaseResponse<string>.Ok(campaignId.ToString(), "Cập nhật chiến dịch thành công.");
			});
		}

		public async Task<BaseResponse<string>> DeleteCampaignAsync(Guid campaignId)
		{
			// BẮT BUỘC SỬA: Dùng GetCampaignWithDetailsAsync để load cả Items và Vouchers lên bộ nhớ
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
			  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			campaign.EnsureIsNotActive("delete");

			var campaignVoucherIds = campaign.Vouchers.Select(v => v.Id).ToList();
			if (campaignVoucherIds.Count > 0)
			{
				// Kiểm tra hàng loạt xem có BẤT KỲ mã nào trong chiến dịch đã được sử dụng chưa
				var hasUsedVouchers = await _unitOfWork.UserVouchers
					.AnyAsync(uv => campaignVoucherIds.Contains(uv.VoucherId) && uv.Status == UsageStatus.Used);

				if (hasUsedVouchers)
				{
					throw AppException.BadRequest("Không thể xóa chiến dịch vì có mã giảm giá bên trong đã được người dùng sử dụng.");
				}
			}

			// Thực hiện đánh dấu xóa mềm cho tất cả các bản ghi con
			campaign.SoftDeleteAllContents();

			// Thực hiện đánh dấu xóa mềm cho bản ghi cha (Repository sẽ intercept và set IsDeleted = true)
			_unitOfWork.Campaigns.Remove(campaign);

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(campaignId.ToString(), "Xóa chiến dịch thành công.");
		}
		#endregion Campaign Management



		#region Promotion Item Management
		public async Task<BaseResponse<string>> AddCampaignItemAsync(Guid campaignId, CreateCampaignPromotionItemRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
			  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			await ValidateCampaignItemsAsync([request]);

			var item = campaign.AddPromotionItem(_mapper.Map<Campaign.PromotionItemConfigFactor>(request));

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(item.Id.ToString(), "Thêm mục chiến dịch thành công.");
		}

		public async Task<BaseResponse<string>> UpdateCampaignItemAsync(Guid campaignId, Guid itemId, UpdateCampaignPromotionItemRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			if (campaign.Status == CampaignStatus.Active)
			{
				throw AppException.BadRequest("Không thể cập nhật mục của chiến dịch đang hoạt động. Vui lòng tạm dừng chiến dịch trước khi cập nhật.");
			}

			if (!campaign.Items.Any(x => x.Id == itemId))
			{
				throw AppException.NotFound("Không tìm thấy mục chiến dịch.");
			}

			await ValidateCampaignItemsAsync([request]);

			campaign.UpdatePromotionItem(itemId, _mapper.Map<Campaign.PromotionItemConfigFactor>(request));

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(itemId.ToString(), "Cập nhật mục chiến dịch thành công.");
		}

		public async Task<BaseResponse<string>> DeleteCampaignItemAsync(Guid campaignId, Guid itemId)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			if (!campaign.Items.Any(x => x.Id == itemId))
			{
				throw AppException.NotFound("Không tìm thấy mục chiến dịch.");
			}

			campaign.RemovePromotionItem(itemId);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(itemId.ToString(), "Xóa mục chiến dịch thành công.");
		}
		#endregion Promotion Item Management



		#region Campaign Voucher Management
		public async Task<BaseResponse<string>> AddCampaignVoucherAsync(Guid campaignId, CreateCampaignVoucherRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code);
			if (codeExists)
			{
				throw AppException.Conflict("Mã giảm giá đã tồn tại");
			}

			var voucher = campaign.AddVoucher(_mapper.Map<Campaign.VoucherConfigFactor>(request));

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucher.Id.ToString(), "Tạo mã giảm giá cho chiến dịch thành công.");
		}

		public async Task<BaseResponse<List<VoucherResponse>>> GetCampaignVouchersByCampaignIdAsync(Guid campaignId)
		{
			_ = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
			  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			var vouchers = await _unitOfWork.Vouchers.GetByCampaignIdAsync(campaignId);

			return BaseResponse<List<VoucherResponse>>.Ok(vouchers, "Lấy danh sách mã giảm giá của chiến dịch thành công.");
		}

		public async Task<BaseResponse<VoucherResponse>> GetCampaignVoucherByIdAsync(Guid campaignId, Guid voucherId)
		{
			_ = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
			  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(
				   v => v.Id == voucherId && v.CampaignId == campaignId && !v.IsDeleted,
				  asNoTracking: true) ?? throw AppException.NotFound("Không tìm thấy mã giảm giá của chiến dịch.");

			var response = _mapper.Map<VoucherResponse>(voucher);
			return BaseResponse<VoucherResponse>.Ok(response, "Lấy thông tin mã giảm giá của chiến dịch thành công.");
		}

		public async Task<BaseResponse<string>> UpdateCampaignVoucherAsync(Guid campaignId, Guid voucherId, UpdateCampaignVoucherRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			var voucher = campaign.Vouchers.FirstOrDefault(v => v.Id == voucherId && !v.IsDeleted) ?? throw AppException.NotFound("Không tìm thấy mã giảm giá của chiến dịch.");

			if (!string.Equals(voucher.Code, request.Code, StringComparison.OrdinalIgnoreCase))
			{
				var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code, voucherId);
				if (codeExists)
				{
					throw AppException.Conflict("Mã giảm giá đã tồn tại");
				}
			}

			campaign.UpdateVoucher(voucherId, _mapper.Map<Campaign.VoucherConfigFactor>(request));

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucherId.ToString(), "Cập nhật mã giảm giá của chiến dịch thành công.");
		}

		public async Task<BaseResponse<string>> DeleteCampaignVoucherAsync(Guid campaignId, Guid voucherId)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				  ?? throw AppException.NotFound("Không tìm thấy chiến dịch.");

			var voucher = campaign.Vouchers.FirstOrDefault(v => v.Id == voucherId && !v.IsDeleted) ?? throw AppException.NotFound("Không tìm thấy mã giảm giá của chiến dịch.");

			if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == voucherId && uv.Status == UsageStatus.Used))
			{
				throw AppException.BadRequest("Không thể xóa mã giảm giá đã được người dùng đổi");
			}

			campaign.RemoveVoucher(voucherId);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucherId.ToString(), "Xóa mã giảm giá của chiến dịch thành công.");
		}
		#endregion Campaign Voucher Management



		#region Private Helpers
		private async Task ValidateCampaignItemsAsync(IEnumerable<CreateCampaignPromotionItemRequest> items)
		{
			var mappedItems = items.Select(x => (x.ProductVariantId, x.BatchId, x.MaxUsage));
			await ValidateCampaignItemsBulkAsync(mappedItems);
		}

		private async Task ValidateCampaignItemsAsync(IEnumerable<UpdateCampaignPromotionItemRequest> items)
		{
			var mappedItems = items.Select(x => (x.ProductVariantId, x.BatchId, x.MaxUsage));
			await ValidateCampaignItemsBulkAsync(mappedItems);
		}

		private async Task ValidateCampaignItemsBulkAsync(IEnumerable<(Guid ProductVariantId, Guid? BatchId, int? MaxUsage)> items)
		{
			var itemList = items.ToList();
			if (itemList.Count == 0) return;

			// 1. Gom IDs: Dùng .HasValue và .Value để giải quyết triệt để lỗi Nullable Warning
			var variantIds = itemList.Select(x => x.ProductVariantId).Distinct().ToList();
			var batchIds = itemList
				.Where(x => x.BatchId.HasValue)
				.Select(x => x.BatchId!.Value)
				.Distinct()
				.ToList();

			// 2. BULK READ
			var variants = await _unitOfWork.Variants.GetAllAsync(v => variantIds.Contains(v.Id), asNoTracking: true);
			var variantDict = variants.ToDictionary(v => v.Id);

			var stocks = await _unitOfWork.Stocks.GetAllAsync(s => variantIds.Contains(s.VariantId), asNoTracking: true);
			var stockDict = stocks.ToDictionary(s => s.VariantId);

			var batches = await _unitOfWork.Batches.GetAllAsync(b => batchIds.Contains(b.Id), asNoTracking: true);
			var batchDict = batches.ToDictionary(b => b.Id);

			// 3. IN-MEMORY VALIDATION
			foreach (var item in itemList)
			{
				if (!variantDict.ContainsKey(item.ProductVariantId))
					throw AppException.NotFound($"Không tìm thấy biến thể sản phẩm: {item.ProductVariantId}");

				if (item.BatchId.HasValue && item.MaxUsage.HasValue)
					throw AppException.BadRequest("Chỉ được thiết lập số lượt dùng tối đa khi không chỉ định lô.");

				if (!item.BatchId.HasValue && item.MaxUsage.HasValue)
				{
					if (!stockDict.TryGetValue(item.ProductVariantId, out var stock) || stock.AvailableQuantity < item.MaxUsage.Value)
						throw AppException.BadRequest($"Số lượt dùng tối đa vượt quá tồn kho khả dụng của biến thể {item.ProductVariantId}.");
				}

				if (item.BatchId.HasValue)
				{
					if (!batchDict.TryGetValue(item.BatchId.Value, out var batch))
						throw AppException.NotFound($"Không tìm thấy lô: {item.BatchId.Value}");

					if (batch.VariantId != item.ProductVariantId)
						throw AppException.BadRequest("Lô không thuộc biến thể sản phẩm đã chỉ định.");
				}
			}
		}
		#endregion Private Helpers
	}
}
