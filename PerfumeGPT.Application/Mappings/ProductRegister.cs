using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Mappings
{
	public class ProductRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Product, ProductInforResponse>()
			.Map(dest => dest.ProductCode, src => src.Id.ToString())
			.Map(dest => dest.BrandName, src => src.Brand.Name)
			.Map(dest => dest.Origin, src => src.Origin)
			.Map(dest => dest.ReleaseYear, src => src.ReleaseYear)
			.Map(dest => dest.Gender, src => src.Gender)
			.Map(dest => dest.ScentGroup, src =>
				string.Join(", ", src.ProductFamilyMaps.Select(pfm => pfm.OlfactoryFamily.Name)))
			.Map(dest => dest.Style, src =>
				string.Join(", ", src.ProductAttributes
					.Where(pa => pa.Attribute.InternalCode == "STYLE")
					.Select(pa => pa.Value.Value)))
			.Map(dest => dest.TopNotes, src =>
				string.Join(", ", src.ProductScentMaps
					.Where(psm => psm.NoteType == NoteType.Top)
					.Select(psm => psm.ScentNote.Name)))
			.Map(dest => dest.HeartNotes, src =>
				string.Join(", ", src.ProductScentMaps
					.Where(psm => psm.NoteType == NoteType.Heart)
					.Select(psm => psm.ScentNote.Name)))
			.Map(dest => dest.BaseNotes, src =>
				string.Join(", ", src.ProductScentMaps
					.Where(psm => psm.NoteType == NoteType.Base)
					.Select(psm => psm.ScentNote.Name)))
			.Map(dest => dest.Description, src => src.Description);

			config.NewConfig<Product, ProductResponse>()
			.Map(dest => dest.Id, src => src.Id)
			.Map(dest => dest.Name, src => src.Name)
			.Map(dest => dest.Origin, src => src.Origin)
			.Map(dest => dest.Gender, src => src.Gender)
			.Map(dest => dest.ReleaseYear, src => src.ReleaseYear)
			.Map(dest => dest.BrandId, src => src.BrandId)
			.Map(dest => dest.BrandName, src => src.Brand.Name)
			.Map(dest => dest.CategoryId, src => src.CategoryId)
			.Map(dest => dest.CategoryName, src => src.Category.Name)
			.Map(dest => dest.Description, src => src.Description)
			.Map(dest => dest.Media, src => src.Media.Where(m => !m.IsDeleted))
			.Map(dest => dest.Variants, src => src.Variants.Where(v => !v.IsDeleted))
			.Map(dest => dest.Attributes, src => src.ProductAttributes)
			.Map(dest => dest.NumberOfVariants, src => src.Variants.Count(v => !v.IsDeleted))
			.Map(dest => dest.OlfactoryFamilies, src => src.ProductFamilyMaps
				.Select(pfm => new ProductOlfactoryFamilyResponse
				{
					OlfactoryFamilyId = pfm.OlfactoryFamilyId,
					Name = pfm.OlfactoryFamily.Name
				}))
			.Map(dest => dest.ScentNotes, src => src.ProductScentMaps
				.Select(psm => new ProductScentNoteResponse
				{
					NoteId = psm.ScentNoteId,
					Name = psm.ScentNote.Name,
					Type = psm.NoteType
				}));

			config.NewConfig<Product, ProductListItem>()
			.Map(dest => dest.Id, src => src.Id)
			.Map(dest => dest.Name, src => src.Name)
			.Map(dest => dest.BrandId, src => src.BrandId)
			.Map(dest => dest.BrandName, src => src.Brand.Name)
			.Map(dest => dest.CategoryId, src => src.CategoryId)
			.Map(dest => dest.CategoryName, src => src.Category.Name)
			.Map(dest => dest.Description, src => src.Description)
			.Map(dest => dest.NumberOfVariants, src => src.Variants.Count(v => !v.IsDeleted))
			.Map(dest => dest.VariantPrices, src =>
				src.Variants.Where(v => !v.IsDeleted).Select(v => v.BasePrice).ToList())
			.Map(dest => dest.PrimaryImage, src =>
				src.Media.Where(m => m.IsPrimary && !m.IsDeleted).Select(m => new MediaResponse
				{
					Id = m.Id,
					Url = m.Url,
					AltText = m.AltText,
					IsPrimary = m.IsPrimary,
					DisplayOrder = m.DisplayOrder,
					MimeType = m.MimeType,
					FileSize = m.FileSize
				}).FirstOrDefault());

			config.NewConfig<Product, ProductListItemWithVariants>()
			.Map(dest => dest.Id, src => src.Id)
			.Map(dest => dest.Name, src => src.Name)
			.Map(dest => dest.BrandId, src => src.BrandId)
			.Map(dest => dest.BrandName, src => src.Brand.Name)
			.Map(dest => dest.CategoryId, src => src.CategoryId)
			.Map(dest => dest.CategoryName, src => src.Category.Name)
			.Map(dest => dest.Description, src => src.Description)
			.Map(dest => dest.PrimaryImage, src =>
				src.Media.Where(m => m.IsPrimary && !m.IsDeleted).Select(m => new MediaResponse
				{
					Id = m.Id,
					Url = m.Url,
					AltText = m.AltText,
					IsPrimary = m.IsPrimary,
					DisplayOrder = m.DisplayOrder
				}).FirstOrDefault())
			.Map(dest => dest.Variants, src => src.Variants.Where(v => !v.IsDeleted));

			config.NewConfig<Product, ProductLookupItem>()
			.Map(dest => dest.Id, src => src.Id)
			.Map(dest => dest.Name, src => src.Name)
			.Map(dest => dest.BrandName, src => src.Brand.Name)
			.Map(dest => dest.PrimaryImageUrl, src =>
				src.Media.Where(m => m.IsPrimary && !m.IsDeleted).Select(m => m.Url).FirstOrDefault());
		}
	}
}
