using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Model;

namespace BauerApps.Dataverse.Extensions.Internal;

internal static class ServiceClientFactory
{
    public static ServiceClient Create(IServiceProvider serviceProvider)
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<DataverseClientOptions>>().Value;


        var credential = options.TokenCredential ?? new DefaultAzureCredential();

        var scope = $"{options.OrganizationUrl.GetLeftPart(UriPartial.Authority)}/.default";

        var logger = serviceProvider.GetService<ILoggerFactory>()
            ?.CreateLogger<ServiceClient>();

        var connectionOptions = new ConnectionOptions
        {
            ServiceUri = options.OrganizationUrl,
            Logger = logger,
            AccessTokenProviderFunctionAsync = async _ =>
            {
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext([scope]), CancellationToken.None);
                return token.Token;
            }
        };

        return new ServiceClient(connectionOptions, deferConnection: options.DeferConnection);
    }

    public static ServiceClient CreateKeyed(IServiceProvider serviceProvider, string key)
    {
        var options = serviceProvider
            .GetRequiredService<IOptionsMonitor<DataverseClientOptions>>()
            .Get(key);

        var credential = options.TokenCredential ?? new DefaultAzureCredential();

        var scope = $"{options.OrganizationUrl.GetLeftPart(UriPartial.Authority)}/.default";

        var logger = serviceProvider.GetService<ILoggerFactory>()
            ?.CreateLogger<ServiceClient>();

        var connectionOptions = new ConnectionOptions
        {
            ServiceUri = options.OrganizationUrl,
            Logger = logger,
            AccessTokenProviderFunctionAsync = async _ =>
            {
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext([scope]), CancellationToken.None);
                return token.Token;
            }
        };

        return new ServiceClient(connectionOptions, deferConnection: options.DeferConnection);
    }
}
