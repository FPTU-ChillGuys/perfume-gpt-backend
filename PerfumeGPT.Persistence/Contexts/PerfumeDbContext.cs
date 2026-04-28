using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Commons.Events;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Converters;
using System.Security.Claims;
using Attribute = PerfumeGPT.Domain.Entities.Attribute;

namespace PerfumeGPT.Persistence.Contexts
{
	public class PerfumeDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
	{
		private readonly IHttpContextAccessor? _httpContextAccessor;
		private readonly IAuditScope? _auditScope;
		private readonly IEncryptionProvider? _encryptionProvider;
		private readonly IPublisher? _publisher;

		public PerfumeDbContext(
			DbContextOptions<PerfumeDbContext> options,
			IPublisher? publisher = null,
			IHttpContextAccessor? httpContextAccessor = null,
			IAuditScope? auditScope = null,
			IEncryptionProvider? encryptionProvider = null)
			: base(options)
		{
			_publisher = publisher;
			_httpContextAccessor = httpContextAccessor;
			_auditScope = auditScope;
			_encryptionProvider = encryptionProvider;
		}

		// Current user identifier for auditing (set externally, e.g. in services)
		public string? CurrentUserId
		{
			get
			{
				// If a system action is in progress, return "system"
				if (_auditScope?.IsSystemAction == true)
				{
					return "system";
				}

				// Try to get user ID from HTTP context
				var userId = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
						  ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value;

				return userId ?? "system";
			}
			set { } // Keep setter for backward compatibility
		}

		private void ApplyAuditRules()
		{
			var now = DateTime.UtcNow;

			foreach (var entry in ChangeTracker.Entries())
			{
				var entity = entry.Entity;
				if (entity == null) continue;

				// Assign Guid Id for BaseEntity<Guid> when adding
				var idProp = entity.GetType().GetProperty("Id");
				if (entry.State == EntityState.Added && idProp != null && idProp.PropertyType == typeof(Guid))
				{
					var current = (Guid?)idProp.GetValue(entity);
					if (current == null || current == Guid.Empty)
					{
						idProp.SetValue(entity, Guid.NewGuid());
					}
				}

				// Created handling
				if (entry.State == EntityState.Added)
				{
					// Full auditable (created + createdBy)
					if (entity is IFullAuditable fullAud)
					{
						fullAud.CreatedAt = now;
						fullAud.CreatedBy = CurrentUserId;
					}
					// Creation auditable (created + createdBy)
					else if (entity is ICreationAuditable creationAud)
					{
						creationAud.CreatedAt = now;
						creationAud.CreatedBy = CurrentUserId;
					}
					// HasCreatedAt only (created)
					else if (entity is IHasCreatedAt hasCreated)
					{
						hasCreated.CreatedAt = now;
					}
				}

				// Modified handling
				if (entry.State == EntityState.Modified)
				{
					// Full auditable (updated + updatedBy)
					if (entity is IFullAuditable fullAud)
					{
						fullAud.UpdatedAt = now;
						fullAud.UpdatedBy = CurrentUserId;
					}
					// Update auditable (updated + updatedBy)
					else if (entity is IUpdateAuditable updateAud)
					{
						updateAud.UpdatedAt = now;
						updateAud.UpdatedBy = CurrentUserId;
					}
					// HasTimestamps only (updated)
					else if (entity is IHasTimestamps hasTimestamps)
					{
						hasTimestamps.UpdatedAt = now;
					}
				}

				// Soft delete: intercept deletions
				if (entry.State == EntityState.Deleted && entity is ISoftDelete soft)
				{
					soft.IsDeleted = true;
					soft.DeletedAt = now;

					// Propagate updated timestamp/actor for soft deletes when applicable
					if (entity is IFullAuditable fullAud)
					{
						fullAud.UpdatedAt = now;
						fullAud.UpdatedBy = CurrentUserId;
					}
					else if (entity is IHasTimestamps hasTimestamps)
					{
						hasTimestamps.UpdatedAt = now;
					}

					entry.State = EntityState.Modified;
				}
			}
		}

		public override int SaveChanges()
		{
			ApplyAuditRules();
			return base.SaveChanges();
		}

		public override int SaveChanges(bool acceptAllChangesOnSuccess)
		{
			ApplyAuditRules();
			return base.SaveChanges(acceptAllChangesOnSuccess);
		}

		public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			ApplyAuditRules();
			await DispatchDomainEventsAsync(cancellationToken);
			return await base.SaveChangesAsync(cancellationToken);
		}

		public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
		{
			ApplyAuditRules();
			await DispatchDomainEventsAsync(cancellationToken);
			return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		}

		private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
		{
			if (_publisher is null)
			{
				return;
			}

			var domainEntities = ChangeTracker
				.Entries<IHasDomainEvents>()
				.Where(entry => entry.Entity.DomainEvents.Count > 0)
				.Select(entry => entry.Entity)
				.ToList();

			if (domainEntities.Count == 0)
			{
				return;
			}

			var domainEvents = domainEntities
				.SelectMany(entity => entity.DomainEvents)
				.ToList();

			foreach (var entity in domainEntities)
			{
				entity.ClearDomainEvents();
			}

			foreach (var domainEvent in domainEvents)
			{
				await _publisher.Publish(domainEvent, cancellationToken);
			}
		}

		// DbSets
		public DbSet<CustomerProfile> CustomerProfiles { get; set; }
		public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
		public DbSet<Address> Addresses { get; set; }
		public DbSet<ImportTicket> ImportTickets { get; set; }
		public DbSet<ImportDetail> ImportDetails { get; set; }
		public DbSet<Supplier> Suppliers { get; set; }
		public DbSet<VariantSupplier> VariantSuppliers { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<ProductVariant> ProductVariants { get; set; }
		public DbSet<Attribute> Attributes { get; set; }
		public DbSet<AttributeValue> AttributeValues { get; set; }
		public DbSet<ProductAttribute> ProductAttributes { get; set; }
		public DbSet<Brand> Brands { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<Concentration> Concentrations { get; set; }
		public DbSet<Batch> Batches { get; set; }
		public DbSet<Stock> Stocks { get; set; }
		public DbSet<CashFlowLedger> CashFlowLedgers { get; set; }
		public DbSet<InventoryLedger> InventoryLedgers { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderDetail> OrderDetails { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<UserNotificationRead> UserNotificationReads { get; set; }
		public DbSet<CartItem> CartItems { get; set; }
		public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
		public DbSet<Receipt> Receipts { get; set; }
		public DbSet<Voucher> Vouchers { get; set; }
		public DbSet<UserVoucher> UserVouchers { get; set; }
		public DbSet<ShippingInfo> ShippingInfos { get; set; }
		public DbSet<ContactAddress> ContactAddresses { get; set; }
		public DbSet<Media> Media { get; set; }
		public DbSet<StockAdjustment> StockAdjustments { get; set; }
		public DbSet<StockAdjustmentDetail> StockAdjustmentDetails { get; set; }
		public DbSet<StockReservation> StockReservations { get; set; }
		public DbSet<Review> Reviews { get; set; }
		public DbSet<TemporaryMedia> TemporaryMedia { get; set; }
		public DbSet<OlfactoryFamily> OlfactoryFamilies { get; set; }
		public DbSet<ScentNote> ScentNotes { get; set; }
		public DbSet<ProductNoteMap> ProductNoteMaps { get; set; }
		public DbSet<ProductFamilyMap> ProductFamilyMaps { get; set; }
		public DbSet<CustomerNotePreference> CustomerNotePreferences { get; set; }
		public DbSet<CustomerFamilyPreference> CustomerFamilyPreferences { get; set; }
		public DbSet<CustomerAttributePreference> CustomerAttributePreferences { get; set; }
		public DbSet<OrderCancelRequest> OrderCancelRequests { get; set; }
		public DbSet<OrderReturnRequest> OrderReturnRequests { get; set; }
		public DbSet<OrderReturnRequestDetail> OrderReturnRequestDetails { get; set; }
		public DbSet<PromotionItem> Promotions { get; set; }
		public DbSet<Campaign> Campaigns { get; set; }
		public DbSet<Banner> Banners { get; set; }
		public DbSet<SystemPolicy> SystemPolicies { get; set; }
		public DbSet<StorePolicy> StorePolicies { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			var encryptionProvider = _encryptionProvider ?? new PassThroughEncryptionProvider();
			var encryptionConverter = new EncryptionConverter(encryptionProvider);

			// Configure BaseEntity primary keys
			builder.Model.GetEntityTypes()
				.Where(t => typeof(BaseEntity<Guid>).IsAssignableFrom(t.ClrType))
				.ToList()
				.ForEach(t => builder.Entity(t.ClrType).HasKey("Id"));

			// User -> CustomerProfile (1:1)
			builder.Entity<User>()
				.HasOne(u => u.CustomerProfile)
				.WithOne(cp => cp.User)
				.HasForeignKey<CustomerProfile>(cp => cp.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<CustomerProfile>()
				.HasIndex(cp => cp.UserId)
				.IsUnique();

			// CustomerProfile -> CustomerNotes (1:M)
			builder.Entity<CustomerProfile>()
				.HasMany(cp => cp.NotePreferences)
				.WithOne(cn => cn.Profile)
				.HasForeignKey(cn => cn.ProfileId)
				.OnDelete(DeleteBehavior.Cascade);

			// CustomerProfile -> CustomerFamilies (1:M)
			builder.Entity<CustomerProfile>()
				.HasMany(cp => cp.FamilyPreferences)
				.WithOne(cf => cf.Profile)
				.HasForeignKey(cf => cf.ProfileId)
				.OnDelete(DeleteBehavior.Cascade);

			// CustomerProfile -> CustomerAttributes (1:M)
			builder.Entity<CustomerProfile>()
				.HasMany(cp => cp.AttributePreferences)
				.WithOne(ca => ca.Profile)
				.HasForeignKey(ca => ca.ProfileId)
				.OnDelete(DeleteBehavior.Cascade);

			// User -> LoyaltyTransactions (1:M)
			builder.Entity<User>()
				.HasMany(u => u.LoyaltyTransactions)
				.WithOne(lt => lt.User)
				.HasForeignKey(lt => lt.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// User -> Addresses (1:M)
			builder.Entity<User>()
				.HasMany(u => u.Addresses)
				.WithOne(a => a.User)
				.HasForeignKey(a => a.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// User -> ImportTickets (1:M)
			builder.Entity<User>()
				.HasMany(u => u.ImportTickets)
				.WithOne(it => it.CreatedByUser)
				.HasForeignKey(it => it.CreatedById)
				.OnDelete(DeleteBehavior.Restrict);

			// User -> ImportTickets as Verifier (1:M)
			builder.Entity<ImportTicket>()
				.HasOne(it => it.VerifiedByUser)
				.WithMany()
				.HasForeignKey(it => it.VerifiedById)
				.OnDelete(DeleteBehavior.Restrict);

			// User -> StockAdjustments (1:M)
			builder.Entity<User>()
				.HasMany(u => u.StockAdjustments)
				.WithOne(sa => sa.CreatedByUser)
				.HasForeignKey(sa => sa.CreatedById)
				.OnDelete(DeleteBehavior.Restrict);

			// User -> StockAdjustments as Verifier (1:M)
			builder.Entity<StockAdjustment>()
				.HasOne(sa => sa.VerifiedByUser)
				.WithMany()
				.HasForeignKey(sa => sa.VerifiedById)
				.OnDelete(DeleteBehavior.Restrict);

			// User -> Notifications (1:M)
			builder.Entity<User>()
				.HasMany(u => u.Notifications)
				.WithOne(n => n.User)
				.HasForeignKey(n => n.UserId)
				.IsRequired(false)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<UserNotificationRead>()
				.HasKey(unr => new { unr.UserId, unr.NotificationId });

			builder.Entity<UserNotificationRead>()
				.HasOne(unr => unr.User)
				.WithMany(u => u.NotificationReadStates)
				.HasForeignKey(unr => unr.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<UserNotificationRead>()
				.HasOne(unr => unr.Notification)
				.WithMany(n => n.UserReadStates)
				.HasForeignKey(unr => unr.NotificationId)
				.OnDelete(DeleteBehavior.Cascade);

			// User -> UserVouchers (1:M)
			builder.Entity<User>()
				.HasMany(u => u.UserVouchers)
				.WithOne(uv => uv.User)
				.HasForeignKey(uv => uv.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// User -> CartItems (1:M)
			builder.Entity<User>()
				.HasMany(u => u.CartItems)
				.WithOne(ci => ci.User)
				.HasForeignKey(ci => ci.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// User -> Orders (1:M) as Customer
			// Order has two navigations to User (Customer and Staff). Explicitly map Orders collection
			// to the Customer navigation to avoid EF Core ambiguity.
			builder.Entity<User>()
				.HasMany(u => u.Orders)
				.WithOne(o => o.Customer)
				.HasForeignKey(o => o.CustomerId)
				.OnDelete(DeleteBehavior.SetNull);

			// Order -> Staff (M:1) (no inverse navigation on User)
			builder.Entity<Order>()
				.HasOne(o => o.Staff)
				.WithMany()
				.HasForeignKey(o => o.StaffId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<UserVoucher>()
				.HasIndex(uv => uv.OrderId)
				.IsUnique();

			builder.Entity<Order>()
				.HasOne(o => o.UserVoucher)
				.WithOne(uv => uv.Order)
				.HasForeignKey<UserVoucher>(uv => uv.OrderId);

			// Supplier -> ImportTickets (1:M)
			builder.Entity<Supplier>()
				.HasMany(s => s.ImportTickets)
				.WithOne(it => it.Supplier)
				.HasForeignKey(it => it.SupplierId)
				.OnDelete(DeleteBehavior.Restrict);

			// VariantSupplier -> Supplier (M:1)
			builder.Entity<VariantSupplier>()
				.HasOne(vs => vs.Supplier)
				.WithMany()
				.HasForeignKey(vs => vs.SupplierId)
				.OnDelete(DeleteBehavior.Restrict);

			// ImportTicket -> Import_Detail (1:M)
			builder.Entity<ImportTicket>()
				.HasMany(it => it.ImportDetails)
				.WithOne(d => d.ImportTicket)
				.HasForeignKey(d => d.ImportId)
				.OnDelete(DeleteBehavior.Cascade);

			// Import_Detail -> Batch (1:M)
			builder.Entity<ImportDetail>()
				.HasMany(d => d.Batches)
				.WithOne(b => b.ImportDetail)
				.HasForeignKey(b => b.ImportDetailId)
				.OnDelete(DeleteBehavior.Cascade);

			// StockAdjustment -> StockAdjustmentDetail (1:M)
			builder.Entity<StockAdjustment>()
				.HasMany(sa => sa.AdjustmentDetails)
				.WithOne(d => d.StockAdjustment)
				.HasForeignKey(d => d.StockAdjustmentId)
				.OnDelete(DeleteBehavior.Cascade);

			// Product -> Variants (1:M)
			builder.Entity<Product>()
				.HasMany(p => p.Variants)
				.WithOne(v => v.Product)
				.HasForeignKey(v => v.ProductId)
				.OnDelete(DeleteBehavior.Cascade);

			// Product -> ProductScentMap (1:M)
			builder.Entity<Product>()
				.HasMany(p => p.ProductScentMaps)
				.WithOne(psm => psm.Product)
				.HasForeignKey(psm => psm.ProductId)
				.OnDelete(DeleteBehavior.Cascade);

			// ScentNote -> ProductScentMap (1:M)
			builder.Entity<ScentNote>()
				.HasMany(sn => sn.ProductScentNoteMaps)
				.WithOne(psm => psm.ScentNote)
				.HasForeignKey(psm => psm.ScentNoteId)
				.OnDelete(DeleteBehavior.Cascade);

			// ScentNote -> CustomerNotePreference (1:M)
			builder.Entity<ScentNote>()
				.HasMany(sn => sn.CustomerScentNotePreferences)
				.WithOne(cnp => cnp.ScentNote)
				.HasForeignKey(cnp => cnp.NoteId)
				.OnDelete(DeleteBehavior.Cascade);

			// OlfactoryFamily -> ProductOlfactoryMap (1:M)
			builder.Entity<OlfactoryFamily>()
				.HasMany(of => of.ProductFamilyMaps)
				.WithOne(pom => pom.OlfactoryFamily)
				.HasForeignKey(pom => pom.OlfactoryFamilyId)
				.OnDelete(DeleteBehavior.Cascade);

			// OlfactoryFamily -> CustomerFamilyPreference (1:M)
			builder.Entity<OlfactoryFamily>()
				.HasMany(of => of.CustomerFamilyPreferences)
				.WithOne(cfp => cfp.Family)
				.HasForeignKey(cfp => cfp.FamilyId)
				.OnDelete(DeleteBehavior.Cascade);

			// Product -> ProductOlfactoryMap (1:M)
			builder.Entity<Product>()
				.HasMany(p => p.ProductFamilyMaps)
				.WithOne(pom => pom.Product)
				.HasForeignKey(pom => pom.ProductId)
				.OnDelete(DeleteBehavior.Cascade);

			// Brand/Category -> Product (1:M)
			builder.Entity<Brand>()
				.HasMany(b => b.Products)
				.WithOne(p => p.Brand)
				.HasForeignKey(p => p.BrandId)
				.OnDelete(DeleteBehavior.Restrict);

			// Product embedding 
			builder.Entity<Product>()
				.Property(p => p.Embedding)
				.HasColumnType("vector(1024)");

			builder.Entity<Category>()
				.HasMany(c => c.Products)
				.WithOne(p => p.Category)
				.HasForeignKey(p => p.CategoryId)
				.OnDelete(DeleteBehavior.Restrict);

			// Concentration -> Variants (1:M)
			builder.Entity<Concentration>()
				.HasMany(c => c.Variants)
				.WithOne(v => v.Concentration)
				.HasForeignKey(v => v.ConcentrationId)
				.OnDelete(DeleteBehavior.Restrict);

			// Variant -> Batches (1:M)
			builder.Entity<ProductVariant>()
				.HasMany(v => v.Batches)
				.WithOne(b => b.ProductVariant)
				.HasForeignKey(b => b.VariantId)
				.OnDelete(DeleteBehavior.Restrict);

			// Variant -> Stock (1:1)
			builder.Entity<ProductVariant>()
				.HasOne(v => v.Stock)
				.WithOne(s => s.ProductVariant)
				.HasForeignKey<Stock>(s => s.VariantId)
				.OnDelete(DeleteBehavior.Cascade);

			// Variant -> CartItem / OrderDetail (1:M)
			builder.Entity<ProductVariant>()
				.HasMany(v => v.CartItems)
				.WithOne(ci => ci.ProductVariant)
				.HasForeignKey(ci => ci.VariantId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<ProductVariant>()
				.HasMany(v => v.OrderDetails)
				.WithOne(od => od.ProductVariant)
				.HasForeignKey(od => od.VariantId)
				.OnDelete(DeleteBehavior.Restrict);

			// Variant -> ImportDetail (1:M)
			builder.Entity<ProductVariant>()
				.HasMany(v => v.ImportDetails)
				.WithOne(d => d.ProductVariant)
				.HasForeignKey(d => d.ProductVariantId)
				.OnDelete(DeleteBehavior.Restrict);

			// VariantSupplier -> ProductVariant (M:1)
			builder.Entity<VariantSupplier>()
				.HasOne(vs => vs.ProductVariant)
				.WithMany()
				.HasForeignKey(vs => vs.ProductVariantId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<VariantSupplier>()
				.HasIndex(vs => new { vs.ProductVariantId, vs.SupplierId })
				.IsUnique();

			// Variant -> StockAdjustmentDetail (1:M)
			builder.Entity<ProductVariant>()
				.HasMany(v => v.StockAdjustmentDetails)
				.WithOne(d => d.ProductVariant)
				.HasForeignKey(d => d.ProductVariantId)
				.OnDelete(DeleteBehavior.Restrict);

			// Variant -> Promotions
			builder.Entity<ProductVariant>()
				.HasMany(v => v.PromotionItems)
				.WithOne(p => p.ProductVariant)
				.HasForeignKey(p => p.TargetProductVariantId)
				.OnDelete(DeleteBehavior.Cascade);

			// Batch -> Promotions
			builder.Entity<Batch>()
				.HasMany(b => b.Promotions)
				.WithOne(p => p.Batch)
				.HasForeignKey(p => p.BatchId)
				.OnDelete(DeleteBehavior.Restrict);

			// Promotion -> OrderDetail (1:M)
			builder.Entity<PromotionItem>()
				.HasMany(p => p.OrderDetails)
				.WithOne(od => od.PromotionItem)
				.HasForeignKey(od => od.PromotionItemId)
				.OnDelete(DeleteBehavior.Restrict);

			// Batch -> OrderDetail (1:M)
			builder.Entity<Batch>()
				.HasMany(b => b.OrderDetails)
			 .WithOne(od => od.FulfilledBatch)
				.HasForeignKey(od => od.FulfilledBatchId)
				.OnDelete(DeleteBehavior.Restrict);

			// Batch -> StockAdjustmentDetail (1:M)
			builder.Entity<Batch>()
				.HasMany(b => b.StockAdjustmentDetails)
				.WithOne(d => d.Batch)
				.HasForeignKey(d => d.BatchId)
				.OnDelete(DeleteBehavior.Restrict);

			// Order -> OrderDetails (1:M)
			builder.Entity<Order>()
				.HasMany(o => o.OrderDetails)
				.WithOne(od => od.Order)
				.HasForeignKey(od => od.OrderId)
				.OnDelete(DeleteBehavior.Cascade);

			// Order -> StockReservations (1:M)
			builder.Entity<Order>()
				.HasMany(o => o.StockReservations)
				.WithOne(sr => sr.Order)
				.HasForeignKey(sr => sr.OrderId)
				.OnDelete(DeleteBehavior.Cascade);

			// Order -> LoyaltyTransactions (1:M)
			builder.Entity<Order>()
				.HasMany(o => o.LoyaltyTransactions)
				.WithOne(lt => lt.Order)
				.HasForeignKey(lt => lt.OrderId)
				.OnDelete(DeleteBehavior.Cascade);

			// Batch -> StockReservations (1:M)
			builder.Entity<Batch>()
				.HasMany(b => b.StockReservations)
				.WithOne(sr => sr.Batch)
				.HasForeignKey(sr => sr.BatchId)
				.OnDelete(DeleteBehavior.Restrict);

			// ProductVariant -> StockReservations (1:M)
			builder.Entity<ProductVariant>()
				.HasMany(pv => pv.StockReservations)
				.WithOne(sr => sr.ProductVariant)
				.HasForeignKey(sr => sr.VariantId)
				.OnDelete(DeleteBehavior.Restrict);

			// Indexes for StockReservation
			builder.Entity<StockReservation>()
				.HasIndex(sr => new { sr.OrderId, sr.Status });

			builder.Entity<StockReservation>()
				.HasIndex(sr => new { sr.Status, sr.ExpiresAt });

			builder.Entity<StockReservation>()
				.HasIndex(sr => sr.BatchId);

			builder.Entity<StockReservation>()
				.HasIndex(sr => sr.VariantId);

			// Order <-> PaymentTransaction (1:M)
			builder.Entity<Order>()
				.HasMany(o => o.PaymentTransactions)
				.WithOne(pt => pt.Order)
				.HasForeignKey(pt => pt.OrderId)
				.OnDelete(DeleteBehavior.Cascade);

			// PaymentTransaction -> Receipt (1:1)
			builder.Entity<PaymentTransaction>()
				.HasOne(pt => pt.Receipt)
				.WithOne(r => r.PaymentTransaction)
				.HasForeignKey<Receipt>(r => r.TransactionId)
				.OnDelete(DeleteBehavior.Cascade);

			// PaymentTransaction self-reference (1:M) Retry & Refund
			builder.Entity<PaymentTransaction>()
				.HasOne(pt => pt.OriginalPayment)
				.WithMany(pt => pt.RetryPayments)
				.HasForeignKey(pt => pt.OriginalPaymentId)
				.OnDelete(DeleteBehavior.Restrict);

			// Order -> ShippingInfo, ContactAddress (1:1)
			builder.Entity<Order>()
				.HasOne(o => o.ForwardShipping)
				.WithMany()
				.HasForeignKey(o => o.ForwardShippingId)
				.OnDelete(DeleteBehavior.Restrict);

			// Voucher -> UserVoucher (1:M)
			builder.Entity<Voucher>()
				.HasMany(v => v.UserVouchers)
				.WithOne(uv => uv.Voucher)
				.HasForeignKey(uv => uv.VoucherId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<Voucher>()
				.HasIndex(v => v.Code)
				.IsUnique().HasFilter("[IsDeleted] = 0");

			// Voucher -> LoyaltyTransaction (1:M)
			builder.Entity<Voucher>()
				.HasMany(v => v.LoyaltyTransactions)
				.WithOne(lt => lt.Voucher)
				.HasForeignKey(lt => lt.VoucherId)
				.OnDelete(DeleteBehavior.Cascade);

			// Media -> Product (M:1) using ProductId
			builder.Entity<Media>()
				.HasOne(m => m.Product)
				.WithMany(p => p.Media)
				.HasForeignKey(m => m.ProductId)
				.OnDelete(DeleteBehavior.Restrict);

			// Media -> ProductVariant (M:1) using ProductVariantId
			builder.Entity<Media>()
				.HasOne(m => m.ProductVariant)
				.WithMany(pv => pv.Media)
				.HasForeignKey(m => m.ProductVariantId)
				.OnDelete(DeleteBehavior.Restrict);

			// Media -> OrderReturnRequest (M:1) using OrderReturnRequestId
			builder.Entity<Media>()
				.HasOne(m => m.OrderReturnRequest)
				.WithMany(orr => orr.ProofImages)
				.HasForeignKey(m => m.OrderReturnRequestId)
				.OnDelete(DeleteBehavior.Restrict);

			// Media -> Review (M:1) using ReviewId
			builder.Entity<Media>()
				.HasOne(m => m.Review)
				.WithMany(r => r.ReviewImages)
				.HasForeignKey(m => m.ReviewId)
				.OnDelete(DeleteBehavior.Restrict);

			// Attribute -> AttributeValue (1:M)
			builder.Entity<Attribute>()
				.HasIndex(a => a.InternalCode)
				.IsUnique();

			builder.Entity<Attribute>()
				.HasMany(a => a.AttributeValues)
				.WithOne(av => av.Attribute)
				.HasForeignKey(av => av.AttributeId)
				.OnDelete(DeleteBehavior.Cascade);

			// Attribute -> ProductAttribute (1:M)
			builder.Entity<Attribute>()
				.HasMany(a => a.ProductAttributes)
				.WithOne(pa => pa.Attribute)
				.HasForeignKey(pa => pa.AttributeId)
				.OnDelete(DeleteBehavior.Restrict);

			// AttributeValue -> ProductAttribute (1:M)
			builder.Entity<AttributeValue>()
				.HasMany(av => av.ProductAttributes)
				.WithOne(pa => pa.Value)
				.HasForeignKey(pa => pa.ValueId)
				.OnDelete(DeleteBehavior.Restrict);

			// AttributeValue -> CustomerAttributePreference (1:M)
			builder.Entity<AttributeValue>()
				.HasMany(av => av.CustomerAttributePreferences)
				.WithOne(cap => cap.AttributeValue)
				.HasForeignKey(cap => cap.AttributeValueId)
				.OnDelete(DeleteBehavior.Restrict);

			// Product -> ProductAttribute (1:M) (product-level attributes)
			builder.Entity<Product>()
				.HasMany(p => p.ProductAttributes)
				.WithOne(pa => pa.Product)
				.HasForeignKey(pa => pa.ProductId)
				.OnDelete(DeleteBehavior.Cascade);

			// ProductVariant -> ProductAttribute (1:M) (variant-level attributes)
			builder.Entity<ProductVariant>()
				.HasMany(v => v.ProductAttributes)
				.WithOne(pa => pa.Variant)
				.HasForeignKey(pa => pa.VariantId)
				.OnDelete(DeleteBehavior.Restrict);

			// Media -> User (1:1) for ProfilePicture using UserId
			builder.Entity<User>()
				.HasOne(u => u.ProfilePicture)
				.WithOne(m => m.User)
				.HasForeignKey<Media>(m => m.UserId)
				.OnDelete(DeleteBehavior.SetNull);

			// Review -> User (M:1)
			builder.Entity<Review>()
				.HasOne(r => r.User)
				.WithMany(u => u.Reviews)
				.HasForeignKey(r => r.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			// Review -> OrderDetail (1:1)
			builder.Entity<Review>()
				.HasOne(r => r.OrderDetail)
				.WithOne(od => od.Review)
				.HasForeignKey<Review>(r => r.OrderDetailId)
				.OnDelete(DeleteBehavior.Restrict);

			// Review -> StaffFeedbackByStaff (M:1, nullable)
			builder.Entity<Review>()
				.HasOne(r => r.StaffFeedbackByStaff)
				.WithMany(u => u.AnswerReviews)
				.HasForeignKey(r => r.StaffFeedbackByStaffId)
				.OnDelete(DeleteBehavior.Restrict);

			// OrderCancelRequest -> Order (M:1)
			builder.Entity<OrderCancelRequest>()
				.HasOne(ocr => ocr.Order)
				.WithMany(orc => orc.CancelRequests)
				.HasForeignKey(ocr => ocr.OrderId)
				.OnDelete(DeleteBehavior.Restrict);

			// OrderCancelRequest -> RequestedBy (M:1)
			builder.Entity<OrderCancelRequest>()
				.HasOne(ocr => ocr.RequestedBy)
				.WithMany(u => u.RequestedCancelRequests)
				.HasForeignKey(ocr => ocr.RequestedById)
				.OnDelete(DeleteBehavior.Restrict);

			// OrderCancelRequest -> ProcessedBy (M:1, nullable)
			builder.Entity<OrderCancelRequest>()
				.HasOne(ocr => ocr.ProcessedBy)
				.WithMany(u => u.ProcessedCancelRequests)
				.HasForeignKey(ocr => ocr.ProcessedById)
				.OnDelete(DeleteBehavior.Restrict);

			// OrderReturnRequest -> Order (M:1)
			builder.Entity<OrderReturnRequest>()
				.HasOne(orr => orr.Order)
				.WithMany(o => o.ReturnRequests)
				.HasForeignKey(orr => orr.OrderId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<OrderReturnRequest>()
				.HasOne(orr => orr.ReturnShipping)
				.WithMany()
				.HasForeignKey(orr => orr.ReturnShippingId)
				.OnDelete(DeleteBehavior.Restrict);

			// OrderReturnRequest -> Customer (M:1)
			builder.Entity<OrderReturnRequest>()
			 .HasOne(orr => orr.Customer)
				.WithMany(u => u.CustomerReturnRequests)
				.HasForeignKey(orr => orr.CustomerId)
				.OnDelete(DeleteBehavior.Restrict);

			// OrderReturnRequest -> ProcessedBy (M:1, nullable)
			builder.Entity<OrderReturnRequest>()
				.HasOne(orr => orr.ProcessedBy)
				.WithMany(u => u.ProcessedReturnRequests)
				.HasForeignKey(orr => orr.ProcessedById)
				.OnDelete(DeleteBehavior.Restrict);

			// OrderReturnRequest -> InspectedBy (M:1, nullable)
			builder.Entity<OrderReturnRequest>()
				.HasOne(orr => orr.InspectedBy)
				.WithMany(u => u.InspectedReturnRequests)
				.HasForeignKey(orr => orr.InspectedById)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<OrderReturnRequest>()
				.HasMany(orr => orr.ReturnDetails)
				.WithOne(rd => rd.ReturnRequest)
				.HasForeignKey(rd => rd.ReturnRequestId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<OrderDetail>()
				.HasMany(od => od.ReturnRequestDetails)
				.WithOne(rd => rd.OrderDetail)
				.HasForeignKey(rd => rd.OrderDetailId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<OrderReturnRequestDetail>()
				.HasIndex(rd => new { rd.ReturnRequestId, rd.OrderDetailId })
				.IsUnique();

			// Campaign -> PromotionItem (1:M)
			builder.Entity<Campaign>()
				.HasMany(c => c.Items)
				.WithOne(pi => pi.Campaign)
				.HasForeignKey(pi => pi.CampaignId)
				.OnDelete(DeleteBehavior.Cascade);

			// Campaign -> Voucher (1:M)
			builder.Entity<Campaign>()
				.HasMany(c => c.Vouchers)
				.WithOne(v => v.Campaign)
				.HasForeignKey(v => v.CampaignId)
				.OnDelete(DeleteBehavior.Cascade);

			// E-commerce hot-path indexes (SKU lookup, order filtering, promotion date windows)
			builder.Entity<ProductVariant>()
				.HasIndex(v => v.Sku)
				.IsUnique()
				.HasFilter("[IsDeleted] = 0");

			builder.Entity<ProductVariant>()
				.HasIndex(v => v.Barcode)
				.IsUnique()
				.HasFilter("[IsDeleted] = 0");

			builder.Entity<ProductVariant>()
				.HasIndex(v => new { v.ProductId, v.Status, v.IsDeleted });

			builder.Entity<Order>()
				.HasIndex(o => o.Code)
				.IsUnique();

			builder.Entity<Order>()
				.HasIndex(o => new { o.CustomerId, o.Status, o.CreatedAt });

			builder.Entity<Order>()
				.HasIndex(o => new { o.StaffId, o.Status, o.CreatedAt });

			builder.Entity<Order>()
				.HasIndex(o => new { o.Status, o.CreatedAt });

			builder.Entity<Order>()
				.HasIndex(o => new { o.PaymentStatus, o.PaymentExpiresAt });

			builder.Entity<UserVoucher>()
				.HasIndex(uv => new { uv.UserId, uv.Status });

			builder.Entity<UserVoucher>()
			   .HasIndex(uv => new { uv.VoucherId, uv.UserId });

			builder.Entity<UserVoucher>()
				.HasIndex(uv => new { uv.GuestIdentifier, uv.UserId });

			builder.Entity<Campaign>()
				.HasIndex(c => new { c.Status, c.IsDeleted, c.StartDate, c.EndDate });

			builder.Entity<PromotionItem>()
				.HasIndex(pi => new { pi.CampaignId, pi.IsActive, pi.IsDeleted });

			builder.Entity<PromotionItem>()
				.HasIndex(pi => new { pi.TargetProductVariantId, pi.IsActive, pi.IsDeleted });

			builder.Entity<Voucher>()
				.HasIndex(v => new { v.CampaignId, v.ExpiryDate, v.IsDeleted });

			builder.Entity<Voucher>()
				.HasIndex(v => new { v.ExpiryDate, v.IsDeleted });

			builder.Entity<PaymentTransaction>()
				.HasIndex(pt => new { pt.CreatedAt, pt.TransactionType, pt.TransactionStatus });

			// Create indexes for efficient queries
			builder.Entity<Media>()
				.HasIndex(m => new { m.EntityType, m.ProductId });

			builder.Entity<Media>()
				.HasIndex(m => new { m.EntityType, m.ProductVariantId });

			builder.Entity<Media>()
				.HasIndex(m => new { m.EntityType, m.ReviewId });

			builder.Entity<Media>()
				.HasIndex(m => new { m.EntityType, m.OrderReturnRequestId });

			builder.Entity<Media>()
				.HasIndex(m => new { m.EntityType, m.UserId });

			// Create index on IsPrimary for quick primary image lookup
			builder.Entity<Media>()
				.HasIndex(m => m.IsPrimary);

			// Review indexes
			builder.Entity<Review>()
				.HasIndex(r => r.UserId);

			builder.Entity<Review>()
				.HasIndex(r => r.OrderDetailId)
				.IsUnique();

			builder.Entity<Review>()
				.HasIndex(r => r.Rating);

			// TemporaryMedia -> User (M:1, nullable)
			builder.Entity<TemporaryMedia>()
				.HasOne(tm => tm.UploadedByUser)
				.WithMany()
				.HasForeignKey(tm => tm.UploadedByUserId)
				.OnDelete(DeleteBehavior.SetNull);

			// TemporaryMedia indexes
			builder.Entity<TemporaryMedia>()
				.HasIndex(tm => tm.ExpiresAt);

			builder.Entity<TemporaryMedia>()
				.HasIndex(tm => tm.UploadedByUserId);

			// Ensure index coverage for all FKs and soft-delete filters across model
			foreach (var entityType in builder.Model.GetEntityTypes().Where(e => e.ClrType != null))
			{
				var clrType = entityType.ClrType;

				foreach (var foreignKey in entityType.GetForeignKeys())
				{
					var fkPropertyNames = foreignKey.Properties
						.Select(p => p.Name)
						.ToArray();

					var hasMatchingIndex = entityType.GetIndexes()
						.Any(i => i.Properties.Select(p => p.Name).SequenceEqual(fkPropertyNames));

					if (!hasMatchingIndex)
					{
						builder.Entity(clrType).HasIndex(fkPropertyNames);
					}
				}

				if (!typeof(ISoftDelete).IsAssignableFrom(clrType)
					|| entityType.FindProperty(nameof(ISoftDelete.IsDeleted)) == null)
				{
					continue;
				}

				var hasIsDeletedIndex = entityType.GetIndexes()
					.Any(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(ISoftDelete.IsDeleted));

				if (!hasIsDeletedIndex)
				{
					builder.Entity(clrType)
						.HasIndex(nameof(ISoftDelete.IsDeleted))
						.HasFilter("[IsDeleted] = 0");
				}
			}

			// Configure encryption for sensitive fields in OrderCancelRequest
			builder.Entity<OrderCancelRequest>()
				.Property(x => x.RefundBankName)
				.HasConversion(encryptionConverter);

			builder.Entity<OrderCancelRequest>()
			.Property(x => x.RefundAccountNumber)
			.HasConversion(encryptionConverter);

			builder.Entity<OrderCancelRequest>()
				.Property(x => x.RefundAccountName)
				.HasConversion(encryptionConverter);

			// Nếu OrderReturnRequest cũng có các trường này, hãy cấu hình luôn:
			builder.Entity<OrderReturnRequest>()
				.Property(x => x.RefundBankName)
				.HasConversion(encryptionConverter);

			builder.Entity<OrderReturnRequest>()
				.Property(x => x.RefundAccountNumber)
				.HasConversion(encryptionConverter);

			builder.Entity<OrderReturnRequest>()
				.Property(x => x.RefundAccountName)
				.HasConversion(encryptionConverter);

			// Decimal precision
			foreach (var entityType in builder.Model.GetEntityTypes())
			{
				foreach (var property in entityType.GetProperties())
				{
					// Decimal
					if ((property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
						&& property.GetPrecision() is null)
					{
						property.SetPrecision(18);
						property.SetScale(2);
					}

					// Enum / string
					var clrType = property.ClrType;
					var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;
					if (underlyingType.IsEnum)
					{
						var converterType = typeof(EnumToStringConverter<>).MakeGenericType(underlyingType);
						var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
						property.SetValueConverter(converter);
					}
				}
			}

			builder.Entity<SystemPolicy>()
				.ToTable("SystemPolicies");

			builder.Entity<SystemPolicy>()
				.Property(sp => sp.Id)
				.HasColumnName("PolicyCode")
				.HasMaxLength(100);

			builder.Entity<SystemPolicy>()
				.Property(sp => sp.Title)
				.HasMaxLength(255)
				.IsRequired();

			builder.Entity<SystemPolicy>()
				.Property(sp => sp.HtmlContent)
				.HasColumnType("nvarchar(max)")
				.IsRequired();

			builder.Entity<StorePolicy>()
				.ToTable("StorePolicies");

			builder.Entity<StorePolicy>()
				.Property(sp => sp.RequiredDepositPercentage)
				.HasPrecision(18, 2);

			// Configure NVarchar for string properties to avoid default max length issues
			builder.Entity<Product>().Property(p => p.Description)
				.HasColumnType("nvarchar(max)");
			builder.Entity<Review>().Property(r => r.Comment)
				.HasColumnType("nvarchar(max)");
			builder.Entity<Review>().Property(r => r.StaffFeedbackComment)
				   .HasColumnType("nvarchar(max)");
			builder.Entity<Notification>().Property(n => n.Message)
				.HasColumnType("nvarchar(max)");
			builder.Entity<Attribute>().Property(a => a.Description)
				.HasColumnType("nvarchar(max)");

			// Seed roles
			builder.Entity<IdentityRole<Guid>>().HasData(PerfumeDbContextSeed.SeedingRoles());
			// Seed users
			builder.Entity<User>().HasData(PerfumeDbContextSeed.SeedingUsers());
			// Seed user roles
			builder.Entity<IdentityUserRole<Guid>>().HasData(PerfumeDbContextSeed.SeedingUserRoles());
			// Seed system policies
			builder.Entity<SystemPolicy>().HasData(PerfumeDbContextSeed.SeedingSystemPolicies());
			// Seed store policies
			builder.Entity<StorePolicy>().HasData(PerfumeDbContextSeed.SeedingStorePolicies());
		}

		internal class PassThroughEncryptionProvider : IEncryptionProvider
		{
			public string? Encrypt(string? plainText) => plainText;
			public string? Decrypt(string? cipherText) => cipherText;
		}
	}
}
