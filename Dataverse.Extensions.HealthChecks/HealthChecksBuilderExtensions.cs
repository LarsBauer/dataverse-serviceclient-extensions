using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace BauerApps.Dataverse.Extensions;

public static class HealthChecksBuilderExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        /// <summary>
        /// Adds a health check that verifies connectivity to the unkeyed Dataverse
        /// <see cref="ServiceClient"/> by executing a <c>WhoAmI</c> request.
        /// </summary>
        /// <param name="name">
        /// The health check name shown in health reports. Defaults to <c>"dataverse"</c>.
        /// </param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> to report when the check fails.
        /// Defaults to <see cref="HealthStatus.Unhealthy"/> when <see langword="null"/>.
        /// </param>
        /// <param name="tags">Optional tags for filtering health checks (e.g. <c>"ready"</c>, <c>"live"</c>).</param>
        /// <param name="timeout">Optional per-check timeout. Defaults to no registration-level timeout.</param>
        /// <returns>The builder for chaining.</returns>
        public IHealthChecksBuilder AddDataverseHealthCheck(
            string name = "dataverse",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            builder.Add(new HealthCheckRegistration(
                name,
                sp => new DataverseHealthCheck(sp.GetRequiredService<ServiceClient>()),
                failureStatus,
                tags,
                timeout));

            return builder;
        }

        /// <summary>
        /// Adds a health check that verifies connectivity to a keyed Dataverse
        /// <see cref="ServiceClient"/> (for multi-environment scenarios) by executing a
        /// <c>WhoAmI</c> request.
        /// </summary>
        /// <param name="serviceKey">
        /// The service key used to resolve the <see cref="ServiceClient"/> from the DI container.
        /// Doubles as the default health check <paramref name="name"/> when no name is provided.
        /// </param>
        /// <param name="name">
        /// The health check name shown in health reports.
        /// Defaults to <paramref name="serviceKey"/> when <see langword="null"/>.
        /// </param>
        /// <param name="failureStatus">
        /// The <see cref="HealthStatus"/> to report when the check fails.
        /// Defaults to <see cref="HealthStatus.Unhealthy"/> when <see langword="null"/>.
        /// </param>
        /// <param name="tags">Optional tags for filtering health checks (e.g. <c>"ready"</c>, <c>"live"</c>).</param>
        /// <param name="timeout">Optional per-check timeout. Defaults to no registration-level timeout.</param>
        /// <returns>The builder for chaining.</returns>
        public IHealthChecksBuilder AddKeyedDataverseHealthCheck(
            string serviceKey,
            string? name = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            builder.Add(new HealthCheckRegistration(
                name ?? serviceKey,
                sp => new DataverseHealthCheck(sp.GetRequiredKeyedService<ServiceClient>(serviceKey)),
                failureStatus,
                tags,
                timeout));

            return builder;
        }
    }
}

