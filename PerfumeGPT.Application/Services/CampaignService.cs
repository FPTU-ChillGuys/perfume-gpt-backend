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
		public async Task<BaseResponse<PagedResult<CampaignResponse>>> GetPagedCampaignsAsync(GetPagedCampaignsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Campaigns.GetPagedCampaignsAsync(request);

			var pagedResult = new PagedResult<CampaignResponse>(items, request.PageNumber, request.PageSize, totalCount);

			return BaseResponse<PagedResult<CampaignResponse>>.Ok(pagedResult, "Campaign list retrieved successfully.");
		}

		public async Task<BaseResponse<CampaignResponse>> GetCampaignByIdAsync(Guid campaignId)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignByIdDtoAsync(campaignId)
				   ?? throw AppException.NotFound("Campaign not found.");

			return BaseResponse<CampaignResponse>.Ok(campaign, "Campaign retrieved successfully.");
		}

		public async Task<BaseResponse<List<CampaignPromotionItemResponse>>> GetCampaignItemsByCampaignIdAsync(Guid campaignId)
		{
			_ = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
				?? throw AppException.NotFound("Campaign not found.");

			var items = await _unitOfWork.Campaigns.GetCampaignItemsAsync(campaignId, asNoTracking: true);

			return BaseResponse<List<CampaignPromotionItemResponse>>.Ok(items, "Campaign items retrieved successfully.");
		}

		public async Task<BaseResponse<string>> CreateCampaignAsync(CreateCampaignRequest request)
		{
			await ValidateCampaignItemsAsync(request.Items);

			foreach (var voucherRequest in request.Vouchers)
			{
				var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(voucherRequest.Code);
				if (codeExists)
				{
					throw AppException.Conflict("Voucher code already exists");
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

				return BaseResponse<string>.Ok(campaign.Id.ToString(), "Campaign created successfully.");
			});
		}

		public async Task<BaseResponse<string>> UpdateCampaignStatusAsync(Guid campaignId, UpdateCampaignStatusRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				   ?? throw AppException.NotFound("Campaign not found.");

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var isItemActive = request.Status == CampaignStatus.Active;

				campaign.UpdateStatus(request.Status, DateTime.UtcNow);

				foreach (var item in campaign.Items)
				{
					item.SetActive(isItemActive);
				}

				await _unitOfWork.SaveChangesAsync();
				return BaseResponse<string>.Ok(campaignId.ToString(), "Campaign status updated successfully.");
			});
		}

		public async Task<BaseResponse<string>> UpdateCampaignAsync(Guid campaignId, UpdateCampaignRequest request)
		{
			// 1. Load Campaign + Items + Vouchers in a single query (requires .Include in repo)
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				   ?? throw AppException.NotFound("Campaign not found.");

			campaign.EnsureIsNotActive("update");

			// 2. Business validations (DB calls)
			await ValidateCampaignItemsAsync(request.Items);

			// Check for duplicate voucher codes
			foreach (var voucherRequest in request.Vouchers)
			{
				var existingVoucherId = voucherRequest.Id.HasValue && voucherRequest.Id != Guid.Empty
					? voucherRequest.Id
					: null;

				if (await _unitOfWork.Vouchers.CodeExistsAsync(voucherRequest.Code, existingVoucherId))
					throw AppException.Conflict($"Voucher code '{voucherRequest.Code}' already exists.");
			}

			// Guard: do not allow removing a voucher that has already been redeemed
			var requestVoucherIds = request.Vouchers
				.Where(v => v.Id.HasValue && v.Id != Guid.Empty)
				.Select(v => v.Id!.Value)
				.ToHashSet();

			var vouchersToRemove = campaign.Vouchers
				.Where(v => !requestVoucherIds.Contains(v.Id))
				.ToList();

			foreach (var voucher in vouchersToRemove)
			{
				if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == voucher.Id && !uv.IsUsed))
					throw AppException.BadRequest($"Cannot remove voucher '{voucher.Code}' because it has already been redeemed.");
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

				return BaseResponse<string>.Ok(campaignId.ToString(), "Campaign updated successfully.");
			});
		}

		public async Task<BaseResponse<string>> DeleteCampaignAsync(Guid campaignId)
		{
			var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
				?? throw AppException.NotFound("Campaign not found.");

			campaign.EnsureIsNotActive("delete");

			_unitOfWork.Campaigns.Remove(campaign);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(campaignId.ToString(), "Campaign deleted successfully.");
		}
		#endregion Campaign Management


		#region Promotion Item Management
		public async Task<BaseResponse<string>> AddCampaignItemAsync(Guid campaignId, CreateCampaignPromotionItemRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
				?? throw AppException.NotFound("Campaign not found.");

			await ValidateCampaignItemAsync(request);

			var item = campaign.AddPromotionItem(_mapper.Map<Campaign.PromotionItemConfigFactor>(request));

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(item.Id.ToString(), "Campaign item added successfully.");
		}

		public async Task<BaseResponse<string>> UpdateCampaignItemAsync(Guid campaignId, Guid itemId, UpdateCampaignPromotionItemRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				   ?? throw AppException.NotFound("Campaign not found.");

			if (campaign.Status == CampaignStatus.Active)
			{
				throw AppException.BadRequest("Cannot update items of an active campaign. Please pause the campaign before updating items.");
			}

			if (!campaign.Items.Any(x => x.Id == itemId))
			{
				throw AppException.NotFound("Campaign item not found.");
			}

			await ValidateCampaignItemAsync(request);

			campaign.UpdatePromotionItem(itemId, _mapper.Map<Campaign.PromotionItemConfigFactor>(request));

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(itemId.ToString(), "Campaign item updated successfully.");
		}

		public async Task<BaseResponse<string>> DeleteCampaignItemAsync(Guid campaignId, Guid itemId)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				   ?? throw AppException.NotFound("Campaign not found.");

			if (!campaign.Items.Any(x => x.Id == itemId))
			{
				throw AppException.NotFound("Campaign item not found.");
			}

			campaign.RemovePromotionItem(itemId);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(itemId.ToString(), "Campaign item deleted successfully.");
		}
		#endregion Promotion Item Management


		#region Campaign Voucher Management
		public async Task<BaseResponse<string>> AddCampaignVoucherAsync(Guid campaignId, CreateCampaignVoucherRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				   ?? throw AppException.NotFound("Campaign not found.");

			if (campaign.Vouchers.Any(v => !v.IsDeleted))
				throw AppException.BadRequest("Campaign already has a voucher.");

			var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code);
			if (codeExists)
			{
				throw AppException.Conflict("Voucher code already exists");
			}

			var voucher = campaign.AddVoucher(_mapper.Map<Campaign.VoucherConfigFactor>(request));

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucher.Id.ToString(), "Campaign voucher created successfully.");
		}

		public async Task<BaseResponse<VoucherResponse>> GetCampaignVoucherByIdAsync(Guid campaignId, Guid voucherId)
		{
			_ = await _unitOfWork.Campaigns.GetByIdAsync(campaignId)
				?? throw AppException.NotFound("Campaign not found.");

			var voucher = await _unitOfWork.Vouchers.FirstOrDefaultAsync(
				   v => v.Id == voucherId && v.CampaignId == campaignId && !v.IsDeleted,
				   asNoTracking: true) ?? throw AppException.NotFound("Campaign voucher not found.");

			var response = _mapper.Map<VoucherResponse>(voucher);
			return BaseResponse<VoucherResponse>.Ok(response, "Campaign voucher retrieved successfully.");
		}

		public async Task<BaseResponse<string>> UpdateCampaignVoucherAsync(Guid campaignId, Guid voucherId, UpdateCampaignVoucherRequest request)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				   ?? throw AppException.NotFound("Campaign not found.");

			var voucher = campaign.Vouchers.FirstOrDefault(v => v.Id == voucherId && !v.IsDeleted) ?? throw AppException.NotFound("Campaign voucher not found.");

			if (!string.Equals(voucher.Code, request.Code, StringComparison.OrdinalIgnoreCase))
			{
				var codeExists = await _unitOfWork.Vouchers.CodeExistsAsync(request.Code, voucherId);
				if (codeExists)
				{
					throw AppException.Conflict("Voucher code already exists");
				}
			}

			campaign.UpdateVoucher(voucherId, _mapper.Map<Campaign.VoucherConfigFactor>(request));

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucherId.ToString(), "Campaign voucher updated successfully.");
		}

		public async Task<BaseResponse<string>> DeleteCampaignVoucherAsync(Guid campaignId, Guid voucherId)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId)
				   ?? throw AppException.NotFound("Campaign not found.");

			var voucher = campaign.Vouchers.FirstOrDefault(v => v.Id == voucherId && !v.IsDeleted) ?? throw AppException.NotFound("Campaign voucher not found.");

			if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == voucherId && !uv.IsUsed))
			{
				throw AppException.BadRequest("Cannot delete voucher that has been redeemed by users");
			}

			campaign.RemoveVoucher(voucherId);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucherId.ToString(), "Campaign voucher deleted successfully.");
		}
		#endregion Campaign Voucher Management

		#region Private Helpers
		private async Task ValidateCampaignItemsAsync(
			 IEnumerable<CreateCampaignPromotionItemRequest> items)
		{
			foreach (var item in items)
			{
				await ValidateCampaignItemAsync(item);
			}
		}

		private async Task ValidateCampaignItemsAsync(
			 IEnumerable<UpdateCampaignPromotionItemRequest> items)
		{
			foreach (var item in items)
			{
				await ValidateCampaignItemAsync(item);
			}
		}

		private async Task ValidateCampaignItemAsync(CreateCampaignPromotionItemRequest item)
		{
			_ = await _unitOfWork.Variants.GetByIdAsync(item.ProductVariantId) ?? throw AppException.NotFound($"Product variant not found: {item.ProductVariantId}");

			if (item.BatchId.HasValue && item.MaxUsage.HasValue)
			{
				throw AppException.BadRequest("Max usage is only allowed when batch is not specified.");
			}

			if (!item.BatchId.HasValue && item.MaxUsage.HasValue)
			{
				var hasSufficientStock = await _unitOfWork.Stocks.HasSufficientStockAsync(item.ProductVariantId, item.MaxUsage.Value);
				if (!hasSufficientStock)
				{
					throw AppException.BadRequest($"Max usage ({item.MaxUsage.Value}) exceeds available stock quantity for variant {item.ProductVariantId}.");
				}
			}

			if (item.BatchId.HasValue)
			{
				var batch = await _unitOfWork.Batches.GetByIdAsync(item.BatchId.Value) ?? throw AppException.NotFound($"Batch not found: {item.BatchId.Value}");
				if (batch.VariantId != item.ProductVariantId)
				{
					throw AppException.BadRequest("Batch does not belong to the specified product variant.");
				}
			}
		}

		private async Task ValidateCampaignItemAsync(UpdateCampaignPromotionItemRequest item)
		{
			_ = await _unitOfWork.Variants.GetByIdAsync(item.ProductVariantId) ?? throw AppException.NotFound($"Product variant not found: {item.ProductVariantId}");

			if (item.BatchId.HasValue && item.MaxUsage.HasValue)
			{
				throw AppException.BadRequest("Max usage is only allowed when batch is not specified.");
			}

			if (!item.BatchId.HasValue && item.MaxUsage.HasValue)
			{
				var hasSufficientStock = await _unitOfWork.Stocks.HasSufficientStockAsync(item.ProductVariantId, item.MaxUsage.Value);
				if (!hasSufficientStock)
				{
					throw AppException.BadRequest($"Max usage ({item.MaxUsage.Value}) exceeds available stock quantity for variant {item.ProductVariantId}.");
				}
			}

			if (item.BatchId.HasValue)
			{
				var batch = await _unitOfWork.Batches.GetByIdAsync(item.BatchId.Value) ?? throw AppException.NotFound($"Batch not found: {item.BatchId.Value}");

				if (batch.VariantId != item.ProductVariantId)
				{
					throw AppException.BadRequest("Batch does not belong to the specified product variant.");
				}
			}
		}
		#endregion Private Helpers
	}
}
