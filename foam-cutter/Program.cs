using System.Drawing;
using FoamCutter.Machine;
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

	public static void Main(string[] args)
	{
		//PathBuilder.DrawTheArc();
		//return;
		//var fn  = @"C:\Users\markparker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1_inkscapeconv_translated.svg";
		var fn = @"C:\Users\MarkParker\Downloads\minnie_ear.dxf";

		var paths = ReadPaths(fn);
		var minX  = float.MaxValue;
		var minY  = float.MaxValue;
		var maxX  = float.MinValue;
		var maxY  = float.MinValue;

		Console.WriteLine($"{paths.Count} paths generated totalling {paths.Sum(p => p.Points.Count())} points.");

		foreach (var point in paths.SelectMany(p => p.Points)) {
			minX = Math.Min(minX, point.X);
			minY = Math.Min(minY, point.Y);
			maxX = Math.Max(maxX, point.X);
			maxY = Math.Max(maxY, point.Y);
		}

		var config = new Config() {
			TravelSpeed  = 6000, // feed rate (in mm/min) used for travel moves (which are G1)
			CuttingSpeed = 600,  // feed rate (in mm/min) used for cut/score moves (which are G0)
			PlungeSpeed  = 150,  // feed rate (in mm/min) used for plunge moves (which are G0)
			RetractSpeed = 1500, // feed rate (in mm/min) used for retract moves (which are G1)
			Translation  = new PointF(-minX, -minY),
			CuttingDepth = 0,  // cutting happens at "full" depth, meaning the machine should be homed such that Z=0 engages the cutter fully through the workpiece
			ScoringDepth = 19, // THIS DISABLES SCORING (because scoring will happen at nearly the travel depth, where no cutting will occur)
			TravelDepth  = 20, // this is the safe height, where rapid moves can occur without dragging the cutter through the workpiece
		};

		Console.WriteLine($"Minimum X,Y coordinate: {minX},{minY}");
		Console.WriteLine($"Maximum X,Y coordinate: {maxX},{maxY}");
		Console.WriteLine($"Translation: {config.Translation}");

		using var of = File.CreateText("minnie_ear_dxf.gcode");
		BuildCode(paths, config, of);

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

	private static List<Path> ReadPaths(string filename) => System.IO.Path.GetExtension(filename) switch {
			".dxf" => PathBuilder.BuildPaths(DxfFile.Load(filename)),
			".svg" => PathBuilder.BuildPaths(SvgDocument.Open(filename)),
			_ => throw new NotSupportedException($"The specified extension is not supported: {System.IO.Path.GetExtension(filename)}"),
		};

	private static void ListSvgColors(string filename)
	{
		var colorDict = typeof(Color)
			.GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
			.Where(pi => pi.PropertyType == typeof(Color))
			.Select(pi => pi.GetValue(null))
			.OfType<Color>()
			.DistinctBy(c => c.ToArgb())
			.ToDictionary(c => c.ToArgb(), c => c.Name);

		var svg    = SvgDocument.Open(filename);
		var colors = GetColors(svg);

		Console.WriteLine("Colors:");
		foreach (var color in colors) {
			if (colorDict.ContainsKey(color)) {
				Console.WriteLine($"  {colorDict[color]}");
			} else {
				var fromArgb = Color.FromArgb(color);
				Console.WriteLine($"  [A={fromArgb.A}, R={fromArgb.R}, G={fromArgb.G}, B={fromArgb.B}]");
			}
		}
	}

	private static HashSet<int> GetColors(SvgDocument svg)
	{
		var colors = new HashSet<int>();

		svg.ApplyRecursive(elem => {
			switch (elem) {
				case SvgDocument doc: // ignore
				case SvgDescription desc: // ignore these
				case SvgGroup group: // ignore these (the groupings in the source seem arbitrary)
					break;
				case SvgPath path:
					if (elem.Stroke != null && elem.Stroke is SvgColourServer colourServer) {
						colors.Add(colourServer.Colour.ToArgb());
					}
					break;
				default:
					throw new NotImplementedException($"The element type {elem.GetType().Name} is not yet implemented.");
			}
		});

		return colors;
	}

	private static void BuildCode(IEnumerable<Path> paths, Config config, TextWriter output)
	{
		static void EmitMove(float toX, float toY, float toZ, State state, Config config, TextWriter output)
		{
			if (state.AtCoordinates(toX, toY, toZ)) {
				return;
			}

			if (toZ != state.Z) {
				if (toX != state.X || toY != state.Y) {
					throw new InvalidOperationException("Cannot combine X-Y and Z moves in a single command");
				}

				if (toZ > state.Z) {
					output.WriteLine($"G0 Z{toZ:F2} F{config.RetractSpeed}");
				} else {
					output.WriteLine($"G1 Z{toZ:F2} F{config.PlungeSpeed}");
				}
			} else {
				if (state.CuttingOrScoring) {
					output.WriteLine($"G1 X{toX + config.Translation.X:F2} Y{toY + config.Translation.Y:F2} F{config.CuttingSpeed}");
				} else {
					output.WriteLine($"G0 X{toX + config.Translation.X:F2} Y{toY + config.Translation.Y:F2} F{config.TravelSpeed}");
				}
			}

			state.X = toX;
			state.Y = toY;
			state.Z = toZ;
		}

		static void GenerateMoves(IEnumerable<Path> paths, State state, float cutDepth, Config config, TextWriter output)
		{
			foreach (var path in paths) {
				// travel to the initial point in the path
				if (!state.AtCoordinates(path.First.X, path.First.Y)) {
					if (state.CuttingOrScoring) {
						// retract the cutter
						EmitMove(state.X, state.Y, config.TravelDepth, state, config, output);
					}

					// move to the initial point
					EmitMove(path.First.X, path.First.Y, config.TravelDepth, state, config, output);
				}

				// plunge to the correct depth
				EmitMove(state.X, state.Y, cutDepth, state, config, output);

				// move to each subsequent point
				foreach (var point in path.Points.Skip(1)) {
					EmitMove(point.X, point.Y, cutDepth, state, config, output);
				}
			}
		}

		var state = new State(config);

		// step ZERO is to configure the machine
		output.WriteLine("G90"); // configure for absolute coordinates

		// step ONE is to move the Z axis to the travel coordinate
		EmitMove(state.X, state.Y, config.TravelDepth, state, config, output);

		GenerateMoves(paths.Where(p => p.SegmentType == SegmentType.Score), state, config.ScoringDepth, config, output);
		GenerateMoves(paths.Where(p => p.SegmentType == SegmentType.Cut),   state, config.CuttingDepth, config, output);

		// step LAST is to move the Z axis to the travel coordinate
		EmitMove(state.X, state.Y, config.TravelDepth, state, config, output);
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
