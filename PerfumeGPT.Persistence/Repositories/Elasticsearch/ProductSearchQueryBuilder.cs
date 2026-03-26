using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace PerfumeGPT.Persistence.Repositories.Elasticsearch;

public static class ProductSearchQueryBuilder
{
    public static Action<QueryDescriptor<ProductDocument>> BuildQuery(
        string searchText,
        string minimumShouldMatch,
        Operator defaultOp)
    {
        return q => q
            .Bool(b => b
                .Should(
                    // 1. Layer: Exact Intent (Tìm chính xác tên/thương hiệu)
                    sh => sh.MultiMatch(m => m
                        .Query(searchText)
                        .Fields(new[] { "name^10", "brand^8" })
                        .Type(TextQueryType.Phrase)
                        .Boost(2.0f)
                    ),

                    // 2. Layer: Semantic & Intent (Kết hợp đa trường để cộng dồn điểm)
                    // Sử dụng MostFields để khi khớp cả Brand + Gender thì điểm sẽ cao nhất
                    sh => sh.MultiMatch(m => m
                        .Query(searchText)
                        .Fields(new[] {
                            "name^5", "brand^4", "category^5", "gender^5",
                            "attributes^3", "concentrations^2", "origin"
                        })
                        .Operator(defaultOp)
                        .Type(TextQueryType.MostFields)
                        .MinimumShouldMatch(minimumShouldMatch)
                    ),

                    // 3. Layer: Discovery (Tìm kiếm sâu trong nốt hương và nhóm hương)
                    sh => sh.MultiMatch(m => m
                        .Query(searchText)
                        .Fields(new[] { "scentNotes^4", "olfactoryFamilies^3" })
                        .Operator(Operator.Or)
                        .Type(TextQueryType.BestFields)
                        .Boost(0.8f)
                    ),

                    // 4. Layer: Typo & Correction (Hỗ trợ gõ sai chính tả)
                    sh => sh.MultiMatch(m => m
                        .Query(searchText)
                        .Fields(new[] { "name^2", "brand^2" })
                        .Fuzziness(new Fuzziness("AUTO"))
                        .Boost(0.4f)
                    ),

                    // 5. Layer: Technical & Patterns (SKU, Barcode, Volume)
                    sh => sh.MultiMatch(m => m
                        .Query(searchText)
                        .Fields(new[] { "volumes^3", "skus^5", "barcodes^5" })
                        .Operator(Operator.Or)
                        .Type(TextQueryType.BestFields)
                    )
                )
            );
    }
}
