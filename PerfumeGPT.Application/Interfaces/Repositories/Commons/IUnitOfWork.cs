namespace PerfumeGPT.Application.Interfaces.Repositories.Commons
{
	public interface IUnitOfWork : IDisposable, IAsyncDisposable
	{
	IPaymentRepository Payments { get; }
	IOrderRepository Orders { get; }
	ICartRepository Carts { get; }
	ICartItemRepository CartItems { get; }
	IVariantRepository Variants { get; }
	IStockRepository Stocks { get; }
	IImportTicketRepository ImportTickets { get; }
	IImportDetailRepository ImportDetails { get; }
	ISupplierRepository Suppliers { get; }
	IBatchRepository Batches { get; }
	IRecipientInfoRepository RecipientInfos { get; }
	IShippingInfoRepository ShippingInfos { get; }
	IReceiptRepository Receipts { get; }
	IVoucherRepository Vouchers { get; }
	IUserVoucherRepository UserVouchers { get; }

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

		/// <summary>
		/// Executes an operation within a transaction using the execution strategy.
		/// This is the recommended approach when using SQL Server retry logic.
		/// </summary>
		Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
	}
}
