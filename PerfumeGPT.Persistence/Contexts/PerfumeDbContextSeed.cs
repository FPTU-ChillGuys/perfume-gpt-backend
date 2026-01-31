using Microsoft.AspNetCore.Identity;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Persistence.Contexts
{
	public static class PerfumeDbContextSeed
	{
		public static ICollection<IdentityRole<Guid>> SeedingRoles()
		{
			return new List<IdentityRole<Guid>>()
			{
				new IdentityRole<Guid>
				{
					Id = Guid.Parse("3631e38b-60dd-4d1a-af7f-a26f21c2ef82"),
					Name = "admin",
					NormalizedName = "ADMIN",
					ConcurrencyStamp = "seed-1"
				},
				new IdentityRole<Guid>
				{
					Id = Guid.Parse("51ef7e08-ff07-459b-8c55-c7ebac505103"),
					Name = "user",
					NormalizedName = "USER",
					ConcurrencyStamp = "seed-2"
				},
				new IdentityRole<Guid>
				{
					Id = Guid.Parse("8f6e1c3d-2d3b-4f4a-9f4a-2e5d6c7b8a9b"),
					Name = "staff",
					NormalizedName = "STAFF",
					ConcurrencyStamp = "seed-3"
				}
			};
		}

		public static ICollection<User> SeedingUsers()
		{
			// Pre-computed password hashes for "12345aA@" to avoid dynamic values
			// You can generate these by running: new PasswordHasher<User>().HashPassword(null, "12345aA@")
			var admin = new User
			{
				Id = Guid.Parse("33f41895-b601-4aa1-8dc4-8229a9d07008"),
				UserName = "admin",
				NormalizedUserName = "ADMIN",
				Email = "admin@example.com",
				NormalizedEmail = "ADMIN@EXAMPLE.COM",
				EmailConfirmed = true,
				PasswordHash = "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==",
				SecurityStamp = "seed-4",
				ConcurrencyStamp = "seed-5",
				CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};

			var user = new User
			{
				Id = Guid.Parse("09097277-2705-40c2-bce5-51dbd1f4c1e6"),
				UserName = "user",
				NormalizedUserName = "USER",
				Email = "user@example.com",
				NormalizedEmail = "USER@EXAMPLE.COM",
				EmailConfirmed = true,
				PasswordHash = "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==",
				SecurityStamp = "seed-6",
				ConcurrencyStamp = "seed-7",
				CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};

			var staff = new User
			{
				Id = Guid.Parse("09097277-5555-40c2-bce5-51dbd1f4c1e6"),
				UserName = "staff",
				NormalizedUserName = "STAFF",
				Email = "staff@example.com",
				NormalizedEmail = "STAFF@EXAMPLE.COM",
				EmailConfirmed = true,
				PasswordHash = "AQAAAAIAAYagAAAAEKHinnJYz3sNmgoyw1lyOSf143VtvFvyCDcYcupcT7XK7Hf+J3UFoVZMKadVq3YmOA==",
				SecurityStamp = "seed-8",
				ConcurrencyStamp = "seed-9",
				CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};

			return new List<User> { admin, user, staff };
		}

		public static ICollection<IdentityUserRole<Guid>> SeedingUserRoles()
		{
			return new List<IdentityUserRole<Guid>>
			{
				new IdentityUserRole<Guid>
				{
					UserId = Guid.Parse("33f41895-b601-4aa1-8dc4-8229a9d07008"),
					RoleId = Guid.Parse("3631e38b-60dd-4d1a-af7f-a26f21c2ef82")
				},
				new IdentityUserRole<Guid>
				{
					UserId = Guid.Parse("09097277-2705-40c2-bce5-51dbd1f4c1e6"),
					RoleId = Guid.Parse("51ef7e08-ff07-459b-8c55-c7ebac505103")
				},
				new IdentityUserRole<Guid>
				{
					UserId = Guid.Parse("09097277-5555-40c2-bce5-51dbd1f4c1e6"),
					RoleId = Guid.Parse("8f6e1c3d-2d3b-4f4a-9f4a-2e5d6c7b8a9b")
				}
			};
		}
	}
}
