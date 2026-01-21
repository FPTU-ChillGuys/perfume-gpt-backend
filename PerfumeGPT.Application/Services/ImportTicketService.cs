using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ImportTicketService : IImportTicketService
	{
		private readonly IImportTicketRepository _importTicketRepository;
		private readonly IImportDetailRepository _importDetailRepository;
		private readonly ISupplierRepository _supplierRepository;
		private readonly IVariantRepository _variantRepository;
		private readonly IStockRepository _stockRepository;
		private readonly IBatchRepository _batchRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public ImportTicketService(
			IImportTicketRepository importTicketRepository,
			IImportDetailRepository importDetailRepository,
			ISupplierRepository supplierRepository,
			IVariantRepository variantRepository,
			IStockRepository stockRepository,
			IBatchRepository batchRepository,
			IUnitOfWork unitOfWork,
			IMapper mapper)
		{
			_importTicketRepository = importTicketRepository;
			_importDetailRepository = importDetailRepository;
			_supplierRepository = supplierRepository;
			_variantRepository = variantRepository;
			_stockRepository = stockRepository;
			_batchRepository = batchRepository;
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<BaseResponse<ImportTicketResponse>> CreateImportTicketAsync(CreateImportTicketRequest request, Guid userId)
		{
			try
			{
				var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId);
				if (supplier == null)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Supplier not found.", ResponseErrorType.NotFound);
				}

				if (request.ImportDetails == null || request.ImportDetails.Count == 0)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Import details cannot be empty.", ResponseErrorType.BadRequest);
				}

				foreach (var detail in request.ImportDetails)
				{
					var variant = await _variantRepository.GetByIdAsync(detail.VariantId);
					if (variant == null)
					{
						return BaseResponse<ImportTicketResponse>.Fail($"Variant with ID {detail.VariantId} not found.", ResponseErrorType.NotFound);
					}
				}

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var totalCost = request.ImportDetails.Sum(d => d.Quantity * d.UnitPrice);

					var importTicket = new ImportTicket
					{
						CreatedById = userId,
						SupplierId = request.SupplierId,
						ImportDate = request.ImportDate,
						TotalCost = totalCost,
						Status = ImportStatus.Pending
					};

					await _importTicketRepository.AddAsync(importTicket);

					foreach (var detailRequest in request.ImportDetails)
					{
						var importDetail = new ImportDetail
						{
							ImportId = importTicket.Id,
							ProductVariantId = detailRequest.VariantId,
							Quantity = detailRequest.Quantity,
							UnitPrice = detailRequest.UnitPrice
						};

						await _importDetailRepository.AddAsync(importDetail);
					}

					await _unitOfWork.SaveChangesAsync();

					return await GetImportTicketByIdAsync(importTicket.Id);
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<ImportTicketResponse>.Fail($"Error creating import ticket: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<ImportTicketResponse>> VerifyImportTicketAsync(VerifyImportTicketRequest request)
		{
			try
			{
				var importTicket = await _importTicketRepository.GetByConditionAsync(
					predicate: it => it.Id == request.ImportTicketId,
					include: query => query.Include(it => it.ImportDetails));

				if (importTicket == null)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
				}

				if (importTicket.Status != ImportStatus.InProgress)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Only in progress import tickets can be verified.", ResponseErrorType.BadRequest);
				}

				if (request.ImportDetails.Count != importTicket.ImportDetails.Count)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Mismatch in number of import details for verification.", ResponseErrorType.BadRequest);
				}

				if (request.ImportDetails.Any(d => d.Batches == null || d.Batches.Count == 0))
				{
					return BaseResponse<ImportTicketResponse>.Fail("Batches for each import detail cannot be empty.", ResponseErrorType.BadRequest);
				}

				// Validate all import details exist and match request
				foreach (var verifyDetail in request.ImportDetails)
				{
					var importDetail = importTicket.ImportDetails.FirstOrDefault(d => d.Id == verifyDetail.ImportDetailId);
					if (importDetail == null)
					{
						return BaseResponse<ImportTicketResponse>.Fail($"Import detail with ID {verifyDetail.ImportDetailId} not found.", ResponseErrorType.NotFound);
					}

					if (verifyDetail.Batches == null || verifyDetail.Batches.Count == 0)
					{
						return BaseResponse<ImportTicketResponse>.Fail($"Batches for import detail {verifyDetail.ImportDetailId} cannot be empty.", ResponseErrorType.BadRequest);
					}

					var totalBatchQuantity = verifyDetail.Batches.Sum(b => b.Quantity);
					if (totalBatchQuantity != importDetail.Quantity)
					{
						return BaseResponse<ImportTicketResponse>.Fail(
							$"Total batch quantity ({totalBatchQuantity}) does not match import quantity ({importDetail.Quantity}) for import detail {verifyDetail.ImportDetailId}.",
							ResponseErrorType.BadRequest);
					}
				}

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					foreach (var verifyDetail in request.ImportDetails)
					{
						var importDetail = importTicket.ImportDetails.First(d => d.Id == verifyDetail.ImportDetailId);

						foreach (var batchRequest in verifyDetail.Batches)
						{
							var batch = new Batch
							{
								VariantId = importDetail.ProductVariantId,
								ImportDetailId = importDetail.Id,
								BatchCode = batchRequest.BatchCode,
								ManufactureDate = batchRequest.ManufactureDate,
								ExpiryDate = batchRequest.ExpiryDate,
								ImportQuantity = batchRequest.Quantity,
								RemainingQuantity = batchRequest.Quantity
							};

							await _batchRepository.AddAsync(batch);
						}

						// Update stock
						var stock = await _stockRepository.FirstOrDefaultAsync(s => s.VariantId == importDetail.ProductVariantId);
						if (stock == null)
						{
							stock = new Stock
							{
								VariantId = importDetail.ProductVariantId,
								TotalQuantity = importDetail.Quantity,
								LowStockThreshold = 10
							};
							await _stockRepository.AddAsync(stock);
						}
						else
						{
							stock.TotalQuantity += importDetail.Quantity;
							_stockRepository.Update(stock);
						}
					}

					// Update import ticket status to Completed
					importTicket.Status = ImportStatus.Completed;
					_importTicketRepository.Update(importTicket);

					await _unitOfWork.SaveChangesAsync();

					return await GetImportTicketByIdAsync(importTicket.Id);
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<ImportTicketResponse>.Fail($"Error verifying import ticket: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<ImportTicketResponse>> GetImportTicketByIdAsync(Guid id)
		{
			try
			{
				var importTicket = await _importTicketRepository.GetByIdWithDetailsAsync(id);

				if (importTicket == null)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
				}

				var response = _mapper.Map<ImportTicketResponse>(importTicket);

				return BaseResponse<ImportTicketResponse>.Ok(response, "Import ticket retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<ImportTicketResponse>.Fail($"Error retrieving import ticket: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<PagedResult<ImportTicketListItem>>> GetPagedImportTicketsAsync(GetPagedImportTicketsRequest request)
		{
			try
			{
				var (items, totalCount) = await _importTicketRepository.GetPagedWithDetailsAsync(request);

				var response = _mapper.Map<List<ImportTicketListItem>>(items);

				var pagedResult = new PagedResult<ImportTicketListItem>(
					response,
					request.PageNumber,
					request.PageSize,
					totalCount
				);

				return BaseResponse<PagedResult<ImportTicketListItem>>.Ok(pagedResult, "Import tickets retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<ImportTicketListItem>>.Fail($"Error retrieving import tickets: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<ImportTicketResponse>> UpdateImportStatusAsync(Guid id, UpdateImportTicketRequest request)
		{
			try
			{
				var importTicket = await _importTicketRepository.GetByIdAsync(id);
				if (importTicket == null)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
				}

				if (importTicket.Status == ImportStatus.Completed || importTicket.Status == ImportStatus.Canceled)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Cannot update status of a completed/Canceled import ticket.", ResponseErrorType.BadRequest);
				}

				if (request.Status < importTicket.Status)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Cannot revert to a previous status.", ResponseErrorType.BadRequest);
				}

				importTicket.Status = request.Status;
				_importTicketRepository.Update(importTicket);
				await _unitOfWork.SaveChangesAsync();

				return await GetImportTicketByIdAsync(id);
			}
			catch (Exception ex)
			{
				return BaseResponse<ImportTicketResponse>.Fail($"Error updating import status: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<bool>> DeleteImportTicketAsync(Guid id)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var importTicket = await _importTicketRepository.GetByIdWithDetailsForDeleteAsync(id);

					if (importTicket == null)
					{
						return BaseResponse<bool>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
					}

					if (importTicket.Status == ImportStatus.Completed)
					{
						return BaseResponse<bool>.Fail("Cannot delete completed import ticket.", ResponseErrorType.BadRequest);
					}

					foreach (var detail in importTicket.ImportDetails)
					{
						foreach (var batch in detail.Batches)
						{
							_batchRepository.Remove(batch);
						}

						var stock = await _stockRepository.FirstOrDefaultAsync(s => s.VariantId == detail.ProductVariantId);
						if (stock != null)
						{
							stock.TotalQuantity -= detail.Quantity;
							if (stock.TotalQuantity < 0) stock.TotalQuantity = 0;
							_stockRepository.Update(stock);
						}

						_importDetailRepository.Remove(detail);
					}

					_importTicketRepository.Remove(importTicket);
					await _unitOfWork.SaveChangesAsync();

					return BaseResponse<bool>.Ok(true, "Import ticket deleted successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail($"Error deleting import ticket: {ex.Message}", ResponseErrorType.InternalError);
			}
		}
	}
}
