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
    /// Registers a keyed singleton <see cref="ServiceClient"/> and a keyed scoped
    /// <see cref="IOrganizationServiceAsync2"/> (via <see cref="ServiceClient.Clone(Microsoft.Extensions.Logging.ILogger)"/>)
    /// in the dependency injection container under the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The service key used to resolve this client via <c>[FromKeyedServices]</c>.</param>
    /// <param name="configureOptions">Action to configure <see cref="DataverseClientOptions"/> for this key.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddKeyedDataverseClient(string key, Action<DataverseClientOptions> configureOptions)
    {
      // AddOptionsWithValidateOnStart(key) scopes the builder to the named instance;
      // Configure(action) without a name argument binds to that same named instance.
      services.AddOptionsWithValidateOnStart<DataverseClientOptions>(key)
        .Configure(configureOptions)
        .ValidateDataverseClientOptions();

      return services.AddKeyedDataverseClientCore(key);
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="ServiceClient"/> and a keyed scoped
    /// <see cref="IOrganizationServiceAsync2"/> (via <see cref="ServiceClient.Clone(Microsoft.Extensions.Logging.ILogger)"/>)
    /// in the dependency injection container under the given <paramref name="key"/>
    /// , binding <see cref="DataverseClientOptions"/> from the supplied configuration section.
    /// </summary>
    /// <param name="key">The service key used to resolve this client via <c>[FromKeyedServices]</c>.</param>
    /// <param name="configuration">Configuration section to bind <see cref="DataverseClientOptions"/> from.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Only non-secret values bind from configuration (e.g. <c>OrganizationUrl</c>,
    /// <c>DeferConnection</c>). Authentication uses <see cref="Azure.Identity.DefaultAzureCredential"/>
    /// by default; to use a custom credential, set <see cref="DataverseClientOptions.TokenCredential"/>
    /// via <see cref="OptionsServiceCollectionExtensions.PostConfigure{TOptions}(IServiceCollection, Action{TOptions})"/>.
    /// </remarks>
    public IServiceCollection AddKeyedDataverseClient(string key, IConfiguration configuration)
    {
      // AddOptionsWithValidateOnStart(key) scopes the builder to the named instance;
      // Bind(configuration) without a name argument binds to that same named instance.
      services.AddOptionsWithValidateOnStart<DataverseClientOptions>(key)
        .Bind(configuration)
        .ValidateDataverseClientOptions();

      return services.AddKeyedDataverseClientCore(key);
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="ServiceClient"/> and a keyed scoped
    /// <see cref="IOrganizationServiceAsync2"/> (via <see cref="ServiceClient.Clone(Microsoft.Extensions.Logging.ILogger)"/>)
    /// in the dependency injection container under the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The service key used to resolve this client via <c>[FromKeyedServices]</c>.</param>
    /// <param name="configureOptions">Action to configure <see cref="DataverseClientOptions"/> for this key.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete("Use AddKeyedDataverseClient instead. Will be removed in v2.0.0.")]
    public IServiceCollection AddDataverseClient(string key, Action<DataverseClientOptions> configureOptions)
        => services.AddKeyedDataverseClient(key, configureOptions);

    /// <summary>
    /// Registers a keyed singleton <see cref="ServiceClient"/> and a keyed scoped
    /// <see cref="IOrganizationServiceAsync2"/> (via <see cref="ServiceClient.Clone(Microsoft.Extensions.Logging.ILogger)"/>)
    /// in the dependency injection container under the given <paramref name="key"/>,
    /// binding <see cref="DataverseClientOptions"/> from the supplied configuration section.
    /// </summary>
    /// <param name="key">The service key used to resolve this client via <c>[FromKeyedServices]</c>.</param>
    /// <param name="configuration">Configuration section to bind <see cref="DataverseClientOptions"/> from.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Only non-secret values bind from configuration (e.g. <c>OrganizationUrl</c>,
    /// <c>DeferConnection</c>). Authentication uses <see cref="Azure.Identity.DefaultAzureCredential"/>
    /// by default; to use a custom credential, set <see cref="DataverseClientOptions.TokenCredential"/>
    /// via <see cref="OptionsServiceCollectionExtensions.PostConfigure{TOptions}(IServiceCollection, Action{TOptions})"/>.
    /// </remarks>
    [Obsolete("Use AddKeyedDataverseClient instead. Will be removed in v2.0.0.")]
    public IServiceCollection AddDataverseClient(string key, IConfiguration configuration)
        => services.AddKeyedDataverseClient(key, configuration);

    private IServiceCollection AddDataverseClientCore()
    {
      services.AddSingleton(ServiceClientFactory.Create);

      // Clone requires OAuth-based connection — guaranteed here
      // because we always use AccessTokenProviderFunctionAsync
      services.AddScoped<IOrganizationServiceAsync2>(sp => sp.GetRequiredService<ServiceClient>().Clone());

      return services;
    }

    private IServiceCollection AddKeyedDataverseClientCore(string key)
    {
      services.AddKeyedSingleton<ServiceClient>(key,
        (sp, k) => ServiceClientFactory.CreateKeyed(sp, (string)k!));

      services.AddKeyedScoped<IOrganizationServiceAsync2>(key,
        (sp, k) => sp.GetRequiredKeyedService<ServiceClient>(k).Clone());

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
