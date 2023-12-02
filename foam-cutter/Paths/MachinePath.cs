using System.Drawing;

namespace FoamCutter.Paths;

public enum SegmentType
{
	Cut,
	Score,
}

public class MachinePath
{
	private readonly List<PointF> _points;

	public MachinePath(SegmentType segmentType, PointF start)
	{
		SegmentType = segmentType;
		_points     = new() {
            start
        };
	}

	public MachinePath(SegmentType segmentType, IEnumerable<PointF> points)
	{
		SegmentType = segmentType;
		_points     = new(points);
	}

	public SegmentType SegmentType { get; init; }

	public PointF First => _points.Count == 0 ? throw new InvalidOperationException("This path has no points.") : _points[0];

	public PointF Last => _points.Count == 0 ? throw new InvalidOperationException("This path has no points.") : _points[_points.Count - 1];

	public IEnumerable<PointF> Points => _points;

	public void Append(PointF point) => _points.Add(point);

	public bool Append(MachinePath path)
	{
		if (path.SegmentType != SegmentType) {
			return false;
		}

		if (_points.Count == 0) {
			// this path is empty; this path *becomes* the path that is being appended
			_points.EnsureCapacity(path._points.Count);
			_points.AddRange(path._points);
			return true;
		} else if (path.First == Last) {
			// the new path starts where this path ends; simple append
			_points.EnsureCapacity(_points.Count + path._points.Count - 1);
			_points.AddRange(path._points.Skip(1));
			return true;
		} else if (path.Last == First) {
			// new path ends where this path begins; simple prepend
			_points.EnsureCapacity(_points.Count + path._points.Count - 1);
			_points.InsertRange(0, path._points.SkipLast(1));
			return true;
		} else if (path.Last == Last) {
			// the new path ends where this path ends; reverse the incoming path and append it
			_points.EnsureCapacity(_points.Count + path._points.Count - 1);
			_points.AddRange(Invert(path._points).Skip(1));
			return true;
		} else if (path.First == First) {
			// the new path starts where this path starts; reverse the incoming path and prepend it
			_points.EnsureCapacity(_points.Count + path._points.Count - 1);
			_points.InsertRange(0, Invert(path._points).SkipLast(1));
			return true;
		} else {
			return false;
		}
	}

	private static IEnumerable<PointF> Invert(List<PointF> points)
	{
		for (var i = points.Count - 1; i >= 0; i--) {
			yield return points[i];
		}
	}
}
