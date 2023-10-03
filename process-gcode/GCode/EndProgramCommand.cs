namespace Mpcnc.GCodeProcessor.GCode;

public partial class EndProgramCommand : Command
{
	private readonly static EndProgramCommand _emptyCommand = new() { RawCommand = string.Empty };

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new EndProgramCommand() {
			RawCommand = line,
		};

		return true;
	}

	[GeneratedRegex(@"^M(2|30)(\s|$)", RegexOptions.ExplicitCapture, "en-US")]
	private static partial Regex ParseExpression();
}
