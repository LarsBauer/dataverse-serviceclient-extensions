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
    public async Task AddDataverseClient_Keyed_RegistersKeyedServiceClient()
    {
        // Arrange
        const string key = "source";
        var services = new ServiceCollection();

        // Act
        services.AddKeyedDataverseClient(key, options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(ServiceClient)
                        && x.ServiceKey as string == key
                        && x.Lifetime == ServiceLifetime.Singleton);
    }

    [Test]
    public async Task AddDataverseClient_Keyed_RegistersKeyedScopedIOrganizationServiceAsync2()
    {
        // Arrange
        const string key = "source";
        var services = new ServiceCollection();

        // Act
        services.AddKeyedDataverseClient(key, options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(IOrganizationServiceAsync2)
                        && x.ServiceKey as string == key
                        && x.Lifetime == ServiceLifetime.Scoped);
    }

    [Test]
    public async Task AddDataverseClient_Keyed_ConfiguresKeyedOptions()
    {
        // Arrange
        const string key = "source";
        var organizationUrl = new Uri("https://my-org.crm4.dynamics.com");
        var services = new ServiceCollection();

        // Act
        services.AddKeyedDataverseClient(key, options =>
        {
            options.OrganizationUrl = organizationUrl;
            options.DeferConnection = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(key);

        await Assert.That(options.OrganizationUrl).IsEqualTo(organizationUrl);
        await Assert.That(options.DeferConnection).IsTrue();
        await Assert.That(options.TokenCredential).IsNull();
    }

    [Test]
    public async Task AddDataverseClient_Keyed_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        const string key = "source";
        var services = new ServiceCollection();

        // Act
        var result = services.AddKeyedDataverseClient(key, options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });

        // Assert
        await Assert.That(result).IsSameReferenceAs(services);
    }

    [Test]
    public async Task AddDataverseClient_Keyed_FromConfiguration_BindsOptionsAndRegistersServices()
    {
        // Arrange
        const string key = "source";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrganizationUrl"] = "https://my-org.crm4.dynamics.com",
                ["DeferConnection"] = "true"
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddKeyedDataverseClient(key, configuration);

        // Assert — services registered with the correct lifetimes
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(ServiceClient)
                        && x.ServiceKey as string == key
                        && x.Lifetime == ServiceLifetime.Singleton);
        await Assert.That(services)
            .Contains(x => x.ServiceType == typeof(IOrganizationServiceAsync2)
                        && x.ServiceKey as string == key
                        && x.Lifetime == ServiceLifetime.Scoped);

        // Assert — non-secret values bound from configuration
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(key);

        await Assert.That(options.OrganizationUrl)
            .IsEqualTo(new Uri("https://my-org.crm4.dynamics.com"));
        await Assert.That(options.DeferConnection).IsTrue();
        await Assert.That(options.TokenCredential).IsNull();
    }

    [Test]
    public async Task AddDataverseClient_Keyed_DoesNotAffectUnkeyedRegistration()
    {
        // Arrange
        const string key = "source";
        var unkeyedOrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
        var keyedOrganizationUrl = new Uri("https://keyed-org.crm4.dynamics.com");

        var provider = new ServiceCollection()
            .AddDataverseClient(options =>
            {
                options.OrganizationUrl = unkeyedOrganizationUrl;
                options.DeferConnection = true;
            })
            .AddKeyedDataverseClient(key, options =>
            {
                options.OrganizationUrl = keyedOrganizationUrl;
                options.DeferConnection = true;
            })
            .BuildServiceProvider();

        // Act
        var unkeyedOptions = provider.GetRequiredService<IOptions<DataverseClientOptions>>().Value;
        var keyedOptions = provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(key);
        var unkeyedClient = provider.GetRequiredService<ServiceClient>();
        var keyedClient = provider.GetRequiredKeyedService<ServiceClient>(key);

        // Assert
        await Assert.That(unkeyedOptions.OrganizationUrl).IsEqualTo(unkeyedOrganizationUrl);
        await Assert.That(keyedOptions.OrganizationUrl).IsEqualTo(keyedOrganizationUrl);
        await Assert.That(unkeyedClient).IsNotNull();
        await Assert.That(keyedClient).IsNotNull();
        await Assert.That(ReferenceEquals(keyedClient, unkeyedClient)).IsFalse();
    }

    [Test]
    public async Task AddDataverseClient_Keyed_TwoKeyedClientsCoexist()
    {
        // Arrange
        const string source = "source";
        const string target = "target";
        var sourceOrganizationUrl = new Uri("https://source-org.crm4.dynamics.com");
        var targetOrganizationUrl = new Uri("https://target-org.crm4.dynamics.com");
        var services = new ServiceCollection();

        // Act
        services.AddKeyedDataverseClient(source, options =>
        {
            options.OrganizationUrl = sourceOrganizationUrl;
            options.DeferConnection = true;
        });
        services.AddKeyedDataverseClient(target, options =>
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
    public async Task AddDataverseClient_Keyed_FromConfiguration_ThrowsOnStartWhenOrganizationUrlMissing()
    {
        // Arrange
        const string key = "source";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DeferConnection"] = "true"
            })
            .Build();
        var provider = new ServiceCollection()
            .AddKeyedDataverseClient(key, configuration)
            .BuildServiceProvider();

        // Act & Assert
        await Assert.That(() => provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(key))
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
    public async Task AddDataverseClient_Keyed_ThrowsOnStartWhenOrganizationUrlIsNotHttps()
    {
        // Arrange
        const string key = "source";
        var provider = new ServiceCollection()
            .AddKeyedDataverseClient(key, options =>
            {
                options.OrganizationUrl = new Uri("http://my-org.crm4.dynamics.com");
                options.DeferConnection = true;
            })
            .BuildServiceProvider();

        // Act & Assert
        await Assert.That(() => provider.GetRequiredService<IOptionsMonitor<DataverseClientOptions>>().Get(key))
            .Throws<OptionsValidationException>();
    }
}
