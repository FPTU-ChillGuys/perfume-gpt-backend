namespace PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs
{
	public interface IBannerStartAppService
	{
		Task MarkBannerAsStartedAsync(Guid bannerId);
	}
}
