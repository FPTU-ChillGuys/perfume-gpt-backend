namespace PerfumeGPT.Domain.Commons.Audits
{
    public interface ICreationAuditable : IHasCreatedAt
    {
        string? CreatedBy { get; set; }
    }
}
