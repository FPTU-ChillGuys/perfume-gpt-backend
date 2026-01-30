using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Infrastructure.ThirdParties;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using PerfumeGPT.Persistence.Services;
using System.Reflection;
using System.Text;

namespace PerfumeGPT.Infrastructure.Extensions
{
	public static class DIs
	{
		public static void AddInfrastructureDIs(this IServiceCollection services, IConfiguration configuration)
		{
			// Register audit scope for tracking system actions
			services.AddScoped<IAuditScope, AuditScope>();

			// Register infrastructure 3rd-party services used by Application layer
			services.AddScoped<IEmailService, EmailService>();
			services.AddScoped<IEmailTemplateService, EmailTemplateService>();
			services.AddHttpClient<IGHNService, GHNService>();
			services.AddSingleton<ISupabaseService, SupabaseService>();
			services.AddScoped<IVnPayService, VnPayService>();

			// Convention-based registration for repository implementations in the Persistence assembly.
			// It will register classes where an interface named "I{ClassName}" exists.
			RegisterRepositoriesByConvention(services, typeof(UnitOfWork).Assembly);

			services.AddScoped<IUnitOfWork, UnitOfWork>();
			services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
			services.AddHttpClient();

			// Configure DbContext - read connection string from configuration or environment
			var connectionString = configuration.GetConnectionString("DefaultConnection")
								   ?? configuration["ConnectionStrings:DefaultConnection"]
								   ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

			if (string.IsNullOrWhiteSpace(connectionString))
			{
				// No connection string found; warn at runtime
				Console.WriteLine("Warning: DefaultConnection not found. Configure a connection string in appsettings.json or environment variables.");
			}

			services.AddDbContext<PerfumeDbContext>(options =>
			{
				if (!string.IsNullOrWhiteSpace(connectionString))
				{
					options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
				}
			});

			// Configure ASP.NET Core Identity using the application's User entity and GUID roles
			services.AddIdentity<User, IdentityRole<Guid>>(options =>
			{
				// Basic password / user settings - tweak as necessary for your environment
				options.Password.RequireDigit = true;
				options.Password.RequireLowercase = true;
				options.Password.RequireUppercase = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequiredLength = 6;

				options.User.RequireUniqueEmail = true;

				// Lockout settings (optional)
				options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
				options.Lockout.MaxFailedAccessAttempts = 5;
				options.Lockout.AllowedForNewUsers = true;
			})
			.AddEntityFrameworkStores<PerfumeDbContext>()
			.AddDefaultTokenProviders();

		// CORS - Allow both frontend and AI backend
		var webUrl = configuration["Front-end:webUrl"] ?? throw new Exception("Missing web url!!");
		var aiBackendUrl = configuration["Back-end:aiUrl"] ?? throw new Exception("Missing ai url!!");
		services.AddCors(options =>
		{
			options.AddPolicy("AllowConfiguredOrigins", builder =>
			{
				builder
					.WithOrigins(webUrl, aiBackendUrl)
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
			});
		});
		}

		public static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Read JWT configuration from configuration or environment variables
			var jwtKey = configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY");
			var jwtIssuer = configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER");
			var jwtAudience = configuration["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE");

			if (string.IsNullOrWhiteSpace(jwtKey))
			{
				// Warn at runtime if key is missing (tokens cannot be validated without it)
				Console.WriteLine("Warning: JWT Key not found. Set 'Jwt:Key' in configuration or 'JWT_KEY' environment variable.");
			}

			var keyBytes = Encoding.UTF8.GetBytes(jwtKey ?? string.Empty);

			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.Events = new JwtBearerEvents
				{
					OnAuthenticationFailed = context =>
					{
						// In ra console để bạn debug nhanh
						Console.WriteLine("Authentication failed: " + context.Exception.Message);
						return Task.CompletedTask;
					},
					OnChallenge = context =>
					{
						// suppress the default WWW-Authenticate header handling and return a JSON body
						context.HandleResponse();

						context.Response.StatusCode = StatusCodes.Status401Unauthorized;
						context.Response.ContentType = "application/json";
						return context.Response.WriteAsJsonAsync(BaseResponse<string>.Fail("Missing or invalid token", ResponseErrorType.Unauthorized));
					},
					OnForbidden = context =>
					{
						context.Response.StatusCode = StatusCodes.Status403Forbidden;
						context.Response.ContentType = "application/json";
						return context.Response.WriteAsJsonAsync(BaseResponse<string>.Fail("You are not authorized to access this resource", ResponseErrorType.Forbidden));
					}
				};

				options.RequireHttpsMetadata = false;
				options.SaveToken = true;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
					ValidIssuer = jwtIssuer,
					ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
					ValidAudience = jwtAudience,
					ValidateIssuerSigningKey = !string.IsNullOrWhiteSpace(jwtKey),
					IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
					ValidateLifetime = false,
					ClockSkew = TimeSpan.FromMinutes(5)
				};
			});

			// Add authorization services
			services.AddAuthorization();
		}

		private static void RegisterRepositoriesByConvention(IServiceCollection services, Assembly repoAssembly)
		{
			if (repoAssembly == null) return;

			var implTypes = repoAssembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType && t.IsPublic);

			foreach (var impl in implTypes)
			{
				// Skip types registered explicitly
				if (impl == typeof(UnitOfWork)) continue;
				if (impl.IsGenericTypeDefinition) continue;

				// find interface named I{ConcreteName}
				var match = impl.GetInterfaces()
					.FirstOrDefault(i => i.Name == $"I{impl.Name}");

				if (match == null) continue;

				// Avoid registering open-generic mapping or duplicates (IGenericRepository<> handled explicitly)
				if (match.IsGenericType) continue;

				services.AddScoped(match, impl);
			}
		}

		public static void AddSemanticKernelServices(this IServiceCollection services, IConfiguration configuration)
		{
            // Register Semantic Kernel related services here
            // Example:
            // services.AddSingleton<ISemanticKernelService, SemanticKernelService>();
        }	
    }
}
