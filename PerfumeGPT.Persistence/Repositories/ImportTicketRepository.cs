using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ImportTicketRepository : GenericRepository<ImportTicket>, IImportTicketRepository
	{
		public ImportTicketRepository(PerfumeDbContext context) : base(context) { }

		public async Task<ImportTicket?> GetByIdWithDetailsAsync(Guid id)
		=> await _context.ImportTickets
			.AsNoTracking()
			.Include(it => it.ImportDetails)
			.FirstOrDefaultAsync(it => it.Id == id);

		public async Task<ImportTicketResponse?> GetResponseByIdAsync(Guid id)
		=> await _context.ImportTickets
			.AsNoTracking()
			.AsSplitQuery()
			.Where(it => it.Id == id)
		  .Select(it => new ImportTicketResponse
		  {
			  Id = it.Id,
			  CreatedByName = it.CreatedByUser.FullName ?? "Unknown",
			  VerifiedByName = it.VerifiedByUser != null ? it.VerifiedByUser.FullName : null,
			  SupplierId = it.SupplierId,
			  SupplierName = it.Supplier.Name ?? "Unknown",
			  ExpectedArrivalDate = it.ExpectedArrivalDate,
			  ActualImportDate = it.ActualImportDate,
			  TotalCost = it.TotalCost,
			  Status = it.Status,
			  CreatedAt = it.CreatedAt,
			  ImportDetails = it.ImportDetails.Select(d => new ImportDetailResponse
			  {
				  Id = d.Id,
				  VariantId = d.ProductVariantId,
				  VariantName = $"{d.ProductVariant.Product.Name ?? "Unknown"} - {d.ProductVariant.VolumeMl}",
				  VariantSku = d.ProductVariant != null ? d.ProductVariant.Sku : "Unknown",
				  ExpectedQuantity = d.ExpectedQuantity,
				  UnitPrice = d.UnitPrice,
				  TotalPrice = d.ExpectedQuantity * d.UnitPrice,
				  RejectedQuantity = d.RejectedQuantity,
				  Note = d.Note,
				  Batches = d.Batches.Select(b => new BatchResponse
				  {
					  Id = b.Id,
					  BatchCode = b.BatchCode,
					  ManufactureDate = b.ManufactureDate,
					  ExpiryDate = b.ExpiryDate,
					  ImportQuantity = b.ImportQuantity,
					  RemainingQuantity = b.RemainingQuantity,
					  CreatedAt = b.CreatedAt
				  }).ToList()
			  }).ToList()
		  })
			.FirstOrDefaultAsync();

		public async Task<ImportTicket?> GetByIdWithDetailsAndBatchesAsync(Guid id)
		=> await _context.ImportTickets
			.Include(it => it.ImportDetails)
				.ThenInclude(d => d.Batches)
			.FirstOrDefaultAsync(it => it.Id == id);

		public async Task<(List<ImportTicketListItem> Items, int TotalCount)> GetPagedAsync(GetPagedImportTicketsRequest request)
		{
			var query = _context.ImportTickets.AsNoTracking().AsQueryable();

			if (request.SupplierId.HasValue)
				query = query.Where(it => it.SupplierId == request.SupplierId.Value);

			if (request.Status.HasValue)
				query = query.Where(it => it.Status == request.Status.Value);

			if (request.FromDate.HasValue)
				query = query.Where(it => it.ExpectedArrivalDate >= request.FromDate.Value);

			if (request.ToDate.HasValue)
				query = query.Where(it => it.ExpectedArrivalDate <= request.ToDate.Value);

			if (request.VerifiedById.HasValue)
				query = query.Where(it => it.VerifiedById == request.VerifiedById.Value);

			var totalCount = await query.CountAsync();

			query = request.SortOrder?.ToLower() == "asc"
				? query.OrderBy(it => it.ExpectedArrivalDate)
				: query.OrderByDescending(it => it.ExpectedArrivalDate);

			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.AsSplitQuery()
			  .Select(it => new ImportTicketListItem
			  {
				  Id = it.Id,
				  CreatedByName = it.CreatedByUser != null ? it.CreatedByUser.FullName : "Unknown",
				  VerifiedByName = it.VerifiedByUser != null ? it.VerifiedByUser.FullName : null,
				  SupplierName = it.Supplier != null ? it.Supplier.Name : "Unknown",
				  ExpectedArrivalDate = it.ExpectedArrivalDate,
				  ActualImportDate = it.ActualImportDate,
				  TotalCost = it.TotalCost,
				  Status = it.Status,
				  TotalItems = it.ImportDetails.Count,
				  CreatedAt = it.CreatedAt
			  })
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
