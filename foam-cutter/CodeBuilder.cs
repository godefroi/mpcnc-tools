using FoamCutter.Machine;
using FoamCutter.Paths;

namespace FoamCutter;

public static class CodeBuilder
{
	public static void BuildCode(IEnumerable<MachinePath> paths, Config config, TextWriter output)
	{
		var state = new State(config);

		// give ourselves a little extra buffer (i.e. +translation) as a defense against rounding error?

		// step ZERO is to configure the machine
		output.WriteLine("G90"); // configure for absolute coordinates
		state.MovementMode = CoordinateMode.Absolute;

		// foreach (var path in paths.Where(p => p.SegmentType == SegmentType.Cut)) {
		// 	Console.WriteLine($"Path ({path.Points.Count()} points):");

		// 	foreach (var point in path.Points) {
		// 		Console.WriteLine($"\t[{point.X,8},{point.Y,8}] -> [{point.X + config.Translation.X,8},{point.Y + config.Translation.Y,8}]");
		// 	}
		// }

		// step ONE is to move the Z axis to the travel coordinate
		EmitMove(state.X, state.Y, config.TravelDepth, state, config, output);

		// // step TWO is to adjust for our translation (all moves are relative, so we only do this once)
		state.X = -config.Translation.X;
		state.Y = -config.Translation.Y;
		output.WriteLine($"                             ; machine state coordinates reset to [{state.X},{state.Y}]");

		GenerateMoves(paths.Where(p => p.SegmentType == SegmentType.Score), state, config.ScoringDepth, config, output);
		GenerateMoves(paths.Where(p => p.SegmentType == SegmentType.Cut),   state, config.CuttingDepth, config, output);

		// step PENULTIMATE is to move the Z axis to the travel coordinate
		EmitMove(state.X, state.Y, config.TravelDepth, state, config, output, "cutting complete, retracting cutter");

		output.WriteLine($"; finishing up; I think we're at [{state.X},{state.Y}]");
		// step LAST is to move us back to where we started
		//Console.WriteLine($"we are now at [{state.X},{state.Y}]");
		EmitMove(-config.Translation.X, -config.Translation.Y, state.Z, state, config, output, "return to starting coordinates");
	}

	private static void EmitMove(decimal toX, decimal toY, decimal toZ, State state, Config config, TextWriter output, string? comment = null)
	{
		if (state.AtCoordinates(toX, toY, toZ)) {
			return;
		}

		if (toZ != state.Z) {
			if (toX != state.X || toY != state.Y) {
				throw new InvalidOperationException("Cannot combine X-Y and Z moves in a single command");
			}

			// Z moves are done using absolute coordinates
			SetMovementMode(CoordinateMode.Absolute, state, output);

			//Console.WriteLine($"Absolute move (for Z axis); we're at [{state.X},{state.Y}], moving Z axis to [{toZ}]");

			if (toZ > state.Z) {
				output.WriteLine($"G0 Z{toZ:F2} F{config.RetractSpeed}");
			} else {
				output.WriteLine($"G1 Z{toZ:F2} F{config.PlungeSpeed}");
			}
		} else {
			// XY moves are done using relative coordinates
			SetMovementMode(CoordinateMode.Relative, state, output);

			// ok, here's the maths
			//   where we think we are (untranslated coordinates) is in state
			//   where our path is sending us to (untranslated coordinates) are toX and toY
			//   how much we want to move is simply the difference, because we're doing rel moves

			var xMove = toX - state.X;
			var yMove = toY - state.Y;

			//Console.WriteLine($"Relative move; we're at [{state.X},{state.Y}], moving to [{toX},{toY}] -> X{xMove:F2} Y{yMove:F2}");
			//Console.WriteLine($"\trel move would be X{state.X - toX} Y{state.Y - toY}");

			if (state.CuttingOrScoring) {
				output.WriteLine($"G1 X{xMove,-8} Y{yMove,-8} F{config.CuttingSpeed,-4} ; {comment}");
			} else {
				output.WriteLine($"G0 X{xMove,-8} Y{yMove,-8} F{config.TravelSpeed,-4} ; {comment}");
			}
		}

		state.X = toX;
		state.Y = toY;
		state.Z = toZ;
	}

	private static void SetMovementMode(CoordinateMode coordinateMode, State state, TextWriter output)
	{
		if (state.MovementMode == coordinateMode) {
			return;
		}

		output.WriteLine(coordinateMode switch {
			CoordinateMode.Absolute => "G90",
			CoordinateMode.Relative => "G91",
			_ => throw new InvalidOperationException("Invalid coordinate mode specified."),
		});

		state.MovementMode = coordinateMode;
	}

	private static void GenerateMoves(IEnumerable<MachinePath> paths, State state, decimal cutDepth, Config config, TextWriter output)
	{
		var pendingPaths = paths.ToList();
		var currentPos   = -config.Translation;
		var pnum         = 0;
		var pcnt         = pendingPaths.Count;

		while (pendingPaths.Count > 0) {
			// find the next path
			var (path, inverted, travel) = FindClosestPath(currentPos, pendingPaths);

			// remove the path from the list of pending paths
			pendingPaths.Remove(path);

			// invert the path if necessary
			if (inverted) {
				path.Reverse();
			}

			// update our current location from the last point in the path
			currentPos = path.Last;

			// give our gcode a bit of context
			output.WriteLine($"                             ; beginning new path at [{path.First.X},{path.First.Y}] ({(inverted ? "inverted" : "non-inverted")}) requiring travel of {travel:0.00}");
			output.WriteLine($"                             ; this is path {++pnum}, {pendingPaths.Count} remain");
			output.WriteLine($"                             ; this path begins at [{path.First.X},{path.First.Y}] and ends at [{path.Last.X},{path.Last.Y}]");
			output.WriteLine($"M117 Path {pnum}/{pcnt}");

			// travel to the initial point in the path
			if (!state.AtCoordinates(path.First.X, path.First.Y)) {
				if (state.CuttingOrScoring) {
					// retract the cutter
					EmitMove(state.X, state.Y, config.TravelDepth, state, config, output);
				}

				// move to the initial point
				EmitMove(path.First.X, path.First.Y, config.TravelDepth, state, config, output, $"moving to start of path at (abs) [{path.First.X},{path.First.Y}]");
			}

			// plunge to the correct depth
			EmitMove(state.X, state.Y, cutDepth, state, config, output);

			// move to each subsequent point
			foreach (var point in path.Points.Skip(1)) {
				EmitMove(point.X, point.Y, cutDepth, state, config, output);
			}
		}
	}

	private static (MachinePath Path, bool Inverted, double Travel) FindClosestPath(Point currentPosition, IEnumerable<MachinePath> paths)
	{
		var minPath  = new MachinePath(SegmentType.Ignore);
		var curDist  = double.MaxValue;
		var inverted = false;

		foreach (var path in paths) {
			var distFromFirst = Point.DistanceBetween(currentPosition, path.First);
			var distFromLast  = Point.DistanceBetween(currentPosition, path.Last);

			if (distFromFirst < curDist) {
				minPath  = path;
				curDist  = distFromFirst;
				inverted = false;
			}

			if (distFromLast < curDist) {
				minPath  = path;
				curDist  = distFromLast;
				inverted = true;
			}
		}

		return (minPath, inverted, curDist);
	}
}
