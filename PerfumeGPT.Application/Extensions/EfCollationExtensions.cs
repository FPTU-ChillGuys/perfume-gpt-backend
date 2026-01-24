using System.Linq.Expressions;
using System.Reflection;

namespace PerfumeGPT.Application.Extensions
{
	public static class EfCollationExtensions
	{
		// use plain string.Contains method on the value expression (EF will translate to SQL LIKE)
		private static readonly MethodInfo s_stringContains =
			typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })
			?? throw new InvalidOperationException("string.Contains method not found");

		/// <summary>
		/// Builds an expression: value != null && value.Contains(keyword)
		/// (Previously attempted to call a custom Collate function that EF could not translate.)
		/// </summary>
		public static Expression<Func<T, bool>> CollateContains<T>(Expression<Func<T, string?>> valueSelector, string keyword)
		{
			if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
			if (keyword == null) throw new ArgumentNullException(nameof(keyword));

			var param = valueSelector.Parameters[0];
			var valueExpr = valueSelector.Body;

			// value.Contains(keyword)
			var contains = Expression.Call(valueExpr, s_stringContains, Expression.Constant(keyword));

			// value != null && value.Contains(keyword)
			var notNull = Expression.NotEqual(valueExpr, Expression.Constant(null, typeof(string)));
			var andExpr = Expression.AndAlso(notNull, contains);

			return Expression.Lambda<Func<T, bool>>(andExpr, param);
		}
	}
}
