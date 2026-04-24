using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers
{
    public static class CatalogNatsHandler
    {
        public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
        {
            var catalogService = scope.ServiceProvider.GetRequiredService<ISourcingCatalogService>();

            return action switch
            {
                "getCatalogs" => await GetCatalogsAsync(catalogService, payload, options),
                _ => throw new ArgumentException($"Invalid catalog action: {action}")
            };
        }

        private static async Task<object> GetCatalogsAsync(ISourcingCatalogService catalogService, JsonElement payload, JsonSerializerOptions options)
        {
            // Parse payload to get variantId (and optionally supplierId)
            Guid? variantId = null;
            int? supplierId = null;

            if (payload.TryGetProperty("variantId", out var variantIdElement) && 
                variantIdElement.ValueKind == JsonValueKind.String)
            {
                if (Guid.TryParse(variantIdElement.GetString(), out var parsedGuid))
                {
                    variantId = parsedGuid;
                }
            }

            if (payload.TryGetProperty("supplierId", out var supplierIdElement) && 
                supplierIdElement.ValueKind == JsonValueKind.Number)
            {
                supplierId = supplierIdElement.GetInt32();
            }

            var result = await catalogService.GetCatalogsAsync(supplierId, variantId);
            
            // Convert to AiCatalogItemResponse array
            var aiCatalogItems = result.Payload?.Select(i => AiCatalogItemResponse.FromCatalogItemResponse(i)).ToArray() 
                                 ?? Array.Empty<AiCatalogItemResponse>();

            // Return AiCatalogResponse to match AI backend expectation with type safety
            // AI backend expects { catalogs: [...] }
            return new AiCatalogResponse
            {
                Catalogs = aiCatalogItems
            };
        }
    }
}
