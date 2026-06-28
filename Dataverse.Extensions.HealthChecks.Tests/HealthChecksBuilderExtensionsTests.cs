using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BauerApps.Dataverse.Extensions.Tests;

public class HealthChecksBuilderExtensionsTests
{
    [Test]
    public async Task AddDataverseHealthCheck_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks().AddDataverseHealthCheck();

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        await Assert.That(options.Value.Registrations).HasSingleItem();
    }

    [Test]
    public async Task AddDataverseHealthCheck_UsesDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks().AddDataverseHealthCheck();

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        await Assert.That(options.Value.Registrations.Single().Name).IsEqualTo("dataverse");
    }

    [Test]
    public async Task AddDataverseHealthCheck_WithCustomName_UsesCustomName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks().AddDataverseHealthCheck(name: "crm");

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        await Assert.That(options.Value.Registrations.Single().Name).IsEqualTo("crm");
    }

    [Test]
    public async Task AddDataverseHealthCheck_WithTags_RegistersWithTags()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks().AddDataverseHealthCheck(tags: ["ready", "dataverse"]);

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        var tags = options.Value.Registrations.Single().Tags;

        await Assert.That(tags).Contains("ready");
        await Assert.That(tags).Contains("dataverse");
    }

    [Test]
    public async Task AddDataverseHealthCheck_WithFailureStatus_RegistersWithFailureStatus()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks().AddDataverseHealthCheck(failureStatus: HealthStatus.Degraded);

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        await Assert.That(options.Value.Registrations.Single().FailureStatus).IsEqualTo(HealthStatus.Degraded);
    }

    [Test]
    public async Task AddDataverseHealthCheck_ReturnsBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        var result = builder.AddDataverseHealthCheck();

        // Assert
        await Assert.That(result).IsSameReferenceAs(builder);
    }

    [Test]
    public async Task AddKeyedDataverseHealthCheck_DefaultsNameToServiceKey()
    {
        // Arrange
        const string serviceKey = "source";
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks().AddKeyedDataverseHealthCheck(serviceKey);

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        await Assert.That(options.Value.Registrations.Single().Name).IsEqualTo(serviceKey);
    }

    [Test]
    public async Task AddKeyedDataverseHealthCheck_WithCustomName_UsesCustomName()
    {
        // Arrange
        const string serviceKey = "source";
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks().AddKeyedDataverseHealthCheck(serviceKey, name: "source-crm");

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        await Assert.That(options.Value.Registrations.Single().Name).IsEqualTo("source-crm");
    }

    [Test]
    public async Task AddKeyedDataverseHealthCheck_TwoKeys_RegistersTwoHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks()
            .AddKeyedDataverseHealthCheck("source")
            .AddKeyedDataverseHealthCheck("target");

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        var names = options.Value.Registrations.Select(r => r.Name).ToList();

        await Assert.That(names).Contains("source");
        await Assert.That(names).Contains("target");
    }

    [Test]
    public async Task AddKeyedDataverseHealthCheck_ReturnsBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        // Act
        var result = builder.AddKeyedDataverseHealthCheck("source");

        // Assert
        await Assert.That(result).IsSameReferenceAs(builder);
    }
}

