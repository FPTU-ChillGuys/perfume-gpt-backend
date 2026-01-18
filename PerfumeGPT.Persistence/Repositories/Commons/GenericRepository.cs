using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Persistence.Contexts;
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Repositories.Commons
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly PerfumeDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(PerfumeDbContext context) : base()
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities is null) throw new ArgumentNullException(nameof(entities));
            await _dbSet.AddRangeAsync(entities);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));
            return await _dbSet.AnyAsync(predicate);
        }

        public void Attach(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Attach(entity);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            if (filter is null)
                return await _dbSet.CountAsync();
            return await _dbSet.CountAsync(filter);
        }

        public void Detach(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            _context.Entry(entity).State = EntityState.Detached;
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool asNoTracking = false)
        {
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            IQueryable<T> query = _dbSet;

            if (include is not null)
                query = include(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool asNoTracking = false)
        {
            IQueryable<T> query = _dbSet;

            if (filter is not null)
                query = query.Where(filter);

            if (include is not null)
                query = include(query);

            if (orderBy is not null)
                query = orderBy(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync();
        }

        public async Task<T?> GetByConditionAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool asNoTracking = false)
        {
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            IQueryable<T> query = _dbSet;

            if (include is not null)
                query = include(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<T?> GetByIdAsync(params object[] keyValues)
        {
            if (keyValues is null || keyValues.Length == 0) throw new ArgumentException("Key values must be provided.", nameof(keyValues));
            var found = await _dbSet.FindAsync(keyValues);
            return found;
        }

        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int pageNumber = 1, int pageSize = 10, bool asNoTracking = false)
        {
            IQueryable<T> query = _dbSet;

            if (filter is not null)
                query = query.Where(filter);

            if (include is not null)
                query = include(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            if (orderBy is not null)
                query = orderBy(query);

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public IQueryable<T> Query(bool asNoTracking = false)
        {
            return asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
        }

        public void Remove(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            if (entities is null) throw new ArgumentNullException(nameof(entities));
            _dbSet.RemoveRange(entities);
        }

        public async Task<int> SaveChangesAndReturnCountAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool asNoTracking = false)
        {
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));

            IQueryable<T> query = _dbSet;

            if (include is not null)
                query = include(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.SingleOrDefaultAsync(predicate);
        }

        public void Update(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Update(entity);
        }

        public async Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            if (entities is null) throw new ArgumentNullException(nameof(entities));
            _dbSet.UpdateRange(entities);
            await Task.CompletedTask;
        }
    }
}
