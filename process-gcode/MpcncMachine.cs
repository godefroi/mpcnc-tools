using System.Text;

using Mpcnc.GCodeProcessor.GCode;

namespace Mpcnc.GCodeProcessor;

/// <summary>
/// Configuration for an MPCNC machine
/// </summary>
/// <param name="TranslationZ">Translation to be applied to all Z positions</param>
/// <param name="IgnoreZMoves">Ignore (output as comments) all Z-axis movements; this is useful for testing before breaking cutters...</param>
public record class MpcncConfiguration(
	decimal TranslationZ,
	bool IgnoreZMoves
);

public class MpcncMachine
{
	private UnitSystem _unitSystem = UnitSystem.Millimeters; // default to mm as units; we'll translate into mm if we have to
	private decimal?   _feedRate   = null; // default to no specified feed rate; hopefully the default is something reasonable...
	private decimal? _overrideG0Rate = 100 * 60; // override the rapid move rate (in mm/min)
	private decimal? _overrideG1Rate = 10 * 60; // override the linear (cutting) move rate (in mm/min)
	private decimal? _xMax = null;
	private decimal? _yMax = null;

	public MpcncMachine(MpcncConfiguration config)
	{
		Configuration = config;
	}

	public MpcncConfiguration Configuration { get; init; }

	public IEnumerable<string> Translate(IEnumerable<Command> commands)
	{
		foreach (var command in commands) {
			switch (command) {
				case SetUnitsCommand suc:
					_unitSystem = suc.Units;
					yield return $"; Units set to ${suc.Units}";
					break;
				case SetCoordinateSystemCommand scsc:
					yield return scsc.CoordinateSystem switch {
						CoordinateSystem.Absolute => "G90",
						CoordinateSystem.Relative => "G91",
						_ => throw new NotSupportedException($"Coordinate system {scsc.CoordinateSystem} is not supported by MPCNC machine."),
					};
					break;
				case MoveCommand mc:
					yield return BuildMoveCommand(mc);
					break;
				case BlankLine:
					yield return string.Empty;
					break;
				case Comment c:
					yield return $"; {c.Text}";
					break;
				case SetFeedRateCommand sfrc:
					_feedRate = sfrc.FeedRate;
					break;
				case Dxf2GCodePreambleCommand:
				case ChangeToolCommand:
				case SpindleSpeedCommand:
				case StartSpindleCommand:
				case CoolantOffCommand:
					break;
				default:
					throw new NotImplementedException($"The command type {command.GetType()} is not yet implemented.");
			}
		}

		yield return $"; XMAX: {_xMax} YMAX: {_yMax}";
	}

	private string BuildMoveCommand(MoveCommand mc)
	{
		static decimal ConvertToMm(UnitSystem fromSystem, decimal value) => fromSystem switch {
			UnitSystem.Millimeters => value,
			UnitSystem.Inches => value * 25.4m,
			_ => throw new NotImplementedException($"Conversion from unit system ${fromSystem} is not implemented."),
		};

		var sb    = new StringBuilder();
		var onlyZ = false;

		if (Configuration.IgnoreZMoves && mc.X == null && mc.Y == null && mc.Z != null && mc.I == null && mc.J == null) {
			// this is a strictly Z-axis move and we'll output it as a comment
			onlyZ = true;
			sb.Append(';');
		}

		sb.Append('G');

		sb.Append(mc.MoveType switch {
			MoveType.Rapid => '0',
			MoveType.Linear => '1',
			MoveType.ArcCW => '2',
			MoveType.ArcCCW => '3',
			_ => throw new NotSupportedException($"Unsupported move type {mc.MoveType}"),
		});

		sb.Append(' ');
		if (mc.X != null) {
			if (_xMax == null || mc.X > _xMax) {
				_xMax = mc.X;
			}

			sb.Append('X');
			sb.Append(ConvertToMm(_unitSystem, mc.X.Value));
			sb.Append(' ');
		}

		if (mc.Y != null) {
			if (_yMax == null || mc.Y > _yMax) {
				_yMax = mc.Y;
			}

			sb.Append('Y');
			sb.Append(ConvertToMm(_unitSystem, mc.Y.Value));
			sb.Append(' ');
		}

		if (mc.Z != null) {
			sb.Append('Z');
			sb.Append(ConvertToMm(_unitSystem, mc.Z.Value + Configuration.TranslationZ));
			sb.Append(' ');
		}

		if (mc.I != null) {
			sb.Append('I');
			sb.Append(ConvertToMm(_unitSystem, mc.I.Value));
			sb.Append(' ');
		}

		if (mc.J != null) {
			sb.Append('J');
			sb.Append(ConvertToMm(_unitSystem, mc.J.Value));
			sb.Append(' ');
		}

		if (mc.MoveType == MoveType.Rapid && _overrideG0Rate != null) {
			sb.Append('F');
			sb.Append(_overrideG0Rate);
			sb.Append(' ');
		} else if ((mc.MoveType == MoveType.Linear || mc.MoveType == MoveType.ArcCW || mc.MoveType == MoveType.ArcCCW) && _overrideG1Rate != null) {
			sb.Append('F');
			sb.Append(_overrideG1Rate);
			sb.Append(' ');
		} else if (mc.Rate != null) {
			sb.Append('F');
			sb.Append(ConvertToMm(_unitSystem, mc.Rate.Value));
			sb.Append(' ');
		} else if (_feedRate != null) {
			sb.Append('F');
			sb.Append(ConvertToMm(_unitSystem, _feedRate.Value));
			sb.Append(' ');
		}

		if (mc.Z != null && Configuration.IgnoreZMoves && !onlyZ) {
			// put the Z component at the end as a comment
			sb.Append(" ; IGNORED Z COMPONENT: ");
			sb.Append('Z');
			sb.Append(ConvertToMm(_unitSystem, Configuration.TranslationZ));
			sb.Append(' ');
		}

		// remove the final character
		sb.Remove(sb.Length - 1, 1);

		return sb.ToString();
	}
}
