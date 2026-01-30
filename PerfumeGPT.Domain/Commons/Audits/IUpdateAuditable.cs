namespace PerfumeGPT.Domain.Commons.Audits
{
	public interface IUpdateAuditable
	{
		DateTime? UpdatedAt { get; set; }
		string? UpdatedBy { get; set; }
	}
}
