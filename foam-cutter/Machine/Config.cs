using FoamCutter.Paths;

namespace FoamCutter.Machine;

public class Config
{
	private HashSet<RgbColor> _cutColors = [];
	private HashSet<RgbColor> _scoreColors = [];
	private HashSet<string> _includeGroups = [];

	public decimal CuttingDepth { get; set; }

	public decimal TravelDepth { get; set; }

	public decimal ScoringDepth { get; set; }

	public int TravelSpeed { get; set; }

	public int CuttingSpeed { get; set; }

	public int PlungeSpeed { get; set; }

	public int RetractSpeed { get; set; }

	public Point Translation { get; set; }

	public IReadOnlySet<RgbColor> CutColors => _cutColors;

	public IReadOnlySet<RgbColor> ScoreColors => _scoreColors;

	public void AddCutColor(RgbColor color) => _cutColors.Add(color);

	public void AddScoreColor(RgbColor color) => _scoreColors.Add(color);

	public void AddGroupName(string groupName) => _includeGroups.Add(groupName);

	public SegmentType GetSegmentType(RgbColor color) => color switch {
			_ when CutColors.Contains(color) => SegmentType.Cut,
			_ when ScoreColors.Contains(color) => SegmentType.Score,
			_ => SegmentType.Ignore,
		};

	public bool GroupsInclude(IEnumerable<string> groupNames) =>
		_includeGroups.Count == 0 ? true : groupNames.Any(n => _includeGroups.Contains(n));
}
