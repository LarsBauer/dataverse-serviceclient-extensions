using BauerApps.Dataverse.Extensions.Internal;
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
    /// <see cref="IOrganizationServiceAsync2"/> (via <see cref="ServiceClient.Clone()"/>) 
    /// in the dependency injection container.
    /// </summary>
    /// <param name="configureOptions">Action to configure <see cref="DataverseClientOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddDataverseClient(Action<DataverseClientOptions> configureOptions)
    {
      services.AddOptionsWithValidateOnStart<DataverseClientOptions>()
        .Configure(configureOptions)
        .Validate(o => o.OrganizationUrl is not null, "OrganizationUrl is required.");

      services.AddSingleton(ServiceClientFactory.Create);

      // Clone requires OAuth-based connection — guaranteed here
      // because we always use AccessTokenProviderFunctionAsync
      services.AddScoped<IOrganizationServiceAsync2>(sp => sp.GetRequiredService<ServiceClient>().Clone());

      return services;
    }
  }
}