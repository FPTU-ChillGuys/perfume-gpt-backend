using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers;

/// <summary>
/// NATS handler for Sourcing Catalog operations
/// Uses dedicated NatsCatalogService for type-safe responses
/// </summary>
public static class CatalogNatsHandler
{
	public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
	{
		var natsCatalogService = scope.ServiceProvider.GetRequiredService<INatsCatalogService>();

		return action switch
		{
			"getCatalogs" => await GetCatalogsAsync(natsCatalogService, payload, options),
			_ => throw new ArgumentException($"Invalid catalog action: {action}")
		};
	}

	private static async Task<NatsCatalogResponse> GetCatalogsAsync(INatsCatalogService natsCatalogService, JsonElement payload, JsonSerializerOptions options)
	{
		// Parse payload to get variantId
		Guid? variantId = null;

		if (payload.TryGetProperty("variantId", out var variantIdElement) &&
			variantIdElement.ValueKind == JsonValueKind.String)
		{
			if (Guid.TryParse(variantIdElement.GetString(), out var parsedGuid))
			{
				variantId = parsedGuid;
			}
		}

		if (!variantId.HasValue)
		{
			return new NatsCatalogResponse { Catalogs = [] };
		}

		return await natsCatalogService.GetCatalogsByVariantIdAsync(variantId.Value);
	}
}
