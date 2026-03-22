using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Auths;
using PerfumeGPT.Application.DTOs.Responses.Users;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class UserRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<RegisterRequest, User>()
				.Map(dest => dest.UserName, src => src.Email)
				.Map(dest => dest.Email, src => src.Email)
				.Map(dest => dest.FullName, src => src.FullName)
				.Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
				.Map(dest => dest.PhoneNumberConfirmed, src => true)
				.Map(dest => dest.IsActive, src => true);

			config.NewConfig<User, UserCredentialsResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.LoyaltyPoint, src => src.LoyaltyTransactions.Sum(lt => lt.PointsChanged))
				.Map(dest => dest.FullName, src => src.FullName)
				.Map(dest => dest.PhoneNumber, src => src.PhoneNumber ?? string.Empty)
				.Map(dest => dest.Email, src => src.Email ?? string.Empty)
				.Map(dest => dest.ProfilePictureUrl, src => src.ProfilePicture != null ? src.ProfilePicture.PublicId : string.Empty);
		}
	}
}
