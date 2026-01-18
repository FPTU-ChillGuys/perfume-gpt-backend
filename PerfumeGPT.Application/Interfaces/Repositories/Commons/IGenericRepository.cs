using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace PerfumeGPT.Application.Interfaces.Repositories.Commons
{
    public interface IGenericRepository<T> where T : class
    {
        // --- Read ---
        Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool asNoTracking = false
        );

        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int pageNumber = 1,
            int pageSize = 10,
            bool asNoTracking = false
        );

        /// <summary>
        /// Returns a single entity that matches the predicate or null. (Existing name kept for compatibility.)
        /// Consider renaming to FirstOrDefaultAsync for clarity in a future non-breaking refactor.
        /// </summary>
        Task<T?> GetByConditionAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
            bool asNoTracking = false
        );

        /// <summary>
        /// Find by primary key values. Works like DbSet.FindAsync(params object[]).
        /// Useful when entities use single or composite keys.
        /// </summary>
        Task<T?> GetByIdAsync(params object[] keyValues);

        /// <summary>
        /// Returns the first element that satisfies the predicate or null.
        /// More explicit alternative to GetByConditionAsync when only the first match is desired.
        /// </summary>
        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
            bool asNoTracking = false
        );

        /// <summary>
        /// Returns a single element that satisfies the predicate or throws if multiple found.
        /// Use only when uniqueness is expected.
        /// </summary>
        Task<T?> SingleOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
            bool asNoTracking = false
        );

        /// <summary>
        /// Exposes an IQueryable for advanced queries. Use with caution in application layer.
        /// </summary>
        IQueryable<T> Query(bool asNoTracking = false);

        // --- Existence / Count ---
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        // --- Create ---
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);

        // --- Update ---
        void Update(T entity);
        Task UpdateRangeAsync(IEnumerable<T> entities);

        // --- Delete ---
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        // --- Attach / Detach helpers (can be useful in unit-of-work contexts) ---
        void Attach(T entity);
        void Detach(T entity);

        // --- Save ---
        /// <summary>
        /// Saves changes and returns true if one or more records were affected.
        /// (Kept for backward compatibility.)
        /// </summary>
        Task<bool> SaveChangesAsync();

        /// <summary>
        /// Saves changes and returns the number of state entries written to the store.
        /// Useful when callers need the affected-row count.
        /// </summary>
        Task<int> SaveChangesAndReturnCountAsync();
    }
}

