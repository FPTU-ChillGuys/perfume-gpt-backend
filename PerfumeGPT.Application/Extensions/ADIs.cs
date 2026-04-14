using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.Services.Helpers;
using System.Reflection;

namespace PerfumeGPT.Application.Extensions
{
	public static class ADIs
	{
		public static IServiceCollection AddApplicationServices(this IServiceCollection services)
		{
			// Use the current assembly for scanning
			var assembly = typeof(ADIs).Assembly;

			// Register implementations that follow the convention: I{TypeName} -> {TypeName}
			RegisterServicesByConvention(services, assembly);

			// Register helper classes that don't follow the interface convention
			services.AddScoped<ExcelTemplateGenerator>();
			services.AddScoped<MediaBulkActionHelper>();

			// Mapster configuration: scan assembly for IRegister implementations
			// Apply to GlobalSettings first (used by ProjectToType static method)
			TypeAdapterConfig.GlobalSettings.Scan(assembly);

			// Create a singleton config that references GlobalSettings for consistency
			services.AddSingleton(TypeAdapterConfig.GlobalSettings);
			services.AddScoped<IMapper, ServiceMapper>();

			// FluentValidation - registers all validators found in the assembly
			services.AddValidatorsFromAssembly(assembly);

			// MediatR - registers all request/notification handlers in application assembly
			services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

			return services;
		}

		// Registers concrete types against the interface named "I{ConcreteTypeName}" as Scoped.
		private static void RegisterServicesByConvention(IServiceCollection services, Assembly assembly)
		{
			var types = assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType && t.IsPublic);

			foreach (var impl in types)
			{
				// Find an interface that matches the convention I{TypeName}
				var match = impl.GetInterfaces()
					.FirstOrDefault(i => i.Name == $"I{impl.Name}");

				if (match != null)
				{
					services.AddScoped(match, impl);
				}
			}
		}
	}
}
