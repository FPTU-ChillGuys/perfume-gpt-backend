namespace PerfumeGPT.Application.DTOs.Requests.StorePolicies
{
	public class UpdateStorePolicyRequest
	{
		public decimal RequiredDepositPercentage { get; set; }
		public int DepositTimeoutMinutes { get; set; }
		public bool IsDepositRequiredForCOD { get; set; }
		public int ReviewRewardPoints { get; set; }
		public int StockAdjustmentAutoApprovalThreshold { get; set; }
	}
}
