namespace PerfumeGPT.Application.Interfaces.Repositories.Commons
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Save changes and return true if one or more rows were affected.
        /// </summary>
        Task<bool> SaveChangesAsync();

        /// <summary>
        /// Save changes and return number of affected rows.
        /// </summary>
        Task<int> SaveChangesAndReturnCountAsync();

        /// <summary>
        /// Transaction helpers.
        /// </summary>
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
