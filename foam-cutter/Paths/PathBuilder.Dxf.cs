using System.Drawing;
using FoamCutter.Machine;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using SkiaSharp;

namespace FoamCutter.Paths;

public partial class PathBuilder
{
	public static void DrawTheArc()
	{
		var fn  = @"C:\Users\MarkParker\Downloads\minnie_ear.dxf";
		var dxf = DxfFile.Load(fn);
		var arc = dxf.Entities.OfType<DxfArc>().Single(a => Math.Abs(300 - a.StartAngle) < 1);

		var circumference = 2 * Math.PI * arc.Radius;
		var sweep         = CalculateSweep(arc);
		var length        = circumference * (sweep / 360d);
		var spath         = new MachinePath(SegmentType.Cut, MakePoint(arc.GetPointFromAngle(arc.StartAngle), 3)); // TODO: figure out the right segment type
		var step          = sweep / Math.Min(sweep, length); // step is min 1 degree, up to 1 per mm
		// Console.WriteLine($"LineTypeName: {arc.LineTypeName} StartAngle: {arc.StartAngle:f3} EndAngle: {arc.EndAngle:f3}");
		// Console.WriteLine($"  start point: {arc.GetPointFromAngle(arc.StartAngle).Round()} end point: {arc.GetPointFromAngle(arc.EndAngle).Round()}");
		// Console.WriteLine($"  circumference: {circumference:f3} sweep: {sweep:f3} length: {length:f3} step: {step:f3}");

		var points = new List<PointF>();

		for (var angle = arc.StartAngle - step; angle >= 0; angle -= step) {
			points.Add(MakePoint(arc.GetPointFromAngle(angle), 3));
		}

		Console.WriteLine($"{points.Count} points.");

		var minX  = float.MaxValue;
		var minY  = float.MaxValue;
		var maxX  = float.MinValue;
		var maxY  = float.MinValue;

		foreach (var point in points) {
			minX = Math.Min(minX, point.X);
			minY = Math.Min(minY, point.Y);
			maxX = Math.Max(maxX, point.X);
			maxY = Math.Max(maxY, point.Y);
		}

		var translation = new PointF(-minX, -minY);
		var color       = new SKColor(255, 0, 0);

		using var bitmap = new SKBitmap(Convert.ToInt32(maxX + translation.X) + 10, Convert.ToInt32(maxY + translation.Y) + 10);

		using (var canvas = new SKCanvas(bitmap)) {
			canvas.Clear(new SKColor(255, 255, 255));

			// foreach (var point in points) {
			// 	//bitmap.SetPixel(Convert.ToInt32((point.X + translation.X) * 100), Convert.ToInt32((point.Y + translation.Y) * 100), color);
			// 	canvas.DrawPoint(new SKPoint(point.X + translation.X, point.Y + translation.Y), color);
			// }

			var angle = arc.StartAngle;
			while (arc.ContainsAngle(angle)) {
				var apoint = arc.GetPointFromAngle(angle);
				canvas.DrawPoint(new SKPoint(Convert.ToSingle(apoint.X) + translation.X, Convert.ToSingle(apoint.Y) + translation.Y), color);
				angle = (angle + step) % 360d;
			}

			var start = MakePoint(arc.GetPointFromAngle(arc.StartAngle), 3);
			var end   = MakePoint(arc.GetPointFromAngle(arc.EndAngle), 3);

			canvas.DrawPoint(start.X + translation.X, start.Y + translation.Y, new SKColor(0, 255, 0));
			canvas.DrawPoint(end.X + translation.X, end.Y + translation.Y, new SKColor(0, 0, 255));
		}

		// foreach (var point in points) {
		// 	bitmap.SetPixel(Convert.ToInt32((point.X + translation.X) * 100), Convert.ToInt32((point.Y + translation.Y) * 100), color);
		// }

		using var fs = File.OpenWrite("arc.png");
		bitmap.Encode(fs, SKEncodedImageFormat.Png, 100);
	}

	private static IEnumerable<MachinePath> GetPaths(DxfFile dxf, Config config)
	{
		foreach (var entity in dxf.Entities) {
			foreach (var path in GetPaths(entity, config)) {
				yield return path;
			}
		}
	}

	private static IEnumerable<MachinePath> GetPaths(DxfEntity entity, Config config)
	{
		var segmentType = config.GetSegmentType(new RgbColor(entity.Color));

//Console.WriteLine($"derived segment type: {segmentType}");

		if (segmentType == SegmentType.Ignore) {
			yield break;
		}

		switch (entity) {
			case DxfArc arc:
				yield return ExpandArc(arc, segmentType);
				break;
			case DxfLwPolyline lwPolyline:
				foreach (var plEntity in lwPolyline.AsSimpleEntities()) {
					foreach (var se in GetPaths(plEntity, config).Where(e => e != null)) {
						yield return se;
					}
				}
				break;
			case DxfPolyline polyline:
				foreach (var plEntity in polyline.AsSimpleEntities()) {
					foreach (var se in GetPaths(plEntity, config).Where(e => e != null)) {
						yield return se;
					}
				}
				break;
			case DxfLine line:
				yield return new MachinePath(segmentType, MakePoint(line.P1, 3), MakePoint(line.P2, 3));
				break;
			case DxfSpline spline:
			default:
				throw new NotImplementedException($"Building paths for element type {entity.GetType().Name} ({entity.EntityType}) is not yet implemented.");
		}
	}

	private static MachinePath ExpandArc2(DxfArc arc)
	{
		// for an arc, 0 degrees is in the +X direction, increasing in a CCW rotation
		var circumference = 2 * Math.PI * arc.Radius;
		var sweep         = CalculateSweep(arc);
		var length        = circumference * (sweep / 360d);
		var spath         = new MachinePath(SegmentType.Cut, MakePoint(arc.GetPointFromAngle(arc.StartAngle), 3)); // TODO: figure out the right segment type
		var step          = sweep / Math.Min(sweep, length); // step is min 1 degree, up to 1 per mm
		// Console.WriteLine($"LineTypeName: {arc.LineTypeName} StartAngle: {arc.StartAngle:f3} EndAngle: {arc.EndAngle:f3}");
		// Console.WriteLine($"  start point: {arc.GetPointFromAngle(arc.StartAngle).Round()} end point: {arc.GetPointFromAngle(arc.EndAngle).Round()}");
		// Console.WriteLine($"  circumference: {circumference:f3} sweep: {sweep:f3} length: {length:f3} step: {step:f3}");

		// if (Math.Round(arc.StartAngle) != 299) {
		// 	Console.WriteLine("skipping.");
		// 	return spath;
		// }


		if (arc.EndAngle < arc.StartAngle) {
			// head toward zero, then from 360 to end
			for (var angle = arc.StartAngle - step; angle >= 0; angle -= step) {
				spath.Append(MakePoint(arc.GetPointFromAngle(angle), 3));
			}

			// for (var angle = 360d; angle > arc.EndAngle; angle -= step) {
			// 	spath.Append(MakePoint(arc.GetPointFromAngle(angle), 3));
			// }

			// for (var angle = arc.StartAngle - step; angle > arc.EndAngle; angle -= step) {
			// 	spath.Append(MakePoint(arc.GetPointFromAngle(angle), 3));
			// }
		} else {
			// simple case, head toward end
			for (var angle = arc.StartAngle + step; angle < arc.EndAngle; angle += step) {
				spath.Append(MakePoint(arc.GetPointFromAngle(angle), 3));
			}
		}

		spath.Append(MakePoint(arc.GetPointFromAngle(arc.EndAngle), 3));

		Console.WriteLine($"Made a path with {spath.Points.Count()} points");

		return spath;
	}

	private static MachinePath ExpandArc(DxfArc arc, SegmentType segmentType)
	{
		// for an arc, 0 degrees is in the +X direction, increasing in a CCW rotation
		var circumference = 2 * Math.PI * arc.Radius;
		var sweep         = CalculateSweep(arc);
		var length        = circumference * (sweep / 360d);
		var spath         = new MachinePath(segmentType, MakePoint(arc.GetPointFromAngle(arc.StartAngle), 3)); // TODO: figure out the right segment type
		var step          = sweep / Math.Min(sweep, length); // step is min 1 degree, up to 1 per mm
		// Console.WriteLine($"LineTypeName: {arc.LineTypeName} StartAngle: {arc.StartAngle:f3} EndAngle: {arc.EndAngle:f3}");
		// Console.WriteLine($"  start point: {arc.GetPointFromAngle(arc.StartAngle).Round()} end point: {arc.GetPointFromAngle(arc.EndAngle).Round()}");
		// Console.WriteLine($"  circumference: {circumference:f3} sweep: {sweep:f3} length: {length:f3} step: {step:f3}");

		var angle = arc.StartAngle;
		while (arc.ContainsAngle(angle)) {
			spath.Append(MakePoint(arc.GetPointFromAngle(angle), 3));
			angle = (angle + step) % 360d;
		}

		spath.Append(MakePoint(arc.GetPointFromAngle(arc.EndAngle), 3));

		//Console.WriteLine($"Made a path with {spath.Points.Count()} points");

		return spath;
	}

	private static PointF MakePoint(DxfPoint point, int decimalPlaces) => new((float)Math.Round(point.X, decimalPlaces), (float)Math.Round(point.Y, decimalPlaces));

	private static double CalculateSweep(DxfArc arc) => arc.EndAngle > arc.StartAngle ? arc.EndAngle - arc.StartAngle : 360d - arc.StartAngle + arc.EndAngle;
}
