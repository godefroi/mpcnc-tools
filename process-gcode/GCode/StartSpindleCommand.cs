namespace Mpcnc.GCodeProcessor.GCode;

public partial class StartSpindleCommand : Command
{
	private readonly static StartSpindleCommand _emptyCommand = new() { RawCommand = string.Empty };

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		// from DXF2GCODE also comes with an M8 (flood coolant on) for the ride...

		command = new StartSpindleCommand() {
			RawCommand = line,
		};

		return true;
	}

	[GeneratedRegex(@"^M3(\s+|$)", RegexOptions.ExplicitCapture, "en-US")]
	private static partial Regex ParseExpression();
}
