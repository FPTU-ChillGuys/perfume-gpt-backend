using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Persistence.Repositories.Elasticsearch;

public static class ProductSearchQueryBuilder
{
    public static Action<QueryDescriptor<ProductDocument>> BuildQuery(
        string searchText,
        string minimumShouldMatch,
        Operator defaultOp,
        GetPagedProductRequest? request = null,
        int? detectedSize = null)
    {
        return q => q.Bool(b =>
        {
            b.Must(BuildSearchQuery(searchText, minimumShouldMatch, defaultOp, detectedSize));

            if (HasFilters(request))
            {
                b.Filter(BuildFilterQuery(request));
            }
        });
    }

    public static Action<QueryDescriptor<ProductDocument>> BuildSearchQuery(
        string searchText,
        string minimumShouldMatch,
        Operator defaultOp,
        int? detectedSize = null)
    {
        return q => q.Bool(mb =>
        {
            var shouldClauses = new List<Action<QueryDescriptor<ProductDocument>>>();

            // Layer 1: Exact Intent
            shouldClauses.Add(sh => sh.MultiMatch(mm => mm
                .Query(searchText)
                .Fields(new[] { "name^10", "brand^8" })
                .Type(TextQueryType.Phrase)
                .Boost(10.0f)
            ));

            // Layer 2: Semantic & Intent
            shouldClauses.Add(sh => sh.MultiMatch(mm => mm
                .Query(searchText)
                .Fields(new[] {
                    "name^5", "brand^4", "category^5", "genderSearch^5",
                    "attributes^3", "concentrations^2", "origin"
                })
                .Operator(defaultOp)
                .Type(TextQueryType.MostFields)
                .MinimumShouldMatch(minimumShouldMatch)
            ));

            // Layer 3: Discovery (Scent Notes & Olfactory Families)
            shouldClauses.Add(sh => sh.MultiMatch(mm => mm
                .Query(searchText)
                .Fields(new[] { "scentNotes^6", "olfactoryFamilies^5" })
                .Operator(Operator.Or)
                .Type(TextQueryType.BestFields)
                .Boost(1.5f)
            ));

            // Layer 4: Typo & Correction
            shouldClauses.Add(sh => sh.MultiMatch(mm => mm
                .Query(searchText)
                .Fields(new[] { "name^2", "brand^2" })
                .Fuzziness(new Fuzziness("AUTO"))
                .Boost(0.4f)
            ));

            // Layer 5: Technical Details
            shouldClauses.Add(sh => sh.MultiMatch(m => m
                .Query(searchText)
                .Fields(new[] { "volumes", "skus^5", "barcodes^5" })
                .Operator(Operator.Or)
                .Type(TextQueryType.BestFields)
            ));

            // Layer 6: Specific Volume Awareness
            if (detectedSize.HasValue)
            {
                shouldClauses.Add(sh => sh.Term(t => t.Field("volumes").Value(detectedSize.Value).Boost(25.0f)));
            }

            mb.Should(shouldClauses.ToArray());
        });
    }

    public static Action<QueryDescriptor<ProductDocument>> BuildFilterQuery(GetPagedProductRequest? request)
    {
        return q => q.Bool(b =>
        {
            if (request == null) return;

            var filters = new List<Action<QueryDescriptor<ProductDocument>>>();

            if (request.Gender.HasValue)
            {
                var val = request.Gender.Value.ToString(); // Exact Enum Name (Male, Female, Unisex)
                filters.Add(f => f.Term(t => t.Field("gender").Value(val)));
            }

            // Category Filter
            if (request.CategoryId.HasValue)
            {
                filters.Add(f => f.Term(t => t.Field("categoryId").Value(request.CategoryId.Value.ToString())));
            }

            // Brand Filter
            if (request.BrandId.HasValue)
            {
                filters.Add(f => f.Term(t => t.Field("brandId").Value(request.BrandId.Value.ToString())));
            }

            // Price Filter
            if (request.FromPrice.HasValue || request.ToPrice.HasValue)
            {
                filters.Add(f => f.Range(r => r
                    .Number(nr =>
                    {
                        nr.Field("variantPrices");
                        if (request.FromPrice.HasValue) nr.Gte((double)request.FromPrice.Value);
                        if (request.ToPrice.HasValue) nr.Lte((double)request.ToPrice.Value);
                    })
                ));
            }

            if (filters.Any())
            {
                b.Must(filters.ToArray());
            }
        });
    }

    public static bool HasFilters(GetPagedProductRequest? request)
    {
        if (request == null) return false;
        return request.Gender.HasValue ||
               request.CategoryId.HasValue ||
               request.BrandId.HasValue ||
               request.FromPrice.HasValue ||
               request.ToPrice.HasValue;
    }
}
