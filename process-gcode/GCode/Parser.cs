namespace Mpcnc.GCodeProcessor.GCode;

public static class Parser
{
	delegate bool TryParseDelegate(string inputLine, out Command command);

	private static TryParseDelegate[] _parseDelegates = new TryParseDelegate[] {
		Comment.TryParse,
		SetUnitsCommand.TryParse,
		SetCoordinateSystemCommand.TryParse,
		Dxf2GCodePreambleCommand.TryParse,
		MoveCommand.TryParse,
		ChangeToolCommand.TryParse,
		SpindleSpeedCommand.TryParse,
		StartSpindleCommand.TryParse,
		SetFeedRateCommand.TryParse,
		CoolantOffCommand.TryParse,
		EndProgramCommand.TryParse,
	};

	public static IEnumerable<Command> Parse(TextReader textReader)
	{
		static Command TryParsers(string line)
		{
			foreach (var parsers in _parseDelegates) {
				if (parsers(line, out var cmd)) {
					return cmd;
				}
			}

			throw new NotSupportedException($"The command line [{line}] could not be parsed.");
		}

		while (textReader.Peek() > -1) {
			var rawLine = textReader.ReadLine()?.Trim();

			if (string.IsNullOrWhiteSpace(rawLine)) {
				yield return new BlankLine() {
					RawCommand = string.Empty,
				};
				continue;
			}

			yield return TryParsers(rawLine);
		}
	}
}
