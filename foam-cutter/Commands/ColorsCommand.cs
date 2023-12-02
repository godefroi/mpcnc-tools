using System.CommandLine;
using System.Drawing;
using System.Security.Cryptography;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Svg;

namespace FoamCutter.Commands;

internal static class ColorsCommand
{
	public static Command GetCommand()
	{
		var command = new Command("colors", "List colors used in an input file") {
			Options.InputFile,
		};

		command.SetHandler(Execute, Options.InputFile);

		return command;
	}

	private async static Task Execute(FileInfo inputFile)
	{
		//DxfColor.FromRawValue()
		if (!inputFile.Exists) {
			throw new FileNotFoundException("The specified input file was not found.", inputFile.FullName);
		}

		switch (inputFile.Extension) {
			case ".svg":
				ListColors(GetColors(SvgDocument.Open(inputFile.FullName)));
				break;
			case ".dxf":
				ListColors(GetColors(DxfFile.Load(inputFile.FullName)));
				break;
		}

		await Task.CompletedTask;
	}

	private static void ListColors(HashSet<Color> argbColors)
	{
		var colorDict = typeof(Color)
			.GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
			.Where(pi => pi.PropertyType == typeof(Color))
			.Select(pi => pi.GetValue(null))
			.OfType<Color>()
			.DistinctBy(c => c.ToArgb())
			.ToDictionary(c => c.ToArgb(), c => c.Name);

		Console.WriteLine("Colors:");
		// foreach (var color in argbColors) {
		// 	if (colorDict.TryGetValue(color, out var value)) {
		// 		Console.WriteLine($"  {value}");
		// 	} else {
		// 		var fromArgb = Color.FromArgb(color);
		// 		Console.WriteLine($"  [A={fromArgb.A}, R={fromArgb.R}, G={fromArgb.G}, B={fromArgb.B}]");
		// 	}
		// }
	}

	private static HashSet<Color> GetColors(SvgDocument svg)
	{
		var colors = new HashSet<Color>();

		svg.ApplyRecursive(elem => {
			switch (elem) {
				case SvgDocument doc: // ignore
				case SvgDescription desc: // ignore these
				case SvgGroup group: // ignore these (the groupings in the source seem arbitrary)
					break;
				case SvgPath path:
					if (elem.Stroke != null && elem.Stroke is SvgColourServer colourServer) {
						colors.Add(Color.FromArgb(colourServer.Colour.ToArgb()));
					}
					break;
				default:
					throw new NotImplementedException($"The element type {elem.GetType().Name} is not yet implemented.");
			}
		});

		return colors;
	}

	private static HashSet<Color> GetColors(DxfFile dxf)
	{
		return dxf.Entities.Aggregate(new HashSet<Color>(), (acc, cur) => {
			switch (cur) {
				case DxfArc arc:
					acc.Add(Color.FromArgb(arc.Color.ToRGB()));
					break;
				case DxfLwPolyline lwPolyLine:
					acc.Add(Color.FromArgb(lwPolyLine.Color.ToRGB()));
					break;
				default:
					break;
			}
			return acc;
		});
	}
}
