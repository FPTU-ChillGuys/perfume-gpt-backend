using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PerfumeGPT.Infrastructure.Elasticsearch
{
    /// <summary>
    /// Ensures the Elasticsearch "products" index exists with the correct Vietnamese analyzer
    /// and perfume-domain synonym filter configured. Runs once at application startup.
    /// </summary>
    public class ElasticsearchIndexInitializer : IHostedService
    {
        private const string IndexName = "products";

        private readonly ElasticsearchClient _esClient;
        private readonly ILogger<ElasticsearchIndexInitializer> _logger;

        public ElasticsearchIndexInitializer(
            ElasticsearchClient esClient,
            ILogger<ElasticsearchIndexInitializer> logger)
        {
            _esClient = esClient;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var existsResponse = await _esClient.Indices.ExistsAsync(IndexName, cancellationToken);

                if (existsResponse.Exists)
                {
                    _logger.LogInformation("[ES] Index '{IndexName}' already exists. Skipping creation.", IndexName);
                    return;
                }

                _logger.LogInformation("[ES] Index '{IndexName}' not found. Creating with Vietnamese analyzer + synonym filter...", IndexName);

                // Perfume domain synonyms: Tiếng Việt ↔ English
                var synonyms = new List<string>
                {
                    "ngọt, sweet, vanilla, gourmand, caramel",
                    "tươi, tươi mát, fresh, citrus, chanh, cam, bưởi",
                    "gỗ, woody, wood, sandalwood, cedar, đàn hương",
                    "hoa, floral, flower, rose, hoa hồng, jasmine, nhài",
                    "nam tính, masculine, musky, musk, xạ hương",
                    "nữ tính, feminine, powdery, phấn",
                    "nhẹ nhàng, nhẹ, light, gentle, airy",
                    "mạnh, nồng, strong, intense, heavy, bold",
                    "biển, aquatic, ocean, marine, nước biển",
                    "đất, earthy, moss, patchouli",
                    "khói, smoky, smoke, oud, trầm hương",
                    "ấm, warm, amber, hổ phách, cozy",
                    "spice, spicy, cay, hương cay",
                    "mint, bạc hà, menthol",
                    "nước hoa, perfume, fragrance, cologne",
                    "chanell => chanel",
                    "diorr => dior",
                    "đi tiệc => party, ban đêm, tiệc tùng"
                };

                var createResponse = await _esClient.Indices.CreateAsync(IndexName, c => c
                    .Settings(s => s
                        .Analysis(a => a
                            .TokenFilters(tf => tf
                                .Synonym("perfume_synonym", syn => syn
                                    .Synonyms(synonyms)
                                )
                            )
                            .Analyzers(an => an
                                .Custom("vi_perfume_analyzer", ca => ca
                                    // Uses Vietnamese tokenizer (requires elasticsearch-analysis-vietnamese plugin)
                                    // https://github.com/duydo/elasticsearch-analysis-vietnamese
                                    .Tokenizer("vi_tokenizer")
                                    .Filter(["lowercase", "perfume_synonym"])
                                )
                            )
                        )
                    )
                    .Mappings(m => m
                        .Properties<object>(p => p
                            .Keyword("id")
                            .Text("name", t => t
                                .Analyzer("vi_perfume_analyzer")
                                .Fields(f => f.Keyword("keyword"))
                            )
                            .Text("brand", t => t
                                .Analyzer("vi_perfume_analyzer")
                                .Fields(f => f.Keyword("keyword"))
                            )
                            .Keyword("brandId")
                            .Text("category", t => t
                                .Analyzer("vi_perfume_analyzer")
                                .Fields(f => f.Keyword("keyword"))
                            )
                            .Keyword("categoryId")
                            .Text("gender", t => t.Analyzer("vi_perfume_analyzer"))
                            .Text("origin", t => t.Analyzer("vi_perfume_analyzer"))
                            .IntegerNumber("releaseYear")
                            .Text("attributes", t => t.Analyzer("vi_perfume_analyzer"))
                            .Text("concentrations", t => t.Analyzer("vi_perfume_analyzer"))
                            .Text("volumes")
                            .Keyword("skus")
                            .Keyword("barcodes")
                            .Text("scentNotes", t => t.Analyzer("vi_perfume_analyzer"))
                            .Text("olfactoryFamilies", t => t.Analyzer("vi_perfume_analyzer"))
                            .DoubleNumber("variantPrices")
                            .DenseVector("embedding", v => v
                                .Index(true)
                                .Dims(1024)
                                .Similarity(DenseVectorSimilarity.Cosine)
                            )
                        )
                    ),
                    cancellationToken);

                if (createResponse.IsValidResponse)
                {
                    _logger.LogInformation("[ES] Index '{IndexName}' created successfully.", IndexName);
                }
                else
                {
                    _logger.LogError("[ES] Failed to create index '{IndexName}': {Reason}",
                        IndexName,
                        createResponse.ElasticsearchServerError?.Error?.Reason ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash the application — search will degrade gracefully
                _logger.LogError(ex, "[ES] Exception during index initialization for '{IndexName}'.", IndexName);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
