# Dataverse.Extensions.DependencyInjection

[![CI](https://img.shields.io/github/actions/workflow/status/LarsBauer/dataverse-serviceclient-extensions/ci.yml?branch=main)](https://github.com/LarsBauer/dataverse-serviceclient-extensions/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BauerApps.Dataverse.Extensions.DependencyInjection)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.DependencyInjection)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BauerApps.Dataverse.Extensions.DependencyInjection)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.DependencyInjection)
[![License](https://img.shields.io/github/license/LarsBauer/dataverse-serviceclient-extensions)](LICENSE)

Dependency injection extensions for [Microsoft Dataverse ServiceClient](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/dataverse-sdk-for-net). Registers a singleton `ServiceClient` and a scoped `IOrganizationServiceAsync2` (via `Clone()`) — the correct pattern most people get wrong.

## Features

- One-line DI registration for `ServiceClient` with proper singleton + scoped `Clone()` lifecycle
- Authentication via [Azure.Identity](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme) (`DefaultAzureCredential` by default, any `TokenCredential` supported)
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

```json
// appsettings.Production.json
{
  "Dataverse": {
    "OrganizationUrl": "https://my-org-prod.crm4.dynamics.com"
  }
}
```

```csharp
builder.Services.AddDataverseClient(options =>
{
    options.OrganizationUrl = new Uri(builder.Configuration["Dataverse:OrganizationUrl"]!);
});
```

## Why scoped `IOrganizationServiceAsync2`?

`ServiceClient` is registered as a **singleton** to share the underlying connection, metadata cache, and authentication token. However, using a single instance across concurrent requests can cause issues (e.g., `CallerId` leaking between requests).

`Clone()` creates a lightweight copy that shares the parent's connection pool but is safe for per-request use. This library registers `IOrganizationServiceAsync2` as **scoped**, so each request gets its own clone automatically.

## License

[MIT](LICENSE)
