using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class LoyaltyPointService : ILoyaltyPointService
	{
		private readonly IUnitOfWork _unitOfWork;

		public LoyaltyPointService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> CreateLoyaltyPointAsync(Guid userId, bool saveChanges = true)
		{
			if (userId == Guid.Empty)
				return false;

			var existing = await _unitOfWork.LoyaltyPoints.AnyAsync(lp => lp.UserId == userId);
			if (existing)
				return false;

			var entity = new LoyaltyPoint { UserId = userId };

			await _unitOfWork.LoyaltyPoints.AddAsync(entity);

			if (saveChanges)
				return await _unitOfWork.SaveChangesAsync();

			return true;
		}

		public async Task<bool> PlusPointAsync(Guid userId, int points, bool saveChanges = true)
		{
			if (userId == Guid.Empty)
				return false;
			if (points <= 0) return false;

			var existing = await _unitOfWork.LoyaltyPoints.FirstOrDefaultAsync(lp => lp.UserId == userId);
			if (existing == null)
			{
				var newLp = new LoyaltyPoint
				{
					UserId = userId,
					PointBalance = points
				};

				await _unitOfWork.LoyaltyPoints.AddAsync(newLp);

				if (saveChanges)
					return await _unitOfWork.SaveChangesAsync();

				return true;
			}

			existing.PointBalance += points;
			_unitOfWork.LoyaltyPoints.Update(existing);

			if (saveChanges)
				return await _unitOfWork.SaveChangesAsync();

			return true;
		}

		public async Task<bool> RedeemPointAsync(Guid userId, int points, bool saveChanges = true)
		{
			if (userId == Guid.Empty)
				return false;
			if (points <= 0) return false;

			var existing = await _unitOfWork.LoyaltyPoints.FirstOrDefaultAsync(lp => lp.UserId == userId);
			if (existing == null)
				return false;

			if (existing.PointBalance < points)
				return false;

			existing.PointBalance -= points;
			_unitOfWork.LoyaltyPoints.Update(existing);

			if (saveChanges)
				return await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
