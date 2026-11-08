namespace LeiMa.Common.AspNetCore.Tests;

public class SmokeTests
{
    [Fact]
    public void Assembly_LoadsWithExpectedName()
    {
        Assert.Equal("LeiMa.Common.AspNetCore", typeof(AssemblyMarker).Assembly.GetName().Name);
    }
}
