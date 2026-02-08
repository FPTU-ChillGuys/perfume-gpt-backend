using System;

namespace PerfumeGPT.Application.DTOs.Responses.Profiles
{
    public class ProfileResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? ScentPreference { get; set; }
        public decimal? MinBudget { get; set; }
        public decimal? MaxBudget { get; set; }
        public string? PreferredStyle { get; set; }
        public string? FavoriteNotes { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
