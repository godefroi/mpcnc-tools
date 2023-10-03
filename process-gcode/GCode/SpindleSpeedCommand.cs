namespace Mpcnc.GCodeProcessor.GCode;

public partial class SpindleSpeedCommand : Command
{
	private readonly static SpindleSpeedCommand _emptyCommand = new() { RawCommand = string.Empty, Speed = 0 };

	public required int Speed { get; init; }

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new SpindleSpeedCommand() {
			RawCommand = line,
			Speed = int.Parse(match.Groups["speed"].Value),
		};

		return true;
	}

	[GeneratedRegex(@"^S(?<speed>\d+)", RegexOptions.ExplicitCapture, "en-US")]
	private static partial Regex ParseExpression();
}
