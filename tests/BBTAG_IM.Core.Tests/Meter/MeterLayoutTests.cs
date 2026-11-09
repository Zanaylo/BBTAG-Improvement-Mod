using BBTAG_IM.Core.Meter;
using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Tests.Meter;

public class MeterLayoutTests
{
    private const float ScreenWidth = 1920;
    private const float ScreenHeight = 1080;
    private const float LetterWidth = 10;

    private static readonly MeterLook Look = new(Scale: 1f, Opacity: 1f, Counts: true, Invulnerability: true, Partners: true, Totals: true, OffsetX: 0, OffsetY: 0);

    public static HudScene Draw(MeterView view, MeterLook? look = null)
    {
        var scene = new HudScene();
        MeterLayout.Draw(scene, view, look ?? Look, ScreenWidth, ScreenHeight, new FakeFont());
        return scene;
    }

    [Fact]
    public void Draw_PointLaneWithAMove_DrawsOneCellPerFrameInTheKindColors()
    {
        // Arrange
        MeterView view = View(player1Point: [.. Cells(FrameKind.Startup, 6), .. Cells(FrameKind.Active, 3)]);

        // Act
        HudScene scene = Draw(view);

        // Assert
        Assert.Equal(6, scene.Rects.Count(rect => rect.Color == MeterColors.Of(FrameKind.Startup)));
        Assert.Equal(3, scene.Rects.Count(rect => rect.Color == MeterColors.Of(FrameKind.Active)));
    }

    [Fact]
    public void Draw_MoreFramesThanFit_ShowsOnlyTheLatest()
    {
        // Arrange
        MeterView view = View(player1Point: [.. Cells(FrameKind.Startup, MeterLayout.VisibleFrames), .. Cells(FrameKind.Recovery, 5)]);

        // Act
        HudScene scene = Draw(view);

        // Assert
        Assert.Equal(MeterLayout.VisibleFrames - 5, scene.Rects.Count(rect => rect.Color == MeterColors.Of(FrameKind.Startup)));
    }

    [Fact]
    public void Draw_LastMove_WritesItsTimingInTheHeader()
    {
        // Arrange
        MeterView view = View(player1Move: new MoveTiming("NmlAtk5A", 7, 3, 15, 24));

        // Act
        HudScene scene = Draw(view);

        // Assert
        Assert.Contains(scene.Texts, text => text.Text == "5A");
        Assert.Contains(scene.Texts, text => text.Text == "STARTUP");
        Assert.Contains(scene.Texts, text => text.Text == "7");
        Assert.Contains(scene.Texts, text => text.Text == "24");
    }

    [Fact]
    public void Draw_PositiveAdvantage_WritesItWithASignInThePlusColor()
    {
        // Arrange
        MeterView view = View(player1Advantage: 2);

        // Act
        HudScene scene = Draw(view);

        // Assert
        Assert.Contains(scene.Texts, text => text.Text == "+2" && text.Color == MeterColors.Plus);
        Assert.Contains(scene.Texts, text => text.Text == "-2" && text.Color == MeterColors.Minus);
    }

    [Fact]
    public void Draw_PartnerAssist_WritesThePartnerTiming()
    {
        // Arrange
        MeterView view = View(player1PartnerMove: new MoveTiming("CmnActChangePartnerAssistAtk_A", 29, 25, 22, 75), player1Partner: Cells(FrameKind.Active, 2));

        // Act
        HudScene scene = Draw(view);

        // Assert
        Assert.Contains(scene.Texts, text => text.Text == "RACHEL");
        Assert.Contains(scene.Texts, text => text.Text == "ASSIST A");
        Assert.Contains(scene.Texts, text => text.Text == "29");
    }

    [Fact]
    public void Draw_PartnersTurnedOff_DrawsNoPartnerCells()
    {
        // Arrange
        MeterView view = View(player1Partner: Cells(FrameKind.Active, 4));

        // Act
        HudScene scene = Draw(view, Look with { Partners = false });

        // Assert
        Assert.DoesNotContain(scene.Rects, rect => rect.Color == MeterColors.Of(FrameKind.Active));
    }

    [Fact]
    public void Draw_RunOfFrames_WritesItsLengthWhenCountsAreOn()
    {
        // Arrange
        MeterView view = View(player1Point: Cells(FrameKind.Startup, 12));

        // Act
        HudScene withCounts = Draw(view);
        HudScene withoutCounts = Draw(view, Look with { Counts = false });

        // Assert
        Assert.Contains(withCounts.Texts, text => text.Text == "12");
        Assert.DoesNotContain(withoutCounts.Texts, text => text.Text == "12");
    }

    [Fact]
    public void Draw_InvulnerableFrames_DrawTheStripAboveThePointLane()
    {
        // Arrange
        MeterCell invulnerable = new(FrameKind.Startup, Invulnerability.Full, CellMarks.None);
        MeterView view = View(player1Point: [invulnerable, invulnerable]);

        // Act
        HudScene scene = Draw(view);

        // Assert
        Assert.Equal(2, scene.Rects.Count(rect => rect.Color == MeterColors.OfInvulnerability(Invulnerability.Full)));
    }

    [Fact]
    public void Draw_ComboOnPlayer2_WritesHitsAndDamage()
    {
        // Arrange
        MeterView view = View(player2Combo: new ComboReadout(3, 2400, 0, 0));

        // Act
        HudScene scene = Draw(view);

        // Assert
        Assert.Contains(scene.Texts, text => text.Text == "3 HITS 2400 DAMAGE");
    }

    [Fact]
    public void Draw_SingleHit_WritesHitInTheSingular()
    {
        // Arrange
        MeterView view = View(player2Combo: new ComboReadout(1, 1200, 0, 0));

        // Act
        HudScene scene = Draw(view);

        // Assert
        Assert.Contains(scene.Texts, text => text.Text == "1 HIT 1200 DAMAGE");
    }

    [Fact]
    public void Draw_HalfOpacity_HalvesEveryAlpha()
    {
        // Arrange
        MeterView view = View(player1Point: Cells(FrameKind.Active, 1));

        // Act
        HudScene scene = Draw(view, Look with { Opacity = 0.5f });

        // Assert
        Assert.Contains(scene.Rects, rect => rect.Color == MeterColors.Fade(MeterColors.Of(FrameKind.Active), 0.5f));
    }

    [Fact]
    public void Draw_PanelAtTheBottomCenter_StaysInsideTheScreen()
    {
        // Act
        HudScene scene = Draw(View());

        // Assert
        Assert.All(scene.Rects, rect => Assert.InRange(rect.X, 0, ScreenWidth));
        Assert.All(scene.Rects, rect => Assert.InRange(rect.Y + rect.Height, 0, ScreenHeight));
    }

    private static MeterView View(
        MeterCell[]? player1Point = null,
        MeterCell[]? player1Partner = null,
        MoveTiming? player1Move = null,
        MoveTiming? player1PartnerMove = null,
        int? player1Advantage = null,
        ComboReadout? player2Combo = null) =>
        new(
            new SideView("YUMI", "RACHEL", player1Point ?? [], player1Partner ?? [], player1Move, player1PartnerMove, player1Advantage, null, ComboReadout.None),
            new SideView("RAGNA", "JIN", [], [], null, null, -player1Advantage, null, player2Combo ?? ComboReadout.None),
            0);

    private static MeterCell[] Cells(FrameKind kind, int count) => [.. Enumerable.Repeat(new MeterCell(kind, Invulnerability.None, CellMarks.None), count)];

    private sealed class FakeFont : IHudFont
    {
        public float CellHeight => 75;

        public float Width(string text, float scale) => text.Length * LetterWidth * scale;
    }
}
