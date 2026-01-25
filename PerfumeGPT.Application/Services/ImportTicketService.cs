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
		private readonly IValidator<UpdateFullImportTicketRequest> _updateFullImportTicketValidator;
		private readonly IMapper _mapper;

		public ImportTicketService(
			IUnitOfWork unitOfWork,
			IStockService stockService,
			IBatchService batchService,
			IMapper mapper,
			IValidator<CreateImportTicketRequest> createImportTicketValidator,
			IValidator<VerifyImportTicketRequest> verifyImportTicketValidator,
			IValidator<UpdateImportTicketRequest> updateImportTicketValidator,
			IValidator<UpdateFullImportTicketRequest> updateFullImportTicketValidator)
		{
			_unitOfWork = unitOfWork;
			_stockService = stockService;
			_batchService = batchService;
			_mapper = mapper;
			_createImportTicketValidator = createImportTicketValidator;
			_verifyImportTicketValidator = verifyImportTicketValidator;
			_updateImportTicketValidator = updateImportTicketValidator;
			_updateFullImportTicketValidator = updateFullImportTicketValidator;
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

				// VALIDATION: Check for duplicate variant IDs
				var duplicateVariants = request.ImportDetails
					.GroupBy(d => d.VariantId)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateVariants.Any())
				{
					var duplicateIds = string.Join(", ", duplicateVariants);
					return BaseResponse<string>.Fail($"Duplicate variant IDs found: {duplicateIds}. Each variant can only appear once per import ticket.", ResponseErrorType.BadRequest);
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
							// ENHANCEMENT: Merge batches with the same batch code before creating
							var mergedBatches = MergeBatchesBySameCode(verifyDetail.Batches);

							// Use BatchService to create batches with validation
							await _batchService.CreateBatchesAsync(
								importDetail.ProductVariantId,
								importDetail.Id,
								mergedBatches);

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

				// Completed and Cancelled are immutable
				if (importTicket.Status == ImportStatus.Completed)
				{
					return BaseResponse<string>.Fail("Completed import tickets are immutable. Create an adjustment ticket if needed.", ResponseErrorType.BadRequest);
				}

				if (importTicket.Status == ImportStatus.Canceled)
				{
					return BaseResponse<string>.Fail("Cancelled import tickets are read-only.", ResponseErrorType.BadRequest);
				}

				// InProgress tickets are locked
				if (importTicket.Status == ImportStatus.InProgress && request.Status != ImportStatus.Canceled)
				{
					return BaseResponse<string>.Fail("Import ticket is locked while in progress. Complete verification or cancel it first.", ResponseErrorType.BadRequest);
				}

				// Only Pending tickets can have their status changed
				// Valid transitions: Pending -> InProgress, Pending -> Canceled
				if (importTicket.Status == ImportStatus.Pending)
				{
					if (request.Status != ImportStatus.InProgress && request.Status != ImportStatus.Canceled)
					{
						return BaseResponse<string>.Fail("Pending tickets can only transition to InProgress or Canceled status.", ResponseErrorType.BadRequest);
					}
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

		public async Task<BaseResponse<string>> UpdateImportTicketAsync(Guid adminId, Guid id, UpdateFullImportTicketRequest request)
		{
			try
			{
				// Validate request using FluentValidation
				var validationResult = await _updateFullImportTicketValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				// Validation - Check if supplier exists
				var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.SupplierId);
				if (supplier == null)
				{
					return BaseResponse<string>.Fail("Supplier not found.", ResponseErrorType.NotFound);
				}

				// Validate all variants exist
				foreach (var detail in request.ImportDetails)
				{
					var variant = await _unitOfWork.Variants.GetByIdAsync(detail.VariantId);
					if (variant == null)
					{
						return BaseResponse<string>.Fail($"Variant with ID {detail.VariantId} not found.", ResponseErrorType.NotFound);
					}
				}

				// Execute within transaction
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var importTicket = await _unitOfWork.ImportTickets.GetByConditionAsync(
						predicate: it => it.Id == id,
						include: query => query.Include(it => it.ImportDetails));

					if (importTicket == null)
					{
						return BaseResponse<string>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
					}

					// SAFETY RULE: Only Pending tickets can be updated
					if (importTicket.Status == ImportStatus.InProgress)
					{
						return BaseResponse<string>.Fail("Import ticket is locked while in progress. Changes will cause process errors.", ResponseErrorType.BadRequest);
					}

					if (importTicket.Status == ImportStatus.Completed)
					{
						return BaseResponse<string>.Fail("Completed import tickets are immutable. Create an adjustment ticket if needed.", ResponseErrorType.BadRequest);
					}

					if (importTicket.Status == ImportStatus.Canceled)
					{
						return BaseResponse<string>.Fail("Cancelled import tickets are read-only.", ResponseErrorType.BadRequest);
					}

					// At this point, status must be Pending - safe to update
					// Update import ticket fields
					importTicket.SupplierId = request.SupplierId;
					importTicket.ImportDate = request.ImportDate;

					// VALIDATION: Check for duplicate variant IDs in request
					var duplicateVariants = request.ImportDetails
						.GroupBy(d => d.VariantId)
						.Where(g => g.Count() > 1)
						.Select(g => g.Key)
						.ToList();

					if (duplicateVariants.Count != 0)
					{
						var duplicateIds = string.Join(", ", duplicateVariants);
						return BaseResponse<string>.Fail($"Duplicate variant IDs found: {duplicateIds}. Each variant can only appear once per import ticket.", ResponseErrorType.BadRequest);
					}

					// Calculate new total cost
					var totalCost = request.ImportDetails.Sum(d => d.Quantity * d.UnitPrice);
					importTicket.TotalCost = totalCost;

					// SAFE UPDATE LOGIC: Handle ImportDetails for Pending status only
					// No stock adjustments needed - stock is only adjusted during verify
					var requestDetailIds = request.ImportDetails.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();

					// Remove details that are not in the request
					var detailsToRemove = importTicket.ImportDetails.Where(d => !requestDetailIds.Contains(d.Id)).ToList();
					foreach (var detail in detailsToRemove)
					{
						// Safe to remove - Pending status has no stock or batches
						_unitOfWork.ImportDetails.Remove(detail);
					}

					// Update existing and add new details
					foreach (var detailRequest in request.ImportDetails)
					{
						if (detailRequest.Id.HasValue)
						{
							// Update existing detail
							var existingDetail = importTicket.ImportDetails.FirstOrDefault(d => d.Id == detailRequest.Id.Value);
							if (existingDetail != null)
							{
								// Simple update - no stock adjustments for Pending status
								existingDetail.ProductVariantId = detailRequest.VariantId;
								existingDetail.Quantity = detailRequest.Quantity;
								existingDetail.UnitPrice = detailRequest.UnitPrice;
								// Note: RejectQuantity and Note are set during verification, not here
								existingDetail.RejectQuantity = 0;
								existingDetail.Note = null;
								_unitOfWork.ImportDetails.Update(existingDetail);
							}
						}
						else
						{
							// Add new detail
							var newDetail = new ImportDetail
							{
								ImportId = importTicket.Id,
								ProductVariantId = detailRequest.VariantId,
								Quantity = detailRequest.Quantity,
								UnitPrice = detailRequest.UnitPrice,
								// RejectQuantity and Note are set during verification
								RejectQuantity = 0,
								Note = null
							};

							await _unitOfWork.ImportDetails.AddAsync(newDetail);
						}
					}

					_unitOfWork.ImportTickets.Update(importTicket);

					return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Import ticket updated successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error updating import ticket: {ex.Message}", ResponseErrorType.InternalError);
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

					// SAFETY RULE: Only Pending tickets can be deleted
					if (importTicket.Status == ImportStatus.InProgress)
					{
						return BaseResponse<bool>.Fail("Cannot delete import ticket that is in progress. Cancel it first.", ResponseErrorType.BadRequest);
					}

					if (importTicket.Status == ImportStatus.Completed)
					{
						return BaseResponse<bool>.Fail("Cannot delete completed import ticket. It is immutable.", ResponseErrorType.BadRequest);
					}

					if (importTicket.Status == ImportStatus.Canceled)
					{
						return BaseResponse<bool>.Fail("Cannot delete cancelled import ticket. It is read-only for history.", ResponseErrorType.BadRequest);
					}

					// At this point, status must be Pending - safe to delete
					// No need to adjust stock or remove batches - Pending tickets don't have them
					foreach (var detail in importTicket.ImportDetails)
					{
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

		/// <summary>
		/// Merges batches with the same batch code into a single batch with combined quantity.
		/// This prevents duplicate batch codes and consolidates inventory.
		/// </summary>
		/// <param name="batches">List of batches to merge</param>
		/// <returns>List of merged batches</returns>
		private List<CreateBatchRequest> MergeBatchesBySameCode(List<CreateBatchRequest> batches)
		{
			// Group batches by batch code
			var groupedBatches = batches
				.GroupBy(b => b.BatchCode, StringComparer.OrdinalIgnoreCase)
				.Select(group =>
				{
					// Take the first batch in the group as the base
					var firstBatch = group.First();

					// Sum quantities of all batches with the same code
					var totalQuantity = group.Sum(b => b.Quantity);

					// Use the earliest manufacture date
					var earliestManufactureDate = group.Min(b => b.ManufactureDate);

					// Use the earliest expiry date (most conservative approach)
					var earliestExpiryDate = group.Min(b => b.ExpiryDate);

					return new CreateBatchRequest
					{
						BatchCode = firstBatch.BatchCode,
						ManufactureDate = earliestManufactureDate,
						ExpiryDate = earliestExpiryDate,
						Quantity = totalQuantity
					};
				})
				.ToList();

			return groupedBatches;
		}
	}
}
