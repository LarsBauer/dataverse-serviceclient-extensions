using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace BauerApps.Dataverse.Extensions.Tests;

public class DataverseHealthCheckTests
{
    private static HealthCheckContext CreateContext(HealthStatus failureStatus = HealthStatus.Unhealthy)
    {
        var check = new DataverseHealthCheck(IOrganizationServiceAsync2.Mock());
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("dataverse", _ => check, failureStatus, null)
        };
    }

    private static WhoAmIResponse BuildResponse(Guid userId, Guid orgId, Guid buId)
    {
        var response = new WhoAmIResponse
        {
            Results =
            {
                ["UserId"] = userId,
                ["OrganizationId"] = orgId,
                ["BusinessUnitId"] = buId
            }
        };
        return response;
    }

    [Test]
    public async Task CheckHealthAsync_WhenWhoAmISucceeds_ReturnsHealthy()
    {
        // Arrange
        var service = IOrganizationServiceAsync2.Mock();
        service.ExecuteAsync(Any<OrganizationRequest>())
            .Returns(BuildResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        var healthCheck = new DataverseHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenWhoAmISucceeds_ReturnsUserIdInData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var service = IOrganizationServiceAsync2.Mock();
        service.ExecuteAsync(Any<OrganizationRequest>())
            .Returns(BuildResponse(userId, Guid.NewGuid(), Guid.NewGuid()));

        var healthCheck = new DataverseHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        await Assert.That(result.Data).ContainsKey("UserId");
        await Assert.That(result.Data["UserId"]).IsEqualTo(userId);
    }

    [Test]
    public async Task CheckHealthAsync_WhenWhoAmISucceeds_ReturnsOrganizationIdInData()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var service = IOrganizationServiceAsync2.Mock();
        service.ExecuteAsync(Any<OrganizationRequest>())
            .Returns(BuildResponse(Guid.NewGuid(), orgId, Guid.NewGuid()));

        var healthCheck = new DataverseHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        await Assert.That(result.Data).ContainsKey("OrganizationId");
        await Assert.That(result.Data["OrganizationId"]).IsEqualTo(orgId);
    }

    [Test]
    public async Task CheckHealthAsync_WhenWhoAmISucceeds_ReturnsBusinessUnitIdInData()
    {
        // Arrange
        var buId = Guid.NewGuid();
        var service = IOrganizationServiceAsync2.Mock();
        service.ExecuteAsync(Any<OrganizationRequest>())
            .Returns(BuildResponse(Guid.NewGuid(), Guid.NewGuid(), buId));

        var healthCheck = new DataverseHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        await Assert.That(result.Data).ContainsKey("BusinessUnitId");
        await Assert.That(result.Data["BusinessUnitId"]).IsEqualTo(buId);
    }

    [Test]
    public async Task CheckHealthAsync_WhenWhoAmIThrows_ReturnsRegistrationFailureStatus()
    {
        // Arrange
        var service = IOrganizationServiceAsync2.Mock();
        service.ExecuteAsync(Any<OrganizationRequest>())
            .Throws(new InvalidOperationException("Connection refused"));

        var healthCheck = new DataverseHealthCheck(service);
        var context = CreateContext(HealthStatus.Degraded);

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        await Assert.That(result.Status).IsEqualTo(HealthStatus.Degraded);
    }

    [Test]
    public async Task CheckHealthAsync_WhenWhoAmIThrows_SurfacesException()
    {
        // Arrange
        var exception = new InvalidOperationException("Connection refused");
        var service = IOrganizationServiceAsync2.Mock();
        service.ExecuteAsync(Any<OrganizationRequest>()).Throws(exception);

        var healthCheck = new DataverseHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        await Assert.That(result.Exception).IsEqualTo(exception);
    }

    [Test]
    public async Task CheckHealthAsync_WhenWhoAmIThrows_DefaultsToUnhealthy()
    {
        // Arrange
        var service = IOrganizationServiceAsync2.Mock();
        service.ExecuteAsync(Any<OrganizationRequest>())
            .Throws(new Exception("failure"));

        var healthCheck = new DataverseHealthCheck(service);

        // Act
        var result = await healthCheck.CheckHealthAsync(CreateContext());

        // Assert
        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
    }
}
