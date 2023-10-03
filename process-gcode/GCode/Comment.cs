namespace Mpcnc.GCodeProcessor.GCode;

public partial class Comment : Command
{
	private readonly static Comment _emptyCommand = new() { Text = string.Empty, RawCommand = string.Empty };

	public required string Text { get; init; }

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new Comment() {
			Text = match.Groups["text"].Value,
			RawCommand = line,
		};

		return true;
	}

	[GeneratedRegex(@"\((?<text>.*)\)$", RegexOptions.None, "en-US")]
	private static partial Regex ParseExpression();
}
