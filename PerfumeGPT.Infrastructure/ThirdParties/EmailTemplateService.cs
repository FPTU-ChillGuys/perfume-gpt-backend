using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using System.Net;
using System.Text;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class EmailTemplateService : IEmailTemplateService
	{
		// Thêm phương thức trợ giúp để tạo ra phần bọc HTML tươi sáng và gam màu xanh chủ đạo
		private string GetCommonWrapperTemplate(string title, string contentBody)
		{
			return $"""
                <!doctype html>
                <html>
                  <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width,initial-scale=1">
                    <title>{title}</title>
                  </head>
                  <body style="font-family:Arial,Helvetica,sans-serif; background-color:#e1f5fe; background-image:url('image_4.png'); background-repeat:no-repeat; background-position:center; background-size:cover; padding:20px;">
                    <table width="100%" cellpadding="0" cellspacing="0"><tr><td align="center">
                      <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff; border-radius:8px; padding:24px; box-shadow:0 2px 4px rgba(0,0,0,0.1); border:1px solid #b3e5fc;">
                        <tr><td><h1 style="margin:0 0 12px; color:#0056b3; font-size:24px; font-weight:700;">{title}</h1></td></tr>
                        {contentBody}
                      </table>
                    </td></tr></table>
                  </body>
                </html>
                """;
		}

		public string GetVoucherGiftTemplate(string voucherCode, DateTime expiryDate)
		{
			var safeVoucherCode = string.IsNullOrWhiteSpace(voucherCode) ? string.Empty : WebUtility.HtmlEncode(voucherCode);
			var expiryText = expiryDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
			var title = "Chào mừng bạn đến với PerfumeGPT";
			var contentBody = $"""
                        <tr><td style="padding-bottom:16px; color:#111827;">Chào bạn, một người dùng vừa tặng bạn mã giảm giá. Bạn có thể sử dụng mã này khi thanh toán tại PerfumeGPT.</td></tr>
                        <tr><td style="padding:16px; background:#b3e5fc; border-radius:6px; text-align:center; font-size:18px; font-weight:700; letter-spacing:1px; color:#0056b3;">{safeVoucherCode}</td></tr>
                        <tr><td style="padding-top:16px; color:#111827;">Hạn sử dụng: <b style="color:#0056b3;">{expiryText}</b></td></tr>
                        <tr><td style="padding-top:8px; color:#6b7280; font-size:13px;">Nếu bạn không mong đợi email này, bạn có thể bỏ qua.</td></tr>
                        """;

			return GetCommonWrapperTemplate(title, contentBody);
		}

		public string GetLowStockAlertTemplate(IEnumerable<LowStockAlertItem> lowStockItems, DateTime generatedAtUtc)
		{
			var rows = new StringBuilder();
			foreach (var item in lowStockItems)
			{
				rows.Append($"""
                        <tr>
                        <td style="padding:8px; border:1px solid #b3e5fc;">{WebUtility.HtmlEncode(item.ProductName)}</td>
                        <td style="padding:8px; border:1px solid #b3e5fc;">{WebUtility.HtmlEncode(item.VariantSku)}</td>
                        <td style="padding:8px; border:1px solid #b3e5fc; text-align:right; color:#0056b3; font-weight:700;">{item.TotalQuantity}</td>
                        <td style="padding:8px; border:1px solid #b3e5fc; text-align:right; color:#0056b3;">{item.AvailableQuantity}</td>
                        <td style="padding:8px; border:1px solid #b3e5fc; text-align:right; color:#ef5350;">{item.LowStockThreshold}</td>
                        </tr>
                        """);
			}

			var title = "Cảnh báo tồn kho thấp";
			var contentBody = $"""
                        <tr><td style="padding-bottom:16px; color:#111827;">Ghi nhận lúc: <b style="color:#0056b3;">{generatedAtUtc:dd/MM/yyyy HH:mm:ss} UTC</b><br/>Các phân loại sản phẩm dưới đây đang ở mức hoặc thấp hơn ngưỡng tồn kho tối thiểu.</td></tr>
                        <tr><td>
                          <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse; font-size:14px;">
                            <thead><tr style="background:#b3e5fc; color:#0056b3;"><th style="padding:8px; border:1px solid #b3e5fc; text-align:left;">Sản phẩm</th><th style="padding:8px; border:1px solid #b3e5fc; text-align:left;">Mã SKU</th><th style="padding:8px; border:1px solid #b3e5fc; text-align:right;">Tổng SL</th><th style="padding:8px; border:1px solid #b3e5fc; text-align:right;">SL có sẵn</th><th style="padding:8px; border:1px solid #b3e5fc; text-align:right;">Ngưỡng tối thiểu</th></tr></thead>
                            <tbody>{rows}</tbody>
                          </table>
                        </td></tr>
                        """;

			return GetCommonWrapperTemplate(title, contentBody);
		}

		public string GetInvoiceTemplate(ReceiptResponse invoice)
		{
			var itemsHtml = new StringBuilder();
			foreach (var item in invoice.Items)
			{
				itemsHtml.Append($"""
                        <tr>
                        <td style="padding:8px; border:1px solid #b3e5fc;">{WebUtility.HtmlEncode(item.ProductName)}</td>
                        <td style="padding:8px; border:1px solid #b3e5fc;">{WebUtility.HtmlEncode(item.VariantInfo)}</td>
                        <td style="padding:8px; border:1px solid #b3e5fc; text-align:right; color:#0056b3;">{item.Quantity}</td>
                        <td style="padding:8px; border:1px solid #b3e5fc; text-align:right;">{item.UnitPrice:N0}₫</td>
                        <td style="padding:8px; border:1px solid #b3e5fc; text-align:right; color:#0056b3; font-weight:700;">{item.Subtotal:N0}₫</td>
                        </tr>
                        """);
			}

			var title = $"Hóa đơn #{invoice.Code}";
			var contentBody = $"""
                        <tr><td style="padding-bottom:16px; color:#111827;">Mã đơn hàng: <b style="color:#0056b3;">{invoice.Code}</b><br/>Ngày đặt: {invoice.OrderDate:dd/MM/yyyy HH:mm:ss}<br/>Khách hàng: {WebUtility.HtmlEncode(invoice.CustomerName)}<br/>Số điện thoại: {WebUtility.HtmlEncode(invoice.RecipientPhone)}<br/>Địa chỉ: {WebUtility.HtmlEncode(invoice.RecipientAddress)}</td></tr>
                        <tr><td>
                          <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse; font-size:14px;">
                            <thead><tr style="background:#b3e5fc; color:#0056b3;"><th style="padding:8px; border:1px solid #b3e5fc; text-align:left;">Sản phẩm</th><th style="padding:8px; border:1px solid #b3e5fc; text-align:left;">Phân loại</th><th style="padding:8px; border:1px solid #b3e5fc; text-align:right;">SL</th><th style="padding:8px; border:1px solid #b3e5fc; text-align:right;">Đơn giá</th><th style="padding:8px; border:1px solid #b3e5fc; text-align:right;">Thành tiền</th></tr></thead>
                            <tbody>{itemsHtml}</tbody>
                          </table>
                        </td></tr>
                        <tr><td style="padding-top:16px; text-align:right; color:#111827;">Tạm tính: <b>{invoice.Subtotal:N0}₫</b><br/>Phí vận chuyển: <b>{invoice.ShippingFee:N0}₫</b><br/>Giảm giá: <b>{invoice.Discount:N0}₫</b><br/>Tổng cộng: <b style="color:#0056b3; font-size:18px;">{invoice.Total:N0}₫</b><br/>Phương thức thanh toán: {WebUtility.HtmlEncode(invoice.PaymentMethod)}</td></tr>
                        """;

			return GetCommonWrapperTemplate(title, contentBody);
		}

		public string GetRegisterTemplate(string username, string verifyUrl)
		{
			var safeName = string.IsNullOrWhiteSpace(username) ? "Bạn" : WebUtility.HtmlEncode(username);
			var safeUrl = string.IsNullOrWhiteSpace(verifyUrl) ? string.Empty : WebUtility.HtmlEncode(verifyUrl);
			var title = "Chào mừng đến với PerfumeGPT";
			var contentBody = $"""
                        <tr><td style="padding-bottom:16px; color:#111827;">Chào {safeName},<br/>Cảm ơn bạn đã đăng ký tài khoản tại PerfumeGPT. Vui lòng xác nhận địa chỉ email của bạn bằng cách nhấn vào nút bên dưới:</td></tr>
                        <tr><td style="padding:16px; text-align:center;">
                          <a href="{safeUrl}" style="display:inline-block; padding:12px 20px; color:#ffffff; background:#0056b3; border-radius:6px; text-decoration:none; font-weight:700;">Xác nhận email</a>
                        </td></tr>
                        <tr><td style="word-break:break-all; font-size:13px; color:#6b7280;">Hoặc sao chép đường dẫn: {safeUrl}</td></tr>
                        <tr><td style="padding-top:12px; font-size:12px; color:#9ca3af; text-align:center;">Nếu bạn không tạo tài khoản này, bạn có thể bỏ qua email này một cách an toàn.</td></tr>
                        """;

			return GetCommonWrapperTemplate(title, contentBody);
		}

		public string GetForgotPasswordTemplate(string username, string resetUrl)
		{
			var safeName = string.IsNullOrWhiteSpace(username) ? "Bạn" : WebUtility.HtmlEncode(username);
			var safeUrl = string.IsNullOrWhiteSpace(resetUrl) ? string.Empty : WebUtility.HtmlEncode(resetUrl);
			var title = "Đặt lại mật khẩu của bạn";
			var contentBody = $"""
                        <tr><td style="padding-bottom:16px; color:#111827;">Chào {safeName},<br/>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản PerfumeGPT của mình. Vui lòng sử dụng nút bên dưới để tiến hành đặt lại. Yêu cầu này chỉ có hiệu lực trong một khoảng thời gian giới hạn.</td></tr>
                        <tr><td style="padding:16px; text-align:center;">
                          <a href="{safeUrl}" style="display:inline-block; padding:12px 20px; color:#ffffff; background:#0056b3; border-radius:6px; text-decoration:none; font-weight:700;">Đặt lại mật khẩu</a>
                        </td></tr>
                        <tr><td style="word-break:break-all; font-size:13px; color:#6b7280;">Hoặc sao chép đường dẫn: {safeUrl}</td></tr>
                        <tr><td style="padding-top:12px; font-size:12px; color:#9ca3af; text-align:center;">Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</td></tr>
                        """;

			return GetCommonWrapperTemplate(title, contentBody);
		}
	}
}