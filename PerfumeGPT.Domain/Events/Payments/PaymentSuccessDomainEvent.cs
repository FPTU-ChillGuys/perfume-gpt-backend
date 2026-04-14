using PerfumeGPT.Domain.Commons.Events;

namespace PerfumeGPT.Domain.Events.Payments
{
	public sealed record PaymentSuccessDomainEvent(Guid OrderId, Guid PaymentTransactionId) : IDomainEvent;
}
