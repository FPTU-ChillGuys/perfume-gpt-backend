namespace PerfumeGPT.Domain.Commons.Audits
{
	public interface IAuditScope
	{
		bool IsSystemAction { get; }
		IDisposable BeginSystemAction();
	}
}
