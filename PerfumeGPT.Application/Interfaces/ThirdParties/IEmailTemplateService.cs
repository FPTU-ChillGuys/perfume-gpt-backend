using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.Inventory;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IEmailTemplateService
	{
		string GetRegisterTemplate(string username, string verifyUrl);
		string GetForgotPasswordTemplate(string username, string resetUrl);
		string GetInvoiceTemplate(ReceiptResponse invoice);
		string GetLowStockAlertTemplate(IEnumerable<LowStockAlertItem> lowStockItems, DateTime generatedAtUtc);
		string GetVoucherGiftTemplate(string voucherCode, DateTime expiryDate);
	}
}
