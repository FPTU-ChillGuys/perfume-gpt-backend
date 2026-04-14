using PerfumeGPT.Domain.Commons.Events;

namespace PerfumeGPT.Domain.Commons
{
	public abstract class BaseEntity<TKey> : IHasDomainEvents
	{
		private readonly List<IDomainEvent> _domainEvents = [];

		public TKey Id { get; protected set; } = default!;

		public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

		public void AddDomainEvent(IDomainEvent domainEvent)
		{
			ArgumentNullException.ThrowIfNull(domainEvent);

			_domainEvents.Add(domainEvent);
		}

		public void ClearDomainEvents()
		{
			_domainEvents.Clear();
		}
	}
}
