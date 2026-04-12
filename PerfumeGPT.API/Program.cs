using Hangfire;
using MicroElements.AspNetCore.OpenApi.FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PerfumeGPT.API.Middlewares;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Infrastructure.Extensions;
using PerfumeGPT.Infrastructure.Hubs;
using PerfumeGPT.Persistence.Contexts;
using Scalar.AspNetCore;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

// Load .env file (search upward from current directory) and set environment variables
static string? FindDotEnv(string startDir)
{
	var dir = new DirectoryInfo(startDir);
	while (dir != null)
	{
		var candidate = Path.Combine(dir.FullName, ".env");
		if (File.Exists(candidate)) return candidate;
		dir = dir.Parent;
	}
	return null;
}

static void LoadDotEnv(string path)
{
	try
	{
		foreach (var raw in File.ReadAllLines(path))
		{
			var line = raw.Trim();
			if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
			var idx = line.IndexOf('=');
			if (idx <= 0) continue;
			var key = line.Substring(0, idx).Trim();
			var val = line.Substring(idx + 1).Trim();
			if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
			{
				val = val.Substring(1, val.Length - 2);
			}
			Environment.SetEnvironmentVariable(key, val);
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Failed to load .env file '{path}': {ex.Message}");
	}
}

var envPath = FindDotEnv(Directory.GetCurrentDirectory());
if (envPath != null)
{
	LoadDotEnv(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddFluentValidationRulesToOpenApi();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
	options.AddFluentValidationRules();
	options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
	options.AddSchemaTransformer<EnumSchemaTransformer>();

	TypeTransformer.MapType<decimal>(new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" });
	TypeTransformer.MapType<decimal?>(new OpenApiSchema { Type = JsonSchemaType.Number | JsonSchemaType.Null, Format = "decimal" });
	TypeTransformer.MapType<double>(new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" });
	TypeTransformer.MapType<double?>(new OpenApiSchema { Type = JsonSchemaType.Number | JsonSchemaType.Null, Format = "double" });
	TypeTransformer.MapType<int>(new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" });
	TypeTransformer.MapType<int?>(new OpenApiSchema { Type = JsonSchemaType.Integer | JsonSchemaType.Null, Format = "int32" });
	TypeTransformer.MapType<long>(new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" });
	TypeTransformer.MapType<long?>(new OpenApiSchema { Type = JsonSchemaType.Integer | JsonSchemaType.Null, Format = "int64" });
	options.AddSchemaTransformer<TypeTransformer>();
});

builder.Services.AddInfrastructureDIs(builder.Configuration);
// Add application services
builder.Services.AddApplicationServices();
// Add identity and JWT services
builder.Services.AddIdentityServices(builder.Configuration);
// Add Semantic Kernel services
builder.Services.AddSemanticKernelServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();

// Force all routes to be lowercase
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// To suppress the automatic model state validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.SuppressModelStateInvalidFilter = true;
});

// JSON options
builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	// options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

//app.UseCors("AllowConfiguredOrigins");
app.UseCors("AllowAllForDev");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Configure Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
	Authorization = []
	// For production, use: Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<PosHub>("/posHub");

ApplyMigration();
app.Run();

void ApplyMigration()
{
	using var scope = app.Services.CreateScope();
	var _db = scope.ServiceProvider.GetRequiredService<PerfumeDbContext>();

	if (_db.Database.GetPendingMigrations().Any())
	{
		_db.Database.Migrate();
	}
}

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
	public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
		if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
		{
			// Add the security scheme at the document level
			var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
			{
				["Bearer"] = new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.Http,
					Scheme = "bearer", // "bearer" refers to the header name here
					In = ParameterLocation.Header,
					BearerFormat = "Json Web Token"
				}
			};
			document.Components ??= new OpenApiComponents();
			document.Components.SecuritySchemes = securitySchemes;

			// Apply it as a requirement for all operations
			foreach (var operation in document.Paths.Values
				.SelectMany(path => path.Operations ?? new Dictionary<HttpMethod, OpenApiOperation>()))
			{
				operation.Value.Security ??= [];
				operation.Value.Security.Add(new OpenApiSecurityRequirement
				{
					[new OpenApiSecuritySchemeReference("Bearer", document)] = []
				});
			}
		}
	}
}

internal sealed class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
	public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
	{
		var type = context.JsonTypeInfo.Type;

		// Lấy kiểu thực sự bên trong Nullable (nếu có)
		var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

		if (underlyingType.IsEnum)
		{
			// Nếu là nullable thì schema type phải bao gồm cả Null
			bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

			schema.Type = isNullable
				? JsonSchemaType.String | JsonSchemaType.Null
				: JsonSchemaType.String;

			schema.Format = null;

			// Lấy tên các giá trị của Enum thực sự (underlyingType)
			schema.Enum = new JsonArray(underlyingType.GetEnumNames()
				.Select(name => JsonValue.Create(name)!)
				.ToArray())!;
		}

		return Task.CompletedTask;
	}
}

internal sealed class TypeTransformer : IOpenApiSchemaTransformer
{
	private static readonly ConcurrentDictionary<Type, (JsonSchemaType Type, string? Format)> _typeMappings = new();

	public static void MapType<T>(OpenApiSchema schema)
	{
		_typeMappings[typeof(T)] = (schema.Type ?? JsonSchemaType.Null, schema.Format);
	}

	public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
	{
		var clrType = context.JsonTypeInfo.Type;

		if (_typeMappings.TryGetValue(clrType, out var mapping))
		{
			schema.Type = mapping.Type;
			schema.Format = mapping.Format;
		}

		return Task.CompletedTask;
	}
}
