using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ProductService : IProductService
	{
		#region Dependencies
		private readonly IProductRepository _productRepo;
		private readonly IMediaService _mediaService;
		private readonly IValidator<CreateProductRequest> _createValidator;
		private readonly IValidator<UpdateProductRequest> _updateValidator;
		private readonly MediaBulkActionHelper _helper;
		private readonly IProductAttributeService _productAttributeService;
		private readonly ISignalRService _signalRService;

		public ProductService(
			IProductRepository productRepo,
			IMediaService mediaService,
			IValidator<CreateProductRequest> createValidator,
			IValidator<UpdateProductRequest> updateValidator,
			MediaBulkActionHelper helper,
			IProductAttributeService productAttributeService,
			ISignalRService signalRService)
		{
			_productRepo = productRepo;
			_mediaService = mediaService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_helper = helper;
			_productAttributeService = productAttributeService;
			_signalRService = signalRService;
		}
		#endregion Dependencies

		public async Task<BaseResponse<BulkActionResult<string>>> CreateProductAsync(CreateProductRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(request.Attributes);
			if (attributeErrors.Count != 0)
				throw AppException.BadRequest("Validation failed", attributeErrors);

			var product = Product.Create(
				request.Name,
				request.BrandId,
				request.CategoryId,
				request.Origin,
				request.Gender,
				request.ReleaseYear,
				request.Description);

			if (request.OlfactoryFamilyIds?.Count > 0)
				product.ReplaceFamilyMaps(request.OlfactoryFamilyIds);

			if (request.ScentNotes != null)
				product.ReplaceScentMaps(
					request.ScentNotes.Select(n => (n.NoteId, n.Type)));

			_productAttributeService.ApplyAttributesToProductEntity(product, request.Attributes);

			await _productRepo.AddAsync(product);
			var saved = await _productRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create product");

			var metadata = new BulkActionMetadata();
			if (request.TemporaryMediaIds?.Count > 0)
			{
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(
					request.TemporaryMediaIds, product.Id);
				if (conversionResult.TotalProcessed > 0)
					metadata.Operations.Add(
						BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
			}

			await _productRepo.AddProductEmbeddingsByIdAsync(product.Id);

			var result = new BulkActionResult<string>(
				product.Id.ToString(),
				metadata.Operations.Count > 0 ? metadata : null);

			var message = metadata.HasPartialFailure
				? $"Product created successfully with {metadata.TotalFailed} media upload failure(s)."
				: "Product created successfully";

			await _signalRService.NotifyProductCreated(product.Id);

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<BulkActionResult<string>>> UpdateProductAsync(Guid productId, UpdateProductRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var product = await _productRepo.FirstOrDefaultAsync(
				p => p.Id == productId,
				include: q => q.Include(x => x.ProductFamilyMaps).Include(x => x.ProductScentMaps))
				?? throw AppException.NotFound("Product not found");

			product.EnsureNotDeleted();

			var attributeErrors = await _productAttributeService.ValidateAttributesAsync(request.Attributes);
			if (attributeErrors.Count != 0)
				throw AppException.BadRequest("Validation failed", attributeErrors);

			var metadata = new BulkActionMetadata();

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

			product.Update(
				request.Name,
				request.BrandId,
				request.CategoryId,
				request.Origin,
				request.Gender,
				request.ReleaseYear,
				request.Description);

			if (request.OlfactoryFamilyIds != null)
				product.ReplaceFamilyMaps(request.OlfactoryFamilyIds);

			if (request.ScentNotes != null)
				product.ReplaceScentMaps(
					request.ScentNotes.Select(n => (n.NoteId, n.Type)));

			if (request.Attributes != null)
				await _productAttributeService.ReplaceAttributesAsync(
					productId, request.Attributes, isVariant: false);

			_productRepo.Update(product);
			var saved = await _productRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update product");

			await _productRepo.AddProductEmbeddingsByIdAsync(productId);

			var result = new BulkActionResult<string>(
				productId.ToString(),
				metadata.Operations.Count > 0 ? metadata : null);

			var message = metadata.HasPartialFailure
				? $"Product updated successfully with {metadata.TotalFailed} media operation failure(s)."
				: "Product updated successfully";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteProductAsync(Guid productId)
		{
			var product = await _productRepo.GetByIdAsync(productId)
				?? throw AppException.NotFound("Product not found");

			product.EnsureNotDeleted();

			var hasActiveVariants = await _productRepo.HasActiveVariantsAsync(productId);
			Product.EnsureCanBeDeleted(hasActiveVariants);

			await _productAttributeService.RemoveAttributesByEntityIdAsync(productId, isVariant: false);
			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Product, productId);

			_productRepo.Remove(product);
			var saved = await _productRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete product");

			return BaseResponse<string>.Ok(productId.ToString(), "Product deleted successfully");
		}

		public async Task<BaseResponse<ProductResponse>> GetProductAsync(Guid productId)
		{
			var response = await _productRepo.GetProductResponseAsync(productId)
				?? throw AppException.NotFound("Product not found");

			return BaseResponse<ProductResponse>.Ok(response, "Product retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetPagedProductListItemsAsync(request);
			return BaseResponse<PagedResult<ProductListItem>>.Ok(
				new PagedResult<ProductListItem>(items, request.PageNumber, request.PageSize, totalCount),
				"Products retrieved successfully");
		}

		public async Task<BaseResponse<List<ProductLookupItem>>> GetProductLookupListAsync()
		{
			var lookupList = await _productRepo.GetProductLookupListAsync();
			return BaseResponse<List<ProductLookupItem>>.Ok(lookupList, "Product lookup list retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetBestSellerProductsAsync(GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetBestSellerProductsAsync(request);
			return BaseResponse<PagedResult<ProductListItem>>.Ok(
				new PagedResult<ProductListItem>(items, request.PageNumber, request.PageSize, totalCount),
				"Best seller products retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetNewArrivalProductsAsync(GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetNewArrivalProductsAsync(request);
			return BaseResponse<PagedResult<ProductListItem>>.Ok(
				new PagedResult<ProductListItem>(items, request.PageNumber, request.PageSize, totalCount),
				"New arrival products retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetCampaignProductsAsync(Guid campaignId, GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetCampaignProductsAsync(campaignId, request);
			return BaseResponse<PagedResult<ProductListItem>>.Ok(
				new PagedResult<ProductListItem>(items, request.PageNumber, request.PageSize, totalCount),
				"Campaign products retrieved successfully");
		}

		public async Task<BaseResponse<ProductInforResponse>> GetProductInforAsync(Guid productId)
		{
			var response = await _productRepo.GetProductInfoAsync(productId)
				?? throw AppException.NotFound("Product not found");

			return BaseResponse<ProductInforResponse>.Ok(
				response, "Product information retrieved successfully");
		}

		public async Task<BaseResponse<ProductFastLookResponse>> GetProductFastLookAsync(Guid productId)
		{
			var response = await _productRepo.GetProductFastLookAsync(productId)
				?? throw AppException.NotFound("Product not found");

			return BaseResponse<ProductFastLookResponse>.Ok(
				response, "Product fast look retrieved successfully");
		}

		#region Media Management
		public async Task<BaseResponse<List<MediaResponse>>> GetProductImagesAsync(Guid productId)
		{
			var exists = await _productRepo.AnyAsync(p => p.Id == productId);
			if (!exists) throw AppException.NotFound("Product not found");

			return await _mediaService.GetMediaByEntityAsync(EntityType.Product, productId);
		}
		#endregion

		#region Semantic Search
		public async Task<BaseResponse<List<ProductDailySaleFigureResponse>>> GetProductDailySaleFiguresAsync(
		DateOnly date)
		{
			var response = await _productRepo.GetProductDailySaleFiguresAsync(date);
			return BaseResponse<List<ProductDailySaleFigureResponse>>.Ok(
				response, "Product daily sale figures retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItemWithVariants>>> GetSemanticSearchProductAsync(
			string searchText, GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetPagedProductsWithSemanticSearch(searchText, request);
			return BaseResponse<PagedResult<ProductListItemWithVariants>>.Ok(
				new PagedResult<ProductListItemWithVariants>(items, request.PageNumber, request.PageSize, totalCount),
				"Semantic search products retrieved successfully");
		}

		public async Task<BaseResponse> UpdateAllProductsEmbeddingAsync()
		{
			await _productRepo.AddAllProductEmbeddingsAsync();
			return BaseResponse.Ok("All product embeddings updated successfully");
		}

		public async Task<BaseResponse> UpdateProductEmbeddingAsync(Guid productId)
		{
			await _productRepo.AddProductEmbeddingsByIdAsync(productId);
			return BaseResponse.Ok("Product embedding updated successfully");
		}
		#endregion

		#region Private Methods
		private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(
				List<Guid> temporaryMediaIds, Guid productId)
				=> await _helper.ConvertTemporaryMediaToPermanentAsync(
					temporaryMediaIds, EntityType.Product, productId);

		private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
			=> await _helper.DeleteMultipleMediaAsync(mediaIds);
		#endregion
	}
}
