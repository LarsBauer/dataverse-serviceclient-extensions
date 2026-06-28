# BauerApps.Dataverse.Extensions.HealthChecks

[![CI](https://img.shields.io/github/actions/workflow/status/LarsBauer/dataverse-serviceclient-extensions/ci.yml?branch=main)](https://github.com/LarsBauer/dataverse-serviceclient-extensions/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BauerApps.Dataverse.Extensions.HealthChecks)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.HealthChecks)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BauerApps.Dataverse.Extensions.HealthChecks)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.HealthChecks)
[![License](https://img.shields.io/github/license/LarsBauer/dataverse-serviceclient-extensions)](../LICENSE)

ASP.NET Core health check for [Microsoft Dataverse](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/overview) that verifies connectivity by executing a `WhoAmI` request against the registered `ServiceClient`.

## Features

- One-line health check registration via `AddDataverseHealthCheck()` on `IHealthChecksBuilder`
- Keyed client support via `AddKeyedDataverseHealthCheck(serviceKey)` for multi-environment scenarios
- Returns `UserId`, `OrganizationId`, and `BusinessUnitId` in the health check result data
- Uses `context.Registration.FailureStatus` so consumers control whether failure is `Unhealthy` or `Degraded`
- Works independently of how `ServiceClient` was registered — pairs naturally with [`BauerApps.Dataverse.Extensions.DependencyInjection`](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.DependencyInjection)

## Get started

Install the package from NuGet:

```bash
dotnet add package BauerApps.Dataverse.Extensions.HealthChecks
```

Register the health check in `Program.cs` alongside your `ServiceClient`:

```csharp
// Register ServiceClient (e.g. via the companion DI package)
builder.Services.AddDataverseClient(options =>
{
    options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
});

// Add health check
builder.Services.AddHealthChecks()
    .AddDataverseHealthCheck();
```

Map the health check endpoint:

```csharp
app.MapHealthChecks("/health");
```

## Health check result

On success the result contains:

```json
{
  "status": "Healthy",
  "description": "Dataverse connection is healthy.",
  "data": {
    "UserId": "a1b2c3d4-...",
    "OrganizationId": "e5f6g7h8-...",
    "BusinessUnitId": "i9j0k1l2-..."
  }
}
```

On failure the result includes the exception and reports the configured `failureStatus` (`Unhealthy` by default).

## Configuration options

| Parameter | Default | Description |
| --- | --- | --- |
| `name` | `"dataverse"` | Name shown in health reports and used for filtering. |
| `failureStatus` | `Unhealthy` | Status to report on failure. Pass `Degraded` for non-critical environments. |
| `tags` | `null` | Tags for filtering (e.g. `"ready"`, `"live"`). |
| `timeout` | `null` | Per-registration timeout. Defaults to the global health check timeout. |

### Custom failure status

```csharp
builder.Services.AddHealthChecks()
    .AddDataverseHealthCheck(failureStatus: HealthStatus.Degraded);
```

### Tags for liveness / readiness probes

```csharp
builder.Services.AddHealthChecks()
    .AddDataverseHealthCheck(tags: ["ready"]);

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

## Keyed clients (multiple environments)

When using keyed `ServiceClient` registrations, use `AddKeyedDataverseHealthCheck`:

```csharp
builder.Services.AddDataverseClient("source", options =>
    options.OrganizationUrl = new Uri("https://source.crm4.dynamics.com"));

builder.Services.AddDataverseClient("target", options =>
    options.OrganizationUrl = new Uri("https://target.crm4.dynamics.com"));

builder.Services.AddHealthChecks()
    .AddKeyedDataverseHealthCheck("source", name: "dataverse-source", tags: ["ready"])
    .AddKeyedDataverseHealthCheck("target", name: "dataverse-target", tags: ["ready"]);
```

The `name` defaults to the `serviceKey` when not provided, so each keyed check appears separately in health reports.

## License

[MIT](../LICENSE)

