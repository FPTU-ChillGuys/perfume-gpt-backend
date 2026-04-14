using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class BannerStartJob : IBannerStartAppService
	{
		private readonly IUnitOfWork _unitOfWork;

		public BannerStartJob(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task MarkBannerAsStartedAsync(Guid bannerId)
		{
			var banner = await _unitOfWork.Banners.GetByIdAsync(bannerId);
			if (banner == null)
			{
				return;
			}

			banner.SetActiveStatus(true);
			_unitOfWork.Banners.Update(banner);
			await _unitOfWork.SaveChangesAsync();
		}
	}
}
