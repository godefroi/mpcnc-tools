namespace FoamCutter;

internal record class Movement(double X, double Y, double Z, MoveType MoveType)
{
	public static Movement NoMove { get; } = new(double.NaN, double.NaN, double.NaN, MoveType.Rapid);
}
