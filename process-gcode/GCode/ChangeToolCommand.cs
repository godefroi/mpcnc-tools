namespace Mpcnc.GCodeProcessor.GCode;

public partial class ChangeToolCommand : Command
{
	private readonly static ChangeToolCommand _emptyCommand = new() { RawCommand = string.Empty, ToolNumber = -1, ExecuteChange = false };

	public required int ToolNumber { get; init; }

	public required bool ExecuteChange { get; init; }

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new ChangeToolCommand() {
			RawCommand = line,
			ToolNumber = int.Parse(match.Groups["tool"].Value),
			ExecuteChange = match.Groups["execute"].Success
		};

		return true;
	}

	[GeneratedRegex(@"^T(?<tool>\d+)((?<execute>\s+M6)|)", RegexOptions.ExplicitCapture, "en-US")]
	private static partial Regex ParseExpression();
}
