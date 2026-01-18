using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Persistence.Contexts;

namespace PerfumeGPT.Persistence.Repositories.Commons
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PerfumeDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _transaction;

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

        // Dispose / async dispose - does not dispose the DbContext if it is managed by DI container,
        // but will clear repository cache and ensure transaction disposed.
        public void Dispose()
        {
            // best-effort synchronous dispose for transaction
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
            _repositories.Clear();
            // Do NOT dispose _context here when context is managed by DI container.
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
