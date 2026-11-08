using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Tests.Overlay;

public class MenuInputRouteTests
{
    private const int Shift = 0x10;
    private const int Control = 0x11;
    private const int LetterA = 0x41;

    [Theory]
    [InlineData(Shift)]
    [InlineData(Control)]
    public void Of_ModifierPressedOutsideATextField_ReachesTheMenuAndTheGame(int key)
    {
        // Act
        MenuInputRoute route = MenuInputRoute.Of(new KeyPressed(key), typing: false);

        // Assert
        Assert.Equal(new MenuInputRoute(ReachesMenu: true, ReachesGame: true), route);
    }

    [Fact]
    public void Of_OtherKeyPressedOutsideATextField_ReachesOnlyTheGame()
    {
        // Act
        MenuInputRoute route = MenuInputRoute.Of(new KeyPressed(LetterA), typing: false);

        // Assert
        Assert.Equal(new MenuInputRoute(ReachesMenu: false, ReachesGame: true), route);
    }

    [Fact]
    public void Of_CharacterOutsideATextField_ReachesOnlyTheGame()
    {
        // Act
        MenuInputRoute route = MenuInputRoute.Of(new CharacterTyped('a'), typing: false);

        // Assert
        Assert.Equal(new MenuInputRoute(ReachesMenu: false, ReachesGame: true), route);
    }

    [Theory]
    [InlineData(Control)]
    [InlineData(LetterA)]
    public void Of_KeyPressedInATextField_ReachesOnlyTheMenu(int key)
    {
        // Act
        MenuInputRoute route = MenuInputRoute.Of(new KeyPressed(key), typing: true);

        // Assert
        Assert.Equal(new MenuInputRoute(ReachesMenu: true, ReachesGame: false), route);
    }

    [Fact]
    public void Of_CharacterInATextField_ReachesOnlyTheMenu()
    {
        // Act
        MenuInputRoute route = MenuInputRoute.Of(new CharacterTyped('a'), typing: true);

        // Assert
        Assert.Equal(new MenuInputRoute(ReachesMenu: true, ReachesGame: false), route);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Of_KeyReleased_AlwaysReachesTheMenu(bool typing, bool reachesGame)
    {
        // Act
        MenuInputRoute route = MenuInputRoute.Of(new KeyReleased(Control), typing);

        // Assert
        Assert.Equal(new MenuInputRoute(ReachesMenu: true, reachesGame), route);
    }

    [Fact]
    public void Of_Mouse_ReachesOnlyTheMenu()
    {
        // Act
        MenuInputRoute route = MenuInputRoute.Of(new MouseButton(0, true), typing: false);

        // Assert
        Assert.Equal(new MenuInputRoute(ReachesMenu: true, ReachesGame: false), route);
    }

    [Fact]
    public void Of_FocusLost_ReachesTheMenuAndTheGame()
    {
        // Act
        MenuInputRoute route = MenuInputRoute.Of(new FocusLost(), typing: false);

        // Assert
        Assert.Equal(new MenuInputRoute(ReachesMenu: true, ReachesGame: true), route);
    }
}
