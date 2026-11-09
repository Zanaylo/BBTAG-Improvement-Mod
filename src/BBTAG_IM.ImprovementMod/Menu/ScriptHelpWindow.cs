using System.Numerics;
using ImGuiNET;

namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class ScriptHelpWindow : IMenuWindow
{
    private const string Title = "Script help";

    private static readonly Vector2 FirstPosition = new(0.5f, 0.06f);

    private static readonly (string Heading, string[] Lines)[] Topics =
    [
        ("Editing live",
        [
            "Script and Effects script write the script the match is running to mods\\DEV\\data\\char\\char_<code>_scr\\ and open it in the program picked in Open scripts with.",
            "Save the file and it is built and put into the running match at once. It is also saved as scr_<code>.bin in the DEV folder, so the next match loads your version.",
            "When the same character is on both teams, both use the same script, so an edit changes both.",
        ]),
        ("When a line has a mistake",
        [
            "A file that does not build changes nothing in the game. The match keeps the last working script.",
            "Every mistake gets a note right above its line, starting with # !!! in Python or // !!! in C#, and the first one shows in this tab.",
            "The notes belong to the mod. They are removed the next time you save.",
        ]),
        ("When an edit makes the game stop",
        [
            "When the game shows an error box because of a script you changed, the mod catches it before the game closes.",
            "The last working version goes back into the match, the line that ran gets a note saying The game stopped here and why, and the moves that were running the broken code start again on the working version.",
            "In a training match, when there is nothing to roll back to, the match pauses until you fix the file and save it.",
            "Every error box the game shows is written to the log.",
        ]),
        ("When the game closes anyway",
        [
            "A crash without an error box (the game closes or freezes) cannot be caught. Your broken version is still in the DEV folder and loads again with the next match.",
            "Fix the file and save it, or delete scr_<code>.bin in the DEV folder, or untick DEV in the Mod Loader to start with the original script.",
            "Tick Write log files in the Mod Loader's Config tab to keep logs\\ImprovementMod.log for a report.",
        ]),
        ("What applies right away",
        [
            "A number, a name, a sprite or a duration shows at once, even in the middle of a move.",
            "Lines added or removed show the next time a move starts. A move already running finishes the old way.",
            "A new state or subroutine, and code that only runs when the match starts (like Move_Register in MatchInit), needs Back to character select. Keep DEV ticked in the Mod Loader so the match starts with your files.",
            "Sprites, sounds, palettes and other files in the DEV folder are read when the match loads: use Back to character select after changing them.",
        ]),
        ("Names and backups",
        [
            "Command names picks the command list. Write scripts in picks C# or Python, and Python layout picks Mango or Readable. Sizes always come from the game, so every choice builds the same script.",
            "Changing the command list or the Python layout writes the saved scripts of the DEV folder again. The old files are copied to DEV\\.backup\\<date and time>\\ first, and comments are not kept.",
            "Both are locked while a script is being edited. Go back to character select to change them.",
        ]),
        ("Editor help",
        [
            "C# scripts open in the bbscript\\Scripts solution of Visual Studio, which marks mistakes while you type and shows what each command does when you point at it.",
            "Python scripts get __builtins__.pyi next to them, for the same descriptions in editors that read it.",
            "The descriptions come from command_docs.json in the bbscript folder.",
        ]),
    ];

    private bool _isOpen;

    public void Toggle() => _isOpen = !_isOpen;

    public void Draw(Vector2 displaySize)
    {
        if (!_isOpen)
            return;

        if (FittedWindow.Begin(Title, ref _isOpen, displaySize * FirstPosition))
            DrawContents();

        ImGui.End();
    }

    private static void DrawContents()
    {
        foreach (var (heading, lines) in Topics)
        {
            ImGui.SeparatorText(heading);

            foreach (var line in lines)
                MenuText.Wrapped(line);
        }
    }
}
