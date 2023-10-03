using System.Drawing;

namespace FoamCutter.Machine;

public class Config
{
	public float CuttingDepth { get; set; }

	public float TravelDepth { get; set; }

	public float ScoringDepth { get; set; }

	public int TravelSpeed { get; set; }

	public int CuttingSpeed { get; set; }

	public int PlungeSpeed { get; set; }

	public int RetractSpeed { get; set; }

	public PointF Translation { get; set; }
}
