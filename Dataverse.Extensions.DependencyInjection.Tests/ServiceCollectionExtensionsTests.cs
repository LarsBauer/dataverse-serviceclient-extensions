using Microsoft.Extensions.Configuration;
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

    [Test]
    public async Task AddDataverseClient_FromConfiguration_BindsOptionsAndRegistersServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrganizationUrl"] = "https://my-org.crm4.dynamics.com",
                ["DeferConnection"] = "true"
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(configuration);

        // Assert — services registered with the correct lifetimes
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(ServiceClient)
                        && x.Lifetime == ServiceLifetime.Singleton);
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(IOrganizationServiceAsync2)
                        && x.Lifetime == ServiceLifetime.Scoped);

        // Assert — non-secret values bound from configuration
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DataverseClientOptions>>().Value;

        await Assert.That(options.OrganizationUrl)
            .IsEqualTo(new Uri("https://my-org.crm4.dynamics.com"));
        await Assert.That(options.DeferConnection).IsTrue();
        await Assert.That(options.TokenCredential).IsNull();
    }

    [Test]
    public async Task AddDataverseClient_FromConfiguration_ThrowsOnStartWhenOrganizationUrlMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DeferConnection"] = "true"
            })
            .Build();
        var provider = new ServiceCollection()
            .AddDataverseClient(configuration)
            .BuildServiceProvider();

        // Act & Assert
        await Assert.That(() => provider.GetRequiredService<IOptions<DataverseClientOptions>>().Value)
            .Throws<OptionsValidationException>();
    }

    [Test]
    public async Task AddDataverseClient_ThrowsOnStartWhenOrganizationUrlIsNotHttps()
    {
        // Arrange
        var provider = new ServiceCollection()
            .AddDataverseClient(options =>
            {
                options.OrganizationUrl = new Uri("http://my-org.crm4.dynamics.com");
                options.DeferConnection = true;
            })
            .BuildServiceProvider();

        // Act & Assert
        await Assert.That(() => provider.GetRequiredService<IOptions<DataverseClientOptions>>().Value)
            .Throws<OptionsValidationException>();
    }
}

