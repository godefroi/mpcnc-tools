using System.Drawing;
using FoamCutter.Paths;

namespace FoamCutter.Machine;

public class Config
{
	private HashSet<RgbColor> _cutColors = [];
	private HashSet<RgbColor> _scoreColors = [];

	public float CuttingDepth { get; set; }

	public float TravelDepth { get; set; }

	public float ScoringDepth { get; set; }

	public int TravelSpeed { get; set; }

	public int CuttingSpeed { get; set; }

	public int PlungeSpeed { get; set; }

	public int RetractSpeed { get; set; }

	public PointF Translation { get; set; }

	public IReadOnlySet<RgbColor> CutColors => _cutColors;

	public IReadOnlySet<RgbColor> ScoreColors => _scoreColors;

	public void AddCutColor(RgbColor color) => _cutColors.Add(color);

	public void AddScoreColor(RgbColor color) => _scoreColors.Add(color);

	public SegmentType GetSegmentType(RgbColor color) => color switch {
			_ when CutColors.Contains(color) => SegmentType.Cut,
			_ when ScoreColors.Contains(color) => SegmentType.Score,
			_ => SegmentType.Ignore,
		};
}
