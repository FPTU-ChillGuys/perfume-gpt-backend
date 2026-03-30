namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IInvoiceEmailJobScheduler
	{
		void EnqueueSendInvoiceEmail(Guid orderId);
	}
}
