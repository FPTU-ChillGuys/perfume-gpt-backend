using System;
using System.Collections.Generic;
using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
    public record AiProductSearchRequest : PagingAndSortingQuery
    {
        public List<string>? GenderValues { get; init; }
        public List<string>? ScentNotes { get; init; }
        public List<string>? OlfactoryFamilies { get; init; }
        public List<string>? ProductNames { get; init; }
        public decimal? MinBudget { get; init; }
        public decimal? MaxBudget { get; init; }
        public bool? SortPriceAscending { get; init; }
    }
}
