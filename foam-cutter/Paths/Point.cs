using System.Drawing;

namespace FoamCutter.Paths;

public readonly record struct Point
{
	public decimal X { get; }

	public decimal Y { get; }

	public Point(decimal x, decimal y)
	{
		X = Math.Round(x, 2);
		Y = Math.Round(y, 2);
	}

	public Point(PointF point) : this(Convert.ToDecimal(point.X), Convert.ToDecimal(point.Y)) {}
}
