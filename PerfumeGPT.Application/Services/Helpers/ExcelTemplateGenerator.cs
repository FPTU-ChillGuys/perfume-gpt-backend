using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
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

		/// <summary>
		/// Generates an Excel template with dropdown lists and auto-fill formulas for import tickets.
		/// </summary>
		/// <returns>Excel template as byte array with filename</returns>
		public async Task<ExcelTemplateResponse> GenerateImportTemplateAsync()
		{
			// Fetch all active variants from database for dropdown
			var variants = await _unitOfWork.Variants.GetAllAsync(
				filter: v => v.Status == VariantStatus.Active,
				include: query => query
					.Include(v => v.Product)
					.Include(v => v.Concentration),
				orderBy: q => q.OrderBy(v => v.Barcode));

			using (var workbook = new XLWorkbook())
			{
				// Create all sheets
				CreateMainTemplateSheet(workbook, variants);
				CreateVariantReferenceSheet(workbook, variants);
				CreateInstructionsSheet(workbook);

				// Convert to byte array
				using (var stream = new MemoryStream())
				{
					workbook.SaveAs(stream);
					var fileContent = stream.ToArray();

					return new ExcelTemplateResponse
					{
						FileContent = fileContent,
						FileName = $"ImportTicket_Template_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx"
					};
				}
			}
		}

		/// <summary>
		/// Creates the main import template sheet with dropdowns and formulas.
		/// </summary>
		private static void CreateMainTemplateSheet(XLWorkbook workbook, IEnumerable<dynamic> variants)
		{
			var worksheet = workbook.Worksheets.Add("Import Template");

			// Set column headers
			SetupHeaders(worksheet);

			// Add data validation (dropdown) for Barcode column
			if (variants.Any())
			{
				SetupBarcodeDropdown(worksheet, variants.Count());
			}

			// Add formulas for auto-filled columns
			AddAutoFillFormulas(worksheet);

			// Apply formatting
			ApplyFormatting(worksheet);

			// Add conditional formatting and totals
			AddConditionalFormattingAndTotals(worksheet);
		}

		/// <summary>
		/// Sets up column headers with professional styling.
		/// </summary>
		private static void SetupHeaders(IXLWorksheet worksheet)
		{
			worksheet.Cell(1, 1).Value = "SKU";
			worksheet.Cell(1, 2).Value = "Barcode (Auto-filled)";
			worksheet.Cell(1, 3).Value = "Product Name (Auto-filled)";
			worksheet.Cell(1, 4).Value = "Quantity";
			worksheet.Cell(1, 5).Value = "Unit Price (VND)";
			worksheet.Cell(1, 6).Value = "Subtotal (Auto-calculated)";

			// Format headers
			var headerRange = worksheet.Range(1, 1, 1, 6);
			headerRange.Style.Font.Bold = true;
			headerRange.Style.Font.FontSize = 11;
			headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
			headerRange.Style.Font.FontColor = XLColor.White;
			headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
			headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
			worksheet.Row(1).Height = 25;
		}

		/// <summary>
		/// Sets up dropdown validation for the SKU column.
		/// </summary>
		private static void SetupBarcodeDropdown(IXLWorksheet worksheet, int variantCount)
		{
			var skuRange = worksheet.Range("A2:A1000");
			var variantSheetName = "Variant_Reference";
			var validationFormula = $"'{variantSheetName}'!$A$2:$A${variantCount + 1}";

			var skuValidation = skuRange.CreateDataValidation();
			skuValidation.List(validationFormula, true);
			skuValidation.InputTitle = "Select SKU";
			skuValidation.InputMessage = "Click the dropdown arrow to select a product SKU from the list. SKUs are more readable than barcodes!";
			skuValidation.ErrorTitle = "Invalid SKU";
			skuValidation.ErrorMessage = "Please select a SKU from the dropdown list.";
		}

		/// <summary>
		/// Adds VLOOKUP and calculation formulas to auto-fill columns.
		/// </summary>
		private static void AddAutoFillFormulas(IXLWorksheet worksheet)
		{
			for (int row = 2; row <= 1000; row++)
			{
				// Barcode lookup (Column B)
				worksheet.Cell(row, 2).FormulaA1 = $"=IFERROR(VLOOKUP(A{row},Variant_Reference!A:B,2,FALSE),\"\")";

				// Product Name lookup (Column C)
				worksheet.Cell(row, 3).FormulaA1 = $"=IFERROR(VLOOKUP(A{row},Variant_Reference!A:C,3,FALSE),\"\")";

				// Subtotal calculation (Column F)
				worksheet.Cell(row, 6).FormulaA1 = $"=IF(AND(ISNUMBER(D{row}),ISNUMBER(E{row})),D{row}*E{row},\"\")";
			}
		}

		/// <summary>
		/// Applies formatting to the worksheet (colors, borders, column widths).
		/// </summary>
		private static void ApplyFormatting(IXLWorksheet worksheet)
		{
			// Format auto-filled columns as read-only (light gray background)
			var autoFilledColumns = worksheet.Range("B2:C1000");
			autoFilledColumns.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
			autoFilledColumns.Style.Font.FontColor = XLColor.FromArgb(89, 89, 89);

			var subtotalColumn = worksheet.Range("F2:F1000");
			subtotalColumn.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
			subtotalColumn.Style.Font.FontColor = XLColor.FromArgb(89, 89, 89);

			// Number formatting for currency
			worksheet.Range("E2:F1000").Style.NumberFormat.Format = "#,##0";

			// Set column widths
			worksheet.Column(1).Width = 18;  // SKU
			worksheet.Column(2).Width = 20;  // Barcode
			worksheet.Column(3).Width = 35;  // Product Name
			worksheet.Column(4).Width = 12;  // Quantity
			worksheet.Column(5).Width = 18;  // Unit Price
			worksheet.Column(6).Width = 18;  // Subtotal

			// Freeze the header row
			worksheet.SheetView.FreezeRows(1);

			// Add borders
			worksheet.Range("A1:F1000").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
			worksheet.Range("A1:F1000").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
		}

		/// <summary>
		/// Adds conditional formatting for validation and total row.
		/// </summary>
		private static void AddConditionalFormattingAndTotals(IXLWorksheet worksheet)
		{
			// Conditional formatting for Quantity (highlight if <= 0 or > 10000)
			var quantityRange = worksheet.Range("D2:D1000");
			var conditionalFormat = quantityRange.AddConditionalFormat();
			conditionalFormat.WhenIsTrue("=OR(D2<=0,D2>10000)").Fill.BackgroundColor = XLColor.FromArgb(255, 199, 206);

			// Add total row at the bottom (row 1002)
			worksheet.Cell(1002, 5).Value = "TOTAL:";
			worksheet.Cell(1002, 5).Style.Font.Bold = true;
			worksheet.Cell(1002, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
			worksheet.Cell(1002, 6).FormulaA1 = "=SUBTOTAL(109,F2:F1000)";
			worksheet.Cell(1002, 6).Style.Font.Bold = true;
			worksheet.Cell(1002, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(217, 217, 217);
			worksheet.Cell(1002, 6).Style.NumberFormat.Format = "#,##0";
		}

		/// <summary>
		/// Creates the hidden variant reference sheet with product data.
		/// </summary>
		private static void CreateVariantReferenceSheet(XLWorkbook workbook, IEnumerable<dynamic> variants)
		{
			var variantSheet = workbook.Worksheets.Add("Variant_Reference");

			// Headers - SKU is now first for the dropdown
			variantSheet.Cell(1, 1).Value = "SKU";
			variantSheet.Cell(1, 2).Value = "Barcode";
			variantSheet.Cell(1, 3).Value = "Product Name";
			variantSheet.Cell(1, 4).Value = "Volume (ml)";
			variantSheet.Cell(1, 5).Value = "Concentration";
			variantSheet.Cell(1, 6).Value = "Base Price";
			variantSheet.Cell(1, 7).Value = "Type";

			// Format headers
			var variantHeaderRange = variantSheet.Range(1, 1, 1, 7);
			variantHeaderRange.Style.Font.Bold = true;
			variantHeaderRange.Style.Fill.BackgroundColor = XLColor.LightGray;

			// Populate variant data - ordered by SKU for better readability
			int variantRow = 2;
			foreach (var variant in variants.OrderBy(v => v.Sku))
			{
				variantSheet.Cell(variantRow, 1).Value = variant.Sku;
				variantSheet.Cell(variantRow, 2).Value = variant.Barcode;
				variantSheet.Cell(variantRow, 3).Value = variant.Product?.Name ?? "N/A";
				variantSheet.Cell(variantRow, 4).Value = variant.VolumeMl;
				variantSheet.Cell(variantRow, 5).Value = variant.Concentration?.Name ?? "N/A";
				variantSheet.Cell(variantRow, 6).Value = variant.BasePrice;
				variantSheet.Cell(variantRow, 7).Value = variant.Type.ToString();
				variantRow++;
			}

			// Auto-fit columns and hide sheet
			variantSheet.Columns().AdjustToContents();
			variantSheet.Visibility = XLWorksheetVisibility.VeryHidden;
		}

		/// <summary>
		/// Creates the instructions sheet with user guide and tips.
		/// </summary>
		private void CreateInstructionsSheet(XLWorkbook workbook)
		{
			var instructionsSheet = workbook.Worksheets.Add("Instructions");

			// Title
			instructionsSheet.Cell(1, 1).Value = "📋 IMPORT TICKET EXCEL TEMPLATE - USER GUIDE";
			instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
			instructionsSheet.Cell(1, 1).Style.Font.FontSize = 16;
			instructionsSheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(68, 114, 196);

			// How to Use Section
			AddHowToUseSection(instructionsSheet);

			// Column Descriptions
			AddColumnDescriptions(instructionsSheet);

			// Important Notes
			AddImportantNotes(instructionsSheet);

			// Tips & Tricks
			AddTipsAndTricks(instructionsSheet);

			// Troubleshooting
			AddTroubleshooting(instructionsSheet);

			// Auto-fit columns and set as active sheet
			instructionsSheet.Columns().AdjustToContents();
			instructionsSheet.SetTabActive();
		}

		/// <summary>
		/// Adds "How to Use" section to instructions sheet.
		/// </summary>
		private static void AddHowToUseSection(IXLWorksheet sheet)
		{
			sheet.Cell(3, 1).Value = "🚀 HOW TO USE THIS TEMPLATE:";
			sheet.Cell(3, 1).Style.Font.Bold = true;
			sheet.Cell(3, 1).Style.Font.FontSize = 12;

			sheet.Cell(4, 1).Value = "1. Select a SKU from the DROPDOWN in Column A (don't type manually!)";
			sheet.Cell(5, 1).Value = "2. Barcode and Product Name will auto-fill (Columns B & C)";
			sheet.Cell(6, 1).Value = "3. Enter the Quantity (Column D)";
			sheet.Cell(7, 1).Value = "4. Enter the Unit Price in VND (Column E)";
			sheet.Cell(8, 1).Value = "5. Subtotal will calculate automatically (Column F)";
			sheet.Cell(9, 1).Value = "6. Total will be shown at the bottom";
			sheet.Cell(10, 1).Value = "7. Save and upload the file to the system";
		}

		/// <summary>
		/// Adds column descriptions to instructions sheet.
		/// </summary>
		private static void AddColumnDescriptions(IXLWorksheet sheet)
		{
			sheet.Cell(12, 1).Value = "📊 COLUMN DESCRIPTIONS:";
			sheet.Cell(12, 1).Style.Font.Bold = true;
			sheet.Cell(12, 1).Style.Font.FontSize = 12;

			sheet.Cell(13, 1).Value = "Column A - SKU: Use DROPDOWN to select (Required) - More readable than barcodes!";
			sheet.Cell(14, 1).Value = "Column B - Barcode: Auto-filled from database (Read-only)";
			sheet.Cell(15, 1).Value = "Column C - Product Name: Auto-filled from database (Read-only)";
			sheet.Cell(16, 1).Value = "Column D - Quantity: Enter number of units (Required, > 0)";
			sheet.Cell(17, 1).Value = "Column E - Unit Price: Enter price per unit in VND (Required, > 0)";
			sheet.Cell(18, 1).Value = "Column F - Subtotal: Auto-calculated (Read-only)";
		}

		/// <summary>
		/// Adds important notes to instructions sheet.
		/// </summary>
		private static void AddImportantNotes(IXLWorksheet sheet)
		{
			sheet.Cell(20, 1).Value = "⚠️ IMPORTANT NOTES:";
			sheet.Cell(20, 1).Style.Font.Bold = true;
			sheet.Cell(20, 1).Style.Font.FontSize = 12;
			sheet.Cell(20, 1).Style.Font.FontColor = XLColor.Red;

			sheet.Cell(21, 1).Value = "✓ Always use the DROPDOWN for SKUs - don't type manually!";
			sheet.Cell(22, 1).Value = "✓ Each SKU can only appear once in the file";
			sheet.Cell(23, 1).Value = "✓ Quantity must be a positive whole number";
			sheet.Cell(24, 1).Value = "✓ Unit Price must be a positive number";
			sheet.Cell(25, 1).Value = "✓ Gray columns are auto-filled - don't edit them";
			sheet.Cell(26, 1).Value = "✓ Maximum file size: 10MB";
			sheet.Cell(27, 1).Value = "✓ Supported formats: .xlsx only";
		}

		/// <summary>
		/// Adds tips and tricks to instructions sheet.
		/// </summary>
		private static void AddTipsAndTricks(IXLWorksheet sheet)
		{
			sheet.Cell(29, 1).Value = "💡 TIPS & TRICKS:";
			sheet.Cell(29, 1).Style.Font.Bold = true;
			sheet.Cell(29, 1).Style.Font.FontSize = 12;
			sheet.Cell(29, 1).Style.Font.FontColor = XLColor.Green;

			sheet.Cell(30, 1).Value = "• Use Ctrl+D to copy down values in Excel";
			sheet.Cell(31, 1).Value = "• The total at the bottom updates automatically";
			sheet.Cell(32, 1).Value = "• Red highlighting in Quantity means invalid value";
			sheet.Cell(33, 1).Value = "• Sort by Product Name to group similar items";
			sheet.Cell(34, 1).Value = "• You can add up to 999 products in one file";
		}

		/// <summary>
		/// Adds troubleshooting section to instructions sheet.
		/// </summary>
		private static void AddTroubleshooting(IXLWorksheet sheet)
		{
			sheet.Cell(36, 1).Value = "🔧 TROUBLESHOOTING:";
			sheet.Cell(36, 1).Style.Font.Bold = true;
			sheet.Cell(36, 1).Style.Font.FontSize = 12;

			sheet.Cell(37, 1).Value = "❓ Dropdown not showing? Click on the cell and look for the arrow";
			sheet.Cell(38, 1).Value = "❓ Barcode/Product name not auto-filling? Make sure you selected SKU from dropdown";
			sheet.Cell(39, 1).Value = "❓ Subtotal not calculating? Check Quantity and Price are numbers";
			sheet.Cell(40, 1).Value = "❓ Upload failed? Check for duplicate SKUs or invalid values";
		}
	}
}
