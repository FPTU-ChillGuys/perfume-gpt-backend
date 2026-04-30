using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class StorePolicy : BaseEntity<Guid>, IHasTimestamps
	{
		private StorePolicy() { }

		public decimal RequiredDepositPercentage { get; private set; }
		public int DepositTimeoutMinutes { get; private set; }
		public bool IsDepositRequiredForCOD { get; private set; }
		public int ReviewRewardPoints { get; private set; }
		public int StockAdjustmentAutoApprovalThreshold { get; private set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		public static StorePolicy Create(Guid id, decimal percentage, int timeoutMinutes, bool isRequired, int reviewRewardPoints, int stockAdjustmentAutoApprovalThreshold)
		{
			var policy = new StorePolicy
			{
				Id = id,
				ReviewRewardPoints = reviewRewardPoints,
				StockAdjustmentAutoApprovalThreshold = stockAdjustmentAutoApprovalThreshold
			};
			policy.UpdateDepositPolicy(percentage, timeoutMinutes, isRequired);
			return policy;
		}

		public void UpdateDepositPolicy(decimal percentage, int timeoutMinutes, bool isRequired)
		{
			if (percentage < 0 || percentage > 100)
				throw DomainException.BadRequest("Phần trăm cọc không hợp lệ.");

			if (timeoutMinutes < 0)
				throw DomainException.BadRequest("Thời gian hết hạn cọc không hợp lệ.");

			RequiredDepositPercentage = percentage;
			DepositTimeoutMinutes = timeoutMinutes;
			IsDepositRequiredForCOD = isRequired;
		}

		public void UpdateReviewPolicy(int reviewRewardPoints)
		{
			if (reviewRewardPoints < 0)
				throw DomainException.BadRequest("Điểm thưởng đánh giá không hợp lệ.");

			ReviewRewardPoints = reviewRewardPoints;
		}

		public void UpdateStockAdjustmentPolicy(int autoApprovalThreshold)
		{
			if (autoApprovalThreshold < 0)
				throw DomainException.BadRequest("Ngưỡng tự động duyệt điều chỉnh kho không hợp lệ.");

			StockAdjustmentAutoApprovalThreshold = autoApprovalThreshold;
		}
	}
}