using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Services.Helpers;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class LowStockAlertJob
	{
		private readonly IStockRepository _stockRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
		private readonly IEmailService _emailService;
		private readonly IEmailTemplateService _emailTemplateService;
		private readonly ILogger<LowStockAlertJob> _logger;

		public LowStockAlertJob(
			IStockRepository stockRepository,
			IUnitOfWork unitOfWork,
			IUserRepository userRepository,
			IEmailService emailService,
			IEmailTemplateService emailTemplateService,
			ILogger<LowStockAlertJob> logger)
		{
			_stockRepository = stockRepository;
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
			_emailService = emailService;
			_emailTemplateService = emailTemplateService;
			_logger = logger;
		}

		public async Task SendLowStockAlertToAdminsAsync()
		{
			try
			{
				var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
				var lowStockItems = await _stockRepository.GetLowStockAlertItemsAsync(sellable);
				if (lowStockItems.Count == 0)
				{
					_logger.LogInformation("No low stock items found.");
					return;
				}

				var adminEmails = await _userRepository.GetActiveAdminEmailsAsync();
				if (adminEmails.Count == 0)
				{
					_logger.LogWarning("Low stock items detected but no active admin emails found.");
					return;
				}

				var subject = $"[PerfumeGPT] Cảnh báo tồn kho thấp ({lowStockItems.Count} phân loại)";
				var body = _emailTemplateService.GetLowStockAlertTemplate(lowStockItems, DateTime.UtcNow);

				foreach (var adminEmail in adminEmails)
				{
					await _emailService.SendEmailAsync(adminEmail, subject, body);
				}

				_logger.LogInformation("Low stock alert email sent to {AdminCount} admins for {ItemCount} variants.", adminEmails.Count, lowStockItems.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while sending low stock alert email.");
				throw;
			}
		}
	}
}
