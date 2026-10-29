using BBTAG_IM.BBScript;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Sprites;

const int Success = 0;
const int Failure = 1;
const int Usage = 2;

var commandLine = CommandLine.Parse(args);

if (commandLine is null || commandLine.Inputs.Count == 0)
{
    Console.Error.WriteLine(ScriptTool.Usage);
    return Usage;
}

try
{
    var tool = new ScriptTool(ScriptDatabases.Open(commandLine.Database, commandLine.Style), commandLine);

    return commandLine.Verb switch
    {
        "decompile" => tool.Decompile() ? Success : Failure,
        "compile" => tool.Compile() ? Success : Failure,
        "check" => tool.Check() ? Success : Failure,
        "stubs" => tool.Stubs() ? Success : Failure,
        "convert" => tool.Convert() ? Success : Failure,
        "project" => tool.Project() ? Success : Failure,
        "import-docs" => tool.ImportDocs() ? Success : Failure,
        "index" => tool.Index() ? Success : Failure,
        "where" => tool.Where() ? Success : Failure,
        _ => WrongVerb(commandLine.Verb),
    };
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidScriptException or InvalidScriptDatabaseException or InvalidSpriteDataException or ScriptCompileException or InvalidDataException or System.Xml.XmlException)
{
    Console.Error.WriteLine(exception.Message);
    return Failure;
}

static int WrongVerb(string verb)
{
    Console.Error.WriteLine($"\"{verb}\" is not a command.");
    Console.Error.WriteLine(ScriptTool.Usage);
    return Usage;
}
