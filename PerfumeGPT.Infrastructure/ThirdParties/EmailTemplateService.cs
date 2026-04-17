using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using System.Net;
using System.Text;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class EmailTemplateService : IEmailTemplateService
	{
		public string GetLowStockAlertTemplate(IEnumerable<LowStockAlertItem> lowStockItems, DateTime generatedAtUtc)
		{
			var rows = new StringBuilder();
			foreach (var item in lowStockItems)
			{
				rows.Append($"""
                    <tr>
                    <td style="padding:8px;border:1px solid #e5e7eb;">{WebUtility.HtmlEncode(item.ProductName)}</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;">{WebUtility.HtmlEncode(item.VariantSku)}</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;text-align:right;">{item.TotalQuantity}</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;text-align:right;">{item.AvailableQuantity}</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;text-align:right;">{item.LowStockThreshold}</td>
                    </tr>
                    """);
			}

			return $"""
                    <!doctype html>
                    <html>
                      <head>
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width,initial-scale=1">
                        <title>Cảnh báo tồn kho thấp</title>
                      </head>
                      <body style="font-family:Arial,Helvetica,sans-serif;background:#f7f7f7;padding:20px;">
                        <table width="100%" cellpadding="0" cellspacing="0"><tr><td align="center">
                          <table width="760" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;padding:24px;">
                            <tr><td><h2 style="margin:0 0 12px;">PerfumeGPT - Cảnh báo tồn kho thấp</h2></td></tr>
                            <tr><td style="padding-bottom:16px;color:#374151;">Ghi nhận lúc: <b>{generatedAtUtc:dd/MM/yyyy HH:mm:ss} UTC</b><br/>Các phân loại sản phẩm dưới đây đang ở mức hoặc thấp hơn ngưỡng tồn kho tối thiểu.</td></tr>
                            <tr><td>
                              <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:14px;">
                                <thead><tr style="background:#f3f4f6;"><th style="padding:8px;border:1px solid #e5e7eb;text-align:left;">Sản phẩm</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:left;">Mã SKU</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">Tổng SL</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">SL có sẵn</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">Ngưỡng tối thiểu</th></tr></thead>
                                <tbody>{rows}</tbody>
                              </table>
                            </td></tr>
                          </table>
                        </td></tr></table>
                      </body>
                    </html>
                    """;
		}

		public string GetInvoiceTemplate(ReceiptResponse invoice)
		{
			var itemsHtml = new StringBuilder();
			foreach (var item in invoice.Items)
			{
				itemsHtml.Append($"""
                    <tr>
                    <td style="padding:8px;border:1px solid #e5e7eb;">{WebUtility.HtmlEncode(item.ProductName)}</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;">{WebUtility.HtmlEncode(item.VariantInfo)}</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;text-align:right;">{item.Quantity}</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;text-align:right;">{item.UnitPrice:N0}₫</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;text-align:right;">{item.Subtotal:N0}₫</td>
                    </tr>
                    """);
			}

			return $"""
                    <!doctype html>
                    <html>
                      <head>
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width,initial-scale=1">
                        <title>Hóa đơn {invoice.Code}</title>
                      </head>
                      <body style="font-family:Arial,Helvetica,sans-serif;background:#f7f7f7;padding:20px;">
                        <table width="100%" cellpadding="0" cellspacing="0"><tr><td align="center">
                          <table width="700" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;padding:24px;">
                            <tr><td><h2 style="margin:0 0 12px;">Hóa đơn PerfumeGPT</h2></td></tr>
                            <tr><td style="padding-bottom:16px;color:#374151;">Mã đơn hàng: <b>{invoice.Code}</b><br/>Ngày đặt: {invoice.OrderDate:dd/MM/yyyy HH:mm:ss}<br/>Khách hàng: {WebUtility.HtmlEncode(invoice.CustomerName)}<br/>Số điện thoại: {WebUtility.HtmlEncode(invoice.RecipientPhone)}<br/>Địa chỉ: {WebUtility.HtmlEncode(invoice.RecipientAddress)}</td></tr>
                            <tr><td>
                              <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:14px;">
                                <thead><tr style="background:#f3f4f6;"><th style="padding:8px;border:1px solid #e5e7eb;text-align:left;">Sản phẩm</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:left;">Phân loại</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">SL</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">Đơn giá</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">Thành tiền</th></tr></thead>
                                <tbody>{itemsHtml}</tbody>
                              </table>
                            </td></tr>
                            <tr><td style="padding-top:16px;text-align:right;color:#111827;">Tạm tính: <b>{invoice.Subtotal:N0}₫</b><br/>Giảm giá: <b>{invoice.Discount:N0}₫</b><br/>Tổng cộng: <b>{invoice.Total:N0}₫</b><br/>Phương thức thanh toán: <b>{WebUtility.HtmlEncode(invoice.PaymentMethod)}</b></td></tr>
                          </table>
                        </td></tr></table>
                      </body>
                    </html>
                    """;
		}

		public string GetRegisterTemplate(string username, string verifyUrl)
		{
			var safeName = string.IsNullOrWhiteSpace(username) ? "Bạn" : WebUtility.HtmlEncode(username);
			var safeUrl = string.IsNullOrWhiteSpace(verifyUrl) ? string.Empty : WebUtility.HtmlEncode(verifyUrl);

			return $@"<!doctype html>
                <html>
                  <head>
                    <meta charset=""utf-8""> 
                    <meta name=""viewport"" content=""width=device-width,initial-scale=1""> 
                    <title>Chào mừng bạn đến với PerfumeGPT</title>
                  </head>
                  <body style=""font-family: Arial,Helvetica,sans-serif; background:#f7f7f7; margin:0; padding:20px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                      <tr>
                        <td align=""center"">
                          <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff; border-radius:8px; overflow:hidden;"">
                            <tr>
                              <td style=""padding:24px; text-align:center; background:#1f2937; color:#ffffff;"">
                                <h1 style=""margin:0; font-size:20px"">Chào mừng đến với PerfumeGPT</h1>
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding:24px; color:#111827;"">
                                <p>Chào {safeName},</p>
                                <p>Cảm ơn bạn đã đăng ký tài khoản tại PerfumeGPT. Vui lòng xác nhận địa chỉ email của bạn bằng cách nhấn vào nút bên dưới:</p>
                                <p style=""text-align:center; margin:28px 0;"">
                                  <a href=""{safeUrl}"" style=""display:inline-block; padding:12px 20px; color:#ffffff; background:#4f46e5; border-radius:6px; text-decoration:none;"">Xác nhận email</a>
                                </p>
                                <p>Nếu nút bấm không hoạt động, hãy sao chép và dán đường dẫn sau vào trình duyệt của bạn:</p>
                                <p style=""word-break:break-all; font-size:13px; color:#6b7280;"">{safeUrl}</p>
                                <p>Trân trọng,<br/>Đội ngũ PerfumeGPT</p>
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding:12px 24px; font-size:12px; color:#9ca3af; text-align:center;"">Nếu bạn không tạo tài khoản này, bạn có thể bỏ qua email này một cách an toàn.</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>";
		}

		public string GetForgotPasswordTemplate(string username, string resetUrl)
		{
			var safeName = string.IsNullOrWhiteSpace(username) ? "Bạn" : WebUtility.HtmlEncode(username);
			var safeUrl = string.IsNullOrWhiteSpace(resetUrl) ? string.Empty : WebUtility.HtmlEncode(resetUrl);

			return $@"<!doctype html>
                <html>
                  <head>
                    <meta charset=""utf-8""> 
                    <meta name=""viewport"" content=""width=device-width,initial-scale=1""> 
                    <title>Đặt lại mật khẩu của bạn</title>
                  </head>
                  <body style=""font-family: Arial,Helvetica,sans-serif; background:#f7f7f7; margin:0; padding:20px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                      <tr>
                        <td align=""center"">
                          <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff; border-radius:8px; overflow:hidden;"">
                            <tr>
                              <td style=""padding:24px; text-align:center; background:#1f2937; color:#ffffff;"">
                                <h1 style=""margin:0; font-size:20px"">Đặt lại mật khẩu</h1>
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding:24px; color:#111827;"">
                                <p>Chào {safeName},</p>
                                <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản PerfumeGPT của mình. Vui lòng sử dụng nút bên dưới để tiến hành đặt lại. Yêu cầu này chỉ có hiệu lực trong một khoảng thời gian giới hạn.</p>
                                <p style=""text-align:center; margin:28px 0;"">
                                  <a href=""{safeUrl}"" style=""display:inline-block; padding:12px 20px; color:#ffffff; background:#4f46e5; border-radius:6px; text-decoration:none;"">Đặt lại mật khẩu</a>
                                </p>
                                <p>Nếu nút bấm không hoạt động, hãy sao chép và dán đường dẫn sau vào trình duyệt của bạn:</p>
                                <p style=""word-break:break-all; font-size:13px; color:#6b7280;"">{safeUrl}</p>
                                <p>Trân trọng,<br/>Đội ngũ PerfumeGPT</p>
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding:12px 24px; font-size:12px; color:#9ca3af; text-align:center;"">Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>";
		}
	}
}