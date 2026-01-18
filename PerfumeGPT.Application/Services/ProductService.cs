using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.DTOs.Responses.Variants;
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

		public ProductService(IProductRepository productRepo, IValidator<CreateProductRequest> createValidator, IValidator<UpdateProductRequest> updateValidator)
		{
			_productRepo = productRepo;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
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

			var product = new Product
			{
				Name = request.Name,
				BrandId = request.BrandId,
				CategoryId = request.CategoryId,
				FamilyId = request.FamilyId,
				Description = request.Description,
				TopNotes = request.TopNotes,
				MiddleNotes = request.MiddleNotes,
				BaseNotes = request.BaseNotes
			};

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

			_productRepo.Update(product);
			var saved = await _productRepo.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete product", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(productId.ToString(), "Product deleted successfully");
		}

		public async Task<BaseResponse<ProductResponse>> GetProductAsync(Guid productId)
		{
			var product = await _productRepo.GetByConditionAsync(
				p => p.Id == productId && !p.IsDeleted,
				include: q => q
					.Include(p => p.Brand)
					.Include(p => p.Category)
					.Include(p => p.FragranceFamily)
					.Include(p => p.Variants)
						.ThenInclude(v => v.Concentration),
				asNoTracking: true
			);

			if (product == null)
			{
				return BaseResponse<ProductResponse>.Fail("Product not found", ResponseErrorType.NotFound);
			}

			var response = new ProductResponse
			{
				Id = product.Id,
				Name = product.Name,
				BrandId = product.BrandId,
				BrandName = product.Brand.Name ?? string.Empty,
				CategoryId = product.CategoryId,
				CategoryName = product.Category.Name ?? string.Empty,
				FamilyId = product.FamilyId,
				FamilyName = product.FragranceFamily.Name ?? string.Empty,
				Description = product.Description,
				TopNotes = product.TopNotes,
				MiddleNotes = product.MiddleNotes,
				BaseNotes = product.BaseNotes,
				Variants = product.Variants.Select(v => new ProductVariantResponse
				{
					Id = v.Id,
					ProductId = v.ProductId,
					Sku = v.Sku,
					VolumeMl = v.VolumeMl,
					ConcentrationId = v.ConcentrationId,
					ConcentrationName = v.Concentration.Name ?? string.Empty,
					Type = v.Type,
					BasePrice = v.BasePrice,
					Status = v.Status
				}).ToList()
			};

			return BaseResponse<ProductResponse>.Ok(response, "Product retrieved successfully");
		}

		public async Task<BaseResponse<PagedResult<ProductListItem>>> GetProductsAsync(GetPagedProductRequest request)
		{
			var (items, totalCount) = await _productRepo.GetPagedAsync(
				filter: p => !p.IsDeleted,
				include: q => q
					.Include(p => p.Brand)
					.Include(p => p.Category)
					.Include(p => p.FragranceFamily),
				orderBy: q => q.OrderByDescending(p => p.CreatedAt),
				pageNumber: request.PageNumber,
				pageSize: request.PageSize,
				asNoTracking: true
			);

			var productList = items.Select(p => new ProductListItem
			{
				Id = p.Id,
				Name = p.Name,
				BrandId = p.BrandId,
				BrandName = p.Brand.Name ?? string.Empty,
				CategoryId = p.CategoryId,
				CategoryName = p.Category.Name ?? string.Empty,
				FamilyId = p.FamilyId,
				FamilyName = p.FragranceFamily.Name ?? string.Empty,
				Description = p.Description,
				TopNotes = p.TopNotes,
				MiddleNotes = p.MiddleNotes,
				BaseNotes = p.BaseNotes
			}).ToList();

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

			product.Name = request.Name;
			product.BrandId = request.BrandId;
			product.CategoryId = request.CategoryId;
			product.FamilyId = request.FamilyId;
			product.Description = request.Description;
			product.TopNotes = request.TopNotes;
			product.MiddleNotes = request.MiddleNotes;
			product.BaseNotes = request.BaseNotes;

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
