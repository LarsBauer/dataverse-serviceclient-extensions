using BauerApps.Dataverse.Extensions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace BauerApps.Dataverse.Extensions;

public static class ServiceCollectionExtensions
{
  extension(IServiceCollection services)
  {
    /// <summary>
    /// Registers a singleton <see cref="ServiceClient"/> and a scoped
    /// <see cref="IOrganizationServiceAsync2"/> (via <see cref="ServiceClient.Clone(Microsoft.Extensions.Logging.ILogger)"/>)
    /// in the dependency injection container.
    /// </summary>
    /// <param name="configureOptions">Action to configure <see cref="DataverseClientOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddDataverseClient(Action<DataverseClientOptions> configureOptions)
    {
      services.AddOptionsWithValidateOnStart<DataverseClientOptions>()
        .Configure(configureOptions)
        .ValidateDataverseClientOptions();

      return services.AddDataverseClientCore();
    }

    /// <summary>
    /// Registers a singleton <see cref="ServiceClient"/> and a scoped
    /// <see cref="IOrganizationServiceAsync2"/> (via <see cref="ServiceClient.Clone(Microsoft.Extensions.Logging.ILogger)"/>)
    /// in the dependency injection container, binding <see cref="DataverseClientOptions"/>
    /// from the supplied configuration section.
    /// </summary>
    /// <param name="configuration">Configuration section to bind <see cref="DataverseClientOptions"/> from.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Only non-secret values bind from configuration (e.g. <c>OrganizationUrl</c>,
    /// <c>DeferConnection</c>). Authentication uses <see cref="Azure.Identity.DefaultAzureCredential"/>
    /// by default; to use a custom credential, set <see cref="DataverseClientOptions.TokenCredential"/>
    /// via <see cref="OptionsServiceCollectionExtensions.PostConfigure{TOptions}(IServiceCollection, Action{TOptions})"/>.
    /// </remarks>
    public IServiceCollection AddDataverseClient(IConfiguration configuration)
    {
      services.AddOptionsWithValidateOnStart<DataverseClientOptions>()
        .Bind(configuration)
        .ValidateDataverseClientOptions();

      return services.AddDataverseClientCore();
    }

    /// <summary>
    /// Registers keyed singleton and scoped Dataverse services for the given client name.
    /// </summary>
    /// <param name="name">The keyed service name.</param>
    /// <param name="configureOptions">Action to configure named <see cref="DataverseClientOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddDataverseClient(string name, Action<DataverseClientOptions> configureOptions)
    {
      services.AddOptionsWithValidateOnStart<DataverseClientOptions>(name)
        .Configure(configureOptions)
        .ValidateDataverseClientOptions();

      return services.AddNamedDataverseClientCore(name);
    }

    /// <summary>
    /// Registers keyed singleton and scoped Dataverse services for the given client name,
    /// binding named options from the supplied configuration section.
    /// </summary>
    /// <param name="name">The keyed service name.</param>
    /// <param name="configuration">Configuration section to bind named options from.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddDataverseClient(string name, IConfiguration configuration)
    {
      services.AddOptionsWithValidateOnStart<DataverseClientOptions>(name)
        .Bind(configuration)
        .ValidateDataverseClientOptions();

      return services.AddNamedDataverseClientCore(name);
    }

    private IServiceCollection AddDataverseClientCore()
    {
      services.AddSingleton(ServiceClientFactory.Create);

      // Clone requires OAuth-based connection — guaranteed here
      // because we always use AccessTokenProviderFunctionAsync
      services.AddScoped<IOrganizationServiceAsync2>(sp => sp.GetRequiredService<ServiceClient>().Clone());

      return services;
    }

    private IServiceCollection AddNamedDataverseClientCore(string name)
    {
      services.AddKeyedSingleton<ServiceClient>(name,
        (sp, key) => ServiceClientFactory.CreateNamed(sp, (string)key!));

      services.AddKeyedScoped<IOrganizationServiceAsync2>(name,
        (sp, key) => sp.GetRequiredKeyedService<ServiceClient>(key).Clone());

      return services;
    }
  }

  private static OptionsBuilder<DataverseClientOptions> ValidateDataverseClientOptions(
    this OptionsBuilder<DataverseClientOptions> builder)
    => builder
      .Validate(o => o.OrganizationUrl is not null,
        "OrganizationUrl is required.")
      .Validate(o => o.OrganizationUrl is null || o.OrganizationUrl.IsAbsoluteUri,
        "OrganizationUrl must be an absolute URI (e.g. https://my-org.crm4.dynamics.com).")
      .Validate(o => o.OrganizationUrl is null || o.OrganizationUrl.Scheme == Uri.UriSchemeHttps,
        "OrganizationUrl must use HTTPS.");
}