using System.Drawing;

namespace FoamCutter.Paths;

public enum SegmentType
{
	Ignore,
	Cut,
	Score,
}

public class MachinePath
{
	private readonly List<Point> _points;

	public MachinePath(SegmentType segmentType, PointF start)
	{
		SegmentType = segmentType;
		_points     = [ new Point(start) ];
	}

	public MachinePath(SegmentType segmentType, params PointF[] points)
	{
		SegmentType = segmentType;
		_points     = new(points.Select(p => new Point(p)));
	}

	public MachinePath(SegmentType segmentType, IEnumerable<PointF> points)
	{
		SegmentType = segmentType;
		_points     = new(points.Select(p => new Point(p)));
	}

	public SegmentType SegmentType { get; init; }

	public Point First => _points.Count == 0 ? throw new InvalidOperationException("This path has no points.") : _points[0];

	public Point Last => _points.Count == 0 ? throw new InvalidOperationException("This path has no points.") : _points[_points.Count - 1];

	public IEnumerable<Point> Points => _points;

	public void Reverse() => _points.Reverse();

	public void Append(PointF point) => _points.Add(new Point(point));

	public void Append(Point point) => _points.Add(point);

	public bool Append(MachinePath path)
	{
		// no need to round when appending a path, as they've all already been rounded
		
		if (path.SegmentType != SegmentType) {
			return false;
		}

		//static double dist(PointF p1, PointF p2) => Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
		//Console.WriteLine($"Appending a path; diff p.f-l {dist(path.First, Last):00.000} p.l-f {dist(path.Last, First):00.000} p.l-l {dist(path.Last, Last):00.000} p.f-f {dist(path.First, First):00.000}");

		if (_points.Count == 0) {
			// this path is empty; this path *becomes* the path that is being appended
			_points.EnsureCapacity(path._points.Count);
			_points.AddRange(path._points);
			return true;
		} else if (path.First == Last) {
			//Console.WriteLine("  path.First == Last");
			// the new path starts where this path ends; simple append
			_points.EnsureCapacity(_points.Count + path._points.Count - 1);
			_points.AddRange(path._points.Skip(1));
			return true;
		} else if (path.Last == First) {
			//Console.WriteLine("  path.Last == First");
			// new path ends where this path begins; simple prepend
			_points.EnsureCapacity(_points.Count + path._points.Count - 1);
			_points.InsertRange(0, path._points.SkipLast(1));
			return true;
		} else if (path.Last == Last) {
			// the new path ends where this path ends; reverse the incoming path and append it
			_points.EnsureCapacity(_points.Count + path._points.Count - 1);
			path.Reverse();
			_points.AddRange(path._points.Skip(1));
			return true;
		} else if (path.First == First) {
			// the new path starts where this path starts; reverse the incoming path and prepend it
			_points.EnsureCapacity(_points.Count + path._points.Count - 1);
			path.Reverse();
			_points.InsertRange(0, path._points.SkipLast(1));
			return true;
		} else {
			return false;
		}
	}

	private static JoinType FindJoinType(MachinePath from, MachinePath to)
	{
		if (from.SegmentType != to.SegmentType) {
			return JoinType.None;
		} else if (from.First == to.Last) {
			// the new path starts where this path ends; simple append
			return JoinType.FirstToLast;
		} else if (from.Last == to.First) {
			// new path ends where this path begins; simple prepend
			return JoinType.LastToFirst;
		} else if (from.Last == to.Last) {
			// the new path ends where this path ends; reverse the incoming path and append it
			return JoinType.LastToLast;
		} else if (from.First == to.First) {
			// the new path starts where this path starts; reverse the incoming path and prepend it
			return JoinType.FirstToFirst;
		} else {
			return JoinType.None;
		}
	}

	private enum JoinType
	{
		None,
		FirstToLast,
		LastToFirst,
		LastToLast,
		FirstToFirst,
	}
}
