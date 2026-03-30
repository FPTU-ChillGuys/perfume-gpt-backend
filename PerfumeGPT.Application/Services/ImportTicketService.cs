using ClosedXML.Excel;
using FluentValidation;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;
using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;

namespace PerfumeGPT.Application.Services
{
	public class ImportTicketService : IImportTicketService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBatchService _batchService;
		private readonly ExcelTemplateGenerator _excelTemplateGenerator;

		public ImportTicketService(
			IUnitOfWork unitOfWork,
			IBatchService batchService,
			ExcelTemplateGenerator excelTemplateGenerator)
		{
			_unitOfWork = unitOfWork;
			_batchService = batchService;
			_excelTemplateGenerator = excelTemplateGenerator;
		}
		#endregion Dependencies

		public async Task<BaseResponse<string>> CreateImportTicketAsync(CreateImportTicketRequest request, Guid userId)
		{
			var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.SupplierId) ?? throw AppException.NotFound("Supplier not found.");

			// Check for duplicate variant IDs in request
			var duplicateVariants = request.ImportDetails
				.GroupBy(d => d.VariantId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateVariants.Count != 0)
			{
				var duplicateIds = string.Join(", ", duplicateVariants);
				throw AppException.BadRequest(
					 $"Duplicate variant IDs found: {duplicateIds}. Each variant can only appear once per import ticket.");
			}

			// Check variantIds exist
			var requestedVariantIds = request.ImportDetails.Select(d => d.VariantId).ToList();
			var existingVariantIds = await _unitOfWork.Variants.GetExistingIdsAsync(requestedVariantIds);
			var missingVariantIds = requestedVariantIds.Except(existingVariantIds).ToList();

			if (missingVariantIds.Count != 0)
			{
				var missingIds = string.Join(", ", missingVariantIds);
				throw AppException.NotFound($"Variants not found: {missingIds}");
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var totalCost = request.ImportDetails.Sum(d => d.ExpectedQuantity * d.UnitPrice);

				var importTicket = ImportTicket.Create(
						   userId,
						   request.SupplierId,
						   request.ExpectedArrivalDate,
						   totalCost);

				foreach (var detail in request.ImportDetails)
				{
					importTicket.AddDetail(ImportDetail.Create(
						detail.VariantId,
						detail.ExpectedQuantity,
						detail.UnitPrice));
				}

				await _unitOfWork.ImportTickets.AddAsync(importTicket);

				return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Import ticket created successfully.");
			});
		}

		public async Task<BaseResponse<CreateImportTicketRequest>> UploadImportTicketFromExcelAsync(UploadImportTicketFromExcelRequest request)
		{
			// Validate Excel file
			if (request.ExcelFile == null || request.ExcelFile.Length == 0)
			{
				throw AppException.BadRequest("Excel file is required.");
			}

			// Validate file extension
			var fileExtension = Path.GetExtension(request.ExcelFile.FileName).ToLowerInvariant();
			if (fileExtension != ".xlsx" && fileExtension != ".xls")
			{
				throw AppException.BadRequest("Only .xlsx and .xls files are supported.");
			}

			// Validate file size (max 10MB)
			if (request.ExcelFile.Length > 10 * 1024 * 1024)
			{
				throw AppException.BadRequest("File size cannot exceed 10MB.");
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
					throw AppException.BadRequest("Excel file is empty or has no data rows.");
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
							errors.Add($"Row {rowNumber}: Expected Quantity must be a positive number (found: '{quantityCell.GetString()}').");
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
							ExpectedQuantity = quantity,
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
				throw AppException.BadRequest($"Excel parsing errors: {errorMessage}");
			}

			// Check if we have any details
			if (importDetails.Count == 0)
			{
				throw AppException.BadRequest("No valid import details found in Excel file.");
			}

			// Create the import ticket request
			var createRequest = new CreateImportTicketRequest
			{
				SupplierId = request.SupplierId,
				ExpectedArrivalDate = request.ExpectedArrivalDate,
				ImportDetails = importDetails
			};

			return BaseResponse<CreateImportTicketRequest>.Ok(createRequest, "Excel parsed successfully. Please confirm and submit import ticket.");
		}

		public async Task<BaseResponse<ExcelTemplateResponse>> GenerateImportTemplateAsync()
		{
			var response = await _excelTemplateGenerator.GenerateImportTemplateAsync();
			return BaseResponse<ExcelTemplateResponse>.Ok(response, "Excel template generated successfully.");
		}

		public async Task<BaseResponse<string>> VerifyImportTicketAsync(Guid ticketId, VerifyImportTicketRequest request, Guid verifiedByUserId)
		{
			var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAsync(ticketId) ?? throw AppException.NotFound("Import ticket not found.");

			if (importTicket.Status != ImportStatus.InProgress)
				throw AppException.BadRequest("Only in progress import tickets can be verified.");

			if (request.ImportDetails.Count != importTicket.ImportDetails.Count)
				throw AppException.BadRequest("Mismatch in number of import details for verification.");

			// Check for duplicate import detail IDs in request
			var duplicateDetailIds = request.ImportDetails
				.GroupBy(d => d.ImportDetailId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateDetailIds.Count != 0)
			{
				var duplicateIds = string.Join(", ", duplicateDetailIds);
				throw AppException.BadRequest($"Duplicate import detail IDs found in request: {duplicateIds}. Each import detail can only appear once.");
			}

			// Ensure all import details from ticket are included in request
			var ticketDetailIds = importTicket.ImportDetails.Select(d => d.Id).ToHashSet();
			var requestDetailIds = request.ImportDetails.Select(d => d.ImportDetailId).ToHashSet();

			var missingDetailIds = ticketDetailIds.Except(requestDetailIds).ToList();
			if (missingDetailIds.Count != 0)
			{
				var missingIds = string.Join(", ", missingDetailIds);
				throw AppException.BadRequest($"Missing import details in request: {missingIds}. All import details must be verified.");
			}

			var extraDetailIds = requestDetailIds.Except(ticketDetailIds).ToList();
			if (extraDetailIds.Count != 0)
			{
				var extraIds = string.Join(", ", extraDetailIds);
				throw AppException.BadRequest($"Unknown import detail IDs in request: {extraIds}. These details do not belong to this import ticket.");
			}

			// Build dictionary for O(1) lookup
			var importDetailLookup = importTicket.ImportDetails.ToDictionary(d => d.Id);
			var mergedBatchesByDetailId = new Dictionary<Guid, List<CreateBatchRequest>>();

			// Validate all import details and collect errors
			var validationErrors = new List<string>();

			foreach (var verifyDetail in request.ImportDetails)
			{
				var importDetail = importDetailLookup[verifyDetail.ImportDetailId];

				// Validate reject quantity does not exceed total quantity
				if (verifyDetail.RejectedQuantity > importDetail.ExpectedQuantity)
				{
					validationErrors.Add($"Reject quantity ({verifyDetail.RejectedQuantity}) cannot exceed total quantity ({importDetail.ExpectedQuantity}) for import detail {verifyDetail.ImportDetailId}.");
					continue;
				}

				var acceptedQuantity = importDetail.ExpectedQuantity - verifyDetail.RejectedQuantity;

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
				else if (!IsTotalQuantityValid(verifyDetail.Batches, acceptedQuantity))
				{
					validationErrors.Add($"Total batch quantity does not match accepted quantity ({acceptedQuantity}) for import detail {verifyDetail.ImportDetailId}.");
				}
				else
				{
					var mergedBatches = MergeBatchesBySameCode(verifyDetail.Batches);
					mergedBatchesByDetailId[verifyDetail.ImportDetailId] = mergedBatches;

					foreach (var batchRequest in mergedBatches)
					{
						var existingBatch = await _unitOfWork.Batches.FirstOrDefaultAsync(
							b => b.VariantId == importDetail.ProductVariantId
								&& b.BatchCode == batchRequest.BatchCode,
							asNoTracking: true);

						if (existingBatch == null)
						{
							continue;
						}

						if (existingBatch.ManufactureDate.Date != batchRequest.ManufactureDate.Date
							|| existingBatch.ExpiryDate.Date != batchRequest.ExpiryDate.Date)
						{
							validationErrors.Add(
								$"Batch code '{batchRequest.BatchCode}' not match with existed batch same batch code in stock.");
						}
					}
				}
			}

			if (validationErrors.Count != 0)
			{
				throw AppException.BadRequest(string.Join("; ", validationErrors));
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				foreach (var verifyDetail in request.ImportDetails)
				{
					var importDetail = importDetailLookup[verifyDetail.ImportDetailId];
					var acceptedQuantity = importDetail.ExpectedQuantity - verifyDetail.RejectedQuantity;

					// Update import detail with reject quantity and note
					importTicket.VerifyDetail(verifyDetail.ImportDetailId, verifyDetail.RejectedQuantity, verifyDetail.Note);
					_unitOfWork.ImportDetails.Update(importDetail);

					if (acceptedQuantity > 0)
					{
						var mergedBatches = mergedBatchesByDetailId[verifyDetail.ImportDetailId];

						await _batchService.CreateBatchesAsync(
							importDetail.ProductVariantId,
							importDetail.Id,
							mergedBatches);
					}
				}

				// Update import ticket status to Completed and set verifier
				importTicket.Complete(verifiedByUserId, DateTime.UtcNow);
				_unitOfWork.ImportTickets.Update(importTicket);

				return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Import ticket verified successfully.");
			});
		}

		public async Task<BaseResponse<ImportTicketResponse>> GetImportTicketByIdAsync(Guid id)
		{
			var response = await _unitOfWork.ImportTickets.GetResponseByIdAsync(id)
				   ?? throw AppException.NotFound("Import ticket not found.");

			return BaseResponse<ImportTicketResponse>.Ok(response, "Import ticket retrieved successfully.");
		}

		public async Task<BaseResponse<PagedResult<ImportTicketListItem>>> GetImportTicketsAsync(GetPagedImportTicketsRequest request)
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

		public async Task<BaseResponse<string>> UpdateImportStatusAsync(Guid id, UpdateImportStatusRequest request)
		{
			var importTicket = await _unitOfWork.ImportTickets.GetByIdAsync(id) ?? throw AppException.NotFound("Import ticket not found.");
			importTicket.UpdateStatus(request.Status);
			_unitOfWork.ImportTickets.Update(importTicket);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not update import status.");

			return BaseResponse<string>.Ok(id.ToString(), "Import ticket status updated successfully.");
		}

		public async Task<BaseResponse<string>> UpdateImportTicketAsync(Guid id, UpdateImportRequest request)
		{
			_ = await _unitOfWork.Suppliers.GetByIdAsync(request.SupplierId) ?? throw AppException.NotFound("Supplier not found.");

			var requestedVariantIds = request.ImportDetails.Select(d => d.VariantId).Distinct().ToList();
			var existingVariantIds = await _unitOfWork.Variants.GetExistingIdsAsync(requestedVariantIds);
			var missingVariantIds = requestedVariantIds.Except(existingVariantIds).ToList();
			if (missingVariantIds.Count != 0)
			{
				throw AppException.NotFound($"Variants not found: {string.Join(", ", missingVariantIds)}");
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAsync(id) ?? throw AppException.NotFound("Import ticket not found.");

				var duplicateVariants = request.ImportDetails
					.GroupBy(d => d.VariantId)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateVariants.Count != 0)
				{
					var duplicateIds = string.Join(", ", duplicateVariants);
					throw AppException.BadRequest($"Duplicate variant IDs found: {duplicateIds}. Each variant can only appear once per import ticket.");
				}

				// Calculate new total cost
				var totalCost = request.ImportDetails.Sum(d => d.ExpectedQuantity * d.UnitPrice);
				importTicket.UpdateForPending(request.SupplierId, request.ExpectedArrivalDate, totalCost);

				var requestDetailIds = request.ImportDetails
					 .Where(d => d.Id.HasValue)
					 .Select(d => d.Id!.Value)
					 .ToHashSet();

				var ticketDetailIds = importTicket.ImportDetails.Select(d => d.Id).ToHashSet();
				var unknownDetailIds = requestDetailIds.Except(ticketDetailIds).ToList();
				if (unknownDetailIds.Count != 0)
				{
					throw AppException.BadRequest($"Unknown import detail IDs in request: {string.Join(", ", unknownDetailIds)}");
				}

				// Remove details that are not in the request
				var detailsToRemove = importTicket.ImportDetails.Where(d => !requestDetailIds.Contains(d.Id)).ToList();
				foreach (var detail in detailsToRemove)
				{
					importTicket.RemoveDetail(detail.Id);
				}

				// Update existing and add new details
				foreach (var detailRequest in request.ImportDetails)
				{
					if (detailRequest.Id.HasValue)
					{
						importTicket.UpdateDetail(
							 detailRequest.Id.Value,
							 detailRequest.VariantId,
							 detailRequest.ExpectedQuantity,
							 detailRequest.UnitPrice);
					}
					else
					{
						var newDetail = ImportDetail.Create(
							detailRequest.VariantId,
							detailRequest.ExpectedQuantity,
							detailRequest.UnitPrice);
						importTicket.AddDetail(newDetail);
					}
				}

				_unitOfWork.ImportTickets.Update(importTicket);

				return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Import ticket updated successfully.");
			});
		}

		public async Task<BaseResponse<bool>> DeleteImportTicketAsync(Guid id)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAndBatchesAsync(id) ?? throw AppException.NotFound("Import ticket not found.");
				importTicket.EnsureIsPendingStatus();

				foreach (var detail in importTicket.ImportDetails)
				{
					_unitOfWork.ImportDetails.Remove(detail);
				}

				_unitOfWork.ImportTickets.Remove(importTicket);

				return BaseResponse<bool>.Ok(true, "Import ticket deleted successfully.");
			});
		}

		#region Private Helpers
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

		private static bool IsTotalQuantityValid(List<CreateBatchRequest> batchRequests, int expectedTotalQuantity)
		{
			if (batchRequests == null || batchRequests.Count == 0)
				return false;

			if (expectedTotalQuantity <= 0)
				return false;

			var totalQuantity = batchRequests.Sum(b => b.Quantity);
			return totalQuantity == expectedTotalQuantity;
		}
		#endregion Private Helpers
	}
}

