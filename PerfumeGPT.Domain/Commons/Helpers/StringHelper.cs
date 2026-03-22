using System.Text.RegularExpressions;

namespace PerfumeGPT.Domain.Commons.Helpers
{
	public static class StringHelper
	{
		public static string ToUrlsFriendly(this string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;

			// 1. convert to lower case
			text = text.ToLowerInvariant();

			// 2. Replace Vietnamese characters with their non-accented counterparts
			string[] vietnameseSigns =
			[
			"aAeEoOuUiIyYdD",
			"áàảãạăắằẳẵặâấầẩẫậ",
			"ÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬ",
			"éèẻẽẹêếềểễệ",
			"ÉÈẺẼẸÊẾỀỂỄỆ",
			"óòỏõọôốồổỗộơớờởỡợ",
			"ÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢ",
			"úùủũụưứừửữự",
			"ÚÙỦŨỤƯỨỪỬỮỰ",
			"íìỉĩị",
			"ÍÌỈĨỊ",
			"ýỳỷỹỵ",
			"ÝỲYỶỸỴ",
			"đ",
			"Đ"
			];

			for (int i = 1; i < vietnameseSigns.Length; i++)
			{
				for (int j = 0; j < vietnameseSigns[i].Length; j++)
				{
					// Replace each accented character with the corresponding non-accented character
					if (i <= 12) // Vowels
						text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
					else if (i == 13) // đ
						text = text.Replace(vietnameseSigns[i][j], 'd');
					else if (i == 14) // Đ
						text = text.Replace(vietnameseSigns[i][j], 'd');
				}
			}

			// 3. Replace non-alphanumeric characters with underscores
			// Using regex to remove any character that is not a letter, number, space, or hyphen
			var result = Regex.Replace(text, @"[^a-z0-9\s-]", "");

			// 4. Replace multiple spaces with a single underscore and trim leading/trailing underscores
			result = Regex.Replace(result, @"\s+", "_").Trim('_');

			return result;
		}
	}
}
