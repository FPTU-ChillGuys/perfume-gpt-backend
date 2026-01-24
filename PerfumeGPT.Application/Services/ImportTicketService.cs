using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ImportTicketService : IImportTicketService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStockService _stockService;
		private readonly IBatchService _batchService;
		private readonly IValidator<CreateImportTicketRequest> _createImportTicketValidator;
		private readonly IValidator<VerifyImportTicketRequest> _verifyImportTicketValidator;
		private readonly IValidator<UpdateImportTicketRequest> _updateImportTicketValidator;
		private readonly IMapper _mapper;

		public ImportTicketService(
			IUnitOfWork unitOfWork,
			IStockService stockService,
			IBatchService batchService,
			IMapper mapper,
			IValidator<CreateImportTicketRequest> createImportTicketValidator,
			IValidator<VerifyImportTicketRequest> verifyImportTicketValidator,
			IValidator<UpdateImportTicketRequest> updateImportTicketValidator)
		{
			_unitOfWork = unitOfWork;
			_stockService = stockService;
			_batchService = batchService;
			_mapper = mapper;
			_createImportTicketValidator = createImportTicketValidator;
			_verifyImportTicketValidator = verifyImportTicketValidator;
			_updateImportTicketValidator = updateImportTicketValidator;
		}

		public async Task<BaseResponse<string>> CreateImportTicketAsync(CreateImportTicketRequest request, Guid userId)
		{
			try
			{
				// Validate request using FluentValidation
				var validationResult = await _createImportTicketValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				// Validation - Entity existence checks
				var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.SupplierId);
				if (supplier == null)
				{
					return BaseResponse<string>.Fail("Supplier not found.", ResponseErrorType.NotFound);
				}

				foreach (var detail in request.ImportDetails)
				{
					var variant = await _unitOfWork.Variants.GetByIdAsync(detail.VariantId);
					if (variant == null)
					{
						return BaseResponse<string>.Fail($"Variant with ID {detail.VariantId} not found.", ResponseErrorType.NotFound);
					}
				}

				// Execute within transaction - orchestrator handles SaveChanges
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

					await _unitOfWork.ImportTickets.AddAsync(importTicket);

					foreach (var detailRequest in request.ImportDetails)
					{
						var importDetail = new ImportDetail
						{
							ImportId = importTicket.Id,
							ProductVariantId = detailRequest.VariantId,
							RejectQuantity = 0,
							Note = null,
							Quantity = detailRequest.Quantity,
							UnitPrice = detailRequest.UnitPrice
						};

						await _unitOfWork.ImportDetails.AddAsync(importDetail);
					}

					return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Import ticket created successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error creating import ticket: {ex.Message}", ResponseErrorType.InternalError);
			}
		}


		public async Task<BaseResponse<string>> VerifyImportTicketAsync(Guid ticketId, VerifyImportTicketRequest request, Guid verifiedByUserId)
		{
			try
			{
				// Validate request using FluentValidation
				var validationResult = await _verifyImportTicketValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				// Validation - Entity existence and business rules
				var importTicket = await _unitOfWork.ImportTickets.GetByConditionAsync(
					predicate: it => it.Id == ticketId,
					include: query => query.Include(it => it.ImportDetails));

				if (importTicket == null)
				{
					return BaseResponse<string>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
				}

				if (importTicket.Status != ImportStatus.InProgress)
				{
					return BaseResponse<string>.Fail("Only in progress import tickets can be verified.", ResponseErrorType.BadRequest);
				}

				if (request.ImportDetails.Count != importTicket.ImportDetails.Count)
				{
					return BaseResponse<string>.Fail("Mismatch in number of import details for verification.", ResponseErrorType.BadRequest);
				}

				// Validate all import details exist and match request
				foreach (var verifyDetail in request.ImportDetails)
				{
					var importDetail = importTicket.ImportDetails.FirstOrDefault(d => d.Id == verifyDetail.ImportDetailId);
					if (importDetail == null)
					{
						return BaseResponse<string>.Fail($"Import detail with ID {verifyDetail.ImportDetailId} not found.", ResponseErrorType.NotFound);
					}

					if (verifyDetail.RejectQuantity > importDetail.Quantity)
					{
						return BaseResponse<string>.Fail($"Reject quantity ({verifyDetail.RejectQuantity}) cannot exceed total quantity ({importDetail.Quantity}) for import detail {verifyDetail.ImportDetailId}.", ResponseErrorType.BadRequest);
					}

					// Calculate accepted quantity
					var acceptedQuantity = importDetail.Quantity - verifyDetail.RejectQuantity;

					if (acceptedQuantity > 0 && (verifyDetail.Batches == null || verifyDetail.Batches.Count == 0))
					{
						return BaseResponse<string>.Fail($"Batches for import detail {verifyDetail.ImportDetailId} cannot be empty when there is accepted quantity.", ResponseErrorType.BadRequest);
					}

					// Use BatchService to validate batches against accepted quantity
					if (acceptedQuantity > 0 && !_batchService.ValidateBatches(verifyDetail.Batches, acceptedQuantity))
					{
						return BaseResponse<string>.Fail(
							$"Total batch quantity does not match accepted quantity ({acceptedQuantity}) for import detail {verifyDetail.ImportDetailId}.",
							ResponseErrorType.BadRequest);
					}
				}

				// Execute within transaction - orchestrator handles SaveChanges
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					foreach (var verifyDetail in request.ImportDetails)
					{
						var importDetail = importTicket.ImportDetails.First(d => d.Id == verifyDetail.ImportDetailId);

						// Calculate accepted quantity
						var acceptedQuantity = importDetail.Quantity - verifyDetail.RejectQuantity;

						// Update import detail with reject quantity and note
						importDetail.RejectQuantity = verifyDetail.RejectQuantity;
						importDetail.Note = verifyDetail.Note;
						_unitOfWork.ImportDetails.Update(importDetail);

						// Only create batches and update stock if there is accepted quantity
						if (acceptedQuantity > 0)
						{
							// Use BatchService to create batches with validation
							await _batchService.CreateBatchesAsync(
								importDetail.ProductVariantId,
								importDetail.Id,
								verifyDetail.Batches);

							// Use StockService to increase stock by accepted quantity only
							var stockIncreased = await _stockService.IncreaseStockAsync(
								importDetail.ProductVariantId,
								acceptedQuantity);

							if (!stockIncreased)
							{
								throw new InvalidOperationException($"Failed to increase stock for variant {importDetail.ProductVariantId}");
							}
						}
					}

					// Update import ticket status to Completed and set verifier
					importTicket.Status = ImportStatus.Completed;
					importTicket.VerifiedById = verifiedByUserId;
					_unitOfWork.ImportTickets.Update(importTicket);

					return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Import ticket verified successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error verifying import ticket: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<ImportTicketResponse>> GetImportTicketByIdAsync(Guid id)
		{
			try
			{
				var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAsync(id);

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
				var (items, totalCount) = await _unitOfWork.ImportTickets.GetPagedWithDetailsAsync(request);

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


		public async Task<BaseResponse<string>> UpdateImportStatusAsync(Guid id, UpdateImportTicketRequest request)
		{
			try
			{
				// Validate request using FluentValidation
				var validationResult = await _updateImportTicketValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				// Validation
				var importTicket = await _unitOfWork.ImportTickets.GetByIdAsync(id);
				if (importTicket == null)
				{
					return BaseResponse<string>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
				}

				if (importTicket.Status == ImportStatus.Completed || importTicket.Status == ImportStatus.Canceled)
				{
					return BaseResponse<string>.Fail("Cannot update status of a completed/Canceled import ticket.", ResponseErrorType.BadRequest);
				}

				if (request.Status < importTicket.Status)
				{
					return BaseResponse<string>.Fail("Cannot revert to a previous status.", ResponseErrorType.BadRequest);
				}

				// Update and save
				importTicket.Status = request.Status;
				_unitOfWork.ImportTickets.Update(importTicket);
				await _unitOfWork.SaveChangesAsync();

				return BaseResponse<string>.Ok(id.ToString(), "Import ticket status updated successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error updating import status: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<bool>> DeleteImportTicketAsync(Guid id)
		{
			try
			{
				// Execute within transaction - orchestrator handles SaveChanges
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsForDeleteAsync(id);

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
							_unitOfWork.Batches.Remove(batch);
						}

						// Use StockService to decrease stock with validation
						var stockDecreased = await _stockService.DecreaseStockAsync(
							detail.ProductVariantId,
							detail.Quantity);

						if (!stockDecreased)
						{
							// Log warning but continue - stock might not exist
							Console.WriteLine($"Warning: Failed to decrease stock for variant {detail.ProductVariantId}");
						}

						_unitOfWork.ImportDetails.Remove(detail);
					}

					_unitOfWork.ImportTickets.Remove(importTicket);
					// SaveChanges is handled by ExecuteInTransactionAsync orchestrator

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
