using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using System.Net;
using System.Text;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class EmailTemplateService : IEmailTemplateService
	{
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
                    <td style="padding:8px;border:1px solid #e5e7eb;text-align:right;">{item.UnitPrice:N0}</td>
                    <td style="padding:8px;border:1px solid #e5e7eb;text-align:right;">{item.Subtotal:N0}</td>
                    </tr>
                    """);
			}

			return $"""
                    <!doctype html>
                    <html>
                      <head>
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width,initial-scale=1">
                        <title>Invoice {invoice.OrderId}</title>
                      </head>
                      <body style="font-family:Arial,Helvetica,sans-serif;background:#f7f7f7;padding:20px;">
                        <table width="100%" cellpadding="0" cellspacing="0"><tr><td align="center">
                          <table width="700" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;padding:24px;">
                            <tr><td><h2 style="margin:0 0 12px;">PerfumeGPT Invoice</h2></td></tr>
                            <tr><td style="padding-bottom:16px;color:#374151;">Order ID: <b>{invoice.OrderId}</b><br/>Date: {invoice.OrderDate:yyyy-MM-dd HH:mm:ss}<br/>Customer: {WebUtility.HtmlEncode(invoice.CustomerName)}<br/>Phone: {WebUtility.HtmlEncode(invoice.RecipientPhone)}<br/>Address: {WebUtility.HtmlEncode(invoice.RecipientAddress)}</td></tr>
                            <tr><td>
                              <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:14px;">
                                <thead><tr style="background:#f3f4f6;"><th style="padding:8px;border:1px solid #e5e7eb;text-align:left;">Product</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:left;">Variant</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">Qty</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">Unit Price</th><th style="padding:8px;border:1px solid #e5e7eb;text-align:right;">Subtotal</th></tr></thead>
                                <tbody>{itemsHtml}</tbody>
                              </table>
                            </td></tr>
                            <tr><td style="padding-top:16px;text-align:right;color:#111827;">Subtotal: <b>{invoice.Subtotal:N0}</b><br/>Discount: <b>{invoice.Discount:N0}</b><br/>Total: <b>{invoice.Total:N0}</b><br/>Payment method: <b>{WebUtility.HtmlEncode(invoice.PaymentMethod)}</b></td></tr>
                          </table>
                        </td></tr></table>
                      </body>
                    </html>
                    """;
		}

		public string GetRegisterTemplate(string username, string verifyUrl)
		{
			// Basic HTML email template for registration verification
			var safeName = string.IsNullOrWhiteSpace(username) ? "User" : WebUtility.HtmlEncode(username);
			var safeUrl = string.IsNullOrWhiteSpace(verifyUrl) ? string.Empty : WebUtility.HtmlEncode(verifyUrl);

			return $@"<!doctype html>
                <html>
                  <head>
                    <meta charset=""utf-8""> 
                    <meta name=""viewport"" content=""width=device-width,initial-scale=1""> 
                    <title>Welcome to PerfumeGPT</title>
                  </head>
                  <body style=""font-family: Arial,Helvetica,sans-serif; background:#f7f7f7; margin:0; padding:20px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                      <tr>
                        <td align=""center"">
                          <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff; border-radius:8px; overflow:hidden;"">
                            <tr>
                              <td style=""padding:24px; text-align:center; background:#1f2937; color:#ffffff;"">
                                <h1 style=""margin:0; font-size:20px"">Welcome to PerfumeGPT</h1>
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding:24px; color:#111827;"">
                                <p>Hi {safeName},</p>
                                <p>Thanks for creating an account with PerfumeGPT. Please confirm your email address by clicking the button below:</p>
                                <p style=""text-align:center; margin:28px 0;"">
                                  <a href=""{safeUrl}"" style=""display:inline-block; padding:12px 20px; color:#ffffff; background:#4f46e5; border-radius:6px; text-decoration:none;"">Verify email</a>
                                </p>
                                <p>If the button doesn't work, copy and paste the following link into your browser:</p>
                                <p style=""word-break:break-all; font-size:13px; color:#6b7280;"">{safeUrl}</p>
                                <p>Welcome aboard,<br/>The PerfumeGPT Team</p>
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding:12px 24px; font-size:12px; color:#9ca3af; text-align:center;"">If you didn't create an account, you can safely ignore this email.</td>
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
			// Basic HTML email template for forgot password
			var safeName = string.IsNullOrWhiteSpace(username) ? "User" : WebUtility.HtmlEncode(username);
			var safeUrl = string.IsNullOrWhiteSpace(resetUrl) ? string.Empty : WebUtility.HtmlEncode(resetUrl);

			return $@"<!doctype html>
                <html>
                  <head>
                    <meta charset=""utf-8""> 
                    <meta name=""viewport"" content=""width=device-width,initial-scale=1""> 
                    <title>Reset your password</title>
                  </head>
                  <body style=""font-family: Arial,Helvetica,sans-serif; background:#f7f7f7; margin:0; padding:20px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                      <tr>
                        <td align=""center"">
                          <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff; border-radius:8px; overflow:hidden;"">
                            <tr>
                              <td style=""padding:24px; text-align:center; background:#1f2937; color:#ffffff;"">
                                <h1 style=""margin:0; font-size:20px"">Reset your password</h1>
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding:24px; color:#111827;"">
                                <p>Hi {safeName},</p>
                                <p>You recently requested to reset your password for your PerfumeGPT account. Use the button below to reset it. This password reset is only valid for a limited time.</p>
                                <p style=""text-align:center; margin:28px 0;"">
                                  <a href=""{safeUrl}"" style=""display:inline-block; padding:12px 20px; color:#ffffff; background:#4f46e5; border-radius:6px; text-decoration:none;"">Reset password</a>
                                </p>
                                <p>If the button doesn't work, copy and paste the following link into your browser:</p>
                                <p style=""word-break:break-all; font-size:13px; color:#6b7280;"">{safeUrl}</p>
                                <p>Thanks,<br/>The PerfumeGPT Team</p>
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding:12px 24px; font-size:12px; color:#9ca3af; text-align:center;"">If you did not request a password reset, please ignore this email.</td>
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
