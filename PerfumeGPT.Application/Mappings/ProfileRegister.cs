using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.DTOs.Responses.Profiles;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
    public class ProfileRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CreateProfileRequest, CustomerProfile>();
            config.NewConfig<UpdateProfileRequest, CustomerProfile>();
            config.NewConfig<CustomerProfile, ProfileResponse>();
        }
    }
}
