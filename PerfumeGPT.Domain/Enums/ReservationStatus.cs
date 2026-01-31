namespace PerfumeGPT.Domain.Enums
{
	public enum ReservationStatus
	{
		Reserved = 1,   // Stock is reserved, waiting for payment
		Committed,      // Payment successful, stock deducted
		Released        // Reservation released (expired or cancelled)
	}
}
