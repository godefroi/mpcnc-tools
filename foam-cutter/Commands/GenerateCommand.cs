using System.CommandLine;
using System.Drawing;
using FoamCutter.Machine;
using FoamCutter.Paths;
using IxMilia.Dxf;
using Svg;

namespace FoamCutter.Commands;

internal static class GenerateCommand
{
	public static Command GetCommand()
	{
		var command = new Command("generate", "generate GCODE for a given input file") {
			Options.InputFile,
			Options.TranslationX,
			Options.TranslationY,
			Options.CutColors,
		};

		command.SetHandler(Execute, Options.InputFile, Options.TranslationX, Options.TranslationY, Options.CutColors);

		return command;
	}

	private async static Task Execute(FileInfo inputFile, float? translationX, float? translationY, List<string> cutColors)
	{
		if (!inputFile.Exists) {
			throw new FileNotFoundException("The specified input file was not found.", inputFile.FullName);
		}

		//var fn  = @"C:\Users\markparker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1_inkscapeconv_translated.svg";
		//var fn = @"C:\Users\MarkParker\Downloads\minnie_ear.dxf";

		var config = new Config() {
			TravelSpeed  = 6000, // feed rate (in mm/min) used for travel moves (which are G1)
			CuttingSpeed = 900,  // feed rate (in mm/min) used for cut/score moves (which are G0)
			PlungeSpeed  = 1500, // feed rate (in mm/min) used for plunge moves (which are G0)
			RetractSpeed = 1500, // feed rate (in mm/min) used for retract moves (which are G1)
			Translation  = new PointF(0, 0),
			CuttingDepth = 0,  // cutting happens at "full" depth, meaning the machine should be homed such that Z=0 engages the cutter fully through the workpiece
			ScoringDepth = 19, // THIS DISABLES SCORING (because scoring will happen at nearly the travel depth, where no cutting will occur)
			TravelDepth  = 20, // this is the safe height, where rapid moves can occur without dragging the cutter through the workpiece
		};

		foreach (var color in cutColors) {
			config.AddCutColor(new RgbColor(color));
		}

		//config.AddCutColor(new RgbColor(Color.Black));
		//config.AddCutColor(new RgbColor(Color.Aqua));

		var paths = ReadPaths(inputFile.FullName, config);
		var minX  = float.MaxValue;
		var minY  = float.MaxValue;
		var maxX  = float.MinValue;
		var maxY  = float.MinValue;

		Console.WriteLine($"{paths.Count} paths generated totalling {paths.Sum(p => p.Points.Count())} points.");

		foreach (var point in paths.SelectMany(p => p.Points)) {
			minX = Math.Min(minX, point.X);
			minY = Math.Min(minY, point.Y);
			maxX = Math.Max(maxX, point.X);
			maxY = Math.Max(maxY, point.Y);
		}

		config.Translation = new PointF(-minX, -minY);

		if (translationX.HasValue) {
			config.Translation = new PointF(translationX.Value, config.Translation.Y);
		}

		if (translationY.HasValue) {
			config.Translation = new PointF(config.Translation.X, translationY.Value);
		}

		Console.WriteLine($"Minimum X,Y coordinate: {minX},{minY}");
		Console.WriteLine($"Maximum X,Y coordinate: {maxX},{maxY}");
		Console.WriteLine($"Translation: {config.Translation}");

		using var of = File.CreateText(Path.ChangeExtension(inputFile.Name, "gcode"));

		CodeBuilder.BuildCode(paths, config, of);

		await Task.CompletedTask;
	}

	private static List<MachinePath> ReadPaths(string filename, Config config) => Path.GetExtension(filename) switch {
			".dxf" => PathBuilder.BuildPaths(DxfFile.Load(filename), config),
			".svg" => PathBuilder.BuildPaths(SvgDocument.Open(filename), config),
			_ => throw new NotSupportedException($"The specified extension is not supported: {Path.GetExtension(filename)}"),
		};
}
