namespace PerfumeGPT.Application.DTOs.Responses.StorePolicies
{
	public class StorePolicyResponse
	{
		public Guid Id { get; set; }
		public decimal RequiredDepositPercentage { get; set; }
		public int DepositTimeoutMinutes { get; set; }
		public bool IsDepositRequiredForCOD { get; set; }
		public int ReviewRewardPoints { get; set; }
		public int StockAdjustmentAutoApprovalThreshold { get; set; }
	}
}
