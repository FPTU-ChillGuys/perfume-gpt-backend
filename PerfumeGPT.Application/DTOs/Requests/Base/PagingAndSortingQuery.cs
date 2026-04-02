using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Requests.Base
{
	public record PagingAndSortingQuery
	{
		private const int MaxPageSize = 50;
		private readonly int _pageSize = 10;
		private readonly int _pageNumber = 1;
		private readonly string _sortOrder = "asc";

		[Range(1, int.MaxValue)]
		public int PageNumber
		{
			get => _pageNumber;
			init => _pageNumber = (value > 0) ? value : 1;
		}

		public int PageSize
		{
			get => _pageSize;
			init => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value > 0 ? value : 10);
		}

		public string? SortBy { get; init; }

		public string SortOrder
		{
			get => _sortOrder;
			init
			{
				if (!string.IsNullOrEmpty(value) &&
					value.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase))
				{
					_sortOrder = "desc";
				}
				else
				{
					_sortOrder = "asc";
				}
			}
		}

		[JsonIgnore]
		public bool IsDescending => _sortOrder == "desc";
	}
}
