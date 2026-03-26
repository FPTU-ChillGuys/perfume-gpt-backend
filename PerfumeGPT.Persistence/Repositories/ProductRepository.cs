using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Text;
using System.Text.RegularExpressions;

namespace PerfumeGPT.Persistence.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly Kernel _kernel;
        private readonly ElasticsearchClient _esClient;
        private const string IndexName = "products";

        public ProductRepository(PerfumeDbContext context, Kernel kernel, ElasticsearchClient esClient) : base(context)
        {
            _kernel = kernel;
            _esClient = esClient;
        }

        public async Task<Product?> GetProductByIdWithAttributesAsync(Guid productId)
            => await _context.Products
                .Where(p => !p.IsDeleted)
                .Include(p => p.ProductAttributes)
                .FirstOrDefaultAsync(p => p.Id == productId);

        public async Task<bool> HasActiveVariantsAsync(Guid productId)
            => await _context.Products
                .AnyAsync(p => !p.IsDeleted && p.Id == productId && p.Variants.Any(v => !v.IsDeleted));

        public async Task<List<ProductLookupItem>> GetProductLookupListAsync()
            => await _context.Products
                .Where(p => !p.IsDeleted)
                .Select(p => new ProductLookupItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    BrandName = p.Brand.Name,
                    PrimaryImageUrl = p.Media
                        .Where(m => m.IsPrimary && !m.IsDeleted)
                        .Select(m => m.Url)
                        .FirstOrDefault()
                })
                .AsNoTracking()
                .ToListAsync();

        public async Task<ProductResponse?> GetProductResponseAsync(Guid productId)
        {
            var now = DateTime.UtcNow;

            var raw = await _context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Id == productId)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Gender,
                    p.Origin,
                    p.ReleaseYear,
                    p.BrandId,
                    BrandName = p.Brand.Name,
                    p.CategoryId,
                    CategoryName = p.Category.Name,
                    p.Description,
                    NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
                    Media = p.Media
                        .Where(m => !m.IsDeleted)
                        .Select(m => new MediaResponse
                        {
                            Id = m.Id,
                            Url = m.Url,
                            AltText = m.AltText,
                            IsPrimary = m.IsPrimary,
                            DisplayOrder = m.DisplayOrder,
                            MimeType = m.MimeType,
                            FileSize = m.FileSize
                        })
                        .ToList(),
                    Attributes = p.ProductAttributes
                        .Select(pa => new ProductAttributeResponse
                        {
                            Id = pa.Id,
                            AttributeId = pa.AttributeId,
                            ValueId = pa.ValueId,
                            Attribute = pa.Attribute.Name,
                            Description = pa.Attribute.Description ?? string.Empty,
                            Value = pa.Value.Value
                        })
                        .ToList(),
                    Variants = p.Variants
                        .Where(v => !v.IsDeleted)
                        .Select(v => new
                        {
                            Variant = new ProductVariantResponse
                            {
                                Id = v.Id,
                                ProductId = v.ProductId,
                                ProductName = p.Name,
                                Barcode = v.Barcode,
                                Sku = v.Sku,
                                VolumeMl = v.VolumeMl,
                                ConcentrationId = v.ConcentrationId,
                                ConcentrationName = v.Concentration.Name,
                                Type = v.Type,
                                BasePrice = v.BasePrice,
                                RetailPrice = v.RetailPrice,
                                Status = v.Status,
                                Sillage = v.Sillage,
                                Longevity = v.Longevity,
                                StockQuantity = v.Stock != null
                                    ? v.Stock.TotalQuantity - v.Stock.ReservedQuantity
                                    : 0,
                                Media = v.Media
                                    .Where(m => !m.IsDeleted)
                                    .OrderBy(m => m.DisplayOrder)
                                    .Select(m => new MediaResponse
                                    {
                                        Id = m.Id,
                                        Url = m.Url,
                                        AltText = m.AltText,
                                        IsPrimary = m.IsPrimary,
                                        DisplayOrder = m.DisplayOrder,
                                        MimeType = m.MimeType,
                                        FileSize = m.FileSize
                                    })
                                    .ToList(),
                                Attributes = v.ProductAttributes
                                    .Select(pa => new ProductAttributeResponse
                                    {
                                        Id = pa.Id,
                                        AttributeId = pa.AttributeId,
                                        ValueId = pa.ValueId,
                                        Attribute = pa.Attribute.Name,
                                        Description = pa.Attribute.Description ?? string.Empty,
                                        Value = pa.Value.Value
                                    })
                                    .ToList()
                            },
                            ActiveVoucher = v.PromotionItems
                                .Where(pi =>
                                    !pi.IsDeleted &&
                                    pi.IsActive &&
                                    !pi.Campaign.IsDeleted &&
                                    pi.Campaign.Status == CampaignStatus.Active &&
                                    pi.Campaign.StartDate <= now &&
                                    pi.Campaign.EndDate >= now)
                                .OrderByDescending(pi => pi.CreatedAt)
                                .SelectMany(pi => pi.Campaign.Vouchers
                                    .Where(voucher =>
                                        !voucher.IsDeleted &&
                                        voucher.ExpiryDate >= now &&
                                        voucher.ApplyType == VoucherType.Product &&
                                        (voucher.RemainingQuantity == null || voucher.RemainingQuantity > 0) &&
                                        (!voucher.TargetItemType.HasValue || voucher.TargetItemType == pi.ItemType))
                                    .Select(voucher => new
                                    {
                                        CampaignName = pi.Campaign.Name,
                                        voucher.Code,
                                        voucher.DiscountType,
                                        voucher.DiscountValue
                                    }))
                                .OrderByDescending(x => x.DiscountValue)
                                .FirstOrDefault()
                        })
                        .ToList(),
                    OlfactoryFamilies = p.ProductFamilyMaps
                        .Select(pfm => new ProductOlfactoryFamilyResponse
                        {
                            OlfactoryFamilyId = pfm.OlfactoryFamilyId,
                            Name = pfm.OlfactoryFamily.Name
                        })
                        .ToList(),
                    ScentNotes = p.ProductScentMaps
                        .Select(psm => new ProductScentNoteResponse
                        {
                            NoteId = psm.ScentNoteId,
                            Name = psm.ScentNote.Name,
                            Type = psm.NoteType
                        })
                        .ToList()
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (raw == null)
                return null;

            var variants = raw.Variants
                .Select(x =>
                {
                    var variant = x.Variant;

                    if (x.ActiveVoucher != null)
                    {
                        variant.CampaignName = x.ActiveVoucher.CampaignName;
                        variant.VoucherCode = x.ActiveVoucher.Code;

                        var discounted = x.ActiveVoucher.DiscountType == DiscountType.Percentage
                            ? variant.BasePrice * (1 - x.ActiveVoucher.DiscountValue / 100m)
                            : variant.BasePrice - x.ActiveVoucher.DiscountValue;

                        variant.DiscountedPrice = discounted < 0 ? 0 : discounted;
                    }

                    return variant;
                })
                .ToList();

            return new ProductResponse
            {
                Id = raw.Id,
                Name = raw.Name,
                Gender = raw.Gender,
                Origin = raw.Origin,
                ReleaseYear = raw.ReleaseYear,
                BrandId = raw.BrandId,
                BrandName = raw.BrandName,
                CategoryId = raw.CategoryId,
                CategoryName = raw.CategoryName,
                Description = raw.Description,
                NumberOfVariants = raw.NumberOfVariants,
                Media = raw.Media,
                Variants = variants,
                Attributes = raw.Attributes,
                OlfactoryFamilies = raw.OlfactoryFamilies,
                ScentNotes = raw.ScentNotes
            };
        }

        public async Task<(List<ProductListItem> Items, int TotalCount)> GetPagedProductListItemsAsync(
            GetPagedProductRequest request)
        {
            var now = DateTime.UtcNow;

            var query = _context.Products
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (request.Gender.HasValue)
                query = query.Where(p => p.Gender == request.Gender.Value);

            if (request.Volume.HasValue)
                query = query.Where(p =>
                    p.Variants.Any(v => !v.IsDeleted && v.VolumeMl == request.Volume.Value));

            if (request.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);

            if (request.BrandId.HasValue)
                query = query.Where(p => p.BrandId == request.BrandId.Value);

            if (request.FromPrice.HasValue)
                query = query.Where(p =>
                    p.Variants.Any(v => !v.IsDeleted && v.BasePrice >= request.FromPrice.Value));

            if (request.ToPrice.HasValue)
                query = query.Where(p =>
                    p.Variants.Any(v => !v.IsDeleted && v.BasePrice <= request.ToPrice.Value));

            if (request.IsAvailable == true)
                query = query.Where(p =>
                    p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.Stock.TotalQuantity - v.Stock.ReservedQuantity > 0));

            var totalCount = await query.CountAsync();

            var itemsWithTags = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new
                {
                    Item = new ProductListItem
                    {
                        Id = p.Id,
                        Name = p.Name,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Name,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        Description = p.Description,
                        NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
                        VariantPrices = p.Variants
                            .Where(v => !v.IsDeleted)
                            .Select(v => v.BasePrice)
                            .ToList(),
                        PrimaryImage = p.Media
                            .Where(m => m.IsPrimary && !m.IsDeleted)
                            .Select(m => new MediaResponse
                            {
                                Id = m.Id,
                                Url = m.Url,
                                AltText = m.AltText,
                                IsPrimary = m.IsPrimary,
                                DisplayOrder = m.DisplayOrder,
                                MimeType = m.MimeType,
                                FileSize = m.FileSize
                            })
                            .FirstOrDefault()
                    },
                    HasSaleTag = p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.PromotionItems.Any(pi =>
                            !pi.IsDeleted &&
                            !pi.Campaign.IsDeleted &&
                            pi.Campaign.Status == CampaignStatus.Active &&
                            pi.Campaign.StartDate <= now &&
                            pi.Campaign.EndDate >= now &&
                            pi.IsActive)),
                    HasNewTag = p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.PromotionItems.Any(pi =>
                            !pi.IsDeleted &&
                            !pi.Campaign.IsDeleted &&
                            pi.ItemType == PromotionType.NewArrival &&
                            pi.Campaign.Status == CampaignStatus.Active &&
                            pi.Campaign.StartDate <= now &&
                            pi.Campaign.EndDate >= now &&
                            pi.IsActive))
                })
                .AsNoTracking()
                .ToListAsync();

            var items = itemsWithTags
                .Select(x =>
                {
                    if (x.HasSaleTag)
                        x.Item.Tags.Add("sale");

                    if (x.HasNewTag)
                        x.Item.Tags.Add("new");

                    return x.Item;
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<(List<ProductListItem> Items, int TotalCount)> GetBestSellerProductsAsync(
            GetPagedProductRequest request)
        {
            var now = DateTime.UtcNow;
            var limitDate = DateTime.UtcNow.AddDays(-30);

            var query = _context.Products
                .Where(p => !p.IsDeleted && p.Variants.Any(v =>
                    !v.IsDeleted &&
                    v.Stock.TotalQuantity - v.Stock.ReservedQuantity > 0))
                .Select(p => new
                {
                    Product = p,
                    OrderCount = p.Variants
                        .Where(v => !v.IsDeleted)
                        .SelectMany(v => v.OrderDetails)
                        .Count(od => od.Order != null && od.Order.CreatedAt >= limitDate)
                })
                .OrderByDescending(x => x.OrderCount)
                .Select(x => x.Product);

            var totalCount = await query.CountAsync();

            var itemsWithTags = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new
                {
                    Item = new ProductListItem
                    {
                        Id = p.Id,
                        Name = p.Name,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Name,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        Description = p.Description,
                        NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
                        VariantPrices = p.Variants
                            .Where(v => !v.IsDeleted)
                            .Select(v => v.BasePrice)
                            .ToList(),
                        PrimaryImage = p.Media
                            .Where(m => m.IsPrimary && !m.IsDeleted)
                            .Select(m => new MediaResponse
                            {
                                Id = m.Id,
                                Url = m.Url,
                                AltText = m.AltText,
                                IsPrimary = m.IsPrimary,
                                DisplayOrder = m.DisplayOrder,
                                MimeType = m.MimeType,
                                FileSize = m.FileSize
                            })
                            .FirstOrDefault()
                    },
                    HasSaleTag = p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.PromotionItems.Any(pi =>
                            !pi.IsDeleted &&
                            !pi.Campaign.IsDeleted &&
                            pi.Campaign.Status == CampaignStatus.Active &&
                            pi.Campaign.StartDate <= now &&
                            pi.Campaign.EndDate >= now &&
                            pi.IsActive)),
                    HasNewTag = p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.PromotionItems.Any(pi =>
                            !pi.IsDeleted &&
                            !pi.Campaign.IsDeleted &&
                            pi.ItemType == PromotionType.NewArrival &&
                            pi.Campaign.Status == CampaignStatus.Active &&
                            pi.Campaign.StartDate <= now &&
                            pi.Campaign.EndDate >= now &&
                            pi.IsActive))
                })
                .AsNoTracking()
                .ToListAsync();

            var items = itemsWithTags
                .Select(x =>
                {
                    if (x.HasSaleTag)
                        x.Item.Tags.Add("sale");

                    if (x.HasNewTag)
                        x.Item.Tags.Add("new");

                    return x.Item;
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<(List<ProductListItem> Items, int TotalCount)> GetNewArrivalProductsAsync(
            GetPagedProductRequest request)
        {
            var now = DateTime.UtcNow;

            var query = _context.Products
                .Where(p => !p.IsDeleted && p.Variants.Any(v =>
                    !v.IsDeleted &&
                    v.Stock.TotalQuantity - v.Stock.ReservedQuantity > 0))
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();

            var itemsWithTags = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new
                {
                    Item = new ProductListItem
                    {
                        Id = p.Id,
                        Name = p.Name,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Name,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        Description = p.Description,
                        NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
                        VariantPrices = p.Variants
                            .Where(v => !v.IsDeleted)
                            .Select(v => v.BasePrice)
                            .ToList(),
                        PrimaryImage = p.Media
                            .Where(m => m.IsPrimary && !m.IsDeleted)
                            .Select(m => new MediaResponse
                            {
                                Id = m.Id,
                                Url = m.Url,
                                AltText = m.AltText,
                                IsPrimary = m.IsPrimary,
                                DisplayOrder = m.DisplayOrder,
                                MimeType = m.MimeType,
                                FileSize = m.FileSize
                            })
                            .FirstOrDefault()
                    },
                    HasSaleTag = p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.PromotionItems.Any(pi =>
                            !pi.IsDeleted &&
                            !pi.Campaign.IsDeleted &&
                            pi.Campaign.Status == CampaignStatus.Active &&
                            pi.Campaign.StartDate <= now &&
                            pi.Campaign.EndDate >= now &&
                            pi.IsActive))
                })
                .AsNoTracking()
                .ToListAsync();

            var items = itemsWithTags
                .Select(x =>
                {
                    if (x.HasSaleTag)
                        x.Item.Tags.Add("sale");

                    x.Item.Tags.Add("new");

                    return x.Item;
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<(List<ProductListItem> Items, int TotalCount)> GetCampaignProductsAsync(
            Guid campaignId,
            GetPagedProductRequest request)
        {
            var now = DateTime.UtcNow;

            var query = _context.Products
                .Where(p => !p.IsDeleted && p.Variants.Any(v =>
                    !v.IsDeleted &&
                    v.PromotionItems.Any(pi =>
                        !pi.IsDeleted &&
                        pi.IsActive &&
                        pi.CampaignId == campaignId &&
                        !pi.Campaign.IsDeleted &&
                        pi.Campaign.Status == CampaignStatus.Active &&
                        pi.Campaign.StartDate <= now &&
                        pi.Campaign.EndDate >= now)));

            if (request.Gender.HasValue)
                query = query.Where(p => p.Gender == request.Gender.Value);

            if (request.Volume.HasValue)
                query = query.Where(p =>
                    p.Variants.Any(v => !v.IsDeleted && v.VolumeMl == request.Volume.Value));

            if (request.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);

            if (request.BrandId.HasValue)
                query = query.Where(p => p.BrandId == request.BrandId.Value);

            if (request.FromPrice.HasValue)
                query = query.Where(p =>
                    p.Variants.Any(v => !v.IsDeleted && v.BasePrice >= request.FromPrice.Value));

            if (request.ToPrice.HasValue)
                query = query.Where(p =>
                    p.Variants.Any(v => !v.IsDeleted && v.BasePrice <= request.ToPrice.Value));

            if (request.IsAvailable == true)
                query = query.Where(p =>
                    p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.Stock.TotalQuantity - v.Stock.ReservedQuantity > 0));

            var totalCount = await query.CountAsync();

            var itemsWithTags = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new
                {
                    Item = new ProductListItem
                    {
                        Id = p.Id,
                        Name = p.Name,
                        BrandId = p.BrandId,
                        BrandName = p.Brand.Name,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        Description = p.Description,
                        NumberOfVariants = p.Variants.Count(v => !v.IsDeleted),
                        VariantPrices = p.Variants
                            .Where(v => !v.IsDeleted)
                            .Select(v => v.BasePrice)
                            .ToList(),
                        PrimaryImage = p.Media
                            .Where(m => m.IsPrimary && !m.IsDeleted)
                            .Select(m => new MediaResponse
                            {
                                Id = m.Id,
                                Url = m.Url,
                                AltText = m.AltText,
                                IsPrimary = m.IsPrimary,
                                DisplayOrder = m.DisplayOrder,
                                MimeType = m.MimeType,
                                FileSize = m.FileSize
                            })
                            .FirstOrDefault()
                    },
                    HasSaleTag = p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.PromotionItems.Any(pi =>
                            !pi.IsDeleted &&
                            pi.IsActive &&
                            pi.CampaignId == campaignId &&
                            !pi.Campaign.IsDeleted &&
                            pi.Campaign.Status == CampaignStatus.Active &&
                            pi.Campaign.StartDate <= now &&
                            pi.Campaign.EndDate >= now)),
                    HasNewTag = p.Variants.Any(v =>
                        !v.IsDeleted &&
                        v.PromotionItems.Any(pi =>
                            !pi.IsDeleted &&
                            pi.IsActive &&
                            pi.CampaignId == campaignId &&
                            !pi.Campaign.IsDeleted &&
                            pi.ItemType == PromotionType.NewArrival &&
                            pi.Campaign.Status == CampaignStatus.Active &&
                            pi.Campaign.StartDate <= now &&
                            pi.Campaign.EndDate >= now))
                })
                .AsNoTracking()
                .ToListAsync();

            var items = itemsWithTags
                .Select(x =>
                {
                    if (x.HasSaleTag)
                        x.Item.Tags.Add("sale");

                    if (x.HasNewTag)
                        x.Item.Tags.Add("new");

                    return x.Item;
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<ProductInforResponse?> GetProductInfoAsync(Guid productId)
            => await _context.Products
                .Where(p => !p.IsDeleted && p.Id == productId)
                .ProjectToType<ProductInforResponse>()
                .AsNoTracking()
                .FirstOrDefaultAsync();

        public async Task<ProductFastLookResponse?> GetProductFastLookAsync(Guid productId)
            => await _context.Products
                .Where(p => !p.IsDeleted && p.Id == productId)
                .Select(p => new ProductFastLookResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description != null
                        ? p.Description.Substring(0, Math.Min(p.Description.Length, 100))
                          + (p.Description.Length > 100 ? "..." : "")
                        : string.Empty,
                    BrandName = p.Brand.Name,
                    Gender = p.Gender,
                    Rating = (int)Math.Round(
                        p.Variants
                            .Where(v => !v.IsDeleted)
                            .SelectMany(v => v.OrderDetails)
                            .Where(od => od.Review != null)
                            .Select(od => (double?)od.Review!.Rating)
                            .Average() ?? 0),
                    ReviewCount = p.Variants
                        .Where(v => !v.IsDeleted)
                        .SelectMany(v => v.OrderDetails)
                        .Count(od => od.Review != null),
                    Variants = p.Variants
                        .Where(v => !v.IsDeleted)
                        .Select(v => new VariantFastLookResponse
                        {
                            Id = v.Id,
                            Sku = v.Sku,
                            DisplayName = $"{v.Concentration.Name} - {v.VolumeMl}ml",
                            Price = v.BasePrice,
                            RetailPrice = v.RetailPrice,
                            StockQuantity = v.Stock.TotalQuantity - v.Stock.ReservedQuantity,
                            Media = v.Media
                                .Where(m => m.IsPrimary && !m.IsDeleted)
                                .Select(m => new MediaResponse
                                {
                                    Id = m.Id,
                                    Url = m.Url,
                                    AltText = m.AltText,
                                    IsPrimary = m.IsPrimary,
                                    DisplayOrder = m.DisplayOrder,
                                    MimeType = m.MimeType,
                                    FileSize = m.FileSize
                                })
                                .FirstOrDefault()
                        })
                        .ToList()
                })
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync();

        #region Search Methods (Elasticsearch)

        /// <summary>Document shape stored in Elasticsearch "products" index.</summary>
        private sealed record ProductDocument(
            string Id,
            string Name,
            string Brand,
            string Category,
            string? Description,
            string Gender,
            int ReleaseYear,
            string Origin,
            IEnumerable<string> Attributes,
            IEnumerable<string> Concentrations,
            IEnumerable<string> Volumes,
            IEnumerable<string> Skus,
            IEnumerable<string> Barcodes);

        public async Task<(List<ProductListItemWithVariants> Items, int TotalCount)>
            GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return ([], 0);

            var from = (request.PageNumber - 1) * request.PageSize;

            var processedText = searchText.ToLowerInvariant();
            var isOrQuery = processedText.Contains(" ho\u1eb7c ");

            // Pre-process Text: Extract age and volume intent
            // Ví dụ: người dùng gõ "nước hoa nam 20 tuổi 100ml"
            var ageMatch = Regex.Match(processedText, @"(\d+)\s*tu\u1ed5i");
            if (ageMatch.Success)
            {
                var ageNum = ageMatch.Groups[1].Value;
                processedText = processedText.Replace(ageMatch.Value, $"age_{ageNum}");
            }

            var volumeMatch = Regex.Match(processedText, @"(\d+)\s*ml");
            if (volumeMatch.Success)
            {
                var volNum = volumeMatch.Groups[1].Value;
                processedText = processedText.Replace(volumeMatch.Value, $"volume_{volNum}");
            }

            // Dọn dẹp từ khoá "và" / "hoặc" dư thừa
            processedText = processedText
                .Replace(" v\u00e0 ", " ")
                .Replace(" ho\u1eb7c ", " ");

            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "có", "co", "và", "va", "hoặc", "hoac", "hương", "huong", "mùi", "mui"
            };

            var normalizedTerms = processedText
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => !stopWords.Contains(t))
                .ToArray();

            processedText = normalizedTerms.Length > 0
                ? string.Join(' ', normalizedTerms)
                : processedText;

            // N?u ngu?i dùng nh?p "ho?c" thì dùng Operator.Or, còn m?c d?nh là Operator.And (ph?i th?a mãn t?t c?)
            var defaultOp = Operator.Or;
            var minimumShouldMatch = isOrQuery ? "1" : normalizedTerms.Length <= 2 ? "100%" : normalizedTerms.Length <= 4 ? "75%" : "60%";

            Action<QueryDescriptor<ProductDocument>> queryDesc = q => q
                .MultiMatch(sq => sq
                    .Query(processedText)
                    .Fields(new[] { "name^5", "brand^4", "attributes^3", "concentrations^2", "category^2", "description", "gender^2", "origin", "volumes", "skus", "barcodes" })
                    .Operator(defaultOp)
                    .Type(TextQueryType.BestFields)
                    .MinimumShouldMatch(minimumShouldMatch)
                    .Fuzziness(new Fuzziness(1))
                );

            // Get total count
            var countResponse = await _esClient.CountAsync<ProductDocument>(c => c
                .Indices(IndexName)
                .Query(queryDesc));
            var totalCount = countResponse.IsValidResponse ? (int)countResponse.Count : 0;

            if (totalCount == 0)
                return ([], 0);

            // Get paged results
            var esResponse = await _esClient.SearchAsync<ProductDocument>(s => s
                .Indices(IndexName)
                .From(from)
                .Size(request.PageSize)
                .Query(queryDesc));

            if (!esResponse.IsValidResponse)
                return ([], totalCount);

            var ids = esResponse.Hits
                .Where(h => h.Source != null)
                .Select(h => Guid.Parse(h.Source!.Id))
                .ToList();

            if (ids.Count == 0)
                return ([], totalCount);

            // Preserve ES ranking order
            var dbItems = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ProjectToType<ProductListItemWithVariants>()
                .AsNoTracking()
                .ToListAsync();

            var map = dbItems.ToDictionary(x => x.Id);
            var ordered = ids
                .Where(map.ContainsKey)
                .Select(id => map[id])
                .ToList();

            return (ordered, totalCount);
        }

        public async Task IndexAllProductsToElasticsearchAsync()
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductAttributes)
                    .ThenInclude(pa => pa.Attribute)
                .Include(p => p.ProductAttributes)
                    .ThenInclude(pa => pa.Value)
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.Concentration)
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.ProductAttributes)
                        .ThenInclude(pa => pa.Attribute)
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.ProductAttributes)
                        .ThenInclude(pa => pa.Value)
                .Where(p => !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            if (products.Count == 0)
            {
                Console.WriteLine("[ES] No products to index.");
                return;
            }

            Console.WriteLine($"[ES] Indexing {products.Count} products...");
            var docs = products.Select(BuildProductDocument).ToList();

            var bulkResponse = await _esClient.BulkAsync(b => b
                .Index(IndexName)
                .IndexMany(docs, (op, doc) => op.Id(doc.Id)));

            if (!bulkResponse.IsValidResponse || bulkResponse.Errors)
            {
                var errorDetails = new StringBuilder();
                errorDetails.AppendLine($"[ES] Bulk operation failed. Valid Response: {bulkResponse.IsValidResponse}, Has Errors: {bulkResponse.Errors}");

                if (bulkResponse.ItemsWithErrors?.Any() == true)
                {
                    foreach (var item in bulkResponse.ItemsWithErrors.Take(5))
                    {
                        errorDetails.AppendLine($"  - Item ID: {item.Id}, Error: {item.Error?.Type} - {item.Error?.Reason}");
                    }
                }

                if (!string.IsNullOrEmpty(bulkResponse.ApiCallDetails?.DebugInformation))
                {
                    var debugInfo = bulkResponse.ApiCallDetails.DebugInformation;
                    errorDetails.AppendLine($"  - Debug: {debugInfo.Substring(0, Math.Min(500, debugInfo.Length))}");
                }

                var errorMessage = errorDetails.ToString();
                Console.WriteLine(errorMessage);
                throw new Exception($"Elasticsearch bulk index failed: {errorMessage}");
            }

            Console.WriteLine($"[ES] Successfully indexed {docs.Count} products.");
        }

        public async Task IndexProductToElasticsearchAsync(Guid productId)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductAttributes)
                    .ThenInclude(pa => pa.Attribute)
                .Include(p => p.ProductAttributes)
                    .ThenInclude(pa => pa.Value)
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.Concentration)
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.ProductAttributes)
                        .ThenInclude(pa => pa.Attribute)
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.ProductAttributes)
                        .ThenInclude(pa => pa.Value)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (product == null) return;

            var doc = BuildProductDocument(product);
            await _esClient.IndexAsync(doc, x => x.Index(IndexName).Id(doc.Id));
        }

        #endregion

        #region Private Methods

        public async Task<List<ProductDailySaleFigureResponse>> GetProductDailySaleFiguresAsync(DateOnly date)
        {
            var startDate = date.ToDateTime(TimeOnly.MinValue);
            var endDate = date.ToDateTime(TimeOnly.MaxValue);
            var dailyFigures = await _context.Products
                .Where(p => !p.IsDeleted)
                .Select(p => new ProductDailySaleFigureResponse
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    DailySaleFigures = p.Variants
                        .Where(v => !v.IsDeleted)
                        .Select(v => new VariantDailySaleFigure
                        {
                            VariantId = v.Id,
                            VariantName = $"{v.Concentration.Name} - {v.VolumeMl}ml",
                            QuantitySold = v.OrderDetails
                                .Where(od => od.Order != null && od.Order.CreatedAt >= startDate && od.Order.CreatedAt <= endDate)
                                .Sum(od => od.Quantity),
                            Date = date
                        })
                        .ToList()
                })
                .AsNoTracking()
                .ToListAsync();

            return dailyFigures.Where(df => df.DailySaleFigures.Any(v => v.QuantitySold > 0)).ToList();
        }

        private static ProductDocument BuildProductDocument(Product product)
        {
            var attributes = new HashSet<string>();

            if (product.ProductAttributes != null)
            {
                foreach (var pa in product.ProductAttributes.Where(pa => pa.Attribute != null && pa.Value != null))
                {
                    attributes.Add($"{pa.Attribute.Name}: {pa.Value.Value}");
                    attributes.Add(pa.Value.Value);

                    // Xử lý dải tuổi mở rộng
                    if (pa.Attribute.Name.Contains("tu\u1ed5i", StringComparison.OrdinalIgnoreCase) || 
                        pa.Value.Value.Contains("tu\u1ed5i", StringComparison.OrdinalIgnoreCase))
                    {
                        var valStr = pa.Value.Value.ToLower();

                        // Trường hợp "trên X tuổi"
                        var matchOver = Regex.Match(valStr, @"tr\u00ean\s+(\d+)"); // "trên"
                        if (matchOver.Success && int.TryParse(matchOver.Groups[1].Value, out int minOver))
                        {
                            for (int i = minOver; i <= 60; i++) attributes.Add($"age_{i}");
                            continue;
                        }

                        // Trường hợp "dưới Y tuổi"
                        var matchUnder = Regex.Match(valStr, @"d\u01b0\u1edbi\s+(\d+)"); // "dưới"
                        if (matchUnder.Success && int.TryParse(matchUnder.Groups[1].Value, out int maxUnder))
                        {
                            for (int i = 12; i <= maxUnder; i++) attributes.Add($"age_{i}");
                            continue;
                        }

                        // Trường hợp "mọi lứa tuổi"
                        if (valStr.Contains("m\u1ecdi l\u1ee9a tu\u1ed5i")) // "mọi lứa tuổi"
                        {
                            for (int i = 12; i <= 60; i++) attributes.Add($"age_{i}");
                            continue;
                        }

                        // Phân tích dải tuổi (VD: "20-29" hoặc "15-19")
                        var matchRange = Regex.Match(valStr, @"(\d+)\s*[-–]\s*(\d+)");
                        if (matchRange.Success)
                        {
                            if (int.TryParse(matchRange.Groups[1].Value, out int min) && 
                                int.TryParse(matchRange.Groups[2].Value, out int max))
                            {
                                for (int i = min; i <= max; i++)
                                {
                                    attributes.Add($"age_{i}");
                                }
                            }
                        }
                    }
                }
            }

            var concentrations = new HashSet<string>();
            var volumes = new HashSet<string>();
            var skus = new HashSet<string>();
            var barcodes = new HashSet<string>();

            if (product.Variants != null)
            {
                foreach (var v in product.Variants)
                {
                    if (v.Concentration != null) concentrations.Add(v.Concentration.Name);

                    // Thêm prefix cho thể tích để tránh lẫn lộn với tuổi (VD: volume_100)
                    volumes.Add($"volume_{v.VolumeMl}");
                    volumes.Add($"{v.VolumeMl}ml");

                    if (!string.IsNullOrEmpty(v.Sku)) skus.Add(v.Sku);
                    if (!string.IsNullOrEmpty(v.Barcode)) barcodes.Add(v.Barcode);

                    if (v.ProductAttributes != null)
                    {
                        foreach (var pa in v.ProductAttributes.Where(pa => pa.Attribute != null && pa.Value != null))
                        {
                            attributes.Add($"{pa.Attribute.Name}: {pa.Value.Value}");
                            attributes.Add(pa.Value.Value);
                        }
                    }
                }
            }

            var genderText = product.Gender switch
            {
                Gender.Male => "Male Nam ng\u01b0\u1eddi nam cho nam",
                Gender.Female => "Female N\u1eef ng\u01b0\u1eddi n\u1eef cho n\u1eef",
                Gender.Unisex => "Unisex Nam N\u1eef c\u1ea3 nam v\u00e0 n\u1eef",
                _ => product.Gender.ToString()
            };

            return new ProductDocument(
                Id: product.Id.ToString(),
                Name: product.Name ?? string.Empty,
                Brand: product.Brand?.Name ?? string.Empty,
                Category: product.Category?.Name ?? string.Empty,
                Description: product.Description,
                Gender: genderText,
                ReleaseYear: product.ReleaseYear,
                Origin: product.Origin ?? string.Empty,
                Attributes: attributes.ToList(),
                Concentrations: concentrations.ToList(),
                Volumes: volumes.ToList(),
                Skus: skus.ToList(),
                Barcodes: barcodes.ToList());
        }

        #endregion Private Methods
    }
}







