using Hangfire;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Infrastructure.BackgroundJobs.Schedulers
{
	public class InvoiceEmailJobScheduler : IInvoiceEmailJobScheduler
	{
		private readonly IBackgroundJobClient _backgroundJobClient;

		public InvoiceEmailJobScheduler(IBackgroundJobClient backgroundJobClient)
		{
			_backgroundJobClient = backgroundJobClient;
		}

		public void EnqueueSendInvoiceEmail(Guid orderId)
		{
			_backgroundJobClient.Enqueue<InvoiceEmailJob>(job => job.SendInvoiceEmailIfNeededAsync(orderId));
		}
	}
}
