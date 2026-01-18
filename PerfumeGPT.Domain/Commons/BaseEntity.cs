namespace PerfumeGPT.Domain.Commons
{
    public abstract class BaseEntity<TKey>
    {
        public TKey Id { get; protected set; } = default!;
    }
}
