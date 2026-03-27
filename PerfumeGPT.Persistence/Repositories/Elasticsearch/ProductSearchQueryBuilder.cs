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
        GetPagedProductRequest? request = null)
    {
        return q => q
            .Bool(b =>
            {
                // 1. MUST clause: Search Intent (BM25 Text Search)
                b.Must(m => m
                    .Bool(mb => mb
                        .Should(
                            // Layer 1: Exact Intent
                            sh => sh.MultiMatch(mm => mm
                                .Query(searchText)
                                .Fields(new[] { "name^10", "brand^8" })
                                .Type(TextQueryType.Phrase)
                                .Boost(2.0f)
                            ),

                            // Layer 2: Semantic & Intent
                            sh => sh.MultiMatch(mm => mm
                                .Query(searchText)
                                .Fields(new[] {
                                    "name^5", "brand^4", "category^5", "gender^5",
                                    "attributes^3", "concentrations^2", "origin"
                                })
                                .Operator(defaultOp)
                                .Type(TextQueryType.MostFields)
                                .MinimumShouldMatch(minimumShouldMatch)
                            ),

                            // Layer 3: Discovery
                            sh => sh.MultiMatch(mm => mm
                                .Query(searchText)
                                .Fields(new[] { "scentNotes^4", "olfactoryFamilies^3" })
                                .Operator(Operator.Or)
                                .Type(TextQueryType.BestFields)
                                .Boost(0.8f)
                            ),

                            // Layer 4: Typo & Correction
                            sh => sh.MultiMatch(mm => mm
                                .Query(searchText)
                                .Fields(new[] { "name^2", "brand^2" })
                                .Fuzziness(new Fuzziness("AUTO"))
                                .Boost(0.4f)
                            ),

                            // Layer 5: Technical Details
                            sh => sh.MultiMatch(mm => mm
                                .Query(searchText)
                                .Fields(new[] { "volumes^3", "skus^5", "barcodes^5" })
                                .Operator(Operator.Or)
                                .Type(TextQueryType.BestFields)
                            )
                        )
                    )
                );

                // 2. FILTER clause: Constraints from Request (Only add if there are filters)
                if (HasFilters(request))
                {
                    b.Filter(fi =>
                    {
                        // Gender Filter
                        if (request!.Gender.HasValue)
                        {
                            var genderPrefix = request.Gender.Value switch
                            {
                                Gender.Male => "Male",
                                Gender.Female => "Female",
                                Gender.Unisex => "Unisex",
                                _ => null
                            };

                            if (genderPrefix != null)
                            {
                                fi.Prefix(p => p.Field("gender").Value(genderPrefix));
                            }
                        }

                        // Category Filter
                        if (request.CategoryId.HasValue)
                        {
                            fi.Term(t => t.Field("categoryId").Value(request.CategoryId.Value.ToString()));
                        }

                        // Brand Filter
                        if (request.BrandId.HasValue)
                        {
                            fi.Term(t => t.Field("brandId").Value(request.BrandId.Value.ToString()));
                        }

                        // Price Filter
                        if (request.FromPrice.HasValue || request.ToPrice.HasValue)
                        {
                            fi.Range(r => r
                                .Number(nr =>
                                {
                                    nr.Field("variantPrices");
                                    if (request.FromPrice.HasValue) nr.Gte((double)request.FromPrice.Value);
                                    if (request.ToPrice.HasValue) nr.Lte((double)request.ToPrice.Value);
                                })
                            );
                        }
                    });
                }
            });
    }

    private static bool HasFilters(GetPagedProductRequest? request)
    {
        if (request == null) return false;
        return request.Gender.HasValue ||
               request.CategoryId.HasValue ||
               request.BrandId.HasValue ||
               request.FromPrice.HasValue ||
               request.ToPrice.HasValue;
    }
}
