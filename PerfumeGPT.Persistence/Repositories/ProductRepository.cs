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
using PerfumeGPT.Persistence.Repositories.Elasticsearch;
using Microsoft.Extensions.AI;

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
        // ProductDocument moved to Infrastructure namespace

        public async Task<(List<SemanticSearchProductResponse> Items, int TotalCount)> GetPagedProductsWithSemanticSearch(string searchText, GetPagedProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return ([], 0);

            var from = (request.PageNumber - 1) * request.PageSize;

            // Dọn dẹp Text Search
            var processedText = searchText.Trim();
            var isOrQuery = request.IsOrSearch ?? false;
            var normalizedTerms = processedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var defaultOp = Operator.Or;
            var minimumShouldMatch = isOrQuery ? "1" : normalizedTerms.Length <= 2 ? "75%" : normalizedTerms.Length <= 4 ? "67%" : "60%";

            // 2. Sinh Query Vector để thực hiện Hybrid Search (Text + Vector)
            var embeddingService = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            var embeddings = await embeddingService.GenerateAsync(processedText);
            var queryVectorArray = embeddings.Vector.ToArray();

            // 3. Thực hiện Diagnostic Search (Debug từng stage)
            var queryDesc = ProductSearchQueryBuilder.BuildQuery(processedText, minimumShouldMatch, defaultOp, request);

            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "es_diagnostics.txt");
            var sbLog = new StringBuilder();
            sbLog.AppendLine($"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] QUERY: '{searchText}' ------------------------------");

            // --- STAGE 1: kNN Only (Retrieval) ---
            var knnOnlyResponse = await _esClient.SearchAsync<ProductDocument>(s => s
                .Indices(IndexName)
                .Size(5)
                .Knn(k => k
                    .Field("embedding")
                    .QueryVector(queryVectorArray)
                    .K(20)
                    .NumCandidates(100)
                )
                .Query(q => q.MatchAll())
            );
            sbLog.AppendLine($"[STAGE 1] kNN Only (Retrieval) found: {knnOnlyResponse.Total} hits");
            foreach (var h in knnOnlyResponse.Hits) sbLog.AppendLine($"  - Found ID: {h.Id} | Score: {h.Score}");

            // --- STAGE 2: BM25 Only (Precision & Filters) ---
            var textOnlyResponse = await _esClient.SearchAsync<ProductDocument>(s => s
                .Indices(IndexName)
                .Size(5)
                .Query(queryDesc)
            );
            sbLog.AppendLine($"[STAGE 2] BM25/Filter Only found: {textOnlyResponse.Total} hits");
            foreach (var h in textOnlyResponse.Hits) sbLog.AppendLine($"  - Found ID: {h.Id} | Score: {h.Score}");

            // --- STAGE 3: Hybrid Final (Combined) ---
            var esResponse = await _esClient.SearchAsync<ProductDocument>(s => s
                .Indices(IndexName)
                .From(from)
                .Size(request.PageSize)
                .Knn(k => k
                    .Field("embedding")
                    .QueryVector(queryVectorArray)
                    .K(20)
                    .NumCandidates(100)
                )
                .Query(queryDesc)
                .Rank(r => r.Rrf(rrf => rrf.RankConstant(60)))
            );

            sbLog.AppendLine($"[STAGE 3] Final Hybrid search found: {esResponse.Total} hits");

            if (!esResponse.IsValidResponse)
            {
                sbLog.AppendLine($"[ES ERROR] {esResponse.DebugInformation}");
                await File.AppendAllTextAsync(logPath, sbLog.ToString());
                return ([], 0);
            }

            // Log detailed explanation for top hits
            foreach (var hit in esResponse.Hits.Take(3))
            {
                sbLog.AppendLine($"\n--- Explanation for ID: {hit.Id} (Score: {hit.Score}) ---");
                // if (hit.Explanation != null) PrintExplanation(hit.Explanation.Description, hit.Explanation.Value, hit.Explanation.Details, 0);
            }

            await File.AppendAllTextAsync(logPath, sbLog.ToString());

            var totalCount = (int)esResponse.Total;
            if (totalCount == 0)
                return ([], 0);

            var idsAndScores = esResponse.Hits
                .Where(h => h.Source != null && h.Score >= 1.0) // Giảm min_score để dễ debug
                .Select(h => Guid.Parse(h.Source!.Id))
                .ToList();

            if (idsAndScores.Count == 0)
                return ([], totalCount);

            // Preserve ES ranking order
            var dbItems = await _context.Products
                .Where(p => idsAndScores.Contains(p.Id))
                .ProjectToType<SemanticSearchProductResponse>()
                .AsNoTracking()
                .ToListAsync();

            // Lấy nhãn nốt hương và nhóm hương từ ES hits để gán vào response (vì DB ProjectToType có thể thiếu cấu hình phức tạp)
            var esSourceMap = esResponse.Hits
                .Where(h => h.Source != null)
                .ToDictionary(h => Guid.Parse(h.Source!.Id), h => h.Source!);

            var map = dbItems.ToDictionary(x => x.Id);
            var ordered = idsAndScores
                .Where(map.ContainsKey)
                .Select(id =>
                {
                    var item = map[id];
                    if (esSourceMap.TryGetValue(id, out var source))
                    {
                        item.ScentNotes = source.ScentNotes?.ToList() ?? [];
                        item.OlfactoryFamilies = source.OlfactoryFamilies?.ToList() ?? [];
                    }
                    return item;
                })
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
                .Include(p => p.ProductScentMaps)
                    .ThenInclude(psm => psm.ScentNote)
                .Include(p => p.ProductFamilyMaps)
                    .ThenInclude(pfm => pfm.OlfactoryFamily)
                .Where(p => !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            if (products.Count == 0)
            {
                Console.WriteLine("[ES] No products to index.");
                return;
            }

            Console.WriteLine($"[ES] Indexing {products.Count} products with embeddings...");
            var embeddingService = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            var docs = new List<ProductDocument>();

            foreach (var p in products)
            {
                var textToEmbed = $"{p.Name} {p.Brand?.Name} {string.Join(' ', p.ProductScentMaps?.Select(sm => sm.ScentNote?.Name) ?? [])}";
                var embeddings = await embeddingService.GenerateAsync(textToEmbed);

                var doc = BuildProductDocument(p, embeddings.Vector.ToArray());
                docs.Add(doc);

                if (docs.Count % 10 == 0)
                {
                    Console.WriteLine($"  - Prepared {docs.Count}/{products.Count} documents...");
                }
            }

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
                .Include(p => p.ProductScentMaps)
                    .ThenInclude(psm => psm.ScentNote)
                .Include(p => p.ProductFamilyMaps)
                    .ThenInclude(pfm => pfm.OlfactoryFamily)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (product == null) return;

            var embeddingService = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            var textToEmbed = $"{product.Name} {product.Brand?.Name} {string.Join(' ', product.ProductScentMaps?.Select(sm => sm.ScentNote?.Name) ?? [])}";
            var embeddings = await embeddingService.GenerateAsync([textToEmbed]);

            var doc = BuildProductDocument(product, embeddings[0].Vector.ToArray());
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

        private ProductDocument BuildProductDocument(Product product, float[]? embedding = null)
        {
            var attributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

            var scentNotes = product.ProductScentMaps?.Select(sm => sm.ScentNote?.Name).Where(x => x != null).ToList() ?? [];
            var olfactoryFamilies = product.ProductFamilyMaps?.Select(fm => fm.OlfactoryFamily?.Name).Where(x => x != null).ToList() ?? [];

            return new ProductDocument(
                Id: product.Id.ToString(),
                Name: product.Name ?? string.Empty,
                Brand: product.Brand?.Name ?? string.Empty,
                BrandId: product.BrandId.ToString(),
                Category: product.Category?.Name ?? string.Empty,
                CategoryId: product.CategoryId.ToString(),
                Gender: product.Gender.ToString(),
                ReleaseYear: product.ReleaseYear,
                Origin: product.Origin ?? string.Empty,
                Attributes: product.ProductAttributes?
                    .Select(am => am.Value?.Value ?? string.Empty)
                    .Distinct().ToList() ?? [],
                Concentrations: product.Variants?
                    .Select(v => v.Concentration?.Name ?? string.Empty)
                    .Distinct().ToList() ?? [],
                Volumes: product.Variants?
                    .Select(v => $"{v.VolumeMl}ml")
                    .Distinct().ToList() ?? [],
                Skus: product.Variants?
                    .Select(v => v.Sku ?? string.Empty)
                    .Distinct().ToList() ?? [],
                Barcodes: product.Variants?
                    .Select(v => v.Barcode ?? string.Empty)
                    .Distinct().ToList() ?? [],
                ScentNotes: product.ProductScentMaps?
                    .Select(sm => sm.ScentNote?.Name ?? string.Empty)
                    .Distinct().ToList() ?? [],
                OlfactoryFamilies: product.ProductFamilyMaps?
                    .Select(fm => fm.OlfactoryFamily?.Name ?? string.Empty)
                    .Distinct().ToList() ?? [],
                VariantPrices: product.Variants?
                    .Select(v => (double)v.BasePrice)
                    .Distinct().ToList() ?? [],
                Embedding: embedding
            );
        }

        private void PrintExplanation(string description, float value, IReadOnlyCollection<Elastic.Clients.Elasticsearch.Core.Explain.ExplanationDetail>? details, int indent)
        {
            var space = new string(' ', indent * 2);
            Console.WriteLine($"{space}- {description} | value = {value}");

            if (details != null)
            {
                foreach (var d in details)
                {
                    PrintExplanation(d.Description, d.Value, d.Details, indent + 1);
                }
            }
        }
        #endregion Private Methods
    }
}
