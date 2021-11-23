namespace LogicMonitor.Provisioning.Extensions;

internal static class StringExtensions
{
	internal static string ToPascalCase(this string text)
		=> string.Concat(
			text
				.Split(" ")
				.Select(word =>
					char.ToUpperInvariant(word[0])
					+ new string(
						word
							.Skip(1)
							.Select(c => char.ToLowerInvariant(c))
							.ToArray()
						)
				)
		);
}
