namespace PerfumeGPT.Domain.Commons.Audits
{
    public interface IFullAuditable : ICreationAuditable
    {
        DateTime? UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }
    }
}
