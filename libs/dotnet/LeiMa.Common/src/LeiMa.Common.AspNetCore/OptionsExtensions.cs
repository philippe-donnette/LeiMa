namespace LeiMa.Common.AspNetCore;

/// <summary>Options helpers: bind a configuration section and fail fast at startup when it is invalid.</summary>
public static class OptionsExtensions
{
    /// <summary>
    /// Binds <typeparamref name="TOptions"/> to the configuration section, validates it with data annotations
    /// and validates it when the host starts, so misconfiguration stops the service immediately.
    /// </summary>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
