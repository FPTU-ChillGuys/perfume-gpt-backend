namespace PerfumeGPT.Persistence.Repositories.Elasticsearch;

public sealed record ProductDocument(
    string Id,
    string Name,
    string Brand,
    string BrandId,
    string Category,
    string CategoryId,
    string Gender,
    int ReleaseYear,
    string Origin,
    IEnumerable<string> Attributes,
    IEnumerable<string> Concentrations,
    IEnumerable<string> Volumes,
    IEnumerable<string> Skus,
    IEnumerable<string> Barcodes,
    IEnumerable<string> ScentNotes,
    IEnumerable<string> OlfactoryFamilies,
    IEnumerable<double> VariantPrices,
    float[]? Embedding = null);
