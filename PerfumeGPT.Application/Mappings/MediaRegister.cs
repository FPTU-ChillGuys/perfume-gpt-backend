using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class MediaRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
		// Media -> MediaResponse
		config.NewConfig<Media, MediaResponse>()
			.Map(dest => dest.Id, src => src.Id)
			.Map(dest => dest.Url, src => src.Url)
			.Map(dest => dest.AltText, src => src.AltText)
			.Map(dest => dest.DisplayOrder, src => src.DisplayOrder)
			.Map(dest => dest.IsPrimary, src => src.IsPrimary)
			.Map(dest => dest.FileSize, src => src.FileSize)
			.Map(dest => dest.MimeType, src => src.MimeType);

		// TemporaryMedia -> TemporaryMediaResponse
		config.NewConfig<TemporaryMedia, TemporaryMediaResponse>()
			.Map(dest => dest.Id, src => src.Id)
			.Map(dest => dest.Url, src => src.Url)
			.Map(dest => dest.AltText, src => src.AltText)
			.Map(dest => dest.DisplayOrder, src => src.DisplayOrder)
			.Map(dest => dest.FileSize, src => src.FileSize)
			.Map(dest => dest.MimeType, src => src.MimeType)
			.Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
			.Map(dest => dest.CreatedAt, src => src.CreatedAt);
	}
}
}
