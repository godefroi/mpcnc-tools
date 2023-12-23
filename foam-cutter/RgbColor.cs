using IxMilia.Dxf;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SystemDrawingColor = System.Drawing.Color;

namespace FoamCutter;

public partial record class RgbColor
{
	private static Dictionary<int, string> _colorNames = typeof(SystemDrawingColor)
		.GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
		.Where(pi => pi.PropertyType == typeof(SystemDrawingColor))
		.Select(pi => pi.GetValue(null))
		.OfType<SystemDrawingColor>()
		.DistinctBy(c => c.ToArgb())
		.ToDictionary(c => c.ToArgb(), c => c.Name);

	public required int R { get; init; }

	public required int G { get; init; }

	public required int B { get; init; }

	public string? Name { get; private set; }

	[SetsRequiredMembers]
	public RgbColor(int argbColor)
	{
		var color = SystemDrawingColor.FromArgb(argbColor);

		if (color.A != 255) {
			throw new NotSupportedException($"Non-opaque colors are not supported (alpha value {color.A}).");
		}

		R = color.R;
		G = color.G;
		B = color.B;

		if (_colorNames.TryGetValue(argbColor, out var colorName)) {
			Name = colorName;
		}
	}

	[SetsRequiredMembers]
	public RgbColor(SystemDrawingColor color) : this(color.ToArgb()) {}

	[SetsRequiredMembers]
	public RgbColor(DxfColor color) : this(color.ToRGB()) {}

	[SetsRequiredMembers]
	public RgbColor(string color) : this(ParseColor(color)) {}

	private static int ParseColor(string color)
	{
		var foundColor = _colorNames.SingleOrDefault(kvp => kvp.Value.Equals(color, StringComparison.InvariantCultureIgnoreCase));

		if (!string.IsNullOrWhiteSpace(foundColor.Value) && foundColor.Value.Equals(color, StringComparison.InvariantCultureIgnoreCase)) {
			return foundColor.Key;
		}

		var reMatch = GetRgbRegex().Match(color);

		if (reMatch.Success) {
			var r = int.Parse(reMatch.Groups["r"].ValueSpan);
			var g = int.Parse(reMatch.Groups["g"].ValueSpan);
			var b = int.Parse(reMatch.Groups["b"].ValueSpan);

			return SystemDrawingColor.FromArgb(r, g, b).ToArgb();
		}

		throw new InvalidOperationException($"Unknown color: {color}");
	}

	[GeneratedRegex(@"\[R=(?<r>\d{1,3}), G=(?<g>\d{1,3}), B=(?<b>\d{1,3})\]")]
	private static partial Regex GetRgbRegex();
}