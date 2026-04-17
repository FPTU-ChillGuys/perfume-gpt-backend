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

			var importDetails = new List<CreateImportDetailRequest>();
			var errors = new List<string>();
			int supplierIdFromFile = 0;

			using (var stream = new MemoryStream())
			{
				await request.ExcelFile.CopyToAsync(stream);
				stream.Position = 0;

				using var workbook = new XLWorkbook(stream);
				var worksheet = workbook.Worksheet(1);

				var supplierIdCell = worksheet.Cell("B1"); // Chuyển sang dùng tọa độ chữ cái
				if (!supplierIdCell.TryGetValue(out supplierIdFromFile) || supplierIdFromFile <= 0)
				{
					throw AppException.BadRequest("Không tìm thấy Mã hệ thống Nhà cung cấp hợp lệ trong file Excel. Vui lòng sử dụng đúng template.");
				}

				var rows = worksheet.RowsUsed().Where(r => r.RowNumber() >= 5);

				if (!rows.Any())
				{
					throw AppException.BadRequest("Tệp Excel không có dòng dữ liệu hàng hóa.");
				}

				foreach (var row in rows)
				{
					// Lấy chính xác tọa độ dòng hiện tại để báo lỗi chuẩn xác (dù có dòng trống ở giữa)
					int currentRowNumber = row.RowNumber();

					try
					{
						// Cột 1 (A): Mã SKU
						var skuCell = row.Cell(1).GetValue<string>()?.Trim();

						// Bỏ qua dòng trống hoặc dòng có chữ "TỔNG CỘNG" ở cuối file
						if (string.IsNullOrWhiteSpace(skuCell) || skuCell.Contains("TỔNG CỘNG") || skuCell.Contains("TOTAL"))
						{
							continue;
						}

						// Cột 4 (D): Số lượng dự kiến
						var quantityCell = row.Cell(4);
						if (!quantityCell.TryGetValue(out int quantity) || quantity <= 0)
						{
							errors.Add($"Dòng {currentRowNumber}: Số lượng dự kiến phải là số nguyên dương.");
							continue;
						}

						// Cột 5 (E) và 6 (F): Xử lý Logic Giá
						row.Cell(5).TryGetValue(out decimal systemPrice);
						row.Cell(6).TryGetValue(out decimal actualPrice);

						// Ưu tiên Giá thực tế (nếu nhân viên có nhập tay), ngược lại lấy Giá hệ thống
						decimal finalUnitPrice = actualPrice > 0 ? actualPrice : systemPrice;

						if (finalUnitPrice <= 0)
						{
							errors.Add($"Dòng {currentRowNumber}: Không xác định được đơn giá hợp lệ. Vui lòng kiểm tra Giá hệ thống hoặc điền Giá thực tế.");
							continue;
						}

						// Tìm biến thể bằng SKU
						var variant = await _unitOfWork.Variants.GetBySkuAsync(skuCell);

						if (variant == null)
						{
							errors.Add($"Dòng {currentRowNumber}: Không tìm thấy mã SKU '{skuCell}' trong hệ thống.");
							continue;
						}

						// Gom dữ liệu hợp lệ
						importDetails.Add(new CreateImportDetailRequest
						{
							VariantId = variant.Id,
							ExpectedQuantity = quantity,
							UnitPrice = finalUnitPrice
						});
					}
					catch (Exception ex)
					{
						errors.Add($"Dòng {currentRowNumber}: Lỗi khi đọc dữ liệu - {ex.Message}");
					}
				}
			}

			// Trả về TOÀN BỘ lỗi (nếu có)
			if (errors.Count != 0)
			{
				var errorMessage = string.Join("\n", errors);
				throw AppException.BadRequest($"Vui lòng sửa các lỗi sau trong tệp Excel:\n{errorMessage}");
			}

			// Nếu chạy hết vòng lặp mà importDetails vẫn bằng 0 (có thể do tất cả các dòng đều lỗi và bị continue)
			// Thực tế đoạn này ít khi chạy vào vì phần bắt lỗi ở trên đã văng Exception rồi.
			if (importDetails.Count == 0)
			{
				throw AppException.BadRequest("Không tìm thấy dữ liệu hợp lệ. Đảm bảo bạn đã điền Mã SKU và Số lượng ở cột A và D.");
			}

			var createRequest = new CreateImportTicketRequest
			{
				SupplierId = supplierIdFromFile,
				ExpectedArrivalDate = request.ExpectedArrivalDate,
				ImportDetails = importDetails
			};

			return BaseResponse<CreateImportTicketRequest>.Ok(createRequest, "Đọc tệp Excel thành công. Vui lòng kiểm tra lại số liệu trước khi tạo phiếu nhập.");
		}

		public async Task<BaseResponse<ExcelTemplateResponse>> GenerateImportTemplateAsync(int supplierId)
		{
			var response = await _excelTemplateGenerator.GenerateImportTemplateAsync(supplierId);
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

