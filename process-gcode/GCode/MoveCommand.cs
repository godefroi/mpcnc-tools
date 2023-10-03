namespace Mpcnc.GCodeProcessor.GCode;

public partial class MoveCommand : Command
{
	private readonly static MoveCommand _emptyCommand = new() { RawCommand = string.Empty, MoveType = MoveType.Rapid, X = null, Y = null, Z = null, I = null, J = null, Rate = null };

	public required MoveType MoveType { get; init; }

	public required decimal? X { get; init; }

	public required decimal? Y { get; init; }

	public required decimal? Z { get; init; }

	public required decimal? I { get; init; }

	public required decimal? J { get; init; }

	public required decimal? Rate { get; init; }

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		var x = match.Groups["xMove"].Success ? decimal.Parse(match.Groups["xMove"].Value) : default(decimal?);
		var y = match.Groups["yMove"].Success ? decimal.Parse(match.Groups["yMove"].Value) : default(decimal?);
		var z = match.Groups["zMove"].Success ? decimal.Parse(match.Groups["zMove"].Value) : default(decimal?);
		var i = match.Groups["iMove"].Success ? decimal.Parse(match.Groups["iMove"].Value) : default(decimal?);
		var j = match.Groups["jMove"].Success ? decimal.Parse(match.Groups["jMove"].Value) : default(decimal?);
		var r = match.Groups["rate"].Success ? decimal.Parse(match.Groups["rate"].Value) : default(decimal?);

		var moveType = match.Groups["type"].Value switch {
			"0" => MoveType.Rapid,
			"1" => MoveType.Linear,
			"2" => MoveType.ArcCW,
			"3" => MoveType.ArcCCW,
			_   => throw new NotSupportedException($"The move type {match.Groups["type"].Value} is not supported."),
		};

		if (moveType == MoveType.ArcCW || moveType == MoveType.ArcCCW) {
			if (z != null) {
				throw new InvalidOperationException("Z coordinate cannot be specified for an arc movement.");
			}

			if (i == null || j == null) {
				throw new InvalidOperationException("I and J parameters must be specified for arc movement.");
			}
		}

		command = new MoveCommand() {
			RawCommand = line,
			MoveType = moveType,
			X = x,
			Y = y,
			Z = z,
			I = i,
			J = j,
			Rate = r,
		};

		return true;
	}

	[GeneratedRegex(@"^G(?<type>0|1|2|3)((\s+X\s*(?<xMove>[\d\.\-]+))|(\s+Y\s*(?<yMove>[\d\.\-]+))|(\s+Z\s*(?<zMove>[\d\.\-]+))|(\s+I\s*(?<iMove>[\d\.\-]+))|(\s+J\s*(?<jMove>[\d\.\-]+))|(\s+F\s*(?<rate>[\d\.\-]+)))+$", RegexOptions.ExplicitCapture, "en-US")]
	private static partial Regex ParseExpression();
}

public enum MoveType
{
	Rapid,
	Linear,
	ArcCW,
	ArcCCW,
}

// ^G(?<type>0|1)(\s+((X\s+?<xMove>[\d\.\-])+)|(?<yMove>Y\s+[\d\.\-]+)))+

// (X\s?(?<xMove>[\d\.\-]+))
// (Y\s?(?<yMove>[\d\.\-]+))
// (Z\s?(?<zMove>[\d\.\-]+))

// ((\s+X\s?(?<xMove>[\d\.\-]+))|(\s+Y\s?(?<yMove>[\d\.\-]+))|(\s+Z\s?(?<zMove>[\d\.\-]+)))+

// works, but you have groups within groups
// ^G(?<type>0|1)\s+(?<axisGroup>(?<axis>X|Y|Z)\s+(?<dist>[\d\.\-]+)\s?)+$

// 
