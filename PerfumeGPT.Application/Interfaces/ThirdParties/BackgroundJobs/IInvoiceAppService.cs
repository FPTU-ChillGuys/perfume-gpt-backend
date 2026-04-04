namespace PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs
{
	public interface IInvoiceAppService
	{
		Task SendInvoiceEmailIfNeededAsync(Guid orderId);
	}
}
