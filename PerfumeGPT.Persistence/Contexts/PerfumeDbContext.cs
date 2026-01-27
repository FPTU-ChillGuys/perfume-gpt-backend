using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Entities;
using System.Security.Claims;

namespace PerfumeGPT.Persistence.Contexts
{
	public class PerfumeDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
	{
		private readonly IHttpContextAccessor? _httpContextAccessor;
		private readonly IAuditScope? _auditScope;

		public PerfumeDbContext(DbContextOptions<PerfumeDbContext> options, IHttpContextAccessor? httpContextAccessor, IAuditScope? auditScope = null)
			: base(options)
		{
			_httpContextAccessor = httpContextAccessor;
			_auditScope = auditScope;
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

		public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			ApplyAuditRules();
			return base.SaveChangesAsync(cancellationToken);
		}

		public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
		{
			ApplyAuditRules();
			return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		}

		// DbSets
		public DbSet<CustomerProfile> CustomerProfiles { get; set; }
		public DbSet<LoyaltyPoint> LoyaltyPoints { get; set; }
		public DbSet<Address> Addresses { get; set; }
		public DbSet<ImportTicket> ImportTickets { get; set; }
		public DbSet<ImportDetail> ImportDetails { get; set; }
		public DbSet<Supplier> Suppliers { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<ProductVariant> ProductVariants { get; set; }
		public DbSet<Brand> Brands { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<FragranceFamily> FragranceFamilies { get; set; }
		public DbSet<Concentration> Concentrations { get; set; }
		public DbSet<Batch> Batches { get; set; }
		public DbSet<Stock> Stocks { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderDetail> OrderDetails { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<Cart> Carts { get; set; }
		public DbSet<CartItem> CartItems { get; set; }
		public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
		public DbSet<Receipt> Receipts { get; set; }
		public DbSet<Voucher> Vouchers { get; set; }
		public DbSet<UserVoucher> UserVouchers { get; set; }
		public DbSet<ShippingInfo> ShippingInfos { get; set; }
		public DbSet<RecipientInfo> RecipientInfos { get; set; }


		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// User -> CustomerProfile (1:1)
			builder.Entity<User>()
				.HasOne(u => u.CustomerProfile)
				.WithOne(cp => cp.User)
				.HasForeignKey<CustomerProfile>(cp => cp.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<CustomerProfile>()
				.HasIndex(cp => cp.UserId)
				.IsUnique();

			// Configure BaseEntity primary keys
			builder.Model.GetEntityTypes()
				.Where(t => typeof(BaseEntity<Guid>).IsAssignableFrom(t.ClrType))
				.ToList()
				.ForEach(t => builder.Entity(t.ClrType).HasKey("Id"));

			// User -> LoyaltyPoint (1:1)
			builder.Entity<User>()
				.HasOne(u => u.LoyaltyPoint)
				.WithOne(lp => lp.User)
				.HasForeignKey<LoyaltyPoint>(lp => lp.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<LoyaltyPoint>()
				.HasIndex(lp => lp.UserId)
				.IsUnique();

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

			// User -> Notifications (1:M)
			builder.Entity<User>()
				.HasMany(u => u.Notifications)
				.WithOne(n => n.User)
				.HasForeignKey(n => n.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			// User -> UserVouchers (1:M)
			builder.Entity<User>()
				.HasMany(u => u.UserVouchers)
				.WithOne(uv => uv.User)
				.HasForeignKey(uv => uv.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// User -> Cart (1:1)
			builder.Entity<User>()
				.HasOne(u => u.Cart)
				.WithOne(c => c.User)
				.HasForeignKey<Cart>(c => c.UserId)
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

			// Supplier -> ImportTickets (1:M)
			builder.Entity<Supplier>()
				.HasMany(s => s.ImportTickets)
				.WithOne(it => it.Supplier)
				.HasForeignKey(it => it.SupplierId)
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

			// Product -> Variants (1:M)
			builder.Entity<Product>()
				.HasMany(p => p.Variants)
				.WithOne(v => v.Product)
				.HasForeignKey(v => v.ProductId)
				.OnDelete(DeleteBehavior.Cascade);

			// Brand/Category/FragranceFamily -> Product (1:M)
			builder.Entity<Brand>()
				.HasMany(b => b.Products)
				.WithOne(p => p.Brand)
				.HasForeignKey(p => p.BrandId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Category>()
				.HasMany(c => c.Products)
				.WithOne(p => p.Category)
				.HasForeignKey(p => p.CategoryId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<FragranceFamily>()
				.HasMany(f => f.Products)
				.WithOne(p => p.FragranceFamily)
				.HasForeignKey(p => p.FamilyId)
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

			// Cart -> CartItems (1:M)
			builder.Entity<Cart>()
				.HasMany(c => c.Items)
				.WithOne(i => i.Cart)
				.HasForeignKey(i => i.CartId)
				.OnDelete(DeleteBehavior.Cascade);

			// Order -> OrderDetails (1:M)
			builder.Entity<Order>()
				.HasMany(o => o.OrderDetails)
				.WithOne(od => od.Order)
				.HasForeignKey(od => od.OrderId)
				.OnDelete(DeleteBehavior.Cascade);

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

			// Order -> Voucher (M:1)
			builder.Entity<Order>()
				.HasOne(o => o.Voucher)
				.WithMany(v => v.Orders)
				.HasForeignKey(o => o.VoucherId)
				.OnDelete(DeleteBehavior.SetNull);

			// Order -> ShippingInfo, RecipientInfo (1:1)
			builder.Entity<Order>()
				.HasOne(o => o.ShippingInfo)
				.WithOne(s => s.Order)
				.HasForeignKey<ShippingInfo>(s => s.OrderId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.Entity<Order>()
				.HasOne(o => o.RecipientInfo)
				.WithOne(r => r.Order)
				.HasForeignKey<RecipientInfo>(r => r.OrderId)
				.OnDelete(DeleteBehavior.Cascade);

			// Voucher -> UserVoucher (1:M)
			builder.Entity<Voucher>()
				.HasMany(v => v.UserVouchers)
				.WithOne(uv => uv.Voucher)
				.HasForeignKey(uv => uv.VoucherId)
				.OnDelete(DeleteBehavior.Cascade);

			// Stock/Order/Voucher/Batch -> Notification relations
			builder.Entity<Stock>()
				.HasMany(s => s.Notifications)
				.WithOne(n => n.Stock)
				.HasForeignKey(n => n.StockId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Order>()
				.HasMany(o => o.Notifications)
				.WithOne(n => n.Order)
				.HasForeignKey(n => n.OrderId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Voucher>()
				.HasMany(v => v.Notifications)
				.WithOne(n => n.Voucher)
				.HasForeignKey(n => n.VoucherId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Batch>()
				.HasMany(b => b.Notifications)
				.WithOne(n => n.Batch)
				.HasForeignKey(n => n.BatchId)
				.OnDelete(DeleteBehavior.Restrict);

			// Configure decimal precision/scale to avoid default truncation warnings
			builder.Entity<CustomerProfile>().Property(cp => cp.MinBudget).HasPrecision(18, 2);
			builder.Entity<CustomerProfile>().Property(cp => cp.MaxBudget).HasPrecision(18, 2);

			builder.Entity<ImportTicket>().Property(it => it.TotalCost).HasPrecision(18, 2);
			builder.Entity<ImportDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);

			builder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
			builder.Entity<OrderDetail>().Property(od => od.UnitPrice).HasPrecision(18, 2);

			builder.Entity<PaymentTransaction>().Property(pt => pt.Amount).HasPrecision(18, 2);
			builder.Entity<ProductVariant>().Property(pv => pv.BasePrice).HasPrecision(18, 2);
			builder.Entity<ShippingInfo>().Property(s => s.ShippingFee).HasPrecision(18, 2);

			builder.Entity<Voucher>().Property(v => v.DiscountValue).HasPrecision(18, 2);
			builder.Entity<Voucher>().Property(v => v.MinOrderValue).HasPrecision(18, 2);

			// Configure enum to string conversions
			builder.Entity<PaymentTransaction>().Property(pt => pt.TransactionStatus).HasConversion<string>();
			builder.Entity<PaymentTransaction>().Property(pt => pt.Method).HasConversion<string>();
			builder.Entity<ShippingInfo>().Property(s => s.Status).HasConversion<string>();
			builder.Entity<Order>().Property(o => o.Status).HasConversion<string>();
			builder.Entity<Order>().Property(o => o.PaymentStatus).HasConversion<string>();
			builder.Entity<ImportTicket>().Property(it => it.Status).HasConversion<string>();
			builder.Entity<Notification>().Property(n => n.Type).HasConversion<string>();
			builder.Entity<ProductVariant>().Property(pv => pv.Type).HasConversion<string>();
			builder.Entity<ProductVariant>().Property(pv => pv.Status).HasConversion<string>();
			builder.Entity<Voucher>().Property(v => v.DiscountType).HasConversion<string>();
			builder.Entity<UserVoucher>().Property(uv => uv.Status).HasConversion<string>();
			builder.Entity<ShippingInfo>().Property(s => s.CarrierName).HasConversion<string>();

			// Seed roles
			builder.Entity<IdentityRole<Guid>>().HasData(SeedingRoles());
			// Seed users
			builder.Entity<User>().HasData(SeedingUsers());
			// Seed user roles
			builder.Entity<IdentityUserRole<Guid>>().HasData(SeedingUserRoles());
		}

		private ICollection<IdentityRole<Guid>> SeedingRoles()
		{
			return new List<IdentityRole<Guid>>()
			{
				new IdentityRole<Guid>
				{
					Id = Guid.Parse("3631e38b-60dd-4d1a-af7f-a26f21c2ef82"),
					Name = "admin",
					NormalizedName = "ADMIN",
					ConcurrencyStamp = "seed-1"
				},
				new IdentityRole<Guid>
				{
					Id = Guid.Parse("51ef7e08-ff07-459b-8c55-c7ebac505103"),
					Name = "user",
					NormalizedName = "USER",
					ConcurrencyStamp = "seed-2"
				},
				new IdentityRole<Guid>
				{
					Id = Guid.Parse("8f6e1c3d-2d3b-4f4a-9f4a-2e5d6c7b8a9b"),
					Name = "staff",
					NormalizedName = "STAFF",
					ConcurrencyStamp = "seed-3"
				}
			};
		}

		private ICollection<User> SeedingUsers()
		{
			// Pre-computed password hashes for "12345aA@" to avoid dynamic values
			// You can generate these by running: new PasswordHasher<User>().HashPassword(null, "12345aA@")
			var admin = new User
			{
				Id = Guid.Parse("33f41895-b601-4aa1-8dc4-8229a9d07008"),
				UserName = "admin",
				NormalizedUserName = "ADMIN",
				Email = "admin@example.com",
				NormalizedEmail = "ADMIN@EXAMPLE.COM",
				EmailConfirmed = true,
				PasswordHash = "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==",
				SecurityStamp = "seed-4",
				ConcurrencyStamp = "seed-5",
				CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};

			var user = new User
			{
				Id = Guid.Parse("09097277-2705-40c2-bce5-51dbd1f4c1e6"),
				UserName = "user",
				NormalizedUserName = "USER",
				Email = "user@example.com",
				NormalizedEmail = "USER@EXAMPLE.COM",
				EmailConfirmed = true,
				PasswordHash = "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==",
				SecurityStamp = "seed-6",
				ConcurrencyStamp = "seed-7",
				CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};

			var staff = new User
			{
				Id = Guid.Parse("09097277-5555-40c2-bce5-51dbd1f4c1e6"),
				UserName = "staff",
				NormalizedUserName = "STAFF",
				Email = "staff@example.com",
				NormalizedEmail = "STAFF@EXAMPLE.COM",
				EmailConfirmed = true,
				PasswordHash = "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==",
				SecurityStamp = "seed-8",
				ConcurrencyStamp = "seed-9",
				CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};

			return new List<User> { admin, user, staff };
		}

		private ICollection<IdentityUserRole<Guid>> SeedingUserRoles()
		{
			return new List<IdentityUserRole<Guid>>
			{
				new IdentityUserRole<Guid>
				{
					UserId = Guid.Parse("33f41895-b601-4aa1-8dc4-8229a9d07008"),
					RoleId = Guid.Parse("3631e38b-60dd-4d1a-af7f-a26f21c2ef82")
				},
				new IdentityUserRole<Guid>
				{
					UserId = Guid.Parse("09097277-2705-40c2-bce5-51dbd1f4c1e6"),
					RoleId = Guid.Parse("51ef7e08-ff07-459b-8c55-c7ebac505103")
				},
				new IdentityUserRole<Guid>
				{
					UserId = Guid.Parse("09097277-5555-40c2-bce5-51dbd1f4c1e6"),
					RoleId = Guid.Parse("8f6e1c3d-2d3b-4f4a-9f4a-2e5d6c7b8a9b")
				}
			};
		}
	}
}
