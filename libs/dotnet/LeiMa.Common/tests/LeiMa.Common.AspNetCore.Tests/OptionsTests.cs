using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LeiMa.Common.AspNetCore.Tests;

public class OptionsTests
{
    private sealed class SampleOptions
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(1, 10)]
        public int Retries { get; set; } = 3;
    }

    private static ServiceProvider Build(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddValidatedOptions<SampleOptions>(configuration, "Sample");
        return services.BuildServiceProvider();
    }

    [Fact]
    public void ValidSection_Binds()
    {
        using var provider = Build(new() { ["Sample:Name"] = "x", ["Sample:Retries"] = "5" });

        var options = provider.GetRequiredService<IOptions<SampleOptions>>().Value;

        Assert.Equal("x", options.Name);
        Assert.Equal(5, options.Retries);
    }

    [Fact]
    public void InvalidSection_ThrowsWhenResolved()
    {
        using var provider = Build(new() { ["Sample:Retries"] = "50" });

        var ex = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<SampleOptions>>().Value);

        Assert.NotEmpty(ex.Failures);
    }

    [Fact]
    public async Task InvalidSection_StopsTheHostAtStartup()
    {
        var settings = new Dictionary<string, string?> { ["Sample:Retries"] = "50" };

        var ex = await Record.ExceptionAsync(() => TestApp.StartAsync(
            builder => builder.Services.AddValidatedOptions<SampleOptions>(builder.Configuration, "Sample"),
            settings: settings));

        Assert.IsType<OptionsValidationException>(ex);
    }
}
