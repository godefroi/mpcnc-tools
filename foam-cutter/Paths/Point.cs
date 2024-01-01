using System.Drawing;

namespace FoamCutter.Paths;

public readonly record struct Point
{
	public decimal X { get; }

	public decimal Y { get; }

	public Point(decimal x, decimal y)
	{
		X = Math.Round(x, 3);
		Y = Math.Round(y, 3);
	}

	public Point(PointF point) : this(Convert.ToDecimal(point.X), Convert.ToDecimal(point.Y)) {}

	public static Point operator +(Point p1, Point p2) => new(p1.X + p2.X, p1.Y + p2.Y);

	public static Point operator -(Point p) => new(-p.X, -p.Y);

	public static double DistanceBetween(Point p1, Point p2)
	{
		var xDiff = p2.X - p1.X;
		var yDiff = p2.Y - p1.Y;

		return Math.Sqrt((double)((xDiff * xDiff) + (yDiff * yDiff)));
	}
}
