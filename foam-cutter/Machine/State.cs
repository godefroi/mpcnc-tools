namespace FoamCutter.Machine;

public class State
{
	private const double COMPARISON_PRECISION = 0.001;

	private readonly Config _config;

	public State(Config machineConfig)
	{
		_config = machineConfig;
	}

	public float X { get; set; }

	public float Y { get; set; }

	public float Z { get; set; }

	public bool Cutting => CoordinateEquals(Z, _config.CuttingDepth);

	public bool Scoring => CoordinateEquals(Z, _config.ScoringDepth);

	public bool CuttingOrScoring => Cutting || Scoring;

	public bool AtCoordinates(float x, float y) => CoordinateEquals(X, x) && CoordinateEquals(Y, y);

	public bool AtCoordinates(float x, float y, float z) => CoordinateEquals(X, x) && CoordinateEquals(Y, y) && CoordinateEquals(Z, z);

	private static bool CoordinateEquals(float a, float b) => Math.Abs(a - b) <= COMPARISON_PRECISION;
}
