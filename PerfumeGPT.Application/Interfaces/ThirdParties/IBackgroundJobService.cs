using System.Linq.Expressions;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IBackgroundJobService
	{
		string Enqueue<T>(Expression<Func<T, Task>> methodCall);

		string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTime scheduledAt);

		bool Delete(string jobId);
	}
}
