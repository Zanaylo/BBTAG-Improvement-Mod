using BBTAG_IM.Core.Hantei.Games;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Porting;

namespace BBTAG_IM.BBScript;

internal sealed partial class ScriptTool
{
    private const int PortInputs = 5;

    private const string PortUsage = """
        bbscript port <French-Bread game folder> <chrNNN> <game data> <slot code> <image prefix> --out <folder> [--foreign] [--roster <code> --name <name> [--series <row>]] [--db <bbscript folder>]

        port         builds a French-Bread character from its native files into the files of a roster slot;
                     --foreign is for a character that does not belong to the slot
        """;

    public bool Port()
    {
        if (commandLine.Inputs.Count != PortInputs || commandLine.Output is null)
        {
            Console.Error.WriteLine(PortUsage);
            return false;
        }

        var inputs = commandLine.Inputs;
        var spritePrefix = commandLine.Foreign ? PortJob.ForeignSpritePrefix : SpriteRemap.NativePrefix;
        var files = new PhysicalFileSystem();
        var game = HanteiGames.Open(inputs[0], files);
        var roster = commandLine.Roster is { } code ? new RosterEntry(code, commandLine.Name ?? code, commandLine.Series ?? RosterEntry.DefaultSeries) : null;
        var request = new PortRequest(game.Files, inputs[1], inputs[2], inputs[3], inputs[4], spritePrefix, commandLine.Foreign, roster);

        foreach (var file in new CharacterPorter(files, databases.For).Port(request))
        {
            var output = Path.Combine(commandLine.Output, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? commandLine.Output);
            File.WriteAllBytes(output, file.Bytes);
            Console.WriteLine($"{output}: {file.Bytes.Length} bytes");
        }

        return true;
    }
}
