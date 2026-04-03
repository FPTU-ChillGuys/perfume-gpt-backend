namespace PerfumeGPT.Domain.Enums
{
	public enum OrderStatus
	{
		Pending = 1,
		Processing,
		Delivering,
		Delivered,
		Returning,
		Cancelled,
		Partial_Returned,
		Returned
	}
}
