using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LeiMa.Common.AspNetCore.Tests;

public class DefaultsTests
{
    private static void MapSamples(WebApplication app)
    {
        app.MapGet("/boom", () => { throw new InvalidOperationException("secret detail"); });
        app.MapGet("/ok", () => Results.Ok(new { hello = "world" })).AllowAnonymous();
        app.MapGet("/missing", () => Error.NotFound("provider.not_found", "No such provider.").ToProblemResult())
            .AllowAnonymous();
        app.MapGet("/result", () => Result.Success(5).ToHttpResult()).AllowAnonymous();
    }

    [Fact]
    public async Task UnhandledException_ReturnsProblemDetails_WithoutLeakingDetails()
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);

        var response = await app.Client.GetAsync("/boom");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("secret detail", body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        Assert.True(json.RootElement.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetails()
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);

        var response = await app.Client.GetAsync("/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ErrorResult_IsMappedToProblemDetailsWithCode()
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);

        var response = await app.Client.GetAsync("/missing");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("provider.not_found", json.RootElement.GetProperty("code").GetString());
        Assert.Equal("No such provider.", json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task SuccessResult_ReturnsOk()
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);

        var response = await app.Client.GetAsync("/result");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, await response.Content.ReadFromJsonAsync<int>());
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Failure, 500)]
    public void ErrorTypes_MapToStatusCodes(ErrorType type, int expected)
    {
        Assert.Equal(expected, ResultHttpExtensions.StatusCodeFor(type));
    }

    [Fact]
    public async Task CorrelationId_IsGenerated_WhenMissing()
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);

        var response = await app.Client.GetAsync("/ok");

        Assert.True(response.Headers.TryGetValues(CorrelationConstants.HeaderName, out var values));
        Assert.False(string.IsNullOrWhiteSpace(values!.Single()));
    }

    [Fact]
    public async Task CorrelationId_IsEchoed_WhenValid()
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.Add(CorrelationConstants.HeaderName, "abc-123_X.y");

        var response = await app.Client.SendAsync(request);

        Assert.Equal("abc-123_X.y", response.Headers.GetValues(CorrelationConstants.HeaderName).Single());
    }

    [Fact]
    public async Task CorrelationId_IsAlsoReturnedOnFailures()
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/boom");
        request.Headers.Add(CorrelationConstants.HeaderName, "from-client");

        var response = await app.Client.SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("from-client", response.Headers.GetValues(CorrelationConstants.HeaderName).Single());
        Assert.Equal("from-client", json.RootElement.GetProperty("correlationId").GetString());
    }

    [Theory]
    [InlineData("has spaces")]
    [InlineData("semi;colon")]
    public async Task CorrelationId_IsReplaced_WhenInvalid(string incoming)
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.TryAddWithoutValidation(CorrelationConstants.HeaderName, incoming);

        var response = await app.Client.SendAsync(request);

        Assert.NotEqual(incoming, response.Headers.GetValues(CorrelationConstants.HeaderName).Single());
    }

    [Fact]
    public async Task CorrelationId_IsReplaced_WhenTooLong()
    {
        await using var app = await TestApp.StartAsync(mapEndpoints: MapSamples);
        var tooLong = new string('a', CorrelationConstants.MaxLength + 1);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.Add(CorrelationConstants.HeaderName, tooLong);

        var response = await app.Client.SendAsync(request);

        Assert.NotEqual(tooLong, response.Headers.GetValues(CorrelationConstants.HeaderName).Single());
    }

    [Fact]
    public async Task Liveness_IsOk_EvenWhenAReadinessCheckFails()
    {
        await using var app = await TestApp.StartAsync(
            builder => builder.Services.AddHealthChecks().AddCheck(
                "down", () => HealthCheckResult.Unhealthy("down"), tags: [LeiMaDefaultsExtensions.ReadyTag]));

        var live = await app.Client.GetAsync(LeiMaDefaultsExtensions.LivePath);
        var ready = await app.Client.GetAsync(LeiMaDefaultsExtensions.ReadyPath);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
    }

    [Fact]
    public async Task Readiness_IsOk_WhenChecksPass()
    {
        await using var app = await TestApp.StartAsync(
            builder => builder.Services.AddHealthChecks().AddCheck(
                "up", () => HealthCheckResult.Healthy(), tags: [LeiMaDefaultsExtensions.ReadyTag]));

        var ready = await app.Client.GetAsync(LeiMaDefaultsExtensions.ReadyPath);

        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoints_AreAnonymous_EvenWithDenyByDefaultAuthorization()
    {
        await using var app = await TestApp.StartAsync(
            builder => builder.Services.AddLeiMaAuthorization(),
            MapSamples);

        var live = await app.Client.GetAsync(LeiMaDefaultsExtensions.LivePath);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
