using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Persistence.Contexts;

namespace PerfumeGPT.Persistence.Repositories.Commons
{
	public abstract class BaseUnitOfWork : IBaseUnitOfWork
	{
		protected readonly PerfumeDbContext _context;
		private readonly Dictionary<Type, object> _repositories = [];
		private IDbContextTransaction? _transaction;

		protected BaseUnitOfWork(PerfumeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		protected T GetRepo<T>(Func<PerfumeDbContext, T> factory) where T : class
		{
			var type = typeof(T);
			if (!_repositories.TryGetValue(type, out var repo))
			{
				repo = factory(_context);
				_repositories[type] = repo;
			}
			return (T)repo;
		}

		public async Task<bool> SaveChangesAsync()
		{
			int attempts = 0;
			while (true)
			{
				try
				{
					return await _context.SaveChangesAsync() > 0;
				}
				catch (DbUpdateConcurrencyException)
				{
					attempts++;
					if (attempts >= 3) return false;
					await Task.Delay(50);
				}
			}
		}

		public async Task<int> SaveChangesAndReturnCountAsync()
		{
			return await _context.SaveChangesAsync();
		}

		public async Task BeginTransactionAsync()
		{
			if (_transaction != null) return;
			_transaction = await _context.Database.BeginTransactionAsync();
		}

		public async Task CommitTransactionAsync()
		{
			if (_transaction == null) return;
			try
			{
				await _context.SaveChangesAsync();
				await _transaction.CommitAsync();
			}
			finally
			{
				await _transaction.DisposeAsync();
				_transaction = null;
			}
		}

		public async Task RollbackTransactionAsync()
		{
			if (_transaction == null) return;
			try
			{
				await _transaction.RollbackAsync();
			}
			finally
			{
				await _transaction.DisposeAsync();
				_transaction = null;
			}
		}

		public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
		{
			var strategy = _context.Database.CreateExecutionStrategy();
			return await strategy.ExecuteAsync(async () =>
			{
				await using var transaction = await _context.Database.BeginTransactionAsync();
				try
				{
					var result = await operation();
					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
					return result;
				}
				catch
				{
					await transaction.RollbackAsync();
					throw;
				}
			});
		}

		public void Dispose()
		{
			_transaction?.Dispose();
			_transaction = null;
			_repositories.Clear();
			GC.SuppressFinalize(this);
		}

		public async ValueTask DisposeAsync()
		{
			if (_transaction != null)
			{
				await _transaction.DisposeAsync();
				_transaction = null;
			}
			_repositories.Clear();
			GC.SuppressFinalize(this);
		}
	}
}
