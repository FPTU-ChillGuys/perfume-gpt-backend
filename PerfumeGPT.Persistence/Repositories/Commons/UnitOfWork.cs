using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Persistence.Contexts;
using Microsoft.SemanticKernel;

namespace PerfumeGPT.Persistence.Repositories.Commons
{
	public class UnitOfWork : BaseUnitOfWork, IUnitOfWork
	{
		private readonly Kernel _kernel;

		public UnitOfWork(PerfumeDbContext context, Kernel kernel) : base(context)
		{
			_kernel = kernel;
		}

		public IScentNoteRepository ScentNotes => GetRepo(ctx => new ScentNoteRepository(ctx));
		public IReviewRepository Reviews => GetRepo(ctx => new ReviewRepository(ctx));
		public IProfileRepository Profiles => GetRepo(ctx => new ProfileRepository(ctx));
		public IOlfactoryFamilyRepository OlfactoryFamilies => GetRepo(ctx => new OlfactoryFamilyRepository(ctx));
		public ICampaignRepository Campaigns => GetRepo(ctx => new CampaignRepository(ctx));
		public IPromotionItemRepository PromotionItems => GetRepo(ctx => new PromotionItemRepository(ctx));
		public IBrandRepository Brands => GetRepo(ctx => new BrandRepository(ctx));
		public IAttributeRepository Attributes => GetRepo(ctx => new AttributeRepository(ctx));
		public IAttributeValueRepository AttributeValues => GetRepo(ctx => new AttributeValueRepository(ctx));
		public IAddressRepository Addresses => GetRepo(ctx => new AddressRepository(ctx));
		public IOrderRepository Orders => GetRepo(ctx => new OrderRepository(ctx));
		public IPaymentRepository Payments => GetRepo(ctx => new PaymentRepository(ctx));
		public ICartItemRepository CartItems => GetRepo(ctx => new CartItemRepository(ctx));
		public IVariantRepository Variants => GetRepo(ctx => new VariantRepository(ctx));
		public IStockRepository Stocks => GetRepo(ctx => new StockRepository(ctx));
		public IImportTicketRepository ImportTickets => GetRepo(ctx => new ImportTicketRepository(ctx));
		public IImportDetailRepository ImportDetails => GetRepo(ctx => new ImportDetailRepository(ctx));
		public ISupplierRepository Suppliers => GetRepo(ctx => new SupplierRepository(ctx));
		public IBatchRepository Batches => GetRepo(ctx => new BatchRepository(ctx));
		public IRecipientInfoRepository RecipientInfos => GetRepo(ctx => new RecipientInfoRepository(ctx));
		public IShippingInfoRepository ShippingInfos => GetRepo(ctx => new ShippingInfoRepository(ctx));
		public IReceiptRepository Receipts => GetRepo(ctx => new ReceiptRepository(ctx));
		public IVoucherRepository Vouchers => GetRepo(ctx => new VoucherRepository(ctx));
		public IUserVoucherRepository UserVouchers => GetRepo(ctx => new UserVoucherRepository(ctx));
		public IStockAdjustmentRepository StockAdjustments => GetRepo(ctx => new StockAdjustmentRepository(ctx));
		public IStockAdjustmentDetailRepository StockAdjustmentDetails => GetRepo(ctx => new StockAdjustmentDetailRepository(ctx));
		public IStockReservationRepository StockReservations => GetRepo(ctx => new StockReservationRepository(ctx));
		public ITemporaryMediaRepository TemporaryMedia => GetRepo(ctx => new TemporaryMediaRepository(ctx));
		public IMediaRepository Media => GetRepo(ctx => new MediaRepository(ctx));
		public ILoyaltyTransactionRepository LoyaltyTransactions => GetRepo(ctx => new LoyaltyTransactionRepository(ctx));
		public IOrderCancelRequestRepository OrderCancelRequests => GetRepo(ctx => new OrderCancelRequestRepository(ctx));
		public IConcentrationRepository Concentrations => GetRepo(ctx => new ConcentrationRepository(ctx));
		public ICategoryRepository Categories => GetRepo(ctx => new CategoryRepository(ctx));
		public IProductRepository Products => GetRepo(ctx => new ProductRepository(ctx, _kernel));
	}
}
