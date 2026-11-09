using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LeiMa.Common.AspNetCore.Tests;

public class AuthTests
{
    private const string Issuer = "https://issuer.test";
    private const string Audience = "leima-test";

    private static readonly SymmetricSecurityKey SigningKey =
        new(new byte[32].Select((_, i) => (byte)(i + 1)).ToArray()) { KeyId = "test-key" };

    private static readonly Dictionary<string, string?> Settings = new()
    {
        ["Authentication:Jwt:Authority"] = Issuer,
        ["Authentication:Jwt:Audience"] = Audience,
    };

    private static string CreateToken(string audience = Audience, params Claim[] claims) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        });

    private static Task<TestApp> StartSecuredAsync(IDictionary<string, string?>? settings = null) =>
        TestApp.StartAsync(
            builder =>
            {
                builder.Services.AddLeiMaJwtAuthentication(builder.Configuration);
                builder.Services.AddLeiMaAuthorization(options =>
                    options.AddPolicy("providers:admin", policy => policy.RequireClaim("permission", "providers:admin")));
                // Use a fixed signing key instead of discovering keys from the (fake) authority.
                builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, bearer =>
                {
                    var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                    configuration.SigningKeys.Add(SigningKey);
                    bearer.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                });
            },
            app =>
            {
                app.MapGet("/secure", () => "secret");
                app.MapGet("/admin", () => "admin").RequireAuthorization("providers:admin");
                app.MapGet("/public", () => "public").AllowAnonymous();
            },
            settings ?? Settings);

    private static HttpRequestMessage Get(string path, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    [Fact]
    public async Task NoToken_IsUnauthorized_ButAnonymousEndpointsStayOpen()
    {
        await using var app = await StartSecuredAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await app.Client.SendAsync(Get("/secure"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.Client.SendAsync(Get("/public"))).StatusCode);
    }

    [Fact]
    public async Task ValidToken_IsAccepted()
    {
        await using var app = await StartSecuredAsync();

        var response = await app.Client.SendAsync(Get("/secure", CreateToken()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WrongAudience_IsRejected()
    {
        await using var app = await StartSecuredAsync();

        var response = await app.Client.SendAsync(Get("/secure", CreateToken(audience: "someone-else")));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Policy_RequiresTheClaim()
    {
        await using var app = await StartSecuredAsync();

        var without = await app.Client.SendAsync(Get("/admin", CreateToken()));
        var with = await app.Client.SendAsync(
            Get("/admin", CreateToken(claims: new Claim("permission", "providers:admin"))));

        Assert.Equal(HttpStatusCode.Forbidden, without.StatusCode);
        Assert.Equal(HttpStatusCode.OK, with.StatusCode);
    }

    [Fact]
    public async Task MissingAuthority_StopsTheHostAtStartup()
    {
        var ex = await Record.ExceptionAsync(() => StartSecuredAsync(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Audience"] = Audience,
        }));

        Assert.IsType<Microsoft.Extensions.Options.OptionsValidationException>(ex);
    }
}
