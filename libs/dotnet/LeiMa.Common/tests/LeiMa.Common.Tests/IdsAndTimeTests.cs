namespace LeiMa.Common.Tests;

public class IdsAndTimeTests
{
    private sealed class StubClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void NewId_IsVersion7_AndTimeOrdered()
    {
        var clock = new StubClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var first = Ids.NewId(clock);
        clock.Now = clock.Now.AddMinutes(1);
        var second = Ids.NewId(clock);

        Assert.Equal(7, first.Version);
        Assert.True(first.CompareTo(second) < 0);
    }

    [Fact]
    public void NewId_WithSystemClock_ReturnsUniqueIds()
    {
        Assert.NotEqual(Ids.NewId(), Ids.NewId());
    }

    [Fact]
    public void GetUtcDateTime_ReturnsUtcKind()
    {
        var clock = new StubClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)));

        var now = clock.GetUtcDateTime();

        Assert.Equal(DateTimeKind.Utc, now.Kind);
        Assert.Equal(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), now);
    }

    [Fact]
    public void HasPassed_ComparesAgainstTheClock()
    {
        var clock = new StubClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var deadline = clock.Now.AddMinutes(5);

        Assert.False(clock.HasPassed(deadline));
        clock.Now = deadline;
        Assert.True(clock.HasPassed(deadline));
    }
}
