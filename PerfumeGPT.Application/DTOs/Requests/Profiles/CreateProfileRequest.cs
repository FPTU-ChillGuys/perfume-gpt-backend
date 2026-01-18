namespace PerfumeGPT.Application.DTOs.Requests.Profiles
{
    public class CreateProfileRequest : UpdateProfileRequest
    {
        public Guid UserId { get; set; }
    }
}
