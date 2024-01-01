using System.CommandLine;
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
			Options.OutputFile,
			Options.TranslationX,
			Options.TranslationY,
			Options.CutColors,
			Options.ScoreColors,
			Options.IncludeGroups,
		};

		command.SetHandler(Execute, Options.InputFile, Options.OutputFile, Options.TranslationX, Options.TranslationY, Options.CutColors, Options.ScoreColors, Options.IncludeGroups);

		return command;
	}

	private async static Task Execute(FileInfo inputFile, FileInfo? outputFile, decimal? translationX, decimal? translationY, List<string> cutColors, List<string> scoreColors, List<string> includeGroups)
	{
		if (!inputFile.Exists) {
			throw new FileNotFoundException("The specified input file was not found.", inputFile.FullName);
		}

		outputFile ??= new FileInfo(Path.ChangeExtension(inputFile.Name, "gcode"));

		//var fn  = @"C:\Users\markparker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1_inkscapeconv_translated.svg";
		//var fn = @"C:\Users\MarkParker\Downloads\minnie_ear.dxf";

		var config = new Config() {
			TravelSpeed  = 6000, // feed rate (in mm/min) used for travel moves (which are G1)
			CuttingSpeed = 900,  // feed rate (in mm/min) used for cut/score moves (which are G0)
			PlungeSpeed  = 1500, // feed rate (in mm/min) used for plunge moves (which are G0)
			RetractSpeed = 1500, // feed rate (in mm/min) used for retract moves (which are G1)
			Translation  = new Point(0, 0),
			CuttingDepth = 0,  // cutting happens at "full" depth, meaning the machine should be homed such that Z=0 engages the cutter fully through the workpiece
			ScoringDepth = 5,  // score at 5mm... which will cut the top paper (at home, at least) but not go deeper. 4mm could be better?
			TravelDepth  = 20, // this is the safe height, where rapid moves can occur without dragging the cutter through the workpiece
		};

		foreach (var color in cutColors) {
			config.AddCutColor(new RgbColor(color));
		}

		foreach (var color in scoreColors) {
			config.AddScoreColor(new RgbColor(color));
		}

		foreach (var group in includeGroups) {
			config.AddGroupName(group);
		}

		var paths = ReadPaths(inputFile.FullName, config);
		var minX  = decimal.MaxValue;
		var minY  = decimal.MaxValue;
		var maxX  = decimal.MinValue;
		var maxY  = decimal.MinValue;

		Console.WriteLine($"{paths.Count} paths generated totalling {paths.Sum(p => p.Points.Count())} points.");

		foreach (var point in paths.SelectMany(p => p.Points)) {
			minX = Math.Min(minX, point.X);
			minY = Math.Min(minY, point.Y);
			maxX = Math.Max(maxX, point.X);
			maxY = Math.Max(maxY, point.Y);
		}

		config.Translation = new Point(-minX, -minY);

		if (translationX.HasValue) {
			config.Translation = new Point(translationX.Value, config.Translation.Y);
		}

		if (translationY.HasValue) {
			config.Translation = new Point(config.Translation.X, translationY.Value);
		}

		Console.WriteLine($"Minimum X,Y coordinate: {minX},{minY}");
		Console.WriteLine($"Maximum X,Y coordinate: {maxX},{maxY}");
		Console.WriteLine($"Translation: {config.Translation}");

		using var of = File.CreateText(outputFile.FullName);

		CodeBuilder.BuildCode(paths, config, of);

		await Task.CompletedTask;
	}

	private static List<MachinePath> ReadPaths(string filename, Config config) => Path.GetExtension(filename) switch {
			".dxf" => PathBuilder.BuildPaths(DxfFile.Load(filename), config),
			".svg" => PathBuilder.BuildPaths(SvgDocument.Open(filename), config),
			_ => throw new NotSupportedException($"The specified extension is not supported: {Path.GetExtension(filename)}"),
		};
}
