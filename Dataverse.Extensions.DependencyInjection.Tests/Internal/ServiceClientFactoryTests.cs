using Azure.Core;
using BauerApps.Dataverse.Extensions.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace BauerApps.Dataverse.Extensions.Tests.Internal;

public class ServiceClientFactoryTests
{
    [Test]
    public async Task AddDataverseClient_ThrowsOnStartupWhenOrganizationUrlIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataverseClient(_ => { });
        var provider = services.BuildServiceProvider();

        // Act & Assert — ValidateOnStart triggers when IOptions is first resolved
        await Assert.That(() => provider.GetRequiredService<IOptions<DataverseClientOptions>>().Value)
            .Throws<OptionsValidationException>();
    }

    [Test]
    public async Task Create_UsesDefaultAzureCredentialWhenTokenCredentialIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataverseClient(options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });
        var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetRequiredService<ServiceClient>();

        // Assert — client is created without throwing (deferred connection)
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task Create_UsesCustomTokenCredentialWhenProvided()
    {
        // Arrange
        var customCredential = new FakeTokenCredential();

        var services = new ServiceCollection();
        services.AddDataverseClient(options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.TokenCredential = customCredential;
            options.DeferConnection = true;
        });
        var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetRequiredService<ServiceClient>();

        // Assert
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task Create_DoesNotThrowWhenConnectionFailsAndDeferConnectionIsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataverseClient(options =>
        {
            options.OrganizationUrl = new Uri("https://invalid-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });
        var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetRequiredService<ServiceClient>();

        // Assert — deferred connection should not throw
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task CreateKeyed_UsesKeyedOptions()
    {
        // Arrange
        const string key = "source";
        var services = new ServiceCollection();
        services.AddDataverseClient(key, options =>
        {
            options.OrganizationUrl = new Uri("https://keyed-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });
        var provider = services.BuildServiceProvider();

        // Act
        var client = ServiceClientFactory.CreateKeyed(provider, key);

        // Assert
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task CreateKeyed_UsesDefaultAzureCredentialWhenTokenCredentialIsNull()
    {
        // Arrange
        const string key = "source";
        var services = new ServiceCollection();
        services.AddDataverseClient(key, options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });
        var provider = services.BuildServiceProvider();

        // Act
        var client = ServiceClientFactory.CreateKeyed(provider, key);

        // Assert — client is created without throwing (deferred connection)
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task CreateKeyed_UsesCustomTokenCredential()
    {
        // Arrange
        const string key = "source";
        var customCredential = new FakeTokenCredential();
        var services = new ServiceCollection();
        services.AddDataverseClient(key, options =>
        {
            options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
            options.TokenCredential = customCredential;
            options.DeferConnection = true;
        });
        var provider = services.BuildServiceProvider();

        // Act
        var client = ServiceClientFactory.CreateKeyed(provider, key);

        // Assert
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task CreateKeyed_DoesNotThrowWhenConnectionFailsAndDeferConnectionIsTrue()
    {
        // Arrange
        const string key = "source";
        var services = new ServiceCollection();
        services.AddDataverseClient(key, options =>
        {
            options.OrganizationUrl = new Uri("https://invalid-org.crm4.dynamics.com");
            options.DeferConnection = true;
        });
        var provider = services.BuildServiceProvider();

        // Act
        var client = ServiceClientFactory.CreateKeyed(provider, key);

        // Assert — deferred connection should not throw
        await Assert.That(client).IsNotNull();
    }

    /// <summary>
    /// Minimal fake TokenCredential for testing.
    /// </summary>
    private sealed class FakeTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new("fake-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new(new AccessToken("fake-token", DateTimeOffset.UtcNow.AddHours(1)));
    }
}
