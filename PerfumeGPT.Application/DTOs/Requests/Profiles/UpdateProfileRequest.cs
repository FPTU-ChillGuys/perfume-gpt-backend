namespace PerfumeGPT.Application.DTOs.Requests.Profiles
{
    public class UpdateProfileRequest
    {
        public string? ScentPreference { get; set; }
        public decimal? MinBudget { get; set; }
        public decimal? MaxBudget { get; set; }
        public string? PreferredStyle { get; set; }
        public string? FavoriteNotes { get; set; }
    }
}
