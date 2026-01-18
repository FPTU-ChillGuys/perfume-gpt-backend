using PerfumeGPT.Application.Interfaces.ThirdParties;
using System.Net;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
    public class EmailTemplateService : IEmailTemplateService
    {
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
    }
}
