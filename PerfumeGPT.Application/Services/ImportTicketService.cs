using ClosedXML.Excel;
using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Batches;
using PerfumeGPT.Application.DTOs.Requests.ImportDetails;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ImportTicketService : IImportTicketService
	{
		#region Dependencies

		private readonly IUnitOfWork _unitOfWork;
		private readonly IStockService _stockService;
		private readonly IBatchService _batchService;
		private readonly IValidator<CreateImportTicketRequest> _createImportTicketValidator;
		private readonly IValidator<VerifyImportTicketRequest> _verifyImportTicketValidator;
		private readonly IValidator<UpdateImportStatusRequest> _updateImportStatusValidator;
		private readonly IValidator<UpdateImportRequest> _updateImportValidator;
		private readonly IValidator<CreateImportTicketFromExcelRequest> _createImportTicketFromExcelValidator;
		private readonly IMapper _mapper;
		private readonly ExcelTemplateGenerator _excelTemplateGenerator;

		public ImportTicketService(
			IUnitOfWork unitOfWork,
			IStockService stockService,
			IBatchService batchService,
			IMapper mapper,
			IValidator<CreateImportTicketRequest> createImportTicketValidator,
			IValidator<VerifyImportTicketRequest> verifyImportTicketValidator,
			IValidator<UpdateImportStatusRequest> updateImportTicketValidator,
			IValidator<UpdateImportRequest> updateFullImportTicketValidator,
			IValidator<CreateImportTicketFromExcelRequest> createImportTicketFromExcelValidator,
			ExcelTemplateGenerator excelTemplateGenerator)
		{
			_unitOfWork = unitOfWork;
			_stockService = stockService;
			_batchService = batchService;
			_mapper = mapper;
			_createImportTicketValidator = createImportTicketValidator;
			_verifyImportTicketValidator = verifyImportTicketValidator;
			_updateImportStatusValidator = updateImportTicketValidator;
			_updateImportValidator = updateFullImportTicketValidator;
			_createImportTicketFromExcelValidator = createImportTicketFromExcelValidator;
			_excelTemplateGenerator = excelTemplateGenerator;
		}

		#endregion Dependencies

		public async Task<BaseResponse<string>> CreateImportTicketAsync(CreateImportTicketRequest request, Guid userId)
		{
			try
			{
				var validationResult = await _createImportTicketValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.SupplierId);
				if (supplier == null)
				{
					return BaseResponse<string>.Fail("Supplier not found.", ResponseErrorType.NotFound);
				}

				// Check for duplicate variant IDs in request
				var duplicateVariants = request.ImportDetails
					.GroupBy(d => d.VariantId)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateVariants.Count != 0)
				{
					var duplicateIds = string.Join(", ", duplicateVariants);
					return BaseResponse<string>.Fail(
						$"Duplicate variant IDs found: {duplicateIds}. Each variant can only appear once per import ticket.",
						ResponseErrorType.BadRequest
					);
				}

				// Check variantIds exist
				var requestedVariantIds = request.ImportDetails.Select(d => d.VariantId).ToList();
				var existingVariantIds = await _unitOfWork.Variants.GetExistingIdsAsync(requestedVariantIds);
				var missingVariantIds = requestedVariantIds.Except(existingVariantIds).ToList();

				if (missingVariantIds.Count != 0)
				{
					var missingIds = string.Join(", ", missingVariantIds);
					return BaseResponse<string>.Fail($"Variants not found: {missingIds}", ResponseErrorType.NotFound);
				}

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var totalCost = request.ImportDetails.Sum(d => d.Quantity * d.UnitPrice);

					var importTicket = new ImportTicket
					{
						CreatedById = userId,
						SupplierId = request.SupplierId,
						ExpectedArrivalDate = request.ExpectedArrivalDate,
						TotalCost = totalCost,
						Status = ImportStatus.Pending
					};

					await _unitOfWork.ImportTickets.AddAsync(importTicket);

					foreach (var detailRequest in request.ImportDetails)
					{
						detailRequest.TicketId = importTicket.Id;
						var importDetail = _mapper.Map<ImportDetail>(detailRequest);

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

		public async Task<BaseResponse<string>> CreateImportTicketFromExcelAsync(CreateImportTicketFromExcelRequest request, Guid userId)
		{
			try
			{
				var validationResult = await _createImportTicketFromExcelValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var validationErrors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(validationErrors, ResponseErrorType.BadRequest);
				}

				// Validate Excel file
				if (request.ExcelFile == null || request.ExcelFile.Length == 0)
				{
					return BaseResponse<string>.Fail("Excel file is required.", ResponseErrorType.BadRequest);
				}

				// Validate file extension
				var fileExtension = Path.GetExtension(request.ExcelFile.FileName).ToLowerInvariant();
				if (fileExtension != ".xlsx" && fileExtension != ".xls")
				{
					return BaseResponse<string>.Fail("Only .xlsx and .xls files are supported.", ResponseErrorType.BadRequest);
				}

				// Validate file size (max 10MB)
				if (request.ExcelFile.Length > 10 * 1024 * 1024)
				{
					return BaseResponse<string>.Fail("File size cannot exceed 10MB.", ResponseErrorType.BadRequest);
				}

				// Parse Excel file
				var importDetails = new List<CreateImportDetailRequest>();
				var errors = new List<string>();

				using (var stream = new MemoryStream())
				{
					await request.ExcelFile.CopyToAsync(stream);
					stream.Position = 0;

					using var workbook = new XLWorkbook(stream);
					var worksheet = workbook.Worksheet(1);
					var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // Skip header row

					if (rows == null || !rows.Any())
					{
						return BaseResponse<string>.Fail("Excel file is empty or has no data rows.", ResponseErrorType.BadRequest);
					}

					int rowNumber = 2; // Start from 2 (1 is header)
					foreach (var row in rows)
					{
						try
						{
							// Column A: SKU (required) - Columns B & C are auto-filled (Barcode, Product Name)
							var skuCell = row.Cell(1).GetValue<string>();

							// Skip empty rows (template has 1000 rows with formulas, but user may only fill a few)
							if (string.IsNullOrWhiteSpace(skuCell))
							{
								rowNumber++;
								continue;
							}

							// Column D: Quantity (required) 
							var quantityCell = row.Cell(4);
							if (!quantityCell.TryGetValue(out int quantity) || quantity <= 0)
							{
								errors.Add($"Row {rowNumber}: Quantity must be a positive number (found: '{quantityCell.GetString()}').");
								rowNumber++;
								continue;
							}

							// Column E: Unit Price (required)
							var unitPriceCell = row.Cell(5);
							if (!unitPriceCell.TryGetValue(out decimal unitPrice) || unitPrice <= 0)
							{
								errors.Add($"Row {rowNumber}: Unit Price must be a positive number (found: '{unitPriceCell.GetString()}').");
								rowNumber++;
								continue;
							}

							// Find variant by SKU
							var variant = await _unitOfWork.Variants.GetBySkuAsync(skuCell.Trim());

							if (variant == null)
							{
								errors.Add($"Row {rowNumber}: Variant with SKU '{skuCell}' not found.");
								rowNumber++;
								continue;
							}

							// Add to import details
							importDetails.Add(new CreateImportDetailRequest
							{
								VariantId = variant.Id,
								Quantity = quantity,
								UnitPrice = unitPrice
							});

							rowNumber++;
						}
						catch (Exception ex)
						{
							errors.Add($"Row {rowNumber}: Error parsing row - {ex.Message}");
							rowNumber++;
						}
					}
				}

				// Check if there are any errors
				if (errors.Count != 0)
				{
					var errorMessage = string.Join("; ", errors);
					return BaseResponse<string>.Fail($"Excel parsing errors: {errorMessage}", ResponseErrorType.BadRequest);
				}

				// Check if we have any details
				if (importDetails.Count == 0)
				{
					return BaseResponse<string>.Fail("No valid import details found in Excel file.", ResponseErrorType.BadRequest);
				}

				// Create the import ticket request
				var createRequest = new CreateImportTicketRequest
				{
					SupplierId = request.SupplierId,
					ExpectedArrivalDate = request.ExpectedArrivalDate,
					ImportDetails = importDetails
				};

				return await CreateImportTicketAsync(createRequest, userId);
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error creating import ticket from Excel: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<ExcelTemplateResponse>> GenerateImportTemplateAsync()
		{
			try
			{
				var response = await _excelTemplateGenerator.GenerateImportTemplateAsync();
				return BaseResponse<ExcelTemplateResponse>.Ok(response, "Excel template generated successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<ExcelTemplateResponse>.Fail($"Error generating Excel template: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> VerifyImportTicketAsync(Guid ticketId, VerifyImportTicketRequest request, Guid verifiedByUserId)
		{
			try
			{
				var validationResult = await _verifyImportTicketValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAsync(ticketId);

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

				// Check for duplicate import detail IDs in request
				var duplicateDetailIds = request.ImportDetails
					.GroupBy(d => d.ImportDetailId)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateDetailIds.Count != 0)
				{
					var duplicateIds = string.Join(", ", duplicateDetailIds);
					return BaseResponse<string>.Fail($"Duplicate import detail IDs found in request: {duplicateIds}. Each import detail can only appear once.", ResponseErrorType.BadRequest);
				}

				// Ensure all import details from ticket are included in request
				var ticketDetailIds = importTicket.ImportDetails.Select(d => d.Id).ToHashSet();
				var requestDetailIds = request.ImportDetails.Select(d => d.ImportDetailId).ToHashSet();

				var missingDetailIds = ticketDetailIds.Except(requestDetailIds).ToList();
				if (missingDetailIds.Count != 0)
				{
					var missingIds = string.Join(", ", missingDetailIds);
					return BaseResponse<string>.Fail($"Missing import details in request: {missingIds}. All import details must be verified.", ResponseErrorType.BadRequest);
				}

				var extraDetailIds = requestDetailIds.Except(ticketDetailIds).ToList();
				if (extraDetailIds.Count != 0)
				{
					var extraIds = string.Join(", ", extraDetailIds);
					return BaseResponse<string>.Fail($"Unknown import detail IDs in request: {extraIds}. These details do not belong to this import ticket.", ResponseErrorType.BadRequest);
				}

				// Build dictionary for O(1) lookup
				var importDetailLookup = importTicket.ImportDetails.ToDictionary(d => d.Id);

				// Validate all import details and collect errors
				var validationErrors = new List<string>();

				foreach (var verifyDetail in request.ImportDetails)
				{
					var importDetail = importDetailLookup[verifyDetail.ImportDetailId];

					// Validate reject quantity does not exceed total quantity
					if (verifyDetail.RejectQuantity > importDetail.Quantity)
					{
						validationErrors.Add($"Reject quantity ({verifyDetail.RejectQuantity}) cannot exceed total quantity ({importDetail.Quantity}) for import detail {verifyDetail.ImportDetailId}.");
						continue;
					}

					var acceptedQuantity = importDetail.Quantity - verifyDetail.RejectQuantity;

					// Full rejection (acceptedQuantity = 0) is allowed - no batches required
					if (acceptedQuantity == 0)
					{
						continue;
					}

					// Partial or full acceptance requires valid batches
					if (verifyDetail.Batches is not { Count: > 0 })
					{
						validationErrors.Add($"Batches for import detail {verifyDetail.ImportDetailId} cannot be empty when there is accepted quantity.");
					}
					else if (!_batchService.IsTotalQuantityValid(verifyDetail.Batches, acceptedQuantity))
					{
						validationErrors.Add($"Total batch quantity does not match accepted quantity ({acceptedQuantity}) for import detail {verifyDetail.ImportDetailId}.");
					}
				}

				if (validationErrors.Count != 0)
				{
					return BaseResponse<string>.Fail(string.Join("; ", validationErrors), ResponseErrorType.BadRequest);
				}

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					foreach (var verifyDetail in request.ImportDetails)
					{
						var importDetail = importDetailLookup[verifyDetail.ImportDetailId];
						var acceptedQuantity = importDetail.Quantity - verifyDetail.RejectQuantity;

						// Update import detail with reject quantity and note
						importDetail.RejectQuantity = verifyDetail.RejectQuantity;
						importDetail.Note = verifyDetail.Note; // Note batchcode issues if any
						_unitOfWork.ImportDetails.Update(importDetail);

						if (acceptedQuantity > 0)
						{
							var mergedBatches = MergeBatchesBySameCode(verifyDetail.Batches);

							await _batchService.CreateBatchesAsync(
								importDetail.ProductVariantId,
								importDetail.Id,
								mergedBatches);

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
					importTicket.ActualImportDate = DateTime.UtcNow;
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
				var response = await _unitOfWork.ImportTickets.GetResponseByIdAsync(id);

				if (response == null)
				{
					return BaseResponse<ImportTicketResponse>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
				}

				return BaseResponse<ImportTicketResponse>.Ok(response, "Import ticket retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<ImportTicketResponse>.Fail($"Error retrieving import ticket: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<PagedResult<ImportTicketListItem>>> GetImportTicketsAsync(GetPagedImportTicketsRequest request)
		{
			try
			{
				var (items, totalCount) = await _unitOfWork.ImportTickets.GetPagedAsync(request);

				var pagedResult = new PagedResult<ImportTicketListItem>(
				items,
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


		public async Task<BaseResponse<string>> UpdateImportStatusAsync(Guid id, UpdateImportStatusRequest request)
		{
			try
			{
				var validationResult = await _updateImportStatusValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

				var importTicket = await _unitOfWork.ImportTickets.GetByIdAsync(id);
				if (importTicket == null)
				{
					return BaseResponse<string>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
				}

				if (importTicket.Status == ImportStatus.Completed)
				{
					return BaseResponse<string>.Fail("Completed import tickets are immutable. Create an adjustment ticket if needed.", ResponseErrorType.BadRequest);
				}

				if (importTicket.Status == ImportStatus.Canceled)
				{
					return BaseResponse<string>.Fail("Cancelled import tickets are read-only.", ResponseErrorType.BadRequest);
				}

				if (importTicket.Status == ImportStatus.InProgress && request.Status != ImportStatus.Canceled)
				{
					return BaseResponse<string>.Fail("Import ticket is locked while in progress. Complete verification or cancel it first.", ResponseErrorType.BadRequest);
				}

				if (importTicket.Status == ImportStatus.Pending)
				{
					if (request.Status != ImportStatus.InProgress && request.Status != ImportStatus.Canceled)
					{
						return BaseResponse<string>.Fail("Pending tickets can only transition to InProgress or Canceled status.", ResponseErrorType.BadRequest);
					}
				}

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

		public async Task<BaseResponse<string>> UpdateImportTicketAsync(Guid id, UpdateImportRequest request)
		{
			try
			{
				var validationResult = await _updateImportValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
					return BaseResponse<string>.Fail(errors, ResponseErrorType.BadRequest);
				}

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

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAsync(id);

					if (importTicket == null)
					{
						return BaseResponse<string>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
					}

					if (importTicket.Status != PerfumeGPT.Domain.Enums.ImportStatus.Pending)
					{
						return BaseResponse<string>.Fail("Only pending import tickets can be updated.", ResponseErrorType.BadRequest);
					}

					importTicket.SupplierId = request.SupplierId;
					importTicket.ExpectedArrivalDate = request.ExpectedArrivalDate;

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

					var requestDetailIds = request.ImportDetails.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();

					// Remove details that are not in the request
					var detailsToRemove = importTicket.ImportDetails.Where(d => !requestDetailIds.Contains(d.Id)).ToList();
					foreach (var detail in detailsToRemove)
					{
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
								existingDetail.ProductVariantId = detailRequest.VariantId;
								existingDetail.Quantity = detailRequest.Quantity;
								existingDetail.UnitPrice = detailRequest.UnitPrice;
								_unitOfWork.ImportDetails.Update(existingDetail);
							}
						}
						else
						{
							var detailRequestTicketId = importTicket.Id;
							var newDetail = _mapper.Map<ImportDetail>(detailRequest);
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
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAndBatchesAsync(id);

					if (importTicket == null)
					{
						return BaseResponse<bool>.Fail("Import ticket not found.", ResponseErrorType.NotFound);
					}

					if (importTicket.Status != ImportStatus.Pending)
					{
						return BaseResponse<bool>.Fail("Only pending import tickets can be deleted.", ResponseErrorType.BadRequest);
					}

					foreach (var detail in importTicket.ImportDetails)
					{
						_unitOfWork.ImportDetails.Remove(detail);
					}

					_unitOfWork.ImportTickets.Remove(importTicket);

					return BaseResponse<bool>.Ok(true, "Import ticket deleted successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail($"Error deleting import ticket: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		private static List<CreateBatchRequest> MergeBatchesBySameCode(List<CreateBatchRequest> batches)
		{
			var groupedBatches = batches
				.Where(b => b.Quantity > 0)
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

