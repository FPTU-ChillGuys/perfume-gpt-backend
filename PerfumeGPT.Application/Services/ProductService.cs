using MapsterMapper;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ProductService : IProductService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMediaService _mediaService;
		private readonly MediaBulkActionHelper _helper;
		private readonly IProductAttributeService _productAttributeService;
		private readonly IMapper _mapper;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<ProductService> _logger;

		public ProductService(
			IMediaService mediaService,
			MediaBulkActionHelper helper,
			IProductAttributeService productAttributeService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			IBackgroundJobService backgroundJobService,
			ILogger<ProductService> logger)
		{
			_mediaService = mediaService;
			_helper = helper;
			_productAttributeService = productAttributeService;
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
		}
		#endregion Dependencies



		public async Task<BaseResponse<BulkActionResult<string>>> CreateProductAsync(CreateProductRequest request)
		{
			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(request.Attributes);
			if (attributeErrors.Count != 0)
				throw AppException.BadRequest("Dữ liệu không hợp lệ", attributeErrors);

			var payload = _mapper.Map<Product.ProductPayload>(request);
			var product = Product.Create(payload);

			if (request.OlfactoryFamilyIds?.Count > 0)
				product.ReplaceFamilyMaps(request.OlfactoryFamilyIds);

			if (request.ScentNotes != null)
				product.ReplaceScentMaps(
					request.ScentNotes.Select(n => (n.NoteId, n.Type)));

			product.SyncAttributes(request.Attributes?.Select(a => (a.AttributeId, a.ValueId)) ?? []);

			await _unitOfWork.Products.AddAsync(product);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Tạo sản phẩm thất bại");

			// Notify NestJS to rebuild embedding + BM25
			_backgroundJobService.EnqueueProductUpdatedRedisEvent(_logger, product.Id, "created");

			var metadata = new BulkActionMetadata { Operations = [] };
			if (request.TemporaryMediaIds?.Count > 0)
			{
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(
					request.TemporaryMediaIds, product.Id);
				if (conversionResult.TotalProcessed > 0)
					metadata.Operations.Add(
						BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
			}

			var result = new BulkActionResult<string>(
				product.Id.ToString(),
				metadata.Operations.Count > 0 ? metadata : null);

			var message = metadata.HasPartialFailure
			  ? $"Tạo sản phẩm thành công nhưng có {metadata.TotalFailed} tệp media tải lên thất bại."
				: "Tạo sản phẩm thành công";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<BulkActionResult<string>>> UpdateProductAsync(Guid productId, UpdateProductRequest request)
		{
			var product = await _unitOfWork.Products.GetProductAggregateForUpdateAsync(productId)
				?? throw AppException.NotFound("Không tìm thấy sản phẩm");

			product.EnsureNotDeleted();

			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(request.Attributes);
			if (attributeErrors.Count != 0)
				throw AppException.BadRequest("Dữ liệu không hợp lệ", attributeErrors);

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
					request.TemporaryMediaIdsToAdd, productId);
				if (conversionResult.TotalProcessed > 0)
					metadata.Operations.Add(
						BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
			}

			var payload = _mapper.Map<Product.UpdateProductPayload>(request);
			product.Update(payload);

			if (request.OlfactoryFamilyIds != null)
				product.ReplaceFamilyMaps(request.OlfactoryFamilyIds);

			if (request.ScentNotes != null)
				product.ReplaceScentMaps(
					request.ScentNotes.Select(n => (n.NoteId, n.Type)));

			if (request.Attributes != null)
				product.SyncAttributes(request.Attributes.Select(a => (a.AttributeId, a.ValueId)));

			_unitOfWork.Products.Update(product);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật sản phẩm thất bại");

			// Notify NestJS to rebuild embedding + BM25
			_backgroundJobService.EnqueueProductUpdatedRedisEvent(_logger, productId, "updated");



			var result = new BulkActionResult<string>(
				productId.ToString(),
				metadata.Operations.Count > 0 ? metadata : null);

			var message = metadata.HasPartialFailure
			   ? $"Cập nhật sản phẩm thành công nhưng có {metadata.TotalFailed} thao tác media thất bại."
				: "Cập nhật sản phẩm thành công";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteProductAsync(Guid productId)
		{
			var product = await _unitOfWork.Products.GetByIdAsync(productId)
				?? throw AppException.NotFound("Không tìm thấy sản phẩm");

			product.EnsureNotDeleted();

			var hasActiveVariants = await _unitOfWork.Products.HasActiveVariantsAsync(productId);
			Product.EnsureCanBeDeleted(hasActiveVariants);

			await _productAttributeService.RemoveAttributesByEntityIdAsync(productId, isVariant: false);
			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Product, productId);

			_unitOfWork.Products.Remove(product);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xóa sản phẩm thất bại");

			// Notify NestJS to delete embedding + refresh BM25
			_backgroundJobService.EnqueueProductUpdatedRedisEvent(_logger, productId, "deleted");

			return BaseResponse<string>.Ok(productId.ToString(), "Xóa sản phẩm thành công");
		}

		public async Task<BaseResponse<ProductResponse>> GetAdminProductAsync(Guid productId)
		{
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var response = await _unitOfWork.Products.GetProductResponseAsync(productId, sellable)
				?? throw AppException.NotFound("Không tìm thấy sản phẩm");

			return BaseResponse<ProductResponse>.Ok(response, "Lấy thông tin sản phẩm thành công");
		}

		public async Task<BaseResponse<PublicProductResponse>> GetPublicProductAsync(Guid productId)
		{
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var response = await _unitOfWork.Products.GetPublicProductResponseAsync(productId, sellable)
				?? throw AppException.NotFound("Không tìm thấy sản phẩm");

			return BaseResponse<PublicProductResponse>.Ok(response, "Lấy chi tiết sản phẩm thành công");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request)
		{
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var policy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();
			var newTagThresholdInDays = policy?.NewTagThresholdInDays ?? 30;
			var (items, totalCount) = await _unitOfWork.Products.GetPagedProductListItemsAsync(request, sellable, newTagThresholdInDays);
			return BaseResponse<PagedResult<ProductListItem>>.Ok(
				new PagedResult<ProductListItem>(items, request.PageNumber, request.PageSize, totalCount),
			 "Lấy danh sách sản phẩm thành công");
		}

		public async Task<BaseResponse<List<ProductLookupItem>>> GetProductLookupListAsync()
		{
			var lookupList = await _unitOfWork.Products.GetProductLookupListAsync();
			return BaseResponse<List<ProductLookupItem>>.Ok(lookupList, "Lấy danh sách tra cứu sản phẩm thành công");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetBestSellerProductsAsync(GetPagedProductRequest request)
		{
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var policy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();
			var newTagThresholdInDays = policy?.NewTagThresholdInDays ?? 30;
			var (items, totalCount) = await _unitOfWork.Products.GetBestSellerProductsAsync(request, sellable, newTagThresholdInDays);
			return BaseResponse<PagedResult<ProductListItem>>.Ok(
				new PagedResult<ProductListItem>(items, request.PageNumber, request.PageSize, totalCount),
			 "Lấy danh sách sản phẩm bán chạy thành công");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetNewArrivalProductsAsync(GetPagedProductRequest request)
		{
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var policy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();
			var newTagThresholdInDays = policy?.NewTagThresholdInDays ?? 30;
			var (items, totalCount) = await _unitOfWork.Products.GetNewArrivalProductsAsync(request, sellable, newTagThresholdInDays);
			return BaseResponse<PagedResult<ProductListItem>>.Ok(
				new PagedResult<ProductListItem>(items, request.PageNumber, request.PageSize, totalCount),
			 "Lấy danh sách sản phẩm mới về thành công");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetCampaignProductsAsync(Guid campaignId, GetPagedProductRequest request)
		{
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var policy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();
			var newTagThresholdInDays = policy?.NewTagThresholdInDays ?? 30;
			var (items, totalCount) = await _unitOfWork.Products.GetCampaignProductsAsync(campaignId, request, sellable, newTagThresholdInDays);
			return BaseResponse<PagedResult<ProductListItem>>.Ok(
				new PagedResult<ProductListItem>(items, request.PageNumber, request.PageSize, totalCount),
				"Lấy danh sách sản phẩm theo chiến dịch thành công");
		}

		public async Task<BaseResponse<ProductInforResponse>> GetProductInforAsync(Guid productId)
		{
			var response = await _unitOfWork.Products.GetProductInfoAsync(productId)
				?? throw AppException.NotFound("Không tìm thấy sản phẩm");

			return BaseResponse<ProductInforResponse>.Ok(
				response, "Lấy thông tin sản phẩm thành công");
		}

		public async Task<BaseResponse<ProductFastLookResponse>> GetProductFastLookAsync(Guid productId)
		{
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var response = await _unitOfWork.Products.GetProductFastLookAsync(productId, sellable)
				?? throw AppException.NotFound("Không tìm thấy sản phẩm");

			return BaseResponse<ProductFastLookResponse>.Ok(
			  response, "Lấy nhanh thông tin sản phẩm thành công");
		}



		#region Media Management
		public async Task<BaseResponse<List<MediaResponse>>> GetProductImagesAsync(Guid productId)
		{
			var exists = await _unitOfWork.Products.AnyAsync(p => p.Id == productId);
			if (!exists) throw AppException.NotFound("Không tìm thấy sản phẩm");

			return await _mediaService.GetMediaByEntityAsync(EntityType.Product, productId);
		}
		#endregion Media Management



		#region Semantic Search
		public async Task<BaseResponse<List<ProductDailySaleFigureResponse>>> GetProductDailySaleFiguresAsync(
		DateOnly date)
		{
			var response = await _unitOfWork.Products.GetProductDailySaleFiguresAsync(date);
			return BaseResponse<List<ProductDailySaleFigureResponse>>.Ok(
			 response, "Lấy số liệu bán hàng theo ngày của sản phẩm thành công");
		}
		#endregion Semantic Search



		#region Private Methods
		private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(
				List<Guid> temporaryMediaIds, Guid productId)
				=> await _helper.ConvertTemporaryMediaToPermanentAsync(
					temporaryMediaIds, EntityType.Product, productId);

		private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
			=> await _helper.DeleteMultipleMediaAsync(mediaIds);
		#endregion Private Methods
	}
}
