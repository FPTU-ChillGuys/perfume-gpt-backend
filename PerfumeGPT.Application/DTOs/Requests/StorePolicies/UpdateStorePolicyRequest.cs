namespace PerfumeGPT.Application.DTOs.Requests.StorePolicies
{
	public record UpdateStorePolicyRequest
	{
		public decimal RequiredDepositPercentage { get; init; }
		public int DepositTimeoutMinutes { get; init; }
		public bool IsDepositRequiredForCOD { get; init; }
		public int ReviewRewardPoints { get; init; }
		public int StockAdjustmentAutoApprovalThreshold { get; init; }
		public int OrderRewardPointsInDays { get; init; }
		public int BatchExpiringSoonThresholdInDays { get; init; }
		public int ReturnOrderAllowanceInDays { get; init; }
		public int MaxAddressesPerUser { get; init; }
	}
}
