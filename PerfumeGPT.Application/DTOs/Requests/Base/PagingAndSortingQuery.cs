using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Requests.Base
{
    public class PagingAndSortingQuery
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;
        private int _pageNumber = 1;

        /// <summary>
        /// The number of the page to retrieve.
        /// Defaults to 1.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = (value > 0) ? value : 1;
        }

        /// <summary>
        /// The number of items to retrieve per page.
        /// Defaults to 10. Max is 50.
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value > 0 ? value : 10);
        }

        // --- Sorting ---

        private string _sortOrder = "asc";

        /// <summary>
        /// The property name to sort by (e.g., "Name", "Price").
        /// If null or empty, the service layer should apply a default sort.
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// The sort direction. Accepts "asc" (ascending) or "desc" (descending).
        /// Defaults to "asc".
        /// </summary>
        public string SortOrder
        {
            get => _sortOrder;
            set
            {
                // Normalize and validate the input
                if (!string.IsNullOrEmpty(value) &&
                    value.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    _sortOrder = "desc";
                }
                else
                {
                    _sortOrder = "asc"; // Default to ascending
                }
            }
        }

        /// <summary>
        /// Helper property to easily check the sort direction.
        /// </summary>
        [JsonIgnore]
        public bool IsDescending => _sortOrder == "desc";
    }
}
