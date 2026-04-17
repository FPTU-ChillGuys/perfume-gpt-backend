using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class VariantService : IVariantService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStockService _stockService;
		private readonly IMediaService _mediaService;
		private readonly MediaBulkActionHelper _helper;
		private readonly IProductAttributeService _productAttributeService;
		private readonly IMapper _mapper;

		public VariantService(
			IMediaService mediaService,
			MediaBulkActionHelper helper,
			IProductAttributeService productAttributeService,
			IStockService stockServcie,
			IUnitOfWork unitOfWork,
			IMapper mapper)
		{
			_mediaService = mediaService;
			_helper = helper;
			_productAttributeService = productAttributeService;
			_stockService = stockServcie;
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}
		#endregion Dependencies



		public async Task<BaseResponse<BulkActionResult<string>>> CreateVariantAsync(CreateVariantRequest request)
		{
			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(
				request.Attributes, isForVariant: true);
			if (attributeErrors.Count != 0)
				throw AppException.BadRequest("Dữ liệu không hợp lệ", attributeErrors);

			var variantId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			  {
				  var payload = _mapper.Map<ProductVariant.VariantPayload>(request);
				  var variant = ProductVariant.Create(request.ProductId, payload);

				  variant.SyncAttributes(request.Attributes?.Select(a => (a.AttributeId, a.ValueId)) ?? []);

				  await _unitOfWork.Variants.AddAsync(variant);

				  await _stockService.InitStockAsync(variant.Id, 0, request.LowStockThreshold);

				  return variant.Id;
			  });

			var metadata = new BulkActionMetadata { Operations = [] };
			if (request.TemporaryMediaIds?.Count > 0)
			{
				var uploadResult = await ConvertTemporaryMediaToPermanentAsync(
					request.TemporaryMediaIds, variantId);
				if (uploadResult.TotalProcessed > 0)
					metadata.Operations.Add(
						BulkOperationResult.FromBulkActionResponse("Media Upload", uploadResult));
			}

			var result = new BulkActionResult<string>(
				variantId.ToString(),
				metadata.Operations.Count > 0 ? metadata : null);

			var message = metadata.HasPartialFailure
			  ? $"Tạo biến thể thành công nhưng có {metadata.TotalFailed} tệp media tải lên thất bại."
				: "Tạo biến thể thành công";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<BulkActionResult<string>>> UpdateVariantAsync(Guid variantId, UpdateVariantRequest request)
		{
			var variant = await _unitOfWork.Variants.GetByIdAsync(variantId)
				?? throw AppException.NotFound("Không tìm thấy biến thể");

			variant.EnsureNotDeleted();

			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(
				request.Attributes, isForVariant: true);
			if (attributeErrors.Count != 0)
				throw AppException.BadRequest("Dữ liệu không hợp lệ", attributeErrors);

			var payload = _mapper.Map<ProductVariant.UpdateVariantPayload>(request);
			variant.Update(payload);

			if (request.Attributes != null)
				await _productAttributeService.ReplaceAttributesAsync(
					variantId, request.Attributes, isVariant: true);

			_unitOfWork.Variants.Update(variant);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật biến thể thất bại");

			var metadata = new BulkActionMetadata { Operations = [] };

			if (request.MediaIdsToDelete?.Count > 0)
			{
				var deleteResult = await DeleteMultipleMediaAsync(request.MediaIdsToDelete);
				if (deleteResult.TotalProcessed > 0)
					metadata.Operations.Add(
						BulkOperationResult.FromBulkActionResponse("Media Deletion", deleteResult));
			}

			if (request.TemporaryMediaIdsToAdd?.Count > 0)
			{
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(
					request.TemporaryMediaIdsToAdd, variantId);
				if (conversionResult.TotalProcessed > 0)
					metadata.Operations.Add(
						BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
			}

			var result = new BulkActionResult<string>(
				variantId.ToString(),
				metadata.Operations.Count > 0 ? metadata : null);

			var message = metadata.HasPartialFailure
			   ? $"Cập nhật biến thể thành công nhưng có {metadata.TotalFailed} thao tác media thất bại."
				: "Cập nhật biến thể thành công";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteVariantAsync(Guid variantId)
		{
			var variant = await _unitOfWork.Variants.GetByIdAsync(variantId)
				?? throw AppException.NotFound("Không tìm thấy biến thể");

			variant.EnsureNotDeleted();

			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.ProductVariant, variantId);
			await _productAttributeService.RemoveAttributesByEntityIdAsync(variantId, isVariant: true);

			_unitOfWork.Variants.Remove(variant);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xóa biến thể thất bại");

			return BaseResponse<string>.Ok(variantId.ToString(), "Xóa biến thể thành công");
		}

		public async Task<BaseResponse<PagedResult<VariantPagedItem>>> GetPagedVariantsAsync(GetPagedVariantsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Variants.GetPagedVariantsWithDetailsAsync(request);
			return BaseResponse<PagedResult<VariantPagedItem>>.Ok(
				new PagedResult<VariantPagedItem>(items, request.PageNumber, request.PageSize, totalCount),
			 "Lấy danh sách biến thể thành công");
		}

		public async Task<BaseResponse<PagedResult<VariantPagedItem>>> GetPagedVariantsByCampaignIdAsync(Guid campaignId, GetPagedVariantsRequest request)
		{
			var campaignExists = await _unitOfWork.Campaigns.AnyAsync(c => c.Id == campaignId && !c.IsDeleted);
			if (!campaignExists)
				throw AppException.NotFound("Không tìm thấy chiến dịch");

			var (items, totalCount) = await _unitOfWork.Variants.GetPagedVariantsByCampaignIdAsync(campaignId, request);
			return BaseResponse<PagedResult<VariantPagedItem>>.Ok(
				new PagedResult<VariantPagedItem>(items, request.PageNumber, request.PageSize, totalCount),
				"Lấy danh sách biến thể theo chiến dịch thành công");
		}

		public async Task<BaseResponse<ProductVariantResponse>> GetVariantByIdAsync(Guid variantId)
		{
			var variant = await _unitOfWork.Variants.GetVariantWithDetailsAsync(variantId)
				?? throw AppException.NotFound("Không tìm thấy biến thể");

			return BaseResponse<ProductVariantResponse>.Ok(variant, "Lấy thông tin biến thể thành công");
		}

		public async Task<VariantCreateOrder?> GetVariantForCreateOrderAsync(Guid variantId)
			=> await _unitOfWork.Variants.GetVariantForCreateOrderAsync(variantId);

		public async Task<BaseResponse<List<VariantLookupItem>>> GetVariantLookupListAsync(Guid? productId = null, int? supplierId = null)
		{
			var lookupItems = await _unitOfWork.Variants.GetLookupList(productId, supplierId);
			return BaseResponse<List<VariantLookupItem>>.Ok(
				lookupItems, "Lấy danh sách tra cứu biến thể thành công");
		}

		public async Task<BaseResponse<ProductVariantForPosResponse>> GetVariantByInfoAsync(string keyword)
		{
			if (string.IsNullOrWhiteSpace(keyword))
			{
				throw AppException.BadRequest("Cần ít nhất một tiêu chí tìm kiếm: barcode, sku hoặc tên.");
			}

			var variant = await _unitOfWork.Variants.GetVariantByInfoAsync(keyword)
				?? throw AppException.NotFound("Không tìm thấy biến thể");

			return BaseResponse<ProductVariantForPosResponse>.Ok(
				variant,
			  "Lấy thông tin biến thể thành công");
		}

		#region Media Management
		public async Task<BaseResponse<List<MediaResponse>>> GetVariantImagesAsync(Guid variantId)
		{
			var exists = await _unitOfWork.Variants.AnyAsync(v => v.Id == variantId);
			if (!exists) throw AppException.NotFound("Không tìm thấy biến thể");

			return await _mediaService.GetMediaByEntityAsync(EntityType.ProductVariant, variantId);
		}

		private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(
			List<Guid> temporaryMediaIds,
			Guid variantId)
		{
			return await _helper.ConvertTemporaryMediaToPermanentAsync(
				temporaryMediaIds,
				EntityType.ProductVariant,
				variantId);
		}

		private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			return await _helper.DeleteMultipleMediaAsync(mediaIds);
		}
		#endregion
	}
}
