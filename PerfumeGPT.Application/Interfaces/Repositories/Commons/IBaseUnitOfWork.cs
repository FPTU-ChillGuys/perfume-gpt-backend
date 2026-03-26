namespace PerfumeGPT.Application.Interfaces.Repositories.Commons
{
	public interface IBaseUnitOfWork : IDisposable, IAsyncDisposable
	{
		Task<bool> SaveChangesAsync();
		Task<int> SaveChangesAndReturnCountAsync();
		Task BeginTransactionAsync();
		Task CommitTransactionAsync();
		Task RollbackTransactionAsync();
		Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
	}
}
