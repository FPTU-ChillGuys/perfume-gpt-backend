namespace PerfumeGPT.Domain.Commons.Events
{
	public interface IHasDomainEvents
	{
		IReadOnlyList<IDomainEvent> DomainEvents { get; }
		void AddDomainEvent(IDomainEvent domainEvent);
		void ClearDomainEvents();
	}
}
