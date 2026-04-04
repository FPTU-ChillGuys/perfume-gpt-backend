using Hangfire;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using System.Linq.Expressions;

namespace PerfumeGPT.Infrastructure.BackgroundJobs.Commons
{
	public class HangfireBackgroundJobService : IBackgroundJobService
	{
		private readonly IBackgroundJobClient _backgroundJobClient;

		public HangfireBackgroundJobService(IBackgroundJobClient backgroundJobClient)
		{
			_backgroundJobClient = backgroundJobClient;
		}

		public string Enqueue<T>(Expression<Func<T, Task>> methodCall) =>
			_backgroundJobClient.Enqueue(methodCall);

		public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTime scheduledAt) =>
			_backgroundJobClient.Schedule(methodCall, scheduledAt);

		public bool Delete(string jobId) =>
			_backgroundJobClient.Delete(jobId);
	}
}
