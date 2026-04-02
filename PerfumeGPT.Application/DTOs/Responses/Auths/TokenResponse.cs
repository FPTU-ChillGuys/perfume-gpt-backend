namespace PerfumeGPT.Application.DTOs.Responses.Auths
{
    public record TokenResponse
    {
        public required string AccessToken { get; init; }
    }
}
