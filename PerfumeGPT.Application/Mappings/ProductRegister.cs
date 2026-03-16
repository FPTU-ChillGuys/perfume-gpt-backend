using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ProductRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<CreateProductRequest, Product>()
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.Origin, src => src.Origin)
				.Map(dest => dest.Gender, src => src.Gender)
				.Map(dest => dest.ReleaseYear, src => src.ReleaseYear)
				.Map(dest => dest.Description, src => src.Description)
				.AfterMapping((src, dest) =>
				{
					if (src.OlfactoryFamilyIds != null)
					{
						dest.ProductFamilyMaps = src.OlfactoryFamilyIds
							.Select(id => new ProductFamilyMap { OlfactoryFamilyId = id })
							.ToList();
					}
					if (src.ScentNotes != null)
					{
						dest.ProductScentMaps = src.ScentNotes
							.Select(n => new ProductNoteMap { ScentNoteId = n.NoteId, NoteType = n.Type })
							.ToList();
					}
				});

			config.NewConfig<UpdateProductRequest, Product>()
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.Origin, src => src.Origin)
				.Map(dest => dest.Gender, src => src.Gender)
				.Map(dest => dest.ReleaseYear, src => src.ReleaseYear)
				.Map(dest => dest.Description, src => src.Description);

			config.NewConfig<Product, ProductInforResponse>()
				.Map(dest => dest.ProductCode, src => src.Id.ToString())
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.Origin, src => src.Origin)
				.Map(dest => dest.ReleaseYear, src => src.ReleaseYear)
				.Map(dest => dest.Gender, src => src.Gender)
				.Map(dest => dest.ScentGroup, src => string.Join(", ", src.ProductFamilyMaps.Select(pfm => pfm.OlfactoryFamily.Name)))
				.Map(dest => dest.Style, src => string.Join(", ", src.ProductAttributes
					.Where(pa => pa.Attribute.InternalCode == "STYLE")
					.Select(pa => pa.Value.Value)))
				.Map(dest => dest.TopNotes, src => string.Join(", ", src.ProductScentMaps.Where(psm => psm.NoteType == Domain.Enums.NoteType.Top).Select(psm => psm.ScentNote.Name)))
				.Map(dest => dest.HeartNotes, src => string.Join(", ", src.ProductScentMaps.Where(psm => psm.NoteType == Domain.Enums.NoteType.Heart).Select(psm => psm.ScentNote.Name)))
				.Map(dest => dest.BaseNotes, src => string.Join(", ", src.ProductScentMaps.Where(psm => psm.NoteType == Domain.Enums.NoteType.Base).Select(psm => psm.ScentNote.Name)))
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
				.Map(dest => dest.NumberOfVariants, src => src.Variants.Count())
				.Map(dest => dest.OlfactoryFamilies, src => src.ProductFamilyMaps.Select(pfm => new ProductOlfactoryFamilyResponse { OlfactoryFamilyId = pfm.OlfactoryFamilyId, Name = pfm.OlfactoryFamily.Name }))
				.Map(dest => dest.ScentNotes, src => src.ProductScentMaps.Select(psm => new ProductScentNoteResponse { NoteId = psm.ScentNoteId, Name = psm.ScentNote.Name, Type = psm.NoteType }));

			config.NewConfig<Product, ProductListItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.NumberOfVariants, src => src.Variants.Count())
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted))
				.Map(dest => dest.Attributes, src => src.ProductAttributes)
				.Map(dest => dest.OlfactoryFamilies, src => src.ProductFamilyMaps.Select(pfm => new ProductOlfactoryFamilyResponse { OlfactoryFamilyId = pfm.OlfactoryFamilyId, Name = pfm.OlfactoryFamily.Name }))
				.Map(dest => dest.ScentNotes, src => src.ProductScentMaps.Select(psm => new ProductScentNoteResponse { NoteId = psm.ScentNoteId, Name = psm.ScentNote.Name, Type = psm.NoteType }));

			config.NewConfig<Product, ProductListItemWithVariants>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandId, src => src.BrandId)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.CategoryId, src => src.CategoryId)
				.Map(dest => dest.CategoryName, src => src.Category.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted))
				.Map(dest => dest.Attributes, src => src.ProductAttributes)
				.Map(dest => dest.Variants, src => src.Variants.Where(v => !v.IsDeleted));

			config.NewConfig<Product, ProductLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.BrandName, src => src.Brand.Name)
				.Map(dest => dest.PrimaryImage, src => src.Media.FirstOrDefault(m => m.IsPrimary && !m.IsDeleted));
		}
	}
}
