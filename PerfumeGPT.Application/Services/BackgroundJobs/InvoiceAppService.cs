using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Application.Services.BackgroundJobs
{
	internal class InvoiceAppService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailService _emailService;
		private readonly IEmailTemplateService _emailTemplateService;

		public InvoiceAppService(
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

			var (customerEmail, invoice) = payload.Value;
			if (string.IsNullOrWhiteSpace(customerEmail))
			{
				return;
			}

			var subject = $"PerfumeGPT Invoice - Order {invoice.OrderId}";
			var body = _emailTemplateService.GetInvoiceTemplate(invoice);
			await _emailService.SendEmailAsync(customerEmail, subject, body);
		}
	}
}
