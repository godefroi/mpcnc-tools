namespace Mpcnc.GCodeProcessor.GCode;

public partial class CoolantOffCommand : Command
{
	private readonly static CoolantOffCommand _emptyCommand = new() { RawCommand = string.Empty };

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new CoolantOffCommand() {
			RawCommand = line,
		};

		return true;
	}

	[GeneratedRegex(@"^M9(\s|$)", RegexOptions.ExplicitCapture, "en-US")]
	private static partial Regex ParseExpression();
}
