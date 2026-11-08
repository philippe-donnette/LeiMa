namespace LeiMa.Common.EntityFrameworkCore.Tests;

public class SmokeTests
{
    [Fact]
    public void Assembly_LoadsWithExpectedName()
    {
        Assert.Equal("LeiMa.Common.EntityFrameworkCore", typeof(AssemblyMarker).Assembly.GetName().Name);
    }
}
