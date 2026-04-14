namespace PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs
{
	public interface IBannerEndAppService
	{
		Task MarkBannerAsEndedAsync(Guid bannerId);
	}
}
