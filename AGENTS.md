# AGENTS.md

## Project overview

A small .NET 10 NuGet library (`BauerApps.Dataverse.Extensions.DependencyInjection`) that provides one-line DI registration for Microsoft Dataverse `ServiceClient`. The key design decision: `ServiceClient` is registered as a **singleton** (shares connection pool & metadata cache), while `IOrganizationServiceAsync2` is registered as **scoped** via `Clone()` (thread-safe per-request usage). Do not change these lifetimes.

## Scope & non-goals

The library has a single responsibility: **wire up `ServiceClient` and register it for DI** with the correct lifetimes and authentication. Keep it a thin wiring layer built only on the public, supported `ServiceClient` surface.

- **In scope**: DI registration (singleton client + scoped `Clone()`), authentication (Azure.Identity + options pattern), logger wiring, startup validation.
- **Non-goals**: behavioral wrappers/decorators over `IOrganizationService*` (request tagging/correlation, retries, caching, auditing, etc.), constructing credentials from primitive config fields (clientId/secret/etc. — accept a `TokenCredential` instead), and anything that depends on or reimplements `ServiceClient` internals or undocumented behavior.

Rationale: maintainability and resilience to changes in the floating `1.*` Dataverse client dependency. Cross-cutting request behavior is the consumer's responsibility. When in doubt, prefer *not* adding the feature.

## Architecture (4 source files)

- `ServiceCollectionExtensions.cs` — Public API surface. `AddDataverseClient()` overloads (one taking `Action<DataverseClientOptions>`, one taking `IConfiguration` to bind from a config section) using C# 14 `extension` blocks. Shared wiring/validation live in private static helpers (`AddDataverseClientCore`, `ValidateDataverseClientOptions`).
- `DataverseClientOptions.cs` — Options POCO with `OrganizationUrl` (required), `TokenCredential`, `DeferConnection`.
- `Internal/ServiceClientFactory.cs` — Singleton factory wiring `Azure.Identity` credentials into `ConnectionOptions.AccessTokenProviderFunctionAsync`. Marked `internal`, tested via `InternalsVisibleTo`.
- Root namespace is `BauerApps.Dataverse.Extensions` (set via `<RootNamespace>` in csproj, differs from folder name).

## Build & test

```powershell
dotnet build Dataverse.Extensions.slnx
dotnet test Dataverse.Extensions.slnx
```

Solution uses `.slnx` format (XML-based), not `.sln`. Tests use **TUnit** (not xUnit/NUnit/MSTest) — uses `[Test]` attribute and `await Assert.That(...)` fluent async assertions. The test runner is configured in `global.json` (`"runner": "Microsoft.Testing.Platform"`).

## Conventions

- **Namespace**: `BauerApps.Dataverse.Extensions` (library), `BauerApps.Dataverse.Extensions.Tests` (tests). Mirror `Internal/` subfolder in both.
- **Internal access**: `InternalsVisibleTo` is configured via `<AssemblyAttribute>` in csproj, not `AssemblyInfo.cs`.
- **Test structure**: Mirrors source layout. `ServiceCollectionExtensionsTests.cs` at root, `Internal/ServiceClientFactoryTests.cs` for internal classes. Tests use Arrange/Act/Assert with comments.
- **Test pattern**: All tests set `DeferConnection = true` to avoid real Dataverse connections. Use `FakeTokenCredential` (private nested class) when testing custom credentials.
- **C# 14 features**: Uses `extension` blocks (not classic `static` extension methods). Keep this style for new extensions.
- **Dependencies**: `Azure.Identity`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Options.ConfigurationExtensions` (for the `IConfiguration` binding overload), `Microsoft.PowerPlatform.Dataverse.Client`. The Dataverse client uses floating version `1.*`.

## CI/CD & versioning

- **Versioning**: Automated via [release-please](https://github.com/googleapis/release-please). Version is tracked in `.release-please-manifest.json` (keyed by package directory) and patched into csproj `<Version>` via the `<!-- x-release-please-version -->` marker comment.
- **Commit messages**: Use [Conventional Commits](https://www.conventionalcommits.org/) — `feat:` (minor bump), `fix:` (patch), `feat!:` or `BREAKING CHANGE` footer (major).
- **CI** (`.github/workflows/ci.yml`): Builds and tests on every push/PR to `main`.
- **Release** (`.github/workflows/release.yml`): On push to `main`, release-please opens/updates a Release PR. Merging it creates a GitHub release + tag, then publishes to NuGet via [trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) (OIDC via `NuGet/login@v1`, no long-lived API key).

## Key patterns

When adding new configuration options:
1. Add property to `DataverseClientOptions`
2. Add validation in `ServiceCollectionExtensions.AddDataverseClient()` via `.Validate()` if required
3. Wire it in `ServiceClientFactory.Create()` using the options pattern (`IOptions<T>`)
4. Test both the registration (service descriptor assertions) and the factory behavior

