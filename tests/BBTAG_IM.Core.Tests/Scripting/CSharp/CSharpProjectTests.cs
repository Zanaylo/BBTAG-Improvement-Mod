using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.CSharp;

namespace BBTAG_IM.Core.Tests.Scripting.CSharp;

public class CSharpProjectTests
{
    [Theory]
    [InlineData(@"D:\mods\DEV\data\char\char_brg_scr\scr_brg.cs", ScriptLanguage.CSharp)]
    [InlineData(@"D:\mods\DEV\data\char\char_brg_scr\scr_brg.CS", ScriptLanguage.CSharp)]
    [InlineData(@"D:\mods\DEV\data\char\char_brg_scr\scr_brg.py", ScriptLanguage.Python)]
    public void LanguageOf_Path_IsTheLanguageOfItsExtension(string path, ScriptLanguage expected)
    {
        // Act
        ScriptLanguage language = ScriptLanguages.Of(path);

        // Assert
        Assert.Equal(expected, language);
    }
}
