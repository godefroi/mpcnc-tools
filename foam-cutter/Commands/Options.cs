using System.CommandLine;

namespace FoamCutter.Commands;

internal static class Options
{
	//public static readonly Option<FileInfo> InputFile = new("--input", "Input filename");
	public static readonly Argument<FileInfo> InputFile = new("inputFilename", "Input filename");

	public static readonly Option<List<string>> CutColors = new("--cut-color", "Cut color");

	public static readonly Option<decimal?> TranslationX = new("--translation-x", "X-axis translation override");

	public static readonly Option<decimal?> TranslationY = new("--translation-y", "Y-axis translation override");
}
