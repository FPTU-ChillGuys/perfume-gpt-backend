namespace PerfumeGPT.Application.Interfaces.Repositories.Commons
{
	public interface IUnitOfWork : IBaseUnitOfWork
	{
		IUserRepository Users { get; }
		INotificationRepository Notifications { get; }
		IScentNoteRepository ScentNotes { get; }
		IReviewRepository Reviews { get; }
		IProfileRepository Profiles { get; }
		IOlfactoryFamilyRepository OlfactoryFamilies { get; }
		ICampaignRepository Campaigns { get; }
		IBannerRepository Banners { get; }
		IConcentrationRepository Concentrations { get; }
		ICategoryRepository Categories { get; }
		IProductRepository Products { get; }
		IPromotionItemRepository PromotionItems { get; }
		IBrandRepository Brands { get; }
		IAttributeRepository Attributes { get; }
		IAttributeValueRepository AttributeValues { get; }
		IAddressRepository Addresses { get; }
		IPaymentRepository Payments { get; }
		IOrderRepository Orders { get; }
		ICartItemRepository CartItems { get; }
		IVariantRepository Variants { get; }
        IVariantSupplierRepository VariantSuppliers { get; }
		IStockRepository Stocks { get; }
		IImportTicketRepository ImportTickets { get; }
		IImportDetailRepository ImportDetails { get; }
		ISupplierRepository Suppliers { get; }
		IBatchRepository Batches { get; }
		ICashFlowLedgerRepository CashFlowLedgers { get; }
		IInventoryLedgerRepository InventoryLedgers { get; }
		IContactAddressRepository ContactAddresses { get; }
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
		IOrderReturnRequestRepository OrderReturnRequests { get; }
		ISystemPolicyRepository SystemPolicyRepository { get; }
	}
}
