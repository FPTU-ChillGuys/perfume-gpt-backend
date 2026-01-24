using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class ProductService : IProductService
	{
		private readonly IProductRepository _productRepo;
		private readonly IValidator<CreateProductRequest> _createValidator;
		private readonly IValidator<UpdateProductRequest> _updateValidator;
		private readonly IMapper _mapper;

		public ProductService(
			IProductRepository productRepo,
			IValidator<CreateProductRequest> createValidator,
			IValidator<UpdateProductRequest> updateValidator,
			IMapper mapper)
		{
			_productRepo = productRepo;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_mapper = mapper;
		}

		public async Task<BaseResponse<string>> CreateProductAsync(CreateProductRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			// Use mapper to create Product entity
			var product = _mapper.Map<Product>(request);

			await _productRepo.AddAsync(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to create product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(product.Id.ToString(), "Product created successfully");
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
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(productId.ToString(), "Product deleted successfully");
		}

		public async Task<BaseResponse<ProductResponse>> GetProductAsync(Guid productId)
		{
			// Use repository method with includes
			var product = await _productRepo.GetProductWithDetailsAsync(productId);

			if (product == null)
			{
				return BaseResponse<ProductResponse>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			// Use mapper to convert to response DTO
			var response = _mapper.Map<ProductResponse>(product);

			return BaseResponse<ProductResponse>.Ok(response, "Product retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request)
		{
			// Use repository method with includes
			var (items, totalCount) = await _productRepo.GetPagedProductsWithDetailsAsync(request);

			// Use mapper to convert to response DTOs
			var productList = _mapper.Map<List<ProductListItem>>(items);

			var pagedResult = new PagedResult<ProductListItem>(
				productList,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<ProductListItem>>.Ok(pagedResult, "Products retrieved successfully");
		}

		public async Task<BaseResponse<string>> UpdateProductAsync(Guid productId, UpdateProductRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			var product = await _productRepo.GetByIdAsync(productId);
			if (product == null)
			{
				return BaseResponse<string>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			if (product.IsDeleted)
			{
				return BaseResponse<string>.Fail("Cannot update deleted product", ResponseErrorType.BadRequest);
			}

			// Use mapper to update entity
			_mapper.Map(request, product);

			_productRepo.Update(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to update product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(productId.ToString(), "Product updated successfully");
		}
	}
}
