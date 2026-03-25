using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class CampaignService : ICampaignService
	{

		#region Dependencies
		private readonly ICampaignRepository _campaignRepository;
		private readonly IPromotionItemRepository _promotionItemRepository;
		private readonly IVariantRepository _variantRepository;
		private readonly IBatchRepository _batchRepository;
		private readonly IVoucherRepository _voucherRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly IValidator<CreateCampaignRequest> _createCampaignValidator;
		private readonly IValidator<UpdateCampaignRequest> _updateCampaignValidator;
		private readonly IValidator<CreateCampaignPromotionItemRequest> _createCampaignPromotionItemValidator;
		private readonly IValidator<CreateCampaignVoucherRequest> _createCampaignVoucherValidator;
		private readonly IValidator<UpdateCampaignVoucherRequest> _updateCampaignVoucherValidator;

		public CampaignService(
			ICampaignRepository campaignRepository,
			IPromotionItemRepository promotionItemRepository,
			IVariantRepository variantRepository,
			IBatchRepository batchRepository,
			IMapper mapper,
			IValidator<CreateCampaignRequest> createCampaignValidator,
			IValidator<UpdateCampaignRequest> updateCampaignValidator,
			IValidator<CreateCampaignPromotionItemRequest> createCampaignPromotionItemValidator,
			IValidator<CreateCampaignVoucherRequest> createCampaignVoucherValidator,
			IValidator<UpdateCampaignVoucherRequest> updateCampaignVoucherValidator,
			IVoucherRepository voucherRepository,
			IUnitOfWork unitOfWork)
		{
			_campaignRepository = campaignRepository;
			_promotionItemRepository = promotionItemRepository;
			_variantRepository = variantRepository;
			_batchRepository = batchRepository;
			_mapper = mapper;
			_createCampaignValidator = createCampaignValidator;
			_updateCampaignValidator = updateCampaignValidator;
			_createCampaignPromotionItemValidator = createCampaignPromotionItemValidator;
			_createCampaignVoucherValidator = createCampaignVoucherValidator;
			_updateCampaignVoucherValidator = updateCampaignVoucherValidator;
			_voucherRepository = voucherRepository;
			_unitOfWork = unitOfWork;
		}
		#endregion Dependencies


		#region Campaign Management
		public async Task<BaseResponse<PagedResult<CampaignResponse>>> GetPagedCampaignsAsync(GetPagedCampaignsRequest request)
		{
			var (items, totalCount) = await _campaignRepository.GetPagedCampaignsAsync(request);

			var responses = _mapper.Map<List<CampaignResponse>>(items);
			var pagedResult = new PagedResult<CampaignResponse>(responses, request.PageNumber, request.PageSize, totalCount);

			return BaseResponse<PagedResult<CampaignResponse>>.Ok(pagedResult, "Campaign list retrieved successfully.");
		}

		public async Task<BaseResponse<CampaignResponse>> GetCampaignByIdAsync(Guid campaignId)
		{
			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<CampaignResponse>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			var response = _mapper.Map<CampaignResponse>(campaign);
			return BaseResponse<CampaignResponse>.Ok(response, "Campaign retrieved successfully.");
		}

		public async Task<BaseResponse<List<CampaignPromotionItemResponse>>> GetCampaignItemsByCampaignIdAsync(Guid campaignId)
		{
			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<List<CampaignPromotionItemResponse>>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			var items = await _campaignRepository.GetCampaignItemsAsync(campaignId, asNoTracking: true);

			var responses = _mapper.Map<List<CampaignPromotionItemResponse>>(items);
			return BaseResponse<List<CampaignPromotionItemResponse>>.Ok(responses, "Campaign items retrieved successfully.");
		}

		public async Task<BaseResponse<string>> CreateCampaignAsync(CreateCampaignRequest request)
		{
			var validationResult = await _createCampaignValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed.", ResponseErrorType.BadRequest, errors);
			}

			try
			{
				var itemValidation = await ValidateCampaignItemsAsync(request.Items, request.StartDate, request.EndDate);
				if (itemValidation != null)
				{
					return itemValidation;
				}

				foreach (var voucherRequest in request.Vouchers)
				{
					var codeExists = await _voucherRepository.CodeExistsAsync(voucherRequest.Code);
					if (codeExists)
					{
						return BaseResponse<string>.Fail("Voucher code already exists", ResponseErrorType.Conflict);
					}
				}

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var campaign = _mapper.Map<Campaign>(request);
					campaign.Status = DateTime.UtcNow < request.StartDate ? CampaignStatus.Upcoming : CampaignStatus.Active;

					foreach (var item in campaign.Items)
					{
						item.Campaign = campaign;
						item.IsActive = campaign.Status == CampaignStatus.Active;
					}

					await _campaignRepository.AddAsync(campaign);

					foreach (var voucherRequest in request.Vouchers)
					{
						var voucher = _mapper.Map<Voucher>(voucherRequest);
						voucher.Campaign = campaign;
						voucher.ExpiryDate = campaign.EndDate;
						await _voucherRepository.AddAsync(voucher);
					}

					return BaseResponse<string>.Ok(campaign.Id.ToString(), "Campaign created successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error creating campaign: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> UpdateCampaignStatusAsync(Guid campaignId, UpdateCampaignStatusRequest request)
		{
			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			if (request.Status == CampaignStatus.Active && campaign.StartDate > DateTime.UtcNow)
			{
				return BaseResponse<string>.Fail("Cannot activate campaign before its start date.", ResponseErrorType.BadRequest);
			}

			if (request.Status < campaign.Status && (request.Status != CampaignStatus.Active && campaign.Status != CampaignStatus.Paused))
			{
				return BaseResponse<string>.Fail("Cannot revert campaign to a previous status.", ResponseErrorType.BadRequest);
			}

			var campaignItems = await _campaignRepository.GetCampaignItemsAsync(campaignId, asNoTracking: false);
			var isItemActive = request.Status == CampaignStatus.Active;

			campaign.Status = request.Status;
			_campaignRepository.Update(campaign);

			foreach (var item in campaignItems)
			{
				item.IsActive = isItemActive;
				_promotionItemRepository.Update(item);
			}

			await _campaignRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(campaignId.ToString(), "Campaign status updated successfully.");
		}

		public async Task<BaseResponse<string>> UpdateCampaignAsync(Guid campaignId, UpdateCampaignRequest request)
		{
			var validationResult = await _updateCampaignValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed.", ResponseErrorType.BadRequest, errors);
			}

			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			if (campaign.Status == CampaignStatus.Active)
			{
				return BaseResponse<string>.Fail("Cannot update an active campaign. Please pause the campaign before updating.", ResponseErrorType.BadRequest);
			}

			var itemValidation = await ValidateCampaignItemsAsync(request.Items, request.StartDate, request.EndDate);
			if (itemValidation != null)
			{
				return itemValidation;
			}

			var existingCampaignVouchers = (await _voucherRepository.GetAllAsync(v => v.CampaignId == campaignId && !v.IsDeleted, asNoTracking: false)).ToList();
			var existingVoucherById = existingCampaignVouchers.ToDictionary(v => v.Id, v => v);

			foreach (var voucherRequest in request.Vouchers)
			{
				if (voucherRequest.Id.HasValue && voucherRequest.Id.Value != Guid.Empty && existingVoucherById.TryGetValue(voucherRequest.Id.Value, out var existingVoucher))
				{
					var codeExists = await _voucherRepository.CodeExistsAsync(voucherRequest.Code, existingVoucher.Id);
					if (codeExists)
					{
						return BaseResponse<string>.Fail("Voucher code already exists", ResponseErrorType.Conflict);
					}
				}
				else
				{
					var codeExists = await _voucherRepository.CodeExistsAsync(voucherRequest.Code);
					if (codeExists)
					{
						return BaseResponse<string>.Fail("Voucher code already exists", ResponseErrorType.Conflict);
					}
				}
			}

			var requestExistingVoucherIds = request.Vouchers
			  .Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty)
				 .Select(x => x.Id!.Value)
				 .ToHashSet();

			foreach (var existingVoucher in existingCampaignVouchers)
			{
				if (requestExistingVoucherIds.Contains(existingVoucher.Id))
				{
					continue;
				}

				if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == existingVoucher.Id && !uv.IsUsed))
				{
					return BaseResponse<string>.Fail("Cannot remove voucher that has been redeemed by users", ResponseErrorType.BadRequest);
				}
			}

			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					_mapper.Map(request, campaign);
					_campaignRepository.Update(campaign);

					var existingItems = await _campaignRepository.GetCampaignItemsAsync(campaignId, asNoTracking: false);
					var existingItemById = existingItems.ToDictionary(x => x.Id, x => x);
					var requestExistingItemIds = request.Items
					 .Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty)
						.Select(x => x.Id!.Value)
						.ToHashSet();

					foreach (var existingItem in existingItems)
					{
						if (!requestExistingItemIds.Contains(existingItem.Id))
						{
							_promotionItemRepository.Remove(existingItem);
						}
					}

					foreach (var requestItem in request.Items)
					{
						if (requestItem.Id.HasValue && requestItem.Id.Value != Guid.Empty && existingItemById.TryGetValue(requestItem.Id.Value, out var existingItem))
						{
							existingItem.ProductVariantId = requestItem.ProductVariantId;
							existingItem.BatchId = requestItem.BatchId;
							existingItem.ItemType = requestItem.PromotionType;
							existingItem.MaxUsage = requestItem.MaxUsage;
							existingItem.AutoStopWhenBatchEmpty = requestItem.BatchId.HasValue;
							existingItem.IsActive = campaign.Status == CampaignStatus.Active;
							_promotionItemRepository.Update(existingItem);
						}
						else
						{
							var item = new PromotionItem
							{
								CampaignId = campaignId,
								ProductVariantId = requestItem.ProductVariantId,
								BatchId = requestItem.BatchId,
								ItemType = requestItem.PromotionType,
								MaxUsage = requestItem.MaxUsage,
								CurrentUsage = 0,
								AutoStopWhenBatchEmpty = requestItem.BatchId.HasValue,
								IsActive = campaign.Status == CampaignStatus.Active
							};
							await _promotionItemRepository.AddAsync(item);
						}
					}

					foreach (var existingVoucher in existingCampaignVouchers)
					{
						if (!requestExistingVoucherIds.Contains(existingVoucher.Id))
						{
							_voucherRepository.Remove(existingVoucher);
						}
					}

					foreach (var voucherRequest in request.Vouchers)
					{
						if (voucherRequest.Id.HasValue && voucherRequest.Id.Value != Guid.Empty && existingVoucherById.TryGetValue(voucherRequest.Id.Value, out var existingVoucher))
						{
							_mapper.Map(voucherRequest, existingVoucher);
							existingVoucher.CampaignId = campaignId;
							existingVoucher.ExpiryDate = campaign.EndDate;
							_voucherRepository.Update(existingVoucher);
						}
						else
						{
							var newVoucher = _mapper.Map<Voucher>(voucherRequest);
							newVoucher.CampaignId = campaignId;
							newVoucher.ExpiryDate = campaign.EndDate;
							await _voucherRepository.AddAsync(newVoucher);
						}
					}

					return BaseResponse<string>.Ok(campaignId.ToString(), "Campaign updated successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error updating campaign: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> DeleteCampaignAsync(Guid campaignId)
		{
			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			if (campaign.Status == CampaignStatus.Active)
			{
				return BaseResponse<string>.Fail("Cannot delete an active campaign. Please pause the campaign before deleting.", ResponseErrorType.BadRequest);
			}

			_campaignRepository.Remove(campaign);
			await _campaignRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(campaignId.ToString(), "Campaign deleted successfully.");
		}
		#endregion Campaign Management


		#region Promotion Item Management
		public async Task<BaseResponse<string>> AddCampaignItemAsync(Guid campaignId, CreateCampaignPromotionItemRequest request)
		{
			var validationResult = await _createCampaignPromotionItemValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed.", ResponseErrorType.BadRequest, errors);
			}

			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			var itemValidation = await ValidateCampaignItemAsync(request, campaign.StartDate, campaign.EndDate);
			if (itemValidation != null)
			{
				return itemValidation;
			}

			var item = _mapper.Map<PromotionItem>(request);
			item.CampaignId = campaignId;
			item.AutoStopWhenBatchEmpty = request.BatchId.HasValue;
			item.IsActive = campaign.Status == CampaignStatus.Active;

			await _promotionItemRepository.AddAsync(item);
			await _promotionItemRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(item.Id.ToString(), "Campaign item added successfully.");
		}

		public async Task<BaseResponse<string>> UpdateCampaignItemAsync(Guid campaignId, Guid itemId, CreateCampaignPromotionItemRequest request)
		{
			var validationResult = await _createCampaignPromotionItemValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed.", ResponseErrorType.BadRequest, errors);
			}

			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			if (campaign.Status == CampaignStatus.Active)
			{
				return BaseResponse<string>.Fail("Cannot update items of an active campaign. Please pause the campaign before updating items.", ResponseErrorType.BadRequest);
			}

			var item = await _promotionItemRepository.FirstOrDefaultAsync(x => x.Id == itemId && x.CampaignId == campaignId);
			if (item == null)
			{
				return BaseResponse<string>.Fail("Campaign item not found.", ResponseErrorType.NotFound);
			}

			var itemValidation = await ValidateCampaignItemAsync(request, campaign.StartDate, campaign.EndDate);
			if (itemValidation != null)
			{
				return itemValidation;
			}

			_mapper.Map(request, item);
			item.IsActive = campaign.Status == CampaignStatus.Active;

			_promotionItemRepository.Update(item);
			await _promotionItemRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(itemId.ToString(), "Campaign item updated successfully.");
		}

		public async Task<BaseResponse<string>> DeleteCampaignItemAsync(Guid campaignId, Guid itemId)
		{
			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			var item = await _promotionItemRepository.FirstOrDefaultAsync(x => x.Id == itemId && x.CampaignId == campaignId);
			if (item == null)
			{
				return BaseResponse<string>.Fail("Campaign item not found.", ResponseErrorType.NotFound);
			}

			_promotionItemRepository.Remove(item);
			await _promotionItemRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(itemId.ToString(), "Campaign item deleted successfully.");
		}
		#endregion Promotion Item Management


		#region Campaign Voucher Management
		public async Task<BaseResponse<string>> AddCampaignVoucherAsync(Guid campaignId, CreateCampaignVoucherRequest request)
		{
			var validationResult = await _createCampaignVoucherValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed.", ResponseErrorType.BadRequest, errors);
			}

			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			var existingVoucher = await _voucherRepository.FirstOrDefaultAsync(v => v.CampaignId == campaignId && !v.IsDeleted);
			if (existingVoucher != null)
			{
				return BaseResponse<string>.Fail("Campaign already has a voucher.", ResponseErrorType.BadRequest);
			}

			var codeExists = await _voucherRepository.CodeExistsAsync(request.Code);
			if (codeExists)
			{
				return BaseResponse<string>.Fail("Voucher code already exists", ResponseErrorType.Conflict);
			}

			var voucher = _mapper.Map<Voucher>(request);
			voucher.CampaignId = campaignId;
			voucher.ExpiryDate = campaign.EndDate;

			await _voucherRepository.AddAsync(voucher);
			await _voucherRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucher.Id.ToString(), "Campaign voucher created successfully.");
		}

		public async Task<BaseResponse<VoucherResponse>> GetCampaignVoucherByIdAsync(Guid campaignId, Guid voucherId)
		{
			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<VoucherResponse>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			var voucher = await _voucherRepository.FirstOrDefaultAsync(
				v => v.Id == voucherId && v.CampaignId == campaignId && !v.IsDeleted,
				asNoTracking: true);

			if (voucher == null)
			{
				return BaseResponse<VoucherResponse>.Fail("Campaign voucher not found.", ResponseErrorType.NotFound);
			}

			var response = _mapper.Map<VoucherResponse>(voucher);
			return BaseResponse<VoucherResponse>.Ok(response, "Campaign voucher retrieved successfully.");
		}

		public async Task<BaseResponse<string>> UpdateCampaignVoucherAsync(Guid campaignId, Guid voucherId, UpdateCampaignVoucherRequest request)
		{
			var validationResult = await _updateCampaignVoucherValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed.", ResponseErrorType.BadRequest, errors);
			}

			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			var voucher = await _voucherRepository.FirstOrDefaultAsync(
				v => v.Id == voucherId && v.CampaignId == campaignId && !v.IsDeleted,
				asNoTracking: false);

			if (voucher == null)
			{
				return BaseResponse<string>.Fail("Campaign voucher not found.", ResponseErrorType.NotFound);
			}

			if (!string.Equals(voucher.Code, request.Code, StringComparison.OrdinalIgnoreCase))
			{
				var codeExists = await _voucherRepository.CodeExistsAsync(request.Code, voucherId);
				if (codeExists)
				{
					return BaseResponse<string>.Fail("Voucher code already exists", ResponseErrorType.Conflict);
				}
			}

			_mapper.Map(request, voucher);
			voucher.CampaignId = campaignId;
			voucher.ExpiryDate = campaign.EndDate;
			_voucherRepository.Update(voucher);
			await _voucherRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucherId.ToString(), "Campaign voucher updated successfully.");
		}

		public async Task<BaseResponse<string>> DeleteCampaignVoucherAsync(Guid campaignId, Guid voucherId)
		{
			var campaign = await _campaignRepository.GetByIdAsync(campaignId);
			if (campaign == null)
			{
				return BaseResponse<string>.Fail("Campaign not found.", ResponseErrorType.NotFound);
			}

			var voucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Id == voucherId && v.CampaignId == campaignId && !v.IsDeleted);
			if (voucher == null)
			{
				return BaseResponse<string>.Fail("Campaign voucher not found.", ResponseErrorType.NotFound);
			}

			if (await _unitOfWork.UserVouchers.AnyAsync(uv => uv.VoucherId == voucherId && !uv.IsUsed))
			{
				return BaseResponse<string>.Fail("Cannot delete voucher that has been redeemed by users", ResponseErrorType.BadRequest);
			}

			_voucherRepository.Remove(voucher);
			await _voucherRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(voucherId.ToString(), "Campaign voucher deleted successfully.");
		}
		#endregion Campaign Voucher Management


		#region Private Helpers
		private async Task<BaseResponse<string>?> ValidateCampaignItemsAsync(
			IEnumerable<CreateCampaignPromotionItemRequest> items,
			DateTime campaignStartDate,
			DateTime campaignEndDate)
		{
			foreach (var item in items)
			{
				var itemValidation = await ValidateCampaignItemAsync(item, campaignStartDate, campaignEndDate);
				if (itemValidation != null)
				{
					return itemValidation;
				}
			}

			return null;
		}

		private async Task<BaseResponse<string>?> ValidateCampaignItemsAsync(
			IEnumerable<UpdateCampaignPromotionItemRequest> items,
			DateTime campaignStartDate,
			DateTime campaignEndDate)
		{
			foreach (var item in items)
			{
				var itemValidation = await ValidateCampaignItemAsync(item, campaignStartDate, campaignEndDate);
				if (itemValidation != null)
				{
					return itemValidation;
				}
			}

			return null;
		}

		private async Task<BaseResponse<string>?> ValidateCampaignItemAsync(
			CreateCampaignPromotionItemRequest item,
			DateTime campaignStartDate,
			DateTime campaignEndDate)
		{
			var variant = await _variantRepository.GetByIdAsync(item.ProductVariantId);
			if (variant == null)
			{
				return BaseResponse<string>.Fail($"Product variant not found: {item.ProductVariantId}", ResponseErrorType.NotFound);
			}

			if (item.BatchId.HasValue)
			{
				var batch = await _batchRepository.GetByIdAsync(item.BatchId.Value);
				if (batch == null)
				{
					return BaseResponse<string>.Fail($"Batch not found: {item.BatchId.Value}", ResponseErrorType.NotFound);
				}

				if (batch.VariantId != item.ProductVariantId)
				{
					return BaseResponse<string>.Fail("Batch does not belong to the specified product variant.", ResponseErrorType.BadRequest);
				}
			}

			return null;
		}

		private async Task<BaseResponse<string>?> ValidateCampaignItemAsync(
			UpdateCampaignPromotionItemRequest item,
			DateTime campaignStartDate,
			DateTime campaignEndDate)
		{
			var variant = await _variantRepository.GetByIdAsync(item.ProductVariantId);
			if (variant == null)
			{
				return BaseResponse<string>.Fail($"Product variant not found: {item.ProductVariantId}", ResponseErrorType.NotFound);
			}

			if (item.BatchId.HasValue)
			{
				var batch = await _batchRepository.GetByIdAsync(item.BatchId.Value);
				if (batch == null)
				{
					return BaseResponse<string>.Fail($"Batch not found: {item.BatchId.Value}", ResponseErrorType.NotFound);
				}

				if (batch.VariantId != item.ProductVariantId)
				{
					return BaseResponse<string>.Fail("Batch does not belong to the specified product variant.", ResponseErrorType.BadRequest);
				}
			}

			return null;
		}
		#endregion Private Helpers
	}
}
