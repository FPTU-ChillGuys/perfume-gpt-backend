using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.Helpers
{
	public class ExcelTemplateGenerator
	{
		private readonly IUnitOfWork _unitOfWork;

		public ExcelTemplateGenerator(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<ExcelTemplateResponse> GenerateImportTemplateAsync(int supplierId)
		{
			var supplier = await _unitOfWork.Suppliers.GetByIdAsync(supplierId)
				?? throw AppException.NotFound("Không tìm thấy nhà cung cấp với ID đã cho.");

			var variantSuppliers = await _unitOfWork.VariantSuppliers.GetAllAsync(
				filter: vs => vs.SupplierId == supplierId && vs.ProductVariant.Status == VariantStatus.Active,
				include: query => query
					.Include(vs => vs.ProductVariant).ThenInclude(v => v.Product)
					.Include(vs => vs.ProductVariant).ThenInclude(v => v.Concentration),
				orderBy: q => q.OrderBy(vs => vs.ProductVariant.Sku));

			using var workbook = new XLWorkbook();

			CreateMainTemplateSheet(workbook, variantSuppliers, supplier);
			CreateVariantReferenceSheet(workbook, variantSuppliers);
			CreateInstructionsSheet(workbook);

			using var stream = new MemoryStream();
			workbook.SaveAs(stream);
			var fileContent = stream.ToArray();

			return new ExcelTemplateResponse
			{
				FileContent = fileContent,
				FileName = $"PhieuNhap_{supplier.Name.Replace(" ", "")}_{DateTime.UtcNow:yyyyMMdd}.xlsx"
			};
		}

		private static void CreateMainTemplateSheet(XLWorkbook workbook, IEnumerable<VariantSupplier> variantSuppliers, Supplier supplier)
		{
			var worksheet = workbook.Worksheets.Add("Biểu mẫu Nhập hàng");

			// Dòng 1: Nhúng ID Nhà cung cấp (Quan trọng để upload)
			worksheet.Cell(1, 1).Value = "MÃ HỆ THỐNG NCC:";
			worksheet.Cell(1, 1).Style.Font.Bold = true;
			worksheet.Cell(1, 2).Value = supplier.Id; // Đây là giá trị sẽ đọc lại khi upload
			worksheet.Cell(1, 2).Style.Font.FontColor = XLColor.Gray;

			// Dòng 2: Tên Nhà cung cấp để người dùng nhận diện
			worksheet.Cell(2, 1).Value = "TÊN NHÀ CUNG CẤP:";
			worksheet.Cell(2, 1).Style.Font.Bold = true;
			worksheet.Cell(2, 2).Value = supplier.Name;
			worksheet.Cell(2, 2).Style.Font.Bold = true;
			worksheet.Cell(2, 2).Style.Font.FontColor = XLColor.DarkBlue;

			// Set column headers (Bắt đầu từ dòng 4 - Dòng 3 để trống cho thoáng)
			worksheet.Cell(4, 1).Value = "Mã SKU";
			worksheet.Cell(4, 2).Value = "Mã vạch (Tự động)";
			worksheet.Cell(4, 3).Value = "Tên sản phẩm (Tự động)";
			worksheet.Cell(4, 4).Value = "SL Dự kiến";
			worksheet.Cell(4, 5).Value = "Giá Hệ thống (VNĐ)";
			worksheet.Cell(4, 6).Value = "Giá Thực tế (VNĐ)";
			worksheet.Cell(4, 7).Value = "Thành tiền (Tự động)";

			var headerRange = worksheet.Range(4, 1, 4, 7);
			headerRange.Style.Font.Bold = true;
			headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
			headerRange.Style.Font.FontColor = XLColor.White;
			headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			worksheet.Row(4).Height = 25;

			if (variantSuppliers.Any())
			{
				SetupBarcodeDropdown(worksheet, variantSuppliers.Count());
			}

			AddAutoFillFormulas(worksheet);
			ApplyFormatting(worksheet);
			AddConditionalFormattingAndTotals(worksheet);
		}

		private static void SetupBarcodeDropdown(IXLWorksheet worksheet, int variantCount)
		{
			// SỬA LỖI: Dữ liệu bắt đầu từ dòng 4
			var skuRange = worksheet.Range("A4:A1000");
			var variantSheetName = "DanhSach_SanPham";
			var validationFormula = $"'{variantSheetName}'!$A$2:$A${variantCount + 1}";

			var skuValidation = skuRange.CreateDataValidation();
			skuValidation.List(validationFormula, true);
			skuValidation.InputTitle = "Chọn Mã SKU";
			skuValidation.InputMessage = "Click vào mũi tên để chọn mã SKU từ danh sách. Không nhập tay!";
			skuValidation.ErrorTitle = "SKU Không hợp lệ";
			skuValidation.ErrorMessage = "Vui lòng chỉ chọn mã SKU có sẵn trong danh sách thả xuống.";
		}

		private static void AddAutoFillFormulas(IXLWorksheet worksheet)
		{
			for (int row = 5; row <= 1000; row++) // Bắt đầu từ 5
			{
				worksheet.Cell(row, 2).FormulaA1 = $"=IFERROR(VLOOKUP(A{row},DanhSach_SanPham!A:E,2,FALSE),\"\")";
				worksheet.Cell(row, 3).FormulaA1 = $"=IFERROR(VLOOKUP(A{row},DanhSach_SanPham!A:E,3,FALSE),\"\")";
				worksheet.Cell(row, 5).FormulaA1 = $"=IFERROR(VLOOKUP(A{row},DanhSach_SanPham!A:E,5,FALSE),\"\")";
				worksheet.Cell(row, 7).FormulaA1 = $"=IF(ISNUMBER(D{row}), D{row} * IF(ISNUMBER(F{row}), F{row}, IF(ISNUMBER(E{row}), E{row}, 0)), \"\")";
			}
		}

		private static void ApplyFormatting(IXLWorksheet worksheet)
		{
			// SỬA LỖI: Cập nhật lại cột làm mờ (Read-only) là B, C, E, G từ dòng 4
			var readOnlyCols1 = worksheet.Range("B4:C1000");
			var readOnlyCols2 = worksheet.Range("E4:E1000");
			var readOnlyCols3 = worksheet.Range("G4:G1000");

			var grayColor = XLColor.FromArgb(242, 242, 242);
			var fontGray = XLColor.FromArgb(89, 89, 89);

			readOnlyCols1.Style.Fill.BackgroundColor = grayColor; readOnlyCols1.Style.Font.FontColor = fontGray;
			readOnlyCols2.Style.Fill.BackgroundColor = grayColor; readOnlyCols2.Style.Font.FontColor = fontGray;
			readOnlyCols3.Style.Fill.BackgroundColor = grayColor; readOnlyCols3.Style.Font.FontColor = fontGray;

			// Format tiền tệ cho cột E, F, G
			worksheet.Range("E4:G1000").Style.NumberFormat.Format = "#,##0";

			// Điều chỉnh độ rộng 7 cột
			worksheet.Columns(1, 7).AdjustToContents(1, 3);
			double[] minWidths = [15, 18, 35, 12, 18, 18, 20];
			for (int col = 1; col <= 7; col++)
			{
				if (worksheet.Column(col).Width < minWidths[col - 1])
					worksheet.Column(col).Width = minWidths[col - 1];
			}

			// SỬA LỖI: Đóng băng ở dòng 4 (Header)
			worksheet.SheetView.FreezeRows(4);

			// Border cho toàn bộ bảng
			worksheet.Range("A4:G1000").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
			worksheet.Range("A4:G1000").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
		}

		private static void AddConditionalFormattingAndTotals(IXLWorksheet worksheet)
		{
			// SỬA LỖI: Áp dụng từ dòng 4
			var quantityRange = worksheet.Range("D4:D1000");
			var conditionalFormat = quantityRange.AddConditionalFormat();
			conditionalFormat.WhenIsTrue("=OR(D4<=0,D4>10000)").Fill.BackgroundColor = XLColor.FromArgb(255, 199, 206);

			// Thêm dòng Tổng cộng ở cuối
			worksheet.Cell(1002, 6).Value = "TỔNG CỘNG:";
			worksheet.Cell(1002, 6).Style.Font.Bold = true;
			worksheet.Cell(1002, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

			// SỬA LỖI: Tính tổng cột G thay vì cột F
			worksheet.Cell(1002, 7).FormulaA1 = "=SUBTOTAL(109,G4:G1000)";
			worksheet.Cell(1002, 7).Style.Font.Bold = true;
			worksheet.Cell(1002, 7).Style.Fill.BackgroundColor = XLColor.FromArgb(217, 217, 217);
			worksheet.Cell(1002, 7).Style.NumberFormat.Format = "#,##0";
		}

		private static void CreateVariantReferenceSheet(XLWorkbook workbook, IEnumerable<VariantSupplier> variantSuppliers)
		{
			var variantSheet = workbook.Worksheets.Add("DanhSach_SanPham");

			variantSheet.Cell(1, 1).Value = "Mã SKU";
			variantSheet.Cell(1, 2).Value = "Mã vạch";
			variantSheet.Cell(1, 3).Value = "Tên sản phẩm";
			variantSheet.Cell(1, 4).Value = "Loại";
			variantSheet.Cell(1, 5).Value = "Giá thương lượng";

			int variantRow = 2;
			foreach (var vs in variantSuppliers)
			{
				variantSheet.Cell(variantRow, 1).Value = vs.ProductVariant.Sku;
				variantSheet.Cell(variantRow, 2).Value = vs.ProductVariant.Barcode;
				variantSheet.Cell(variantRow, 3).Value = vs.ProductVariant.Product?.Name ?? "N/A";
				variantSheet.Cell(variantRow, 4).Value = vs.ProductVariant.Type.ToString();
				variantSheet.Cell(variantRow, 5).Value = vs.NegotiatedPrice;
				variantRow++;
			}

			variantSheet.Columns().AdjustToContents();
			variantSheet.Visibility = XLWorksheetVisibility.VeryHidden;
		}

		private static void CreateInstructionsSheet(XLWorkbook workbook)
		{
			var instructionsSheet = workbook.Worksheets.Add("Hướng dẫn");

			instructionsSheet.Cell(1, 1).Value = "📋 HƯỚNG DẪN SỬ DỤNG BIỂU MẪU NHẬP HÀNG";
			instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
			instructionsSheet.Cell(1, 1).Style.Font.FontSize = 16;
			instructionsSheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(68, 114, 196);

			AddHowToUseSection(instructionsSheet);
			AddColumnDescriptions(instructionsSheet);
			AddImportantNotes(instructionsSheet);
			AddTipsAndTricks(instructionsSheet);
			AddTroubleshooting(instructionsSheet);

			instructionsSheet.Columns().AdjustToContents();
			instructionsSheet.SetTabActive();
		}

		private static void AddHowToUseSection(IXLWorksheet sheet)
		{
			sheet.Cell(3, 1).Value = "🚀 CÁCH SỬ DỤNG:";
			sheet.Cell(3, 1).Style.Font.Bold = true;
			sheet.Cell(3, 1).Style.Font.FontSize = 12;

			sheet.Cell(4, 1).Value = "1. Chọn Mã SKU từ DANH SÁCH THẢ XUỐNG ở Cột A (Tuyệt đối không nhập tay!)";
			sheet.Cell(5, 1).Value = "2. Mã vạch, Tên sản phẩm và Giá Hệ thống sẽ tự động điền (Cột B, C, E)";
			sheet.Cell(6, 1).Value = "3. Nhập số lượng cần nhập kho vào Cột D (SL Dự kiến)";
			sheet.Cell(7, 1).Value = "4. Nếu giá nhập có thay đổi, nhập giá mới vào Cột F (Giá Thực tế). Nếu không, bỏ trống.";
			sheet.Cell(8, 1).Value = "5. Thành tiền sẽ tự động được tính toán ở Cột G";
			sheet.Cell(9, 1).Value = "6. Lưu file và tải lên hệ thống để tạo Phiếu nhập hàng";
		}

		private static void AddColumnDescriptions(IXLWorksheet sheet)
		{
			sheet.Cell(12, 1).Value = "📊 GIẢI THÍCH CÁC CỘT:";
			sheet.Cell(12, 1).Style.Font.Bold = true;
			sheet.Cell(12, 1).Style.Font.FontSize = 12;

			sheet.Cell(13, 1).Value = "Cột A - Mã SKU: Bắt buộc CHỌN từ danh sách.";
			sheet.Cell(14, 1).Value = "Cột B - Mã vạch: Tự động điền (Chỉ đọc).";
			sheet.Cell(15, 1).Value = "Cột C - Tên sản phẩm: Tự động điền (Chỉ đọc).";
			sheet.Cell(16, 1).Value = "Cột D - SL Dự kiến: Số lượng hàng hóa muốn nhập (Bắt buộc > 0).";
			sheet.Cell(17, 1).Value = "Cột E - Giá Hệ thống: Giá nhập đã thương lượng lưu trên phần mềm (Chỉ đọc).";
			sheet.Cell(18, 1).Value = "Cột F - Giá Thực tế: Nhập giá mới nếu có biến động. Nếu để trống, hệ thống sẽ lấy Giá Hệ thống.";
			sheet.Cell(19, 1).Value = "Cột G - Thành tiền: Tự động tính toán (Chỉ đọc).";
		}

		private static void AddImportantNotes(IXLWorksheet sheet)
		{
			sheet.Cell(21, 1).Value = "⚠️ LƯU Ý QUAN TRỌNG:";
			sheet.Cell(21, 1).Style.Font.Bold = true;
			sheet.Cell(21, 1).Style.Font.FontSize = 12;
			sheet.Cell(21, 1).Style.Font.FontColor = XLColor.Red;

			sheet.Cell(22, 1).Value = "✓ Luôn CHỌN SKU từ danh sách thả xuống, hệ thống sẽ báo lỗi nếu bạn gõ sai.";
			sheet.Cell(23, 1).Value = "✓ Mỗi mã SKU chỉ được xuất hiện MỘT LẦN trong file.";
			sheet.Cell(24, 1).Value = "✓ Số lượng phải là số nguyên dương.";
			sheet.Cell(25, 1).Value = "✓ Các cột màu xám là cột công thức tự động - KHÔNG ĐƯỢC XÓA HOẶC SỬA.";
			sheet.Cell(26, 1).Value = "✓ Kích thước file tối đa: 10MB (Chỉ hỗ trợ đuôi .xlsx).";
		}

		private static void AddTipsAndTricks(IXLWorksheet sheet)
		{
			sheet.Cell(28, 1).Value = "💡 MẸO & THỦ THUẬT:";
			sheet.Cell(28, 1).Style.Font.Bold = true;
			sheet.Cell(28, 1).Style.Font.FontSize = 12;
			sheet.Cell(28, 1).Style.Font.FontColor = XLColor.Green;

			sheet.Cell(29, 1).Value = "• Nhấn Ctrl+D để copy nhanh giá trị từ ô phía trên xuống.";
			sheet.Cell(30, 1).Value = "• Ô Số lượng chuyển màu đỏ nghĩa là bạn nhập số âm hoặc quá lớn.";
			sheet.Cell(31, 1).Value = "• Bạn có thể nhập tối đa 1000 dòng sản phẩm trong một file.";
		}

		private static void AddTroubleshooting(IXLWorksheet sheet)
		{
			sheet.Cell(33, 1).Value = "🔧 KHẮC PHỤC SỰ CỐ:";
			sheet.Cell(33, 1).Style.Font.Bold = true;
			sheet.Cell(33, 1).Style.Font.FontSize = 12;

			sheet.Cell(34, 1).Value = "❓ Không thấy mũi tên chọn SKU? Click thẳng chuột vào ô trống ở Cột A, mũi tên sẽ hiện ra bên cạnh.";
			sheet.Cell(35, 1).Value = "❓ Tên sản phẩm không nhảy tự động? Đảm bảo bạn đã chọn SKU đúng từ danh sách thay vì gõ tay.";
			sheet.Cell(36, 1).Value = "❓ Thành tiền bị lỗi #VALUE? Kiểm tra xem Số lượng và Giá có bị dính chữ cái nào không.";
		}
	}
}
