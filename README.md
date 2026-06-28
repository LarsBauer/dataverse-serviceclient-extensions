# dataverse-serviceclient-extensions

[![CI](https://img.shields.io/github/actions/workflow/status/LarsBauer/dataverse-serviceclient-extensions/ci.yml?branch=main)](https://github.com/LarsBauer/dataverse-serviceclient-extensions/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/LarsBauer/dataverse-serviceclient-extensions)](LICENSE)

A collection of .NET libraries for working with [Microsoft Dataverse](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/overview) in modern .NET applications.

## Packages

| Package | Version | Description |
| --- | --- | --- |
| [`BauerApps.Dataverse.Extensions.DependencyInjection`](Dataverse.Extensions.DependencyInjection/README.md) | [![NuGet](https://img.shields.io/nuget/v/BauerApps.Dataverse.Extensions.DependencyInjection)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.DependencyInjection) | One-line DI registration for `ServiceClient` with singleton + scoped `Clone()` lifecycle. Supports keyed (multi-environment) registrations and Azure.Identity authentication. |
| [`BauerApps.Dataverse.Extensions.HealthChecks`](Dataverse.Extensions.HealthChecks/README.md) | [![NuGet](https://img.shields.io/nuget/v/BauerApps.Dataverse.Extensions.HealthChecks)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.HealthChecks) | ASP.NET Core health check that verifies Dataverse connectivity via a `WhoAmI` request. |

## Quick start

```bash
# DI registration
dotnet add package BauerApps.Dataverse.Extensions.DependencyInjection

# Health checks
dotnet add package BauerApps.Dataverse.Extensions.HealthChecks
```

```csharp
// Program.cs
builder.Services.AddDataverseClient(options =>
{
    options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
});

builder.Services.AddHealthChecks()
    .AddDataverseHealthCheck();
```

See the individual package READMEs for full documentation.

## Build & test

```powershell
dotnet build Dataverse.Extensions.slnx
dotnet test Dataverse.Extensions.slnx
```

## License

[MIT](LICENSE)
