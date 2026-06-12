using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace BauerApps.Dataverse.Extensions.Tests;

public class ServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddDataverseClient_RegistersServiceClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(ServiceClient)
                        && x.Lifetime == ServiceLifetime.Singleton);
    }

    [Test]
    public async Task AddDataverseClient_RegistersScopedIOrganizationServiceAsync2()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(IOrganizationServiceAsync2)
                        && x.Lifetime == ServiceLifetime.Scoped);
    }

    [Test]
    public async Task AddDataverseClient_ConfiguresOptions()
    {
        // Arrange
        var organizationUrl = new Uri("https://my-org.crm4.dynamics.com");
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(options =>
        {
            options.OrganizationUrl = organizationUrl;
            options.DeferConnection = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DataverseClientOptions>>().Value;

        await Assert.That(options.OrganizationUrl).IsEqualTo(organizationUrl);
        await Assert.That(options.DeferConnection).IsTrue();
        await Assert.That(options.TokenCredential).IsNull();
    }

    [Test]
    public async Task AddDataverseClient_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddDataverseClient(options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(result).IsSameReferenceAs(services);
    }
}

