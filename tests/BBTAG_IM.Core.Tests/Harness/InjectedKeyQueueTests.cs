using BBTAG_IM.Core.Harness;

namespace BBTAG_IM.Core.Tests.Harness;

public class InjectedKeyQueueTests
{
    private const int A = 0x41;
    private const int B = 0x42;

    [Fact]
    public void OnFrame_StepQueued_PressesItsKeys()
    {
        // Arrange
        InjectedKeyQueue queue = CreateInjectedKeyQueue();
        queue.Enqueue([new KeyStep([A], 2, 1)]);

        // Act
        queue.OnFrame();

        // Assert
        Assert.True(queue.IsDown(A));
    }

    [Fact]
    public void OnFrame_HoldFramesPassed_ReleasesTheKeys()
    {
        // Arrange
        InjectedKeyQueue queue = CreateInjectedKeyQueue();
        queue.Enqueue([new KeyStep([A], 2, 1)]);
        queue.OnFrame();
        queue.OnFrame();

        // Act
        queue.OnFrame();

        // Assert
        Assert.False(queue.IsDown(A));
    }

    [Fact]
    public void OnFrame_TwoSteps_PlaysThemOneAfterTheOther()
    {
        // Arrange
        InjectedKeyQueue queue = CreateInjectedKeyQueue();
        queue.Enqueue([new KeyStep([A], 1, 1), new KeyStep([B], 1, 1)]);
        List<(bool A, bool B)> frames = [];

        // Act
        for (int frame = 0; frame < 5; frame++)
        {
            queue.OnFrame();
            frames.Add((queue.IsDown(A), queue.IsDown(B)));
        }

        // Assert
        Assert.Equal([(true, false), (false, false), (false, true), (false, false), (false, false)], frames);
    }

    [Fact]
    public void IsIdle_AllStepsPlayed_ReturnsTrue()
    {
        // Arrange
        InjectedKeyQueue queue = CreateInjectedKeyQueue();
        queue.Enqueue([new KeyStep([A], 1, 1)]);

        for (int frame = 0; frame < 3; frame++)
            queue.OnFrame();

        // Act
        bool idle = queue.IsIdle;

        // Assert
        Assert.True(idle);
    }

    [Fact]
    public void Merge_KeyDown_SetsTheHighBitAndKeepsTheRealState()
    {
        // Arrange
        InjectedKeyQueue queue = CreateInjectedKeyQueue();
        queue.Enqueue([new KeyStep([A], 2, 1)]);
        queue.OnFrame();
        byte[] state = new byte[256];
        state[B] = 0x81;

        // Act
        queue.Merge(state);

        // Assert
        Assert.Equal(((byte)0x80, (byte)0x81), (state[A], state[B]));
    }

    [Fact]
    public void Clear_StepsWaiting_ReleasesEverything()
    {
        // Arrange
        InjectedKeyQueue queue = CreateInjectedKeyQueue();
        queue.Enqueue([new KeyStep([A], 5, 1), new KeyStep([B], 5, 1)]);
        queue.OnFrame();

        // Act
        queue.Clear();

        // Assert
        Assert.Equal((false, true), (queue.IsDown(A), queue.IsIdle));
    }

    public InjectedKeyQueue CreateInjectedKeyQueue() => new();
}
