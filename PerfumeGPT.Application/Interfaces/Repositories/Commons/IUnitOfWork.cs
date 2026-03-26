namespace PerfumeGPT.Application.Interfaces.Repositories.Commons
{
	public interface IUnitOfWork : IBaseUnitOfWork
	{
		IAttributeRepository Attributes { get; }
		IAttributeValueRepository AttributeValues { get; }
		IAddressRepository Addresses { get; }
		IPaymentRepository Payments { get; }
		IOrderRepository Orders { get; }
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
		IStockAdjustmentRepository StockAdjustments { get; }
		IStockAdjustmentDetailRepository StockAdjustmentDetails { get; }
		IStockReservationRepository StockReservations { get; }
		ITemporaryMediaRepository TemporaryMedia { get; }
		IMediaRepository Media { get; }
		ILoyaltyTransactionRepository LoyaltyTransactions { get; }
		IOrderCancelRequestRepository OrderCancelRequests { get; }
	}
}
