using System.CommandLine;
using FoamCutter.Commands;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Svg;

namespace FoamCutter;

public class Program
{
	// the "dimension marker" paths are ~path500 (path500 )
	// path500 is the middle "inch" one that is filled; it has dX=824.624; that should be one inch.
	// path497 is the middle "cm" one that is filled; it has dX=324.64966; that should be one cm.
	// the ratio of these is 2.5400 so it looks like we're on the right track.
	// if we want the output svg in mm, we should scale the whole file by (0.3080243484622777673631323069921)

	public static async Task Main(string[] args)
	{
		var inputFileOption = new Option<FileInfo>("--input", "Input filename");

		var rootCommand = new RootCommand("G-CODE generation tool for the MPCNC foam (needle) cutter") {
			ColorsCommand.GetCommand(),
			GenerateCommand.GetCommand(),
		};

		rootCommand.SetHandler(() => {
			Console.WriteLine("here we are.");
		});

		await rootCommand.InvokeAsync(args);
	}

	private static void OldMain()
	{
		//PathBuilder.DrawTheArc();
		//return;

		//ScaleSvg(svg, 0.1f); // first scale was 0.3080243484622777673631323069921f, then it was still 10x; so I scaled by .1... should've been 0.0308... the first time?

		//svg.Write(@"C:\Users\mark.parker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1_inkscapeconv_translated.svg");

		// svg.ApplyRecursive(descendent => {
		// 	if (descendent is SvgPath path && path.ID == "path494") {
		// 		var pf = default(PointF?);

		// 		foreach (var pd in path.PathData) {
		// 			var lpe = pf;

		// 			pf = pd.End;

		// 			if (lpe != null) {
		// 				Console.WriteLine($"dX={pf.Value.X - lpe.Value.X}, dY={pf.Value.Y - lpe.Value.Y}");
		// 			}
		// 		}
		// 	}
		// });
	}

	private static void ScaleSvg(SvgDocument svg, float scaleFactor)
	{
		svg.ApplyRecursive(elem => {
			switch (elem) {
				case SvgDocument doc: // ignore
				case SvgDescription desc: // ignore these
				case SvgGroup group: // ignore these (the groupings in the source seem arbitrary)
					break;
				case SvgPath path:
					// transform the coordinates
					foreach (var pd in path.PathData) {
						pd.End = new System.Drawing.PointF(pd.End.X * scaleFactor, pd.End.Y * scaleFactor);
					}
					break;
				default:
					throw new NotImplementedException($"Scaling for element type {elem.GetType().Name} is not yet implemented.");
			}
		});
	}

	private static void ConvertDxfToSvg()
	{
		var fn = @"C:\Users\mark.parker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1.dxf";
		var df = DxfFile.Load(fn);

		//Console.WriteLine(df.ActiveViewPort.ViewHeight);

		//Console.WriteLine($"{df.GetBoundingBox().MinimumPoint.X},{df.GetBoundingBox().MinimumPoint.Y} - {df.GetBoundingBox().MaximumPoint.X},{df.GetBoundingBox().MaximumPoint.Y}");
		//df.BlockRecords.First().



		// var hatches = df.Entities.OfType<DxfHatch>().ToList();

		// foreach (var hatch in hatches) {
		// 	df.Entities.Remove(hatch);
		// }

		// df.Save(@"C:\Users\mark.parker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1_nohatches.dxf");

		var svg = new SvgDocument()
		{
			//Color = new SvgColourServer(Color.Blue)
			Width = new SvgUnit(SvgUnitType.Millimeter, 1000),
			Height = new SvgUnit(SvgUnitType.Millimeter, 1000),
		};

		svg.Children.Add(new SvgRectangle()
		{
			Width = new SvgUnit(SvgUnitType.Percentage, 100),
			Height = new SvgUnit(SvgUnitType.Percentage, 100),
			Fill = new SvgColourServer(System.Drawing.Color.Blue)
		});

		var longestLine = df.Entities.OfType<DxfLine>().First();

		foreach (var entity in df.Entities.ToArray()) {
			// we're definitely not dealing with hatch entities
			if (entity.EntityType == DxfEntityType.Hatch) {
				df.Entities.Remove(entity);
				continue;
			}

			// only deal with grayscale entities
			var color = GetColor(entity.Color);

			if (color.R != color.G || color.R != color.B) {
				df.Entities.Remove(entity);
				continue;
			}

			//Console.WriteLine(entity.EntityType);
			switch (entity) {
				case DxfLine line:
					// find the longest line
					if ((line.P1 - line.P2).Length > (longestLine.P1 - longestLine.P2).Length) {
						longestLine = line;
					}

					var sl = new SvgLine() {
						StartX = new SvgUnit(SvgUnitType.Millimeter, (float)line.P1.X / 10f),
						StartY = new SvgUnit(SvgUnitType.Millimeter, (float)line.P1.Y / 10f),
						EndX = new SvgUnit(SvgUnitType.Millimeter, (float)line.P2.X / 10f),
						EndY = new SvgUnit(SvgUnitType.Millimeter, (float)line.P2.Y / 10f),
						Stroke = new SvgColourServer(System.Drawing.Color.Black),
						//StrokeWidth = new SvgUnit(SvgUnitType.Millimeter, (float)line.Thickness / 10f)
						StrokeWidth = new SvgUnit(SvgUnitType.Millimeter, 0.5f),
					};
					svg.Children.Add(sl);
					break;
				case DxfLwPolyline lwPolyLine:
					//lwPolyLine.AsSimpleEntities()
					break;
				case DxfArc arc:
					//arc.GetPointFromAngle
					//new SvgArcSegment(arc.Radius, arc.Radius, 0, SvgArcSize.Small, SvgArcSweep.Positive, false, 
					//break;
				default:
					Console.WriteLine(entity.EntityTypeString);
					break;
			}
		}

		svg.Write(@"C:\Users\mark.parker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1_stripped.svg");

		using var img = svg.Draw();
		img.Save(@"C:\Users\mark.parker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1_stripped.png", System.Drawing.Imaging.ImageFormat.Png);

		//DxfColor.FromRawValue(1234).
		longestLine.Color24Bit = 0x000000FF;

		Console.WriteLine($"Longest line is {(longestLine.P1 - longestLine.P2).Length}");

		df.Save(@"C:\Users\mark.parker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1_stripped.dxf");


		(byte R, byte G, byte B) GetColor(DxfColor color) {
			var rgb = color.ToRGB();

			return ((byte)((rgb & 0x00FF0000) >> 16), (byte)((rgb & 0x0000FF00) >> 8), (byte)(rgb & 0x000000FF));
		}
	}
}
