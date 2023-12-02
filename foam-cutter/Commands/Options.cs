using System.CommandLine;

namespace FoamCutter.Commands;

internal static class Options
{
	public static readonly Option<FileInfo> InputFile = new("--input", "Input filename");
}