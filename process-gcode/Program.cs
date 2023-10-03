// See https://aka.ms/new-console-template for more information
using Mpcnc.GCodeProcessor;
using Mpcnc.GCodeProcessor.GCode;

using var tr = new StreamReader(@"C:\Users\mark.parker\OneDrive\RC Stuff\Flite Test bits\tiny_trainer1.ngc");

var sourceProgram = Parser.Parse(tr).ToList();
var minZPos       = sourceProgram.OfType<MoveCommand>().Min(mc => mc.Z ?? decimal.MaxValue);

if (minZPos < 0) {
	throw new InvalidOperationException("Cannot move to negative Z coordinates.");
}

var mpcnc = new MpcncMachine(new MpcncConfiguration(
	TranslationZ: 0 - minZPos,
	IgnoreZMoves: false
));

Console.Error.WriteLine($"Minimum Z position in source program: {minZPos}; applying translation of {0 - minZPos}");

foreach (var str in mpcnc.Translate(sourceProgram)) {
	Console.WriteLine(str);
}

// ok, so here's how the Z works...
// the DXF2GCODE assumes that the machine starts at Z=0, which is the tool touching the top
// surface of the stock. The first move it makes raises (+Z) the toolhead to the "retraction coordinate"
// as configured in DXF2GCODE -> Options -> Configuration -> Machine config -> Retraction coordinate