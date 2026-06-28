using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace BauerApps.Dataverse.Extensions;

/// <summary>
/// Health check that verifies connectivity to a Microsoft Dataverse environment
/// by executing a <see cref="WhoAmIRequest"/>.
/// </summary>
public sealed class DataverseHealthCheck : IHealthCheck
{
    private readonly IOrganizationServiceAsync2 _service;

    /// <summary>
    /// Initializes a new instance of <see cref="DataverseHealthCheck"/>.
    /// </summary>
    /// <param name="service">
    /// The Dataverse service used to execute the <see cref="WhoAmIRequest"/>.
    /// Pass the singleton <see cref="ServiceClient"/> — it implements
    /// <see cref="IOrganizationServiceAsync2"/> and is safe to hold at any lifetime.
    /// </param>
    public DataverseHealthCheck(IOrganizationServiceAsync2 service)
    {
        _service = service;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = (WhoAmIResponse)await _service.ExecuteAsync(new WhoAmIRequest());

            var data = new Dictionary<string, object>
            {
                ["UserId"] = response.UserId,
                ["OrganizationId"] = response.OrganizationId,
                ["BusinessUnitId"] = response.BusinessUnitId,
            };

            return HealthCheckResult.Healthy("Dataverse connection is healthy.", data);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Dataverse connection failed.",
                ex);
        }
    }
}

