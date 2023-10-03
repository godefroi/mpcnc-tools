namespace Mpcnc.GCodeProcessor.GCode;

public partial class SetUnitsCommand : Command
{
	private readonly static SetUnitsCommand _emptyCommand = new() { RawCommand = string.Empty, Units = UnitSystem.Millimeters };

	public required UnitSystem Units { get; init; }

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new SetUnitsCommand() {
			RawCommand = line,
			Units = match.Groups["system"].Value switch {
				"0" => UnitSystem.Inches,
				"1" => UnitSystem.Millimeters,
				_   => throw new NotSupportedException($"Unit system {match.Groups["system"].Value} not supported."),
			}
		};

		return true;
	}

	[GeneratedRegex(@"^G2(?<system>0|1)( |$)", RegexOptions.None, "en-US")]
	private static partial Regex ParseExpression();
}

public enum UnitSystem
{
	Inches,
	Millimeters,
}