namespace PerfumeGPT.Domain.Commons.Audits
{
    public interface IHasTimestamps : IHasCreatedAt
    {
        DateTime? UpdatedAt { get; set; }
    }
}
