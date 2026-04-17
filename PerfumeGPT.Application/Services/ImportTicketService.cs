using ClosedXML.Excel;
using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Requests.Imports.ImportDetails;
using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using static PerfumeGPT.Domain.Entities.ImportDetail;
using static PerfumeGPT.Domain.Entities.ImportTicket;

namespace PerfumeGPT.Application.Services
{
	public class ImportTicketService : IImportTicketService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBatchService _batchService;
		private readonly ExcelTemplateGenerator _excelTemplateGenerator;
		private readonly INotificationService _notificationService;

		public ImportTicketService(
			IUnitOfWork unitOfWork,
			IBatchService batchService,
			ExcelTemplateGenerator excelTemplateGenerator,
			INotificationService notificationService)
		{
			_unitOfWork = unitOfWork;
			_batchService = batchService;
			_excelTemplateGenerator = excelTemplateGenerator;
			_notificationService = notificationService;
		}
		#endregion Dependencies

		public async Task<BaseResponse<string>> CreateImportTicketAsync(CreateImportTicketRequest request, Guid userId)
		{
			var supplier = await _unitOfWork.Suppliers.GetByIdAsync(request.SupplierId) ?? throw AppException.NotFound("Không tìm thấy nhà cung cấp.");

			var variantIds = request.ImportDetails.Select(d => d.VariantId).ToList();
			ValidateVariantDuplicates(variantIds);
			await ValidateVariantsExistAsync(variantIds);

			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			  {
				  var itemInfos = request.ImportDetails
					  .Select(d => new ImportItemInfo(d.VariantId, d.ExpectedQuantity, d.UnitPrice))
					  .ToList();

				  var totalCost = request.ImportDetails.Sum(d => d.ExpectedQuantity * d.UnitPrice);

				  var header = new ImportHeader(
					  request.SupplierId,
					  request.ExpectedArrivalDate,
					  totalCost);

				  var importTicket = ImportTicket.Create(userId, header);

				  foreach (var info in itemInfos)
				  {
					  importTicket.AddDetail(ImportDetail.Create(info));
				  }

				  await _unitOfWork.ImportTickets.AddAsync(importTicket);
				  return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Tạo phiếu nhập thành công.");
			  });

			if (response.Success)
			{
				_ = Guid.TryParse(response.Payload, out Guid importTicketId);

				await _notificationService.SendToRoleAsync(
					UserRole.staff,
					"Lệnh nhập kho mới",
					$"Có lô hàng mới #{response.Payload} sắp giao đến, vui lòng chuẩn bị nhận.",
					NotificationType.Info,
					referenceId: importTicketId == Guid.Empty ? null : importTicketId,
					referenceType: NotifiReferecneType.ImportTicket);
			}

			return response;
		}

		public async Task<BaseResponse<CreateImportTicketRequest>> UploadImportTicketFromExcelAsync(UploadImportTicketFromExcelRequest request)
		{
			// Validate Excel file
			if (request.ExcelFile == null || request.ExcelFile.Length == 0)
			{
				throw AppException.BadRequest("Bắt buộc tải lên tệp Excel.");
			}

			// Validate file extension
			var fileExtension = Path.GetExtension(request.ExcelFile.FileName).ToLowerInvariant();
			if (fileExtension != ".xlsx" && fileExtension != ".xls")
			{
				throw AppException.BadRequest("Chỉ hỗ trợ tệp .xlsx và .xls.");
			}

			// Validate file size (max 10MB)
			if (request.ExcelFile.Length > 10 * 1024 * 1024)
			{
				throw AppException.BadRequest("Dung lượng tệp không được vượt quá 10MB.");
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
					throw AppException.BadRequest("Tệp Excel trống hoặc không có dòng dữ liệu.");
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
							errors.Add($"Dòng {rowNumber}: Số lượng dự kiến phải là số dương (giá trị hiện tại: '{quantityCell.GetString()}').");
							rowNumber++;
							continue;
						}

						// Column E: Unit Price (required)
						var unitPriceCell = row.Cell(5);
						if (!unitPriceCell.TryGetValue(out decimal unitPrice) || unitPrice <= 0)
						{
							errors.Add($"Dòng {rowNumber}: Đơn giá phải là số dương (giá trị hiện tại: '{unitPriceCell.GetString()}').");
							rowNumber++;
							continue;
						}

						// Find variant by SKU
						var variant = await _unitOfWork.Variants.GetBySkuAsync(skuCell.Trim());

						if (variant == null)
						{
							errors.Add($"Dòng {rowNumber}: Không tìm thấy biến thể có SKU '{skuCell}'.");
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
						errors.Add($"Dòng {rowNumber}: Lỗi khi đọc dữ liệu - {ex.Message}");
						rowNumber++;
					}
				}
			}

			// Check if there are any errors
			if (errors.Count != 0)
			{
				var errorMessage = string.Join("; ", errors);
				throw AppException.BadRequest($"Lỗi khi đọc tệp Excel: {errorMessage}");
			}

			// Check if we have any details
			if (importDetails.Count == 0)
			{
				throw AppException.BadRequest("Không tìm thấy chi tiết nhập hợp lệ trong tệp Excel.");
			}

			// Create the import ticket request
			var createRequest = new CreateImportTicketRequest
			{
				SupplierId = request.SupplierId,
				ExpectedArrivalDate = request.ExpectedArrivalDate,
				ImportDetails = importDetails
			};

			return BaseResponse<CreateImportTicketRequest>.Ok(createRequest, "Đọc tệp Excel thành công. Vui lòng xác nhận và gửi phiếu nhập.");
		}

		public async Task<BaseResponse<ExcelTemplateResponse>> GenerateImportTemplateAsync()
		{
			var response = await _excelTemplateGenerator.GenerateImportTemplateAsync();
			return BaseResponse<ExcelTemplateResponse>.Ok(response, "Tạo mẫu Excel thành công.");
		}

		public async Task<BaseResponse<string>> VerifyImportTicketAsync(Guid ticketId, VerifyImportTicketRequest request, Guid verifiedByUserId)
		{
			var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAsync(ticketId) ?? throw AppException.NotFound("Không tìm thấy phiếu nhập.");

			// 1.Structural Validation
			var detailMap = AlignAndValidateStructure(importTicket, request.ImportDetails);

			// 2. Business Logic Validation (Pre-calculation)
			var validatedItems = await ValidateBusinessRulesAsync(detailMap, request.ImportDetails);

			// 3. Execution (Atomic Transaction)
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				foreach (var item in validatedItems)
				{
					importTicket.VerifyDetail(item.Detail.Id, item.Verification);

					if (item.AcceptedQuantity > 0 && item.MergedBatches != null)
					{
						await _batchService.CreateBatchesAsync(
							item.Detail.ProductVariantId,
							item.Detail.Id,
							item.MergedBatches);
					}
				}

				importTicket.Complete(verifiedByUserId, DateTime.UtcNow);
				_unitOfWork.ImportTickets.Update(importTicket);

				return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Xác nhận phiếu nhập thành công.");
			});
		}

		private static Dictionary<Guid, ImportDetail> AlignAndValidateStructure(ImportTicket ticket, List<VerifyImportDetailRequest> requests)
		{
			if (ticket.Status != ImportStatus.InProgress)
				throw AppException.BadRequest("Chỉ có thể xác nhận phiếu nhập đang ở trạng thái InProgress.");

			var ticketDetailIds = ticket.ImportDetails.Select(d => d.Id).ToHashSet();
			var requestDetailIds = requests.Select(r => r.ImportDetailId).ToHashSet();

			if (!ticketDetailIds.SetEquals(requestDetailIds))
				throw AppException.BadRequest("Chi tiết request không khớp với chi tiết phiếu (thiếu, dư hoặc trùng ID).");

			return ticket.ImportDetails.ToDictionary(d => d.Id);
		}

		private async Task<List<DetailValidationResult>> ValidateBusinessRulesAsync(
			Dictionary<Guid, ImportDetail> detailMap,
			List<VerifyImportDetailRequest> requests)
		{
			var results = new List<DetailValidationResult>();
			var errors = new List<string>();

			foreach (var req in requests)
			{
				var detail = detailMap[req.ImportDetailId];
				var acceptedQty = detail.ExpectedQuantity - req.RejectedQuantity;
				var verification = new DetailVerification(req.RejectedQuantity, req.Note);

				if (req.RejectedQuantity > detail.ExpectedQuantity)
				{
					errors.Add($"Chi tiết {detail.Id}: Số lượng từ chối vượt quá số lượng dự kiến.");
					continue;
				}

				List<CreateBatchRequest>? mergedBatches = null;
				if (acceptedQty > 0)
				{
					if (req.Batches is not { Count: > 0 })
						errors.Add($"Chi tiết {detail.Id}: Bắt buộc có thông tin lô cho số lượng được nhận.");
					else if (!IsTotalQuantityValid(req.Batches, acceptedQty))
						errors.Add($"Chi tiết {detail.Id}: Tổng số lượng các lô không khớp với số lượng được nhận.");
					else
					{
						mergedBatches = MergeBatchesBySameCode(req.Batches);
						// Check Batch Integrity (Manufacture/Expiry Date)
						await ValidateBatchIntegrityAsync(detail.ProductVariantId, mergedBatches, errors);
					}
				}

				results.Add(new DetailValidationResult(detail, verification, mergedBatches, acceptedQty));
			}

			if (errors.Count != 0) throw AppException.BadRequest(string.Join(" | ", errors));
			return results;
		}

		private record DetailValidationResult(
			ImportDetail Detail,
			DetailVerification Verification,
			List<CreateBatchRequest>? MergedBatches,
			int AcceptedQuantity
		);

		private async Task ValidateBatchIntegrityAsync(
			Guid variantId,
			List<CreateBatchRequest> mergedBatches,
			List<string> errors)
		{
			foreach (var batchRequest in mergedBatches)
			{
				var existingBatch = await _unitOfWork.Batches.FirstOrDefaultAsync(
					b => b.VariantId == variantId && b.BatchCode == batchRequest.BatchCode,
					asNoTracking: true);

				if (existingBatch == null) continue;

				if (existingBatch.ManufactureDate.Date != batchRequest.ManufactureDate.Date
					|| existingBatch.ExpiryDate.Date != batchRequest.ExpiryDate.Date)
				{
					errors.Add($"Mã lô '{batchRequest.BatchCode}' không khớp với lô đã có trong kho (khác ngày sản xuất/hạn dùng).");
				}
			}
		}

		public async Task<BaseResponse<ImportTicketResponse>> GetImportTicketByIdAsync(Guid id)
		{
			var response = await _unitOfWork.ImportTickets.GetResponseByIdAsync(id)
				 ?? throw AppException.NotFound("Không tìm thấy phiếu nhập.");

			return BaseResponse<ImportTicketResponse>.Ok(response, "Lấy thông tin phiếu nhập thành công.");
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

			return BaseResponse<PagedResult<ImportTicketListItem>>.Ok(pagedResult, "Lấy danh sách phiếu nhập thành công.");
		}

		public async Task<BaseResponse<string>> UpdateImportStatusAsync(Guid id, UpdateImportStatusRequest request)
		{
			var importTicket = await _unitOfWork.ImportTickets.GetByIdAsync(id) ?? throw AppException.NotFound("Không tìm thấy phiếu nhập.");
			importTicket.UpdateStatus(request.Status);
			_unitOfWork.ImportTickets.Update(importTicket);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Không thể cập nhật trạng thái phiếu nhập.");

			return BaseResponse<string>.Ok(id.ToString(), "Cập nhật trạng thái phiếu nhập thành công.");
		}

		public async Task<BaseResponse<string>> UpdateImportTicketAsync(Guid id, UpdateImportRequest request)
		{
			_ = await _unitOfWork.Suppliers.GetByIdAsync(request.SupplierId) ?? throw AppException.NotFound("Không tìm thấy nhà cung cấp.");

			var variantIds = request.ImportDetails.Select(d => d.VariantId).ToList();
			ValidateVariantDuplicates(variantIds);
			await ValidateVariantsExistAsync(variantIds);

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAsync(id) ?? throw AppException.NotFound("Không tìm thấy phiếu nhập.");

				var duplicateVariants = request.ImportDetails
					.GroupBy(d => d.VariantId)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateVariants.Count != 0)
				{
					var duplicateIds = string.Join(", ", duplicateVariants);
					throw AppException.BadRequest($"Phát hiện ID biến thể trùng: {duplicateIds}. Mỗi biến thể chỉ được xuất hiện một lần trong một phiếu nhập.");
				}

				// Calculate new total cost
				var totalCost = request.ImportDetails.Sum(d => d.ExpectedQuantity * d.UnitPrice);
				var header = new ImportHeader(request.SupplierId, request.ExpectedArrivalDate, totalCost);

				importTicket.UpdateForPending(header);
				SyncImportDetails(importTicket, request.ImportDetails);

				_unitOfWork.ImportTickets.Update(importTicket);
				return BaseResponse<string>.Ok(importTicket.Id.ToString(), "Cập nhật phiếu nhập thành công.");
			});
		}

		private static void SyncImportDetails(ImportTicket ticket, List<UpdateImportDetailRequest> requestedDetails)
		{
			var requestDetailIds = requestedDetails.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();
			var currentDetailIds = ticket.ImportDetails.Select(d => d.Id).ToHashSet();

			var unknownIds = requestDetailIds.Except(currentDetailIds).ToList();
			if (unknownIds.Count != 0)
				throw AppException.BadRequest($"ID chi tiết không tồn tại: {string.Join(", ", unknownIds)}");

			var toRemove = ticket.ImportDetails.Where(d => !requestDetailIds.Contains(d.Id)).ToList();
			foreach (var d in toRemove) ticket.RemoveDetail(d.Id);

			foreach (var req in requestedDetails)
			{
				var itemInfo = new ImportItemInfo(req.VariantId, req.ExpectedQuantity, req.UnitPrice);

				if (req.Id.HasValue)
					ticket.UpdateDetail(req.Id.Value, itemInfo);
				else
					ticket.AddDetail(ImportDetail.Create(itemInfo));
			}
		}

		public async Task<BaseResponse<bool>> DeleteImportTicketAsync(Guid id)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var importTicket = await _unitOfWork.ImportTickets.GetByIdWithDetailsAndBatchesAsync(id) ?? throw AppException.NotFound("Không tìm thấy phiếu nhập.");
				importTicket.EnsureIsPendingStatus();

				foreach (var detail in importTicket.ImportDetails)
				{
					_unitOfWork.ImportDetails.Remove(detail);
				}

				_unitOfWork.ImportTickets.Remove(importTicket);

				return BaseResponse<bool>.Ok(true, "Xóa phiếu nhập thành công.");
			});
		}

		#region Private Helpers
		private static void ValidateVariantDuplicates(IEnumerable<Guid> variantIds)
		{
			var duplicates = variantIds
				.GroupBy(id => id)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicates.Count != 0)
			{
				throw AppException.BadRequest(
					 $"Phát hiện ID biến thể trùng: {string.Join(", ", duplicates)}. Mỗi biến thể chỉ được xuất hiện một lần trong một phiếu nhập.");
			}
		}

		private async Task ValidateVariantsExistAsync(IEnumerable<Guid> variantIds)
		{
			var requestedIds = variantIds.Distinct().ToList();
			var existingIds = await _unitOfWork.Variants.GetExistingIdsAsync(requestedIds);
			var missingIds = requestedIds.Except(existingIds).ToList();

			if (missingIds.Count != 0)
			{
				throw AppException.NotFound($"Không tìm thấy các biến thể: {string.Join(", ", missingIds)}");
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

					return firstBatch with
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

