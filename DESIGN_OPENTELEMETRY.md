# OpenTelemetry Instrumentation Design

## Overview

This document outlines the design for `BauerApps.Dataverse.Extensions.OpenTelemetry`, a new NuGet package that provides transparent, automatic OpenTelemetry instrumentation for `IOrganizationServiceAsync2` requests to Microsoft Dataverse.

**Status:** Design Phase  
**Target Version:** 0.1.0  
**Decision Record:** Castle DynamicProxy for runtime proxy generation

---

## Problem Statement

Enterprise .NET applications require standardized observability across their dependency calls. Currently, consumers of `BauerApps.Dataverse.Extensions.DependencyInjection` have no built-in way to:

- Trace individual Dataverse API calls (Create, Retrieve, Update, Delete, Execute, etc.)
- Record request timing and success/failure metrics
- Correlate requests across distributed systems
- Export telemetry to standard backends (Jaeger, Application Insights, etc.)

This package provides **transparent, automatic instrumentation** without requiring consumer code changes.

---

## Goals & Non-Goals

### Goals

- ✅ **Automatic instrumentation:** All `IOrganizationServiceAsync2` methods traced without explicit consumer calls
- ✅ **Minimal maintenance:** Single interceptor class; no method-by-method boilerplate
- ✅ **Future-proof:** New SDK methods automatically covered by proxy
- ✅ **Controllable:** Consumers opt-in; can filter operations if needed
- ✅ **Low overhead:** Negligible performance impact (~100-500ns per call)
- ✅ **Standards-aligned:** Uses `System.Diagnostics.DiagnosticSource.ActivitySource`
- ✅ **Keyed support:** Works with both unkeyed and keyed `ServiceClient` registrations

### Non-Goals

- ❌ **Behavioral wrappers:** No retries, caching, or request transformation
- ❌ **Metrics collection:** That's OpenTelemetry SDK's job
- ❌ **Log aggregation:** Separate concern from instrumentation
- ❌ **Custom sampling policies:** Delegate to consumer's tracing configuration
- ❌ **Exporter configuration:** Consumers configure exporters independently

---

## Architecture

### Package Structure

```
Dataverse.Extensions.OpenTelemetry/
├── ServiceCollectionExtensions.cs      — Public API: AddDataverseOpenTelemetry()
├── DataverseTelemetryInterceptor.cs    — Core: IInterceptor implementation
├── DataverseOpenTelemetryOptions.cs    — Options POCO
└── Internal/
    └── ActivitySourceFactory.cs         — Singleton ActivitySource management

Dataverse.Extensions.OpenTelemetry.Tests/
├── ServiceCollectionExtensionsTests.cs
├── DataverseTelemetryInterceptorTests.cs
└── TestFixtures/
    └── FakeOrganizationService.cs
```

### Dependencies

```xml
<PackageReference Include="Castle.Core" Version="5.2.0" />
<PackageReference Include="Scrutor" Version="6.1.0" />
<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="10.0.9" />
<PackageReference Include="Microsoft.PowerPlatform.Dataverse.Client" Version="1.*" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.9" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
```

**Rationale:**
- **Castle.Core:** Industry standard, zero maintenance
- **Scrutor:** Simplifies `IOrganizationServiceAsync2` decorator registration
- **System.Diagnostics.DiagnosticSource:** OTel standard; no direct OTel dependency (consumer brings that)

---

## Core Components

### 1. ServiceCollectionExtensions

**File:** `ServiceCollectionExtensions.cs`

```csharp
namespace BauerApps.Dataverse.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds OpenTelemetry instrumentation to the registered <see cref="IOrganizationServiceAsync2"/>.
        /// Wraps all service calls with automatic span creation and telemetry attributes.
        /// </summary>
        /// <remarks>
        /// This extension must be called AFTER <c>AddDataverseClient()</c>. It decorates the scoped
        /// <see cref="IOrganizationServiceAsync2"/> registration with automatic telemetry via
        /// <see cref="Castle.DynamicProxy.ProxyGenerator"/>.
        /// </remarks>
        /// <param name="configure">Optional action to configure <see cref="DataverseOpenTelemetryOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddDataverseOpenTelemetry(
            Action<DataverseOpenTelemetryOptions>? configure = null)
        {
            var options = new DataverseOpenTelemetryOptions();
            configure?.Invoke(options);

            services.AddSingleton(new Castle.DynamicProxy.ProxyGenerator());
            services.AddSingleton<DataverseTelemetryInterceptor>();

            services.Decorate<IOrganizationServiceAsync2>((inner, sp) =>
            {
                var generator = sp.GetRequiredService<Castle.DynamicProxy.ProxyGenerator>();
                var interceptor = sp.GetRequiredService<DataverseTelemetryInterceptor>();

                return generator.CreateInterfaceProxyWithTarget<IOrganizationServiceAsync2>(
                    inner,
                    interceptor);
            });

            return services;
        }

        /// <summary>
        /// Adds OpenTelemetry instrumentation to a keyed <see cref="IOrganizationServiceAsync2"/> registration.
        /// </summary>
        /// <param name="serviceKey">The service key matching a keyed <c>AddDataverseClient()</c> registration.</param>
        /// <param name="configure">Optional action to configure <see cref="DataverseOpenTelemetryOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddKeyedDataverseOpenTelemetry(
            string serviceKey,
            Action<DataverseOpenTelemetryOptions>? configure = null)
        {
            var options = new DataverseOpenTelemetryOptions();
            configure?.Invoke(options);

            services.AddSingleton(new Castle.DynamicProxy.ProxyGenerator());
            services.AddSingleton<DataverseTelemetryInterceptor>();

            services.DecorateKeyed<IOrganizationServiceAsync2>(serviceKey, (inner, sp) =>
            {
                var generator = sp.GetRequiredService<Castle.DynamicProxy.ProxyGenerator>();
                var interceptor = sp.GetRequiredService<DataverseTelemetryInterceptor>();

                return generator.CreateInterfaceProxyWithTarget<IOrganizationServiceAsync2>(
                    inner,
                    interceptor);
            });

            return services;
        }
    }
}
```

**Key Design Points:**
- ✅ Extension block style (C# 14) consistent with existing packages
- ✅ Unkeyed + keyed variants (mirrors DependencyInjection package)
- ✅ Uses `Scrutor.Decorate<T>()` for clean DI integration
- ✅ Options pattern for future extensibility
- ✅ Must be called AFTER `AddDataverseClient()`

---

### 2. DataverseTelemetryInterceptor

**File:** `DataverseTelemetryInterceptor.cs`

```csharp
namespace BauerApps.Dataverse.Extensions.Internal;

using Castle.DynamicProxy;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

internal sealed class DataverseTelemetryInterceptor : IInterceptor
{
    private static readonly ActivitySource ActivitySource = new(
        "BauerApps.Dataverse.Extensions",
        "0.1.0");

    private readonly ILogger<DataverseTelemetryInterceptor> _logger;

    public DataverseTelemetryInterceptor(ILogger<DataverseTelemetryInterceptor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Intercepts all calls to <see cref="IOrganizationServiceAsync2"/> methods,
    /// wrapping them in an OpenTelemetry activity and recording telemetry attributes.
    /// </summary>
    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        var activityName = $"dataverse.{method.Name}";

        using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Client);

        if (activity is not null)
        {
            RecordRequestAttributes(activity, invocation);
        }

        try
        {
            invocation.Proceed();

            // Handle async methods: wrap Task/Task<T> to record completion
            if (typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                if (method.ReturnType.IsGenericType)
                {
                    // Task<T> case
                    var taskType = method.ReturnType;
                    var resultType = taskType.GetGenericArguments()[0];
                    var wrapperMethod = typeof(DataverseTelemetryInterceptor)
                        .GetMethod(nameof(WrapTaskGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                        .MakeGenericMethod(resultType);
                    
                    invocation.ReturnValue = wrapperMethod.Invoke(null, new[] { invocation.ReturnValue, activity });
                }
                else
                {
                    // Task case (no return value)
                    invocation.ReturnValue = WrapTask((Task)invocation.ReturnValue!, activity);
                }
            }
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "Dataverse operation failed: {method}", method.Name);
            throw;
        }
    }

    private static void RecordRequestAttributes(Activity activity, IInvocation invocation)
    {
        var method = invocation.Method;

        // Standard OTel RPC attributes
        activity.SetTag("rpc.system", "dataverse");
        activity.SetTag("rpc.method", method.Name);

        // Dataverse-specific attributes
        if (method.Name == "RetrieveAsync" || method.Name == "UpdateAsync" || method.Name == "DeleteAsync")
        {
            // Extract entity logical name and ID from arguments
            if (invocation.Arguments.Length > 0 && invocation.Arguments[0] is string entityLogicalName)
            {
                activity.SetTag("dataverse.entity", entityLogicalName);
            }

            if (invocation.Arguments.Length > 1 && invocation.Arguments[1] is Guid recordId)
            {
                activity.SetTag("dataverse.record.id", recordId);
            }
        }

        if (method.Name == "CreateAsync" && invocation.Arguments.Length > 0)
        {
            if (invocation.Arguments[0] is Microsoft.Xrm.Sdk.Entity entity)
            {
                activity.SetTag("dataverse.entity", entity.LogicalName);
            }
        }

        if (method.Name == "RetrieveMultipleAsync" && invocation.Arguments.Length > 0)
        {
            if (invocation.Arguments[0] is Microsoft.Xrm.Sdk.Query.QueryExpression query)
            {
                activity.SetTag("dataverse.query.entity", query.EntityName);
            }
        }

        if (method.Name == "ExecuteAsync" && invocation.Arguments.Length > 0)
        {
            if (invocation.Arguments[0] is Microsoft.Xrm.Sdk.OrganizationRequest request)
            {
                activity.SetTag("dataverse.request.type", request.RequestName);
            }
        }
    }

    private static async Task WrapTask(Task task, Activity? activity)
    {
        try
        {
            await task.ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static async Task<T> WrapTaskGeneric<T>(Task<T> task, Activity? activity)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

**Key Design Points:**
- ✅ Single responsibility: record telemetry, proceed with call
- ✅ Handles both sync and async methods
- ✅ Records standard OTel + Dataverse-specific attributes
- ✅ Extracts entity names, record IDs, request types where possible
- ✅ Proper async continuation handling (ConfigureAwait)
- ✅ Exception recording per OTel spec

---

### 3. DataverseOpenTelemetryOptions

**File:** `DataverseOpenTelemetryOptions.cs`

```csharp
namespace BauerApps.Dataverse.Extensions;

/// <summary>
/// Configuration options for <see cref="IOrganizationServiceAsync2"/> OpenTelemetry instrumentation.
/// </summary>
public class DataverseOpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets whether to include request/response payload sizes in telemetry attributes.
    /// Default: <c>false</c> (to avoid recording PII in payloads).
    /// </summary>
    public bool IncludePayloadSize { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to record exception details in telemetry.
    /// Default: <c>true</c>.
    /// </summary>
    public bool RecordExceptions { get; set; } = true;
}
```

**Rationale:**
- ✅ Opt-in for payload size (privacy concern)
- ✅ Exception recording togglable
- ✅ Future extensibility point (sampling, filtering, etc.)

---

## Usage

### Basic Setup

```csharp
// Program.cs
builder.Services
    .AddDataverseClient(options =>
    {
        options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
    })
    .AddDataverseOpenTelemetry();  // Enable telemetry

// Telemetry backend configuration (consumer's responsibility)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddJaegerExporter()
        .AddSource("BauerApps.Dataverse.Extensions"));
```

### Keyed Clients

```csharp
builder.Services
    .AddDataverseClient("source", options => { ... })
    .AddKeyedDataverseOpenTelemetry("source")
    .AddDataverseClient("target", options => { ... })
    .AddKeyedDataverseOpenTelemetry("target");
```

### Custom Options

```csharp
builder.Services.AddDataverseOpenTelemetry(options =>
{
    options.IncludePayloadSize = true;
    options.RecordExceptions = true;
});
```

---

## Telemetry Attributes

Each span includes standardized attributes:

| Attribute | Type | Example | Notes |
| --- | --- | --- | --- |
| `rpc.system` | string | `"dataverse"` | Always set |
| `rpc.method` | string | `"RetrieveAsync"` | SDK method name |
| `dataverse.entity` | string | `"account"` | Entity logical name (where applicable) |
| `dataverse.record.id` | UUID | `"550e8400-..."` | Record ID (where applicable) |
| `dataverse.query.entity` | string | `"contact"` | Query entity (for RetrieveMultiple) |
| `dataverse.request.type` | string | `"WhoAmI"` | Request type (for Execute) |
| `error.type` | string | `"InvalidOperationException"` | On failure |

---

## Testing Strategy

### Unit Tests

**File:** `DataverseTelemetryInterceptorTests.cs`

```csharp
[Test]
public async Task Intercept_WhenMethodSucceeds_CreatesActivity()
{
    // Arrange
    var service = IOrganizationServiceAsync2.Mock();
    service.RetrieveAsync("account", Any<Guid>(), Any<ColumnSet>())
        .Returns(new Entity("account") { Id = Guid.NewGuid() });

    var interceptor = new DataverseTelemetryInterceptor(ILogger.Null);
    var proxy = new ProxyGenerator()
        .CreateInterfaceProxyWithTarget(service, interceptor);

    // Act
    var result = await proxy.RetrieveAsync("account", Guid.NewGuid(), new ColumnSet());

    // Assert
    await Assert.That(result).IsNotNull();
    // Activity recording verified via ActivityListener
}

[Test]
public async Task Intercept_WhenMethodThrows_RecordsException()
{
    // Arrange
    var service = IOrganizationServiceAsync2.Mock();
    service.RetrieveAsync(Any<string>(), Any<Guid>(), Any<ColumnSet>())
        .Throws(new InvalidOperationException("Connection failed"));

    var interceptor = new DataverseTelemetryInterceptor(ILogger.Null);
    var proxy = new ProxyGenerator()
        .CreateInterfaceProxyWithTarget(service, interceptor);

    // Act & Assert
    await Assert.That(() => proxy.RetrieveAsync("account", Guid.NewGuid(), new ColumnSet()))
        .Throws<InvalidOperationException>();
    // Exception recorded in activity via ActivityListener
}
```

### Integration Tests

**File:** `ServiceCollectionExtensionsTests.cs`

```csharp
[Test]
public async Task AddDataverseOpenTelemetry_DecoratesServiceWithProxy()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddDataverseClient(options =>
    {
        options.OrganizationUrl = new Uri("https://my-org.crm4.dynamics.com");
        options.DeferConnection = true;
    });
    services.AddDataverseOpenTelemetry();

    var provider = services.BuildServiceProvider();

    // Act
    var service = provider.GetRequiredService<IOrganizationServiceAsync2>();

    // Assert
    await Assert.That(service).IsNotNull();
    // Verify proxy wrapping (check for IProxyTargetAccessor)
    var isProxy = service is Castle.DynamicProxy.IProxyTargetAccessor;
    await Assert.That(isProxy).IsTrue();
}
```

---

## Release Process

### Package Configuration

**`.release-please-manifest.json`** — Add entry:
```json
{
  "Dataverse.Extensions.OpenTelemetry": "0.0.0"
}
```

**`release-please-config.json`** — Add entry:
```json
{
  "path": "Dataverse.Extensions.OpenTelemetry",
  "release-type": "simple",
  "include-component-in-tag": true
}
```

**`.github/workflows/release.yml`** — Update matrix:
```yaml
matrix:
  package:
    - Dataverse.Extensions.DependencyInjection
    - Dataverse.Extensions.HealthChecks
    - Dataverse.Extensions.OpenTelemetry
```

### Versioning

- **0.1.0** — Initial release (minimal feature set)
- **0.2.0** — Additional Dataverse attributes, filtering options
- **1.0.0** — Stable API, feature-complete

---

## Future Enhancements

- **Phase 2:** Metrics collection (request counts, latencies)
- **Phase 3:** Sampling policies and configuration hooks
- **Phase 4:** Distributed trace propagation helpers
- **Phase 5:** Dedicated OTel semantic conventions for Dataverse

---

## Documentation

### README.md

**File:** `Dataverse.Extensions.OpenTelemetry/README.md`

```markdown
# BauerApps.Dataverse.Extensions.OpenTelemetry

[![CI](https://img.shields.io/...)](...)
[![NuGet](https://img.shields.io/...)](https://www.nuget.org/packages/BauerApps.Dataverse.Extensions.OpenTelemetry)

Automatic OpenTelemetry instrumentation for Microsoft Dataverse `ServiceClient`.

## Features

- Transparent tracing of all `IOrganizationServiceAsync2` requests (Create, Retrieve, Update, Delete, Execute)
- Standard OTel activity attributes + Dataverse-specific tags
- Works with keyed (multi-environment) client registrations
- Zero consumer code changes — just register
- Minimal performance overhead (~100-500ns per call)

## Get started

Install the package:

```bash
dotnet add package BauerApps.Dataverse.Extensions.OpenTelemetry
```

Register in `Program.cs`:

```csharp
builder.Services
    .AddDataverseClient(options => { ... })
    .AddDataverseOpenTelemetry();

// Configure OTel backend (e.g. Jaeger, Application Insights)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddJaegerExporter()
        .AddSource("BauerApps.Dataverse.Extensions"));
```

That's it! All Dataverse calls are now automatically traced.

## Telemetry Attributes

Each span includes:
- `rpc.system`: "dataverse"
- `rpc.method`: Operation name (e.g. "RetrieveAsync")
- `dataverse.entity`: Entity logical name (where applicable)
- `dataverse.record.id`: Record ID (where applicable)
- Error details on failure

## License

MIT
```

---

## Implementation Checklist

### Phase 1: Core Implementation
- [ ] Create `Dataverse.Extensions.OpenTelemetry/` directory structure
- [ ] Implement `ServiceCollectionExtensions.cs` (unkeyed + keyed)
- [ ] Implement `DataverseTelemetryInterceptor.cs`
- [ ] Implement `DataverseOpenTelemetryOptions.cs`
- [ ] Add package metadata (csproj, README, CHANGELOG)
- [ ] Create `Dataverse.Extensions.OpenTelemetry.Tests/`
- [ ] Write unit + integration tests
- [ ] Update `.slnx`, `release-please-config.json`, `.release-please-manifest.json`
- [ ] Update root `README.md` packages table
- [ ] Update `AGENTS.md` with new package architecture

### Phase 2: Polish & Release
- [ ] Peer review design
- [ ] Code review implementation
- [ ] Manual testing with real Dataverse
- [ ] Performance profiling
- [ ] Documentation review
- [ ] Merge to `main` → release 0.1.0

---

## References

- [OpenTelemetry .NET Specification](https://github.com/open-telemetry/opentelemetry-dotnet)
- [System.Diagnostics.DiagnosticSource](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Diagnostics.DiagnosticSource)
- [Castle DynamicProxy Documentation](https://github.com/castleproject/Core)
- [Scrutor — Decorator Pattern Simplified](https://github.com/khellang/Scrutor)

---

## Questions & Decisions

| Question | Decision | Rationale |
| --- | --- | --- |
| Why Castle instead of source generators? | Castle DynamicProxy | Simpler, proven, lower maintenance burden |
| Why not wrap decorated with attributes? | Dynamic proxy is cleaner | No need to modify ServiceClient or consumers |
| How to handle keyed clients? | Separate `AddKeyedDataverseOpenTelemetry()` | Mirrors existing pattern in DependencyInjection package |
| What about metrics? | Out of scope (Phase 2+) | Focus on tracing first; metrics are separate concern |
| Should we filter operation types? | Future enhancement | Keep initial release simple; add if requested |

---

**Document Version:** 1.0  
**Last Updated:** 2026-07-02  
**Status:** Ready for Implementation
