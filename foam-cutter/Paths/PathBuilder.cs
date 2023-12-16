using FoamCutter.Machine;

namespace FoamCutter.Paths;

public partial class PathBuilder
{
	public static List<MachinePath> BuildPaths(IxMilia.Dxf.DxfFile dxf, Config config) => BuildPaths(GetPaths(dxf, config));

	public static List<MachinePath> BuildPaths(Svg.SvgDocument svg, Config config) => BuildPaths(GetPaths(svg, config));

	private static List<MachinePath> BuildPaths(IEnumerable<MachinePath> paths)
	{
		var allPaths = new List<MachinePath>();

		foreach (var newPath in paths) {
			AddPath(allPaths, newPath);
		}

		return allPaths;
	}

	private static void AddPath(List<MachinePath> paths, MachinePath newPath)
	{
//Console.WriteLine($"Have {paths.Count} paths, adding a path of length {newPath.Points.Count()} newPath.First: {newPath.First} newPath.Last: {newPath.Last}");
		// go through our list of paths and find one that connects to either the beginning or the end
		foreach (var path in paths.Where(p => p.SegmentType == newPath.SegmentType)) {
//Console.WriteLine($"path.first: {path.First} last: {path.Last}");
			if (newPath.Last == path.Last || newPath.Last == path.First || newPath.First == path.Last) {
				path.Append(newPath);
				return;
			}
		}

		// newPath is unconnected to any candidate paths we know about
//Console.WriteLine("(disconnected)");
		paths.Add(newPath);
	}
}
