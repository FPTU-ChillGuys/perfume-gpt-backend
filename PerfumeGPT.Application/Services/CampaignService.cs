using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Promotions;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Application.Interfaces.Repositories;
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
		private readonly IMapper _mapper;
		private readonly IValidator<CreateCampaignRequest> _createCampaignValidator;
		private readonly IValidator<UpdateCampaignRequest> _updateCampaignValidator;
		private readonly IValidator<CreateCampaignPromotionItemRequest> _createCampaignPromotionItemValidator;

		public CampaignService(
			ICampaignRepository campaignRepository,
			IPromotionItemRepository promotionItemRepository,
			IVariantRepository variantRepository,
			IBatchRepository batchRepository,
			IMapper mapper,
			IValidator<CreateCampaignRequest> createCampaignValidator,
			IValidator<UpdateCampaignRequest> updateCampaignValidator,
			IValidator<CreateCampaignPromotionItemRequest> createCampaignPromotionItemValidator)
		{
			_campaignRepository = campaignRepository;
			_promotionItemRepository = promotionItemRepository;
			_variantRepository = variantRepository;
			_batchRepository = batchRepository;
			_mapper = mapper;
			_createCampaignValidator = createCampaignValidator;
			_updateCampaignValidator = updateCampaignValidator;
			_createCampaignPromotionItemValidator = createCampaignPromotionItemValidator;
		}

		public async Task<BaseResponse<PagedResult<CampaignResponse>>> GetPagedCampaignsAsync(GetPagedCampaignsRequest request)
		{
			Func<IQueryable<Campaign>, IOrderedQueryable<Campaign>> orderBy = request.SortBy?.ToLower() switch
			{
				"name" => request.IsDescending
					? q => q.OrderByDescending(x => x.Name)
					: q => q.OrderBy(x => x.Name),
				"startdate" => request.IsDescending
					? q => q.OrderByDescending(x => x.StartDate)
					: q => q.OrderBy(x => x.StartDate),
				"enddate" => request.IsDescending
					? q => q.OrderByDescending(x => x.EndDate)
					: q => q.OrderBy(x => x.EndDate),
				_ => request.IsDescending
					? q => q.OrderByDescending(x => x.CreatedAt)
					: q => q.OrderBy(x => x.CreatedAt)
			};

			var (items, totalCount) = await _campaignRepository.GetPagedAsync(
				filter: x => !x.IsDeleted
					&& (string.IsNullOrWhiteSpace(request.SearchTerm) || x.Name.Contains(request.SearchTerm))
					&& (!request.Status.HasValue || x.Status == request.Status.Value)
					&& (!request.Type.HasValue || x.Type == request.Type.Value),
				orderBy: orderBy,
				pageNumber: request.PageNumber,
				pageSize: request.PageSize,
				asNoTracking: true);

			var responses = _mapper.Map<List<CampaignResponse>>(items);
			var pagedResult = new PagedResult<CampaignResponse>(responses, request.PageNumber, request.PageSize, totalCount);

			return BaseResponse<PagedResult<CampaignResponse>>.Ok(pagedResult, "Campaign list retrieved successfully.");
		}
		#endregion Dependencies

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

				var campaign = _mapper.Map<Campaign>(request);
				campaign.Status = DateTime.UtcNow < request.StartDate ? CampaignStatus.Upcoming : CampaignStatus.Active;

				foreach (var item in campaign.Items)
				{
					item.StartDate ??= campaign.StartDate;
					item.EndDate ??= campaign.EndDate;
				}

				await _campaignRepository.AddAsync(campaign);
				await _campaignRepository.SaveChangesAsync();

				return BaseResponse<string>.Ok(campaign.Id.ToString(), "Campaign created successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error creating campaign: {ex.Message}", ResponseErrorType.InternalError);
			}
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

			var items = await _promotionItemRepository.GetAllAsync(
				filter: x => x.CampaignId == campaignId && !x.IsDeleted,
				orderBy: q => q.OrderByDescending(x => x.CreatedAt),
				asNoTracking: true);

			var responses = _mapper.Map<List<CampaignPromotionItemResponse>>(items);
			return BaseResponse<List<CampaignPromotionItemResponse>>.Ok(responses, "Campaign items retrieved successfully.");
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

			campaign.Status = request.Status;
			_campaignRepository.Update(campaign);
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

			_mapper.Map(request, campaign);
			_campaignRepository.Update(campaign);
			await _campaignRepository.SaveChangesAsync();

			return BaseResponse<string>.Ok(campaignId.ToString(), "Campaign updated successfully.");
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
			item.StartDate ??= campaign.StartDate;
			item.EndDate ??= campaign.EndDate;

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
			item.StartDate ??= campaign.StartDate;
			item.EndDate ??= campaign.EndDate;

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

			if (item.StartDate.HasValue && item.StartDate.Value < campaignStartDate)
			{
				return BaseResponse<string>.Fail("Item start date cannot be earlier than campaign start date.", ResponseErrorType.BadRequest);
			}

			if (item.EndDate.HasValue && item.EndDate.Value > campaignEndDate)
			{
				return BaseResponse<string>.Fail("Item end date cannot be later than campaign end date.", ResponseErrorType.BadRequest);
			}

			return null;
		}
	}
}
