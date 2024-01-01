using System.CommandLine;
using System.Drawing;
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
		if (!inputFile.Exists) {
			throw new FileNotFoundException("The specified input file was not found.", inputFile.FullName);
		}

		ListColors(inputFile.Extension switch {
			".svg" => GetColors(SvgDocument.Open(inputFile.FullName)),
			".dxf" => GetColors(DxfFile.Load(inputFile.FullName)),
			_ => throw new InvalidOperationException("Only DXF and SVG files are supported."),
		});

		await Task.CompletedTask;
	}

	private static void ListColors(HashSet<RgbColor> colors)
	{
		Console.WriteLine("Colors:");

		foreach (var color in colors) {
			if (!string.IsNullOrWhiteSpace(color.Name)) {
				Console.WriteLine($"{color.Name} [R={color.R}, G={color.G}, B={color.B}]");
			} else {
				Console.WriteLine($"[R={color.R}, G={color.G}, B={color.B}]");
			}
		}
	}

	private static HashSet<RgbColor> GetColors(SvgDocument svg)
	{
		var colors = new HashSet<RgbColor>();

		svg.ApplyRecursive(elem => {
			switch (elem) {
				case NonSvgElement nonSvg: // ignore
				case SvgDefinitionList defs: // ignore
				case SvgDocument doc: // ignore
				case SvgDescription desc: // ignore these
				case SvgGroup group: // ignore these (the groupings in the source seem arbitrary)
					break;
				case SvgPath path:
					if (elem.Stroke != null && elem.Stroke is SvgColourServer colourServer) {
						colors.Add(new RgbColor(colourServer.Colour.ToArgb()));
					}
					break;
				default:
					throw new NotImplementedException($"The element type {elem.GetType().Name} is not yet implemented.");
			}
		});

		return colors;
	}

	private static HashSet<RgbColor> GetColors(DxfFile dxf)
	{
		return dxf.Entities.Aggregate(new HashSet<RgbColor>(), (acc, cur) => { acc.Add(cur switch {
			DxfArc arc => new RgbColor(arc.Color),
			DxfLwPolyline lwPolyline => new RgbColor(lwPolyline.Color),
			DxfPolyline polyline => new RgbColor(polyline.Color),
			DxfLine line => new RgbColor(line.Color),
			_ => throw new NotImplementedException($"Entity type {cur.GetType().Name} not yet implemented."),
		}); return acc; });
	}
}
