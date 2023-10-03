namespace Mpcnc.GCodeProcessor.GCode;

public partial class Dxf2GCodePreambleCommand : Command
{
	private readonly static Dxf2GCodePreambleCommand _emptyCommand = new() { RawCommand = string.Empty };

	public static bool TryParse(string line, out Command command)
	{
		var match = ParseExpression().Match(line);

		if (!match.Success) {
			command = _emptyCommand;
			return false;
		}

		command = new Dxf2GCodePreambleCommand() {
			RawCommand = line,
		};

		return true;
	}

	[GeneratedRegex(@"^G64 \(Default cutting\) G17 \(XY plane\) G40 \(Cancel radius comp\.\) G49 \(Cancel length comp\.\)", RegexOptions.None, "en-US")]
	private static partial Regex ParseExpression();
}
