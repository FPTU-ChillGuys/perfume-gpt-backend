using Mapster;
using PerfumeGPT.Application.DTOs.Requests.LoyaltyPoints;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
    public class LoyaltyPointRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CreateLoyaltyPointRequest, LoyaltyPoint>();
            config.NewConfig<UpdateLoyaltyPointRequest, LoyaltyPoint>();
        }
    }
}
