using PerfumeGPT.Domain.Commons.Events;

namespace PerfumeGPT.Domain.Events
{
    public record OrderRefusedDomainEvent(Guid UserId, Guid OrderId) : IDomainEvent;
}
