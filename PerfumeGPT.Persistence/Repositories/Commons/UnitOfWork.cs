using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Persistence.Contexts;

namespace PerfumeGPT.Persistence.Repositories.Commons
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly PerfumeDbContext _context;
		private readonly Dictionary<Type, object> _repositories = new();
		private IDbContextTransaction? _transaction;

		public IOrderRepository Orders
		{
			get
			{
				var key = typeof(IOrderRepository);
				if (!_repositories.ContainsKey(key))
				{
					var repository = new OrderRepository(_context);
					_repositories[key] = repository;
				}
				return (IOrderRepository)_repositories[key];
			}
		}

		public IPaymentRepository Payments
		{
			get
			{
				var key = typeof(IPaymentRepository);
				if (!_repositories.TryGetValue(key, out var repo))
				{
					repo = new PaymentRepository(_context);
					_repositories[key] = repo!;
				}
				return (IPaymentRepository)repo!;
			}
		}

		public ICartRepository Carts
		{
			get
			{
				var key = typeof(ICartRepository);
				if (!_repositories.TryGetValue(key, out var repo))
				{
					repo = new CartRepository(_context);
					_repositories[key] = repo!;
				}
				return (ICartRepository)repo!;
			}
		}

		public UnitOfWork(PerfumeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
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
				catch (DbUpdateConcurrencyException ex)
				{
					attempts++;
					if (attempts >= 3)
					{
						// Give up after several retries
						return false;
					}

					// Try to resolve concurrency by refreshing the entries from the database
					foreach (var entry in ex.Entries)
					{
						try
						{
							await entry.ReloadAsync();
						}
						catch
						{
							// If reload fails, continue to next entry; we'll retry the save
						}
					}
					// Small delay before retrying
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
			if (_transaction != null)
			{
				_transaction.Dispose();
				_transaction = null;
			}
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
