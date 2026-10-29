using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Tests.Game;

public class RecoveryPointCodeTests
{
    private const uint Block = 0x50000000;
    private const uint SceneSite = 0x0043A058;
    private const uint ScriptSite = 0x00768734;
    private const uint ScriptTarget = 0x00772F00;
    private const uint Managed = 0x60001000;

    private static readonly byte[] SceneStep =
        [0x8B, 0x46, 0x04, 0x8D, 0x4E, 0x04, 0xC7, 0x45, 0xFC, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x10, 0x85, 0xC0, 0x7F, 0x05];

    private static readonly byte[] ObjectScript =
        [0x8B, 0xCB, 0xE8, 0xC5, 0xA7, 0x00, 0x00, 0xF7, 0x83, 0x5C, 0x01, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00];

    [Fact]
    public void Locate_SceneStep_ReturnsTheStoreBeforeTheCall()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(0x0043A052, SceneStep)]);

        // Act
        uint site = RecoveryPointCode.Locate(image, RecoveryPoints.SceneStep);

        // Assert
        Assert.Equal(SceneSite, site);
    }

    [Fact]
    public void Locate_ObjectScript_ReturnsTheMoveBeforeTheCall()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(ScriptSite, ObjectScript)]);

        // Act
        uint site = RecoveryPointCode.Locate(image, RecoveryPoints.ObjectScript);

        // Assert
        Assert.Equal(ScriptSite, site);
    }

    [Fact]
    public void CallOf_ObjectScript_CallsTheOriginalTarget()
    {
        // Arrange
        CodeImage image = new([new CodeBlock(ScriptSite, ObjectScript)]);

        // Act
        uint target = RecoveryPointCode.CallTargetOf(image, RecoveryPoints.ObjectScript, ScriptSite);

        // Assert
        Assert.Equal(ScriptTarget, target);
    }

    [Fact]
    public void Code_SceneStep_ResumesWithTheSavedStateAndAFinishedStep()
    {
        // Arrange
        byte[] restore =
        [
            0x8B, 0x25, .. Address(Block + RecoveryPointCode.SavedEspOffset),
            0x8B, 0x2D, .. Address(Block + RecoveryPointCode.SavedEbpOffset),
            0x8B, 0x1D, .. Address(Block + RecoveryPointCode.SavedEbxOffset),
            0x8B, 0x35, .. Address(Block + RecoveryPointCode.SavedEsiOffset),
            0x8B, 0x3D, .. Address(Block + RecoveryPointCode.SavedEdiOffset),
            0x8B, 0x15, .. Address(Block + RecoveryPointCode.HandlersOffset),
            0x64, 0x89, 0x15, 0x00, 0x00, 0x00, 0x00,
            0xDB, 0xE3,
            0xD9, 0x2D, .. Address(Block + RecoveryPointCode.FpuControlOffset),
            0x0F, 0xAE, 0x15, .. Address(Block + RecoveryPointCode.SseControlOffset),
            0xFC,
            0xC7, 0x05, .. Address(Block + RecoveryPointCode.ArmedOffset), 0x00, 0x00, 0x00, 0x00,
            0xFF, 0x05, .. Address(Block + RecoveryPointCode.RecoveriesOffset),
            0xB8, 0x01, 0x00, 0x00, 0x00,
        ];
        uint jumpEnd = Block + RecoveryPointCode.ResumeOffset + (uint)restore.Length + 5;

        // Act
        byte[] code = RecoveryPointCode.Code(RecoveryPoints.SceneStep, SceneSite, Block, 0);

        // Assert
        Assert.Equal([.. restore, 0xE9, .. Address(SceneSite + 9 - jumpEnd)], code[..(restore.Length + 5)]);
    }

    [Fact]
    public void Code_SceneStep_ArmsSavesCallsThroughEaxAndDisarms()
    {
        // Arrange
        uint arm = Block + RecoveryPointCode.ArmOffset;
        byte[] armed =
        [
            0xC7, 0x45, 0xFC, 0x00, 0x00, 0x00, 0x00,
            .. Saves(),
            0xC7, 0x05, .. Address(Block + RecoveryPointCode.ArmedOffset), 0x01, 0x00, 0x00, 0x00,
            0xFF, 0x10,
            0xC7, 0x05, .. Address(Block + RecoveryPointCode.ArmedOffset), 0x00, 0x00, 0x00, 0x00,
        ];
        uint jumpEnd = arm + (uint)armed.Length + 5;

        // Act
        byte[] code = RecoveryPointCode.Code(RecoveryPoints.SceneStep, SceneSite, Block, 0);

        // Assert
        int start = RecoveryPointCode.ArmOffset - RecoveryPointCode.ResumeOffset;
        Assert.Equal([.. armed, 0xE9, .. Address(SceneSite + 9 - jumpEnd)], code[start..]);
    }

    [Fact]
    public void Code_ObjectScript_CallsTheTargetDirectlyAndResumesWithZero()
    {
        // Arrange
        uint callAt = Block + RecoveryPointCode.ArmOffset + 2 + (uint)Saves().Length + 10;
        byte[] call = [0xE8, .. Address(ScriptTarget - (callAt + 5))];

        // Act
        byte[] code = RecoveryPointCode.Code(RecoveryPoints.ObjectScript, ScriptSite, Block, ScriptTarget);

        // Assert
        int start = RecoveryPointCode.ArmOffset - RecoveryPointCode.ResumeOffset;
        Assert.Equal(new byte[] { 0x8B, 0xCB }, code[start..(start + 2)]);
        Assert.True(code.AsSpan().IndexOf(call) > 0);
        Assert.True(code.AsSpan().IndexOf(new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xE9 }) > 0);
    }

    [Fact]
    public void Code_EitherPoint_FitsBeforeTheThunk()
    {
        // Act
        int scene = RecoveryPointCode.ResumeOffset + RecoveryPointCode.Code(RecoveryPoints.SceneStep, SceneSite, Block, 0).Length;
        int script = RecoveryPointCode.ResumeOffset + RecoveryPointCode.Code(RecoveryPoints.ObjectScript, ScriptSite, Block, ScriptTarget).Length;

        // Assert
        Assert.True(scene <= RecoveryPointCode.ThunkOffset && script <= RecoveryPointCode.ThunkOffset);
    }

    [Fact]
    public void Jump_ObjectScript_JumpsToTheArmCodeAndPadsTheReplacedBytes()
    {
        // Act
        byte[] jump = RecoveryPointCode.Jump(RecoveryPoints.ObjectScript, ScriptSite, Block);

        // Assert
        Assert.Equal([0xE9, .. Address(Block + RecoveryPointCode.ArmOffset - (ScriptSite + 5)), 0x90, 0x90], jump);
    }

    [Fact]
    public void MessageBoxThunk_ManagedHandler_ReturnsSmallResultsAndJumpsToAddresses()
    {
        // Act
        byte[] thunk = RecoveryPointCode.MessageBoxThunk(Managed);

        // Assert
        Assert.Equal(
        [
            0xFF, 0x74, 0x24, 0x10,
            0xFF, 0x74, 0x24, 0x10,
            0xFF, 0x74, 0x24, 0x10,
            0xFF, 0x74, 0x24, 0x10,
            0xB8, .. Address(Managed),
            0xFF, 0xD0,
            0x3D, 0x00, 0x00, 0x01, 0x00,
            0x73, 0x03,
            0xC2, 0x10, 0x00,
            0xFF, 0xE0,
        ],
        thunk);
    }

    private static byte[] Saves() =>
    [
        0x89, 0x25, .. Address(Block + RecoveryPointCode.SavedEspOffset),
        0x89, 0x2D, .. Address(Block + RecoveryPointCode.SavedEbpOffset),
        0x89, 0x1D, .. Address(Block + RecoveryPointCode.SavedEbxOffset),
        0x89, 0x35, .. Address(Block + RecoveryPointCode.SavedEsiOffset),
        0x89, 0x3D, .. Address(Block + RecoveryPointCode.SavedEdiOffset),
        0x64, 0x8B, 0x15, 0x24, 0x00, 0x00, 0x00,
        0x89, 0x15, .. Address(Block + RecoveryPointCode.ThreadOffset),
        0x64, 0x8B, 0x15, 0x00, 0x00, 0x00, 0x00,
        0x89, 0x15, .. Address(Block + RecoveryPointCode.HandlersOffset),
        0xD9, 0x3D, .. Address(Block + RecoveryPointCode.FpuControlOffset),
        0x0F, 0xAE, 0x1D, .. Address(Block + RecoveryPointCode.SseControlOffset),
    ];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
