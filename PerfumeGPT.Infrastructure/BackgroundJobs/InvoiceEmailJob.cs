using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class InvoiceEmailJob : IInvoiceAppService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailService _emailService;
		private readonly IEmailTemplateService _emailTemplateService;

		public InvoiceEmailJob(
			IUnitOfWork unitOfWork,
			IEmailService emailService,
			IEmailTemplateService emailTemplateService)
		{
			_unitOfWork = unitOfWork;
			_emailService = emailService;
			_emailTemplateService = emailTemplateService;
		}

		public async Task SendInvoiceEmailIfNeededAsync(Guid orderId)
		{
			var payload = await _unitOfWork.Orders.GetOnlineOrderInvoiceEmailPayloadAsync(orderId);
			if (!payload.HasValue)
			{
				return;
			}

			var (customerEmail, invoice, orderCode) = payload.Value;
			if (string.IsNullOrWhiteSpace(customerEmail))
			{
				return;
			}

			var subject = $"Hóa đơn PerfumeGPT - Đơn hàng {orderCode}";
			var body = _emailTemplateService.GetInvoiceTemplate(invoice);
			await _emailService.SendEmailAsync(customerEmail, subject, body);
		}
	}
}
