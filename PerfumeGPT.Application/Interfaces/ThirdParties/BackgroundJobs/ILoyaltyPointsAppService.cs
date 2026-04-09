namespace PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs
{
	public interface ILoyaltyPointsAppService
	{
		Task GrantPointsIfEligibleAsync(Guid orderId);
	}
}
