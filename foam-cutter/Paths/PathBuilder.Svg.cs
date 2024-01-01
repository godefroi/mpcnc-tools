using System.Drawing;
using FoamCutter.Machine;
using Svg;

namespace FoamCutter.Paths;

public partial class PathBuilder
{
	private static List<MachinePath> GetPaths(SvgDocument svg, Config config)
	{
		var paths = new List<MachinePath>();

		svg.ApplyRecursive(elem => {
			foreach (var path in GetPaths(elem, config)) {
				paths.Add(path);
			}
		});

		return paths;
	}

	private static IEnumerable<MachinePath> GetPaths(SvgElement element, Config config)
	{
		if (element.Stroke is not SvgColourServer strokeServer) {
			//throw new InvalidOperationException($"element {element.GetType().Name} stroke is not a colour server");
			// if the element doesn't have a color, we'll just skip it
			yield break;
		}

		var segmentType = config.GetSegmentType(new RgbColor(strokeServer.Colour));

		// if this segment isn't one we're going to use, we can skip the rest of this
		if (segmentType == SegmentType.Ignore) {
			yield break;
		}

		// if this element is not included based on group name, bail here
		if (!config.GroupsInclude(element.Parents.OfType<SvgGroup>().Select(g => g.ID))) {
			yield break;
		}

		switch (element) {
			case SvgDocument doc: // ignore
			case SvgDescription desc: // ignore
			case SvgGroup group: // ignore
				break;
			case SvgPath path:
				var newPath = ExpandPath(path.PathData, segmentType);

				// find a path in our list that connects with this path
				if (newPath != null) {
					yield return newPath;
				}
				break;
			default:
				throw new NotImplementedException($"Building paths for element type {element.GetType().Name} is not yet implemented.");
		}
	}

	private static MachinePath? ExpandPath(Svg.Pathing.SvgPathSegmentList segmentList, SegmentType segmentType)
	{
		var path = new SvgPathProperties.SvgPath(segmentList.ToString(), false);

		if (path.Segments.Count == 0) {
			return null;
		}

		if (path.Segments[0] is not SvgPathProperties.MoveCommand initalMove) {
			throw new NotSupportedException("Beginning an SVG path with anything but a move command is not supported.");
		}

		var spath = new MachinePath(segmentType, MakePoint(initalMove.X, initalMove.Y, 3));

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
