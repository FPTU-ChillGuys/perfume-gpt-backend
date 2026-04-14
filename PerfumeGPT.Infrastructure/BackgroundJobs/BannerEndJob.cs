using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class BannerEndJob : IBannerEndAppService
	{
		private readonly IUnitOfWork _unitOfWork;

		public BannerEndJob(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task MarkBannerAsEndedAsync(Guid bannerId)
		{
			var banner = await _unitOfWork.Banners.GetByIdAsync(bannerId);
			if (banner == null)
			{
				return;
			}

			banner.SetActiveStatus(false);
			_unitOfWork.Banners.Update(banner);
			await _unitOfWork.SaveChangesAsync();
		}
	}
}
