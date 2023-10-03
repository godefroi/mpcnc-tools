namespace Mpcnc.GCodeProcessor.GCode;

public abstract class Command
{
	public required string RawCommand { get; init; }
}
