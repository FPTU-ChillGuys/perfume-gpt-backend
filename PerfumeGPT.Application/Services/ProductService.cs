using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ProductService : IProductService
	{
		private readonly IProductRepository _productRepo;
		private readonly IMediaService _mediaService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<CreateProductRequest> _createValidator;
		private readonly IValidator<UpdateProductRequest> _updateValidator;
		private readonly IMapper _mapper;

		public ProductService(
			IProductRepository productRepo,
			IMediaService mediaService,
			IUnitOfWork unitOfWork,
			IValidator<CreateProductRequest> createValidator,
			IValidator<UpdateProductRequest> updateValidator,
			IMapper mapper)
		{
			_productRepo = productRepo;
			_mediaService = mediaService;
			_unitOfWork = unitOfWork;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_mapper = mapper;
		}

		public async Task<BaseResponse<BulkActionResult<string>>> CreateProductAsync(CreateProductRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
				);
			}

			var product = _mapper.Map<Product>(request);

			await _productRepo.AddAsync(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Failed to create product", ResponseErrorType.InternalError);
			}

			var metadata = new BulkActionMetadata();
			// Convert temporary media to permanent media if provided
			if (request.TemporaryMediaIds != null && request.TemporaryMediaIds.Count != 0)
			{
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIds, product.Id);
				if (conversionResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
				}
			}

			var result = new BulkActionResult<string>(product.Id.ToString(), metadata.Operations.Count > 0 ? metadata : null);
			var message = metadata.HasPartialFailure
				? $"Product created successfully with {metadata.TotalFailed} media upload failure(s)."
				: "Product created successfully";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteProductAsync(Guid productId)
		{
			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<string>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			if (product.IsDeleted)
			{
				return BaseResponse<string>.Fail("Product already deleted", ResponseErrorType.BadRequest);
			}

			_productRepo.Remove(product);
			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Product, productId);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(productId.ToString(), "Product deleted successfully");
		}

		public async Task<BaseResponse<ProductResponse>> GetProductAsync(Guid productId)
		{
			var product = await _productRepo.GetProductWithDetailsAsync(productId);

			if (product == null)
			{
				return BaseResponse<ProductResponse>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			var response = _mapper.Map<ProductResponse>(product);

			return BaseResponse<ProductResponse>.Ok(response, "Product retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetPagedProductsWithDetailsAsync(request);

			var productList = _mapper.Map<List<ProductListItem>>(items);

			var pagedResult = new PagedResult<ProductListItem>(
				productList,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

		return BaseResponse<PagedResult<ProductListItem>>.Ok(pagedResult, "Products retrieved successfully");
	}

	public async Task<BaseResponse<List<ProductLookupItem>>> GetProductLookupListAsync()
	{
		var products = await _productRepo.GetAllAsync();
		var lookupList = _mapper.Map<List<ProductLookupItem>>(products);
		return BaseResponse<List<ProductLookupItem>>.Ok(lookupList, "Product lookup list retrieved successfully");
	}


		public async Task<BaseResponse<BulkActionResult<string>>> UpdateProductAsync(Guid productId, UpdateProductRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			if (product.IsDeleted)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Cannot update deleted product", ResponseErrorType.BadRequest);
			}

			// === IMAGE MANAGEMENT ===
			var metadata = new BulkActionMetadata();

			// Delete specified images first
			if (request.MediaIdsToDelete != null && request.MediaIdsToDelete.Count != 0)
			{
				var deleteResult = await DeleteMultipleMediaAsync(request.MediaIdsToDelete);
				if (deleteResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Deletion", deleteResult));
				}
			}

			// Add new images from temporary media
			if (request.TemporaryMediaIdsToAdd != null && request.TemporaryMediaIdsToAdd.Count != 0)
			{
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIdsToAdd, productId);
				if (conversionResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
				}
			}

			_mapper.Map(request, product);

			_productRepo.Update(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Failed to update product", ResponseErrorType.InternalError);
			}

			var result = new BulkActionResult<string>(productId.ToString(), metadata.Operations.Count > 0 ? metadata : null);
			var message = metadata.HasPartialFailure
				? $"Product updated successfully with {metadata.TotalFailed} media operation failure(s)."
				: "Product updated successfully";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		#region Media Management

		public async Task<BaseResponse<List<MediaResponse>>> GetProductImagesAsync(Guid productId)
		{
			// Verify product exists
			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<List<MediaResponse>>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			// Get media
			var result = await _mediaService.GetMediaByEntityAsync(EntityType.Product, productId);
			return result;
		}

		#endregion

		#region Semantic Search

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetSemanticSearchProductAsync(string searchText, GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetPagedProductsWithSemanticSearch(searchText, request);

			var productList = _mapper.Map<List<ProductListItem>>(items ?? new List<Product>());

			var pagedResult = new PagedResult<ProductListItem>(
				productList,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<ProductListItem>>.Ok(pagedResult, "Semantic search products retrieved successfully");
		}

		public async Task<BaseResponse> UpdateAllProductsEmbeddingAsync()
		{
			await _productRepo.AddAllProductEmbeddingsAsync();
			return BaseResponse.Ok("All product embeddings updated successfully");
		}

		public async Task<BaseResponse> UpdateProductEmbeddingAsync(Guid productId)
		{
			await _productRepo.AddProductEmbeddingsAsync(productId);
			return BaseResponse.Ok("Product embedding updated successfully");
		}

		#endregion

		#region Private Methods

		private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(List<Guid> temporaryMediaIds, Guid productId)
		{
			var response = new BulkActionResponse();

			foreach (var tempMediaId in temporaryMediaIds)
			{
				try
				{
					// Get temporary media
					var tempMedia = await _unitOfWork.TemporaryMedia.GetByIdAsync(tempMediaId);
					if (tempMedia == null)
					{
						response.FailedItems.Add(new BulkActionError
						{
							Id = tempMediaId,
							ErrorMessage = "Temporary media not found"
						});
						continue;
					}

					if (tempMedia.IsExpired)
					{
						response.FailedItems.Add(new BulkActionError
						{
							Id = tempMediaId,
							ErrorMessage = "Temporary media has expired"
						});
						continue;
					}

					// Create permanent media from temporary
					var media = new Media
					{
						Url = tempMedia.Url,
						AltText = tempMedia.AltText,
						EntityType = EntityType.Product,
						ProductId = productId,
						DisplayOrder = tempMedia.DisplayOrder,
						IsPrimary = tempMedia.IsPrimary,
						PublicId = tempMedia.PublicId,
						FileSize = tempMedia.FileSize,
						MimeType = tempMedia.MimeType
					};

					await _unitOfWork.Media.AddAsync(media);
					_unitOfWork.TemporaryMedia.Remove(tempMedia);

					response.SucceededIds.Add(tempMediaId);
				}
				catch (Exception ex)
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = tempMediaId,
						ErrorMessage = $"Failed to convert media: {ex.Message}"
					});
				}
			}

			if (response.SucceededIds.Count > 0)
			{
				await _unitOfWork.SaveChangesAsync();
			}

			return response;
		}

		private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			var response = new BulkActionResponse();

			foreach (var mediaId in mediaIds)
			{
				try
				{
					var deleteResult = await _mediaService.DeleteMediaAsync(mediaId);
					if (deleteResult.Success)
					{
						response.SucceededIds.Add(mediaId);
					}
					else
					{
						response.FailedItems.Add(new BulkActionError
						{
							Id = mediaId,
							ErrorMessage = deleteResult.Message ?? "Unknown error"
						});
					}
				}
				catch (Exception ex)
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = mediaId,
						ErrorMessage = $"Exception during deletion: {ex.Message}"
					});
				}
			}

			return response;
		}

		#endregion
	}
}
