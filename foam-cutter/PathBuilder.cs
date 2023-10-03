namespace FoamCutter;

public partial class PathBuilder
{
	public static List<Path> BuildPaths(IxMilia.Dxf.DxfFile dxf) => BuildPaths(GetPaths(dxf));

	public static List<Path> BuildPaths(Svg.SvgDocument svg) => BuildPaths(GetPaths(svg));

	private static List<Path> BuildPaths(IEnumerable<Path> paths)
	{
		var allPaths = new List<Path>();

		foreach (var newPath in paths) {
			AddPath(allPaths, newPath);
		}

		return allPaths;
	}

	private static void AddPath(List<Path> paths, Path newPath)
	{
		Console.WriteLine($"Have {paths.Count} paths, adding a path of length {newPath.Points.Count()} newPath.First: {newPath.First} newPath.Last: {newPath.Last}");
		// go through our list of paths and find one that connects to either the beginning or the end
		foreach (var path in paths.Where(p => p.SegmentType == newPath.SegmentType)) {
Console.WriteLine($"path.first: {path.First} last: {path.Last}");
			if (newPath.Last == path.Last || newPath.Last == path.First || newPath.First == path.Last) {
				path.Append(newPath);
				return;
			}
		}

		// newPath is unconnected to any candidate paths we know about
Console.WriteLine("(disconnected)");
		paths.Add(newPath);
	}
}