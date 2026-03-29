namespace PerfumeGPT.Domain.Enums
{
	public enum ReturnRequestStatus
	{
		Pending = 1,
		ApprovedForReturn,
		Inspecting,
		ReadyForRefund,
		Completed,
		Rejected
	}
}
