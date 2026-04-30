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
		public int OrderRewardPointsInDays { get; private set; }
		public int BatchExpiringSoonThresholdInDays { get; private set; }
		public int ReturnOrderAllowanceInDays { get; private set; }
		public int MaxAddressesPerUser { get; private set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		public static StorePolicy Create(
			Guid id,
			decimal percentage,
			int timeoutMinutes,
			bool isRequired,
			int reviewRewardPoints,
			int stockAdjustmentAutoApprovalThreshold,
			int orderRewardPointsInDays,
			int batchExpiringSoonThresholdInDays,
			int returnOrderAllowanceInDays,
			int maxAddressesPerUser)
		{
			var policy = new StorePolicy
			{
				Id = id,
			};
			policy.UpdateBatchExpiringSoonPolicy(batchExpiringSoonThresholdInDays);
			policy.UpdateReviewPolicy(reviewRewardPoints);
			policy.UpdateStockAdjustmentPolicy(stockAdjustmentAutoApprovalThreshold);
			policy.UpdateOrderRewardPointsPolicy(orderRewardPointsInDays);
			policy.UpdateDepositPolicy(percentage, timeoutMinutes, isRequired);
			policy.UpdateReturnPolicy(returnOrderAllowanceInDays);
			policy.UpdateAddressPolicy(maxAddressesPerUser);
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

		public void UpdateOrderRewardPointsPolicy(int orderRewardPointsInDays)
		{
			if (orderRewardPointsInDays < 0)
				throw DomainException.BadRequest("Số ngày tính điểm thưởng đơn hàng không hợp lệ.");
			OrderRewardPointsInDays = orderRewardPointsInDays;
		}

		public void UpdateBatchExpiringSoonPolicy(int batchExpiringSoonThresholdInDays)
		{
			if (batchExpiringSoonThresholdInDays < 0)
				throw DomainException.BadRequest("Số ngày để xác định lô hàng sắp hết hạn không hợp lệ.");
			BatchExpiringSoonThresholdInDays = batchExpiringSoonThresholdInDays;
		}

		public void UpdateReturnPolicy(int returnOrderAllowanceInDays)
		{
			if (returnOrderAllowanceInDays < 0)
				throw DomainException.BadRequest("Số ngày cho phép trả hàng không hợp lệ.");
			ReturnOrderAllowanceInDays = returnOrderAllowanceInDays;
		}

		public void UpdateAddressPolicy(int maxAddressesPerUser)
		{
			if (maxAddressesPerUser <= 0)
				throw DomainException.BadRequest("Số lượng địa chỉ tối đa mỗi người dùng phải lớn hơn 0.");
			MaxAddressesPerUser = maxAddressesPerUser;
		}
	}
}