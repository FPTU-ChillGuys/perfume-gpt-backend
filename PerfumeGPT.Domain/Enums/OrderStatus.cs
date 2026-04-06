namespace PerfumeGPT.Domain.Enums
{
	public enum OrderStatus
	{
		Pending = 1,
		Preparing,
		ReadyToPick,
		Delivering,
		Delivered,
		Returning,
		Cancelled,
		Partial_Returned,
		Returned
	}
}
