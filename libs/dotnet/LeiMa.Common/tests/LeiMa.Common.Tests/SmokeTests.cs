namespace LeiMa.Common.Tests;

public class SmokeTests
{
    [Fact]
    public void Assembly_LoadsWithExpectedName()
    {
        Assert.Equal("LeiMa.Common", typeof(AssemblyMarker).Assembly.GetName().Name);
    }
}
