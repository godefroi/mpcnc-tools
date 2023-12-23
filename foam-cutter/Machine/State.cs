namespace FoamCutter.Machine;

public class State
{
	private const decimal COMPARISON_PRECISION = 0.001m;

	private readonly Config _config;

	public State(Config machineConfig)
	{
		_config = machineConfig;
	}

	public decimal X { get; set; }

	public decimal Y { get; set; }

	public decimal Z { get; set; }

	public CoordinateMode MovementMode { get; set; }

	public bool Cutting => CoordinateEquals(Z, _config.CuttingDepth);

	public bool Scoring => CoordinateEquals(Z, _config.ScoringDepth);

	public bool CuttingOrScoring => Cutting || Scoring;

	public bool AtCoordinates(decimal x, decimal y) => CoordinateEquals(X, x) && CoordinateEquals(Y, y);

	public bool AtCoordinates(decimal x, decimal y, decimal z) => CoordinateEquals(X, x) && CoordinateEquals(Y, y) && CoordinateEquals(Z, z);

	private static bool CoordinateEquals(decimal a, decimal b) => Math.Abs(a - b) <= COMPARISON_PRECISION;
}

public enum CoordinateMode
{
	Absolute,
	Relative,
}
