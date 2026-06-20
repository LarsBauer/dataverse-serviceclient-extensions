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
    public async Task AddDataverseClient_Named_RegistersKeyedServiceClient()
    {
        // Arrange
        const string name = "source";
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(name, options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(ServiceClient)
                        && x.ServiceKey as string == name
                        && x.Lifetime == ServiceLifetime.Singleton);
    }

    [Test]
    public async Task AddDataverseClient_Named_RegistersKeyedScopedIOrganizationServiceAsync2()
    {
        // Arrange
        const string name = "source";
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(name, options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(IOrganizationServiceAsync2)
                        && x.ServiceKey as string == name
                        && x.Lifetime == ServiceLifetime.Scoped);
    }

    [Test]
    public async Task AddDataverseClient_Named_ConfiguresNamedOptions()
    {
        // Arrange
        const string name = "source";
        var organizationUrl = new Uri("https://my-org.crm4.dynamics.com");
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(name, options =>
        {
            options.OrganizationUrl = organizationUrl;
            options.DeferConnection = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(name);

        await Assert.That(options.OrganizationUrl).IsEqualTo(organizationUrl);
        await Assert.That(options.DeferConnection).IsTrue();
        await Assert.That(options.TokenCredential).IsNull();
    }

    [Test]
    public async Task AddDataverseClient_Named_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        const string name = "source";
        var services = new ServiceCollection();

        // Act
        var result = services.AddDataverseClient(name, options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(result).IsSameReferenceAs(services);
    }

    [Test]
    public async Task AddDataverseClient_Named_FromConfiguration_BindsOptionsAndRegistersServices()
    {
        // Arrange
        const string name = "source";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrganizationUrl"] = "https://my-org.crm4.dynamics.com",
                ["DeferConnection"] = "true"
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(name, configuration);

        // Assert — services registered with the correct lifetimes
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(ServiceClient)
                        && x.ServiceKey as string == name
                        && x.Lifetime == ServiceLifetime.Singleton);
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(IOrganizationServiceAsync2)
                        && x.ServiceKey as string == name
                        && x.Lifetime == ServiceLifetime.Scoped);

        // Assert — non-secret values bound from configuration
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(name);

        await Assert.That(options.OrganizationUrl)
            .IsEqualTo(new Uri("https://my-org.crm4.dynamics.com"));
        await Assert.That(options.DeferConnection).IsTrue();
        await Assert.That(options.TokenCredential).IsNull();
    }

    [Test]
    public async Task AddDataverseClient_Named_DoesNotAffectUnnamedRegistration()
    {
        // Arrange
        const string name = "source";
        var unnamedOrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
        var namedOrganizationUrl = new Uri("https://named-org.crm4.dynamics.com");

        var provider = new ServiceCollection()
            .AddDataverseClient(options =>
            {
                options.OrganizationUrl = unnamedOrganizationUrl;
                options.DeferConnection = true;
            })
            .AddDataverseClient(name, options =>
            {
                options.OrganizationUrl = namedOrganizationUrl;
                options.DeferConnection = true;
            })
            .BuildServiceProvider();

        // Act
        var unnamedOptions = provider.GetRequiredService<IOptions<DataverseClientOptions>>().Value;
        var namedOptions = provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(name);
        var unnamedClient = provider.GetRequiredService<ServiceClient>();
        var namedClient = provider.GetRequiredKeyedService<ServiceClient>(name);

        // Assert
        await Assert.That(unnamedOptions.OrganizationUrl).IsEqualTo(unnamedOrganizationUrl);
        await Assert.That(namedOptions.OrganizationUrl).IsEqualTo(namedOrganizationUrl);
        await Assert.That(unnamedClient).IsNotNull();
        await Assert.That(namedClient).IsNotNull();
        await Assert.That(ReferenceEquals(namedClient, unnamedClient)).IsFalse();
    }

    [Test]
    public async Task AddDataverseClient_Named_TwoNamedClientsCoexist()
    {
        // Arrange
        const string source = "source";
        const string target = "target";
        var sourceOrganizationUrl = new Uri("https://source-org.crm4.dynamics.com");
        var targetOrganizationUrl = new Uri("https://target-org.crm4.dynamics.com");
        var services = new ServiceCollection();

        // Act
        services.AddDataverseClient(source, options =>
        {
            options.OrganizationUrl = sourceOrganizationUrl;
            options.DeferConnection = true;
        });
        services.AddDataverseClient(target, options =>
        {
            options.OrganizationUrl = targetOrganizationUrl;
            options.DeferConnection = true;
        });

        // Assert — both keyed registrations exist
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(ServiceClient)
                        && x.ServiceKey as string == source
                        && x.Lifetime == ServiceLifetime.Singleton);
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(ServiceClient)
                        && x.ServiceKey as string == target
                        && x.Lifetime == ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>();

        await Assert.That(optionsMonitor.Get(source).OrganizationUrl).IsEqualTo(sourceOrganizationUrl);
        await Assert.That(optionsMonitor.Get(target).OrganizationUrl).IsEqualTo(targetOrganizationUrl);
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
    public async Task AddDataverseClient_Named_FromConfiguration_ThrowsOnStartWhenOrganizationUrlMissing()
    {
        // Arrange
        const string name = "source";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DeferConnection"] = "true"
            })
            .Build();
        var provider = new ServiceCollection()
            .AddDataverseClient(name, configuration)
            .BuildServiceProvider();

        // Act & Assert
        await Assert.That(() => provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(name))
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

    [Test]
    public async Task AddDataverseClient_Named_ThrowsOnStartWhenOrganizationUrlIsNotHttps()
    {
        // Arrange
        const string name = "source";
        var provider = new ServiceCollection()
            .AddDataverseClient(name, options =>
            {
                options.OrganizationUrl = new Uri("http://my-org.crm4.dynamics.com");
                options.DeferConnection = true;
            })
            .BuildServiceProvider();

        // Act & Assert
        await Assert.That(() => provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(name))
            .Throws<OptionsValidationException>();
    }
}
