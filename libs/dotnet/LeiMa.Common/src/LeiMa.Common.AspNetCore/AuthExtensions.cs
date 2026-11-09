using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace LeiMa.Common.AspNetCore;

/// <summary>Settings for JWT bearer authentication (configuration section <c>Authentication:Jwt</c> by default).</summary>
public sealed class LeiMaJwtOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string DefaultSectionName = "Authentication:Jwt";

    /// <summary>The token issuer (OpenID Connect authority) used to discover signing keys.</summary>
    [Required]
    public string Authority { get; set; } = string.Empty;

    /// <summary>The audience this service accepts.</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Require HTTPS for metadata discovery. Disable only for local development.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}

/// <summary>Authentication and authorization wiring shared by all services.</summary>
public static class AuthExtensions
{
    /// <summary>
    /// Adds JWT bearer authentication configured from <see cref="LeiMaJwtOptions"/>. The options are validated at startup.
    /// </summary>
    public static IServiceCollection AddLeiMaJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = LeiMaJwtOptions.DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatedOptions<LeiMaJwtOptions>(configuration, sectionName);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptions<LeiMaJwtOptions>>((bearer, jwt) =>
            {
                bearer.Authority = jwt.Value.Authority;
                bearer.Audience = jwt.Value.Audience;
                bearer.RequireHttpsMetadata = jwt.Value.RequireHttpsMetadata;
            });
        return services;
    }

    /// <summary>
    /// Adds authorization that denies by default: every endpoint requires an authenticated user unless it opts out
    /// with <c>AllowAnonymous</c>. Register permission policies in <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddLeiMaAuthorization(
        this IServiceCollection services, Action<AuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            configure?.Invoke(options);
        });
    }
}
