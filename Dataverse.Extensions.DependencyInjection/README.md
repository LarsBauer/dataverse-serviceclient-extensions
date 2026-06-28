# BauerApps.Dataverse.Extensions.DependencyInjection

[![CI](https://img.shields.io/github/actions/workflow/status/LarsBauer/dataverse-serviceclient-extensions/ci.yml?branch=main)](https://github.com/LarsBauer/dataverse-serviceclient-extensions/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BauerApps.Dataverse.Extensions.DependencyInjection)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.DependencyInjection)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BauerApps.Dataverse.Extensions.DependencyInjection)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.DependencyInjection)
[![License](https://img.shields.io/github/license/LarsBauer/dataverse-serviceclient-extensions)](../LICENSE)

Dependency injection extensions for [Microsoft.PowerPlatform.Dataverse.Client](https://www.nuget.org/packages/Microsoft.PowerPlatform.Dataverse.Client). Registers a singleton `ServiceClient` and a scoped `IOrganizationServiceAsync2` (via `Clone()`) with a single method call.

## Features

- One-line DI registration for `ServiceClient` with proper singleton + scoped `Clone()` lifecycle
- Keyed (multi-environment) client registration via native .NET keyed DI
- Authentication via [Azure.Identity](https://www.nuget.org/packages/Azure.Identity) (`DefaultAzureCredential` by default, any `TokenCredential` supported)
- Automatic logger wiring from the DI container
- Options validation at startup — fail fast on misconfiguration
- Targeted at ASP.NET Core and Azure Functions

## Get started

Install the package from NuGet:

```bash
dotnet add package BauerApps.Dataverse.Extensions.DependencyInjection
```

Register the client in `Program.cs`:

```csharp
builder.Services.AddDataverseClient(options =>
{
    options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
});
```

Inject `IOrganizationServiceAsync2` anywhere:

```csharp
public class AccountsController(IOrganizationServiceAsync2 dataverse) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var entity = await dataverse.RetrieveAsync("account", id,
            new ColumnSet("name", "revenue"));
        return Ok(entity);
    }
}
```

## Configuration

Configure via `DataverseClientOptions`:

| Option | Required | Default | Description |
| --- | :---: | --- | --- |
| `OrganizationUrl` | ✔ | — | Base URL of your Dataverse environment (e.g. `https://my-org.crm4.dynamics.com`) |
| `TokenCredential` | | `DefaultAzureCredential` | Custom `TokenCredential` for authentication. Supports any Azure.Identity credential. |
| `DeferConnection` | | `false` | When `true`, connection to Dataverse is deferred until first use. |

### Authentication examples

**Default (system-assigned managed identity / local dev):**

```csharp
builder.Services.AddDataverseClient(options =>
{
    options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
});
```

**User-assigned managed identity:**

```csharp
builder.Services.AddDataverseClient(options =>
{
    options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
    options.TokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = "d0f19fa6-76ef-46cb-93ac-fcde5a4a6143"
    });
});
```

**Client secret:**

```csharp
builder.Services.AddDataverseClient(options =>
{
    options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
    options.TokenCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
});
```

### Environment-specific configuration

Bind the options directly from a configuration section:

```jsonc
// appsettings.Production.json
{
  "Dataverse": {
    "OrganizationUrl": "https://my-org-prod.crm4.dynamics.com",
    "DeferConnection": false
  }
}
```

```csharp
builder.Services.AddDataverseClient(builder.Configuration.GetSection("Dataverse"));
```

Only non-secret values bind from configuration. Authentication uses `DefaultAzureCredential` by
default; to supply a custom credential, layer it on with the options pattern:

```csharp
builder.Services.AddDataverseClient(builder.Configuration.GetSection("Dataverse"));
builder.Services.PostConfigure<DataverseClientOptions>(options =>
    options.TokenCredential = new ClientSecretCredential(tenantId, clientId, clientSecret));
```

## Keyed clients (multiple environments)

Use keyed registration when your application needs to connect to more than one Dataverse environment — for example a data migration that reads from a source org and writes to a target org.

Register each client with a string key:

```csharp
builder.Services.AddDataverseClient("source", options =>
{
    options.OrganizationUrl = new Uri("https://source.crm4.dynamics.com");
});

builder.Services.AddDataverseClient("target",
    builder.Configuration.GetSection("Dataverse:Target"));
```

Resolve via `[FromKeyedServices]`:

```csharp
public sealed class MigrationService(
    [FromKeyedServices("source")] IOrganizationServiceAsync2 source,
    [FromKeyedServices("target")] IOrganizationServiceAsync2 target)
{
    public async Task MigrateAsync()
    {
        // read from source, write to target
    }
}
```

Each keyed registration is fully independent — its own singleton `ServiceClient` and its own scoped `IOrganizationServiceAsync2`. Keyed and unkeyed registrations coexist without conflict.

## Why scoped `IOrganizationServiceAsync2`?

`ServiceClient` is registered as a **singleton** to share the underlying connection, metadata cache, and authentication token. However, using a single instance across concurrent requests can cause subtle threading issues.

`Clone()` creates a lightweight copy that shares the parent's connection pool but is safe for per-request use. This library registers `IOrganizationServiceAsync2` as **scoped**, so each request gets its own clone automatically.

## License

[MIT](../LICENSE)

