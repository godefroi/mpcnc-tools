using System.Drawing;
using Svg;

namespace FoamCutter;

public partial class PathBuilder
{
	private static List<Path> GetPaths(SvgDocument svg)
	{
		var paths = new List<Path>();

		svg.ApplyRecursive(elem => {
			foreach (var path in GetPaths(elem)) {
				paths.Add(path);
			}
		});

		return paths;
	}

	private static IEnumerable<Path> GetPaths(SvgElement element)
	{
		switch (element) {
			case SvgDocument doc: // ignore
			case SvgDescription desc: // ignore
			case SvgGroup group: // ignore
				break;
			case SvgPath path:
				var newPath = ExpandPath(path.PathData, SegmentType.Cut); // TODO: figure out the right segment type

				// find a path in our list that connects with this path
				if (newPath != null) {
					yield return newPath;
				}
				break;
			default:
				throw new NotImplementedException($"Building paths for element type {element.GetType().Name} is not yet implemented.");
		}
	}

	private static Path? ExpandPath(Svg.Pathing.SvgPathSegmentList segmentList, SegmentType segmentType)
	{
		var path = new SvgPathProperties.SvgPath(segmentList.ToString(), false);

		if (path.Segments.Count == 0) {
			return null;
		}

		if (path.Segments[0] is not SvgPathProperties.MoveCommand initalMove) {
			throw new NotSupportedException("Beginning an SVG path with anything but a move command is not supported.");
		}

		var spath = new Path(segmentType, MakePoint(initalMove.X, initalMove.Y, 3));

		foreach (var command in path.Segments.Skip(1)) {
			switch (command) {
				case SvgPathProperties.MoveCommand moveCommand:
					throw new NotImplementedException("Paths with embedded move commands are not implemented.");
				case SvgPathProperties.LineCommand line:
					spath.Append(MakePoint(line.ToX, line.ToY, 3));
					break;
				case SvgPathProperties.ArcCommand arc:
					for (var i = 0d; i <= arc.Length; i += (arc.Length / Math.Ceiling(arc.Length))) {
						var point = arc.GetPointAtLength(i);
						spath.Append(MakePoint(point.X, point.Y, 3));
					}
					break;
				default:
					throw new NotImplementedException($"The path command {command.GetType().Name} is not yet implemented.");
			}
		}

		return spath;
	}

	private static PointF MakePoint(double x, double y, int decimalPlaces) => new((float)Math.Round(x, decimalPlaces), (float)Math.Round(y, decimalPlaces));
}