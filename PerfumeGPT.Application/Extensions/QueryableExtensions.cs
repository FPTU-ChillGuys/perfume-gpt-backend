using System.Linq.Expressions;

namespace PerfumeGPT.Application.Extensions
{
	public static class QueryableExtensions
	{
		public static IOrderedQueryable<T> ApplySorting<T>(
			this IQueryable<T> query,
			string? sortBy,
			bool descending)
		{
			if (string.IsNullOrWhiteSpace(sortBy))
				return query.OrderBy(e => 0); // default no-op sort to avoid exception

			var parameter = Expression.Parameter(typeof(T), "x");
			Expression propertyAccess = parameter;

			// Handle nested properties (e.g., "Voucher.Code")
			var propertyNames = sortBy.Split('.');
			Type currentType = typeof(T);

			foreach (var propertyName in propertyNames)
			{
				var property = currentType.GetProperty(propertyName);
				if (property == null)
					return query.OrderBy(e => 0); // fallback if prop not found

				propertyAccess = Expression.MakeMemberAccess(propertyAccess, property);
				currentType = property.PropertyType;
			}

			var orderByExp = Expression.Lambda(propertyAccess, parameter);

			var methodName = descending ? "OrderByDescending" : "OrderBy";
			var resultExp = Expression.Call(
				typeof(Queryable),
				methodName,
				new Type[] { typeof(T), currentType },
				query.Expression,
				Expression.Quote(orderByExp));

			return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(resultExp);
		}
	}
}
