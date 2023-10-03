namespace Mpcnc.GCodeProcessor.GCode;

public partial class SetCoordinateSystemCommand : Command
{
	private readonly static SetCoordinateSystemCommand _emptyCommand = new() { RawCommand = string.Empty, CoordinateSystem = CoordinateSystem.Absolute };

	public required CoordinateSystem CoordinateSystem { get; init; }

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new SetCoordinateSystemCommand() {
			RawCommand = line,
			CoordinateSystem = match.Groups["system"].Value switch {
				"0" => CoordinateSystem.Absolute,
				"1" => CoordinateSystem.Relative,
				_   => throw new NotSupportedException($"Coordinate system {match.Groups["system"].Value} not supported."),
			}
		};

		return true;
	}

	[GeneratedRegex(@"^G9(?<system>0|1)( |$)", RegexOptions.None, "en-US")]
	private static partial Regex ParseExpression();
}

public enum CoordinateSystem
{
	Absolute,
	Relative,
}