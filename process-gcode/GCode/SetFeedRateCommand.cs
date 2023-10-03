namespace Mpcnc.GCodeProcessor.GCode;

public partial class SetFeedRateCommand : Command
{
	private readonly static SetFeedRateCommand _emptyCommand = new() { RawCommand = string.Empty, FeedRate = 0 };

	public required decimal FeedRate { get; init; }

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new SetFeedRateCommand() {
			RawCommand = line,
			FeedRate = decimal.Parse(match.Groups["rate"].Value),
		};

		return true;
	}

	[GeneratedRegex(@"^F(?<rate>[\d\.]+)", RegexOptions.None, "en-US")]
	private static partial Regex ParseExpression();
}
