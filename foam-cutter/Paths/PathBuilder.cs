using FoamCutter.Machine;

namespace FoamCutter.Paths;

public partial class PathBuilder
{
	public static List<MachinePath> BuildPaths(IxMilia.Dxf.DxfFile dxf, Config config) => BuildPaths(GetPaths(dxf, config));

	public static List<MachinePath> BuildPaths(Svg.SvgDocument svg, Config config) => BuildPaths(GetPaths(svg, config));

	private static List<MachinePath> BuildPaths(IEnumerable<MachinePath> paths)
	{
		var allPaths = new List<MachinePath>();

		// add each incoming path
		foreach (var newPath in paths) {
			AddPath(allPaths, newPath);
		}

		// now, see if we can consolidate the paths down
		while (allPaths.Count >= 2) {
			if (!ConsolidatePaths(allPaths)) {
				break;
			}
		}

		return allPaths;
	}

	private static bool ConsolidatePaths(List<MachinePath> paths)
	{
		for (var i = 0; i < paths.Count; i++) {
			var path1 = paths[i];

			for (var j = 0; j < paths.Count; j++) {
				if (j == i) {
					continue;
				}

				var path2 = paths[j];

				if (path1.Append(path2)) {
					Console.WriteLine("Consolidated a path");
					paths.RemoveAt(j);
					return true;
				}
			}
		}

		return false;
	}

	private static void AddPath2(List<MachinePath> paths, MachinePath newPath)
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

	private static void AddPath(List<MachinePath> paths, MachinePath newPath)
	{
		foreach (var path in paths) {
			if (path.Append(newPath)) {
				return;
			}
		}

		paths.Add(newPath);
	}
}
