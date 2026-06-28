# AGENTS.md

## Project overview

A monorepo containing two .NET 10 NuGet libraries for Microsoft Dataverse:

- **`BauerApps.Dataverse.Extensions.DependencyInjection`** — one-line DI registration for `ServiceClient`. The key design decision: `ServiceClient` is a singleton (shared connection + token cache) while `IOrganizationServiceAsync2` is scoped (per-request `Clone()`).
- **`BauerApps.Dataverse.Extensions.HealthChecks`** — ASP.NET Core health check that verifies Dataverse connectivity by executing a `WhoAmI` request against the registered `ServiceClient`.

The two packages are **peers**: HealthChecks does not depend on DependencyInjection. Both reference `Microsoft.PowerPlatform.Dataverse.Client` independently. Consumers typically use both together.

## Scope & non-goals

Each library has a single responsibility. Keep them thin wiring layers built only on the public, supported Dataverse SDK surface.

- **DependencyInjection — in scope**: DI registration (singleton client + scoped `Clone()`), keyed (multi-environment) client registration, authentication (Azure.Identity + options pattern), logger wiring, startup validation.
- **HealthChecks — in scope**: `IHealthCheck` implementation executing `WhoAmI`, `IHealthChecksBuilder` extension methods (unkeyed + keyed), result data (UserId, OrganizationId, BusinessUnitId).
- **Non-goals (both packages)**: behavioral wrappers/decorators over `IOrganizationService*` (retries, caching, auditing, etc.), constructing credentials from primitive config fields, FetchXml helpers, entity mapping.

When in doubt, prefer *not* adding something.

## Architecture

### DependencyInjection (`Dataverse.Extensions.DependencyInjection/`)

- `ServiceCollectionExtensions.cs` — Public API. `AddDataverseClient()` overloads: unkeyed (action + `IConfiguration`) and keyed (same with leading `string key`). C# 14 `extension` block style.
- `DataverseClientOptions.cs` — Options POCO: `OrganizationUrl` (required), `TokenCredential`, `DeferConnection`.
- `Internal/ServiceClientFactory.cs` — Factory: `Create` (unkeyed, `IOptions<T>`) and `CreateKeyed` (`IOptionsMonitor<T>.Get(key)`). Marked `internal`, tested via `InternalsVisibleTo`.
- Root namespace: `BauerApps.Dataverse.Extensions`.

### HealthChecks (`Dataverse.Extensions.HealthChecks/`)

- `HealthChecksBuilderExtensions.cs` — Public API. `AddDataverseHealthCheck()` (unkeyed) and `AddKeyedDataverseHealthCheck(serviceKey, ...)` (keyed) on `IHealthChecksBuilder`. C# 14 `extension` block style.
- `DataverseHealthCheck.cs` — `IHealthCheck` implementation. Constructor takes `IOrganizationServiceAsync2`; the registration factory always passes the singleton `ServiceClient` (which implements the interface), keeping the lifetime safe while allowing the interface to be mocked in tests. Uses `context.Registration.FailureStatus` for failures. Returns UserId/OrganizationId/BusinessUnitId in result data.
- Root namespace: `BauerApps.Dataverse.Extensions`.

## Build & test

```powershell
dotnet build Dataverse.Extensions.slnx
dotnet test Dataverse.Extensions.slnx
```

Solution uses `.slnx` format (XML-based). Tests use **TUnit** — `[Test]` attribute and `await Assert.That(...)` fluent async assertions.

## Conventions

- **Namespace**: `BauerApps.Dataverse.Extensions` (libraries), `BauerApps.Dataverse.Extensions.Tests` (test projects).
- **Internal access**: `InternalsVisibleTo` via `<AssemblyAttribute>` in csproj, not `AssemblyInfo.cs`.
- **Test structure**: Mirrors source layout. Tests use Arrange/Act/Assert with comments.
- **Test pattern (DI)**: Set `DeferConnection = true` to avoid real connections. Use `FakeTokenCredential` (private nested class) for custom credential tests.
- **Test pattern (HealthChecks)**: Use `IOrganizationServiceAsync2.Mock()` (TUnit.Mocks source-generated) to mock the service. Set up return values with `.Returns(...)` and exceptions with `.Throws(...)`. Use `Any<T>()` for argument matching.
- **Test naming**: Method name directly for unkeyed (e.g. `AddDataverseHealthCheck_RegistersHealthCheck`), `_Keyed_` segment for keyed (e.g. `AddKeyedDataverseHealthCheck_DefaultsNameToServiceKey`).
- **C# 14 features**: Uses `extension` blocks inside a `public static class`. Keep this style for new extensions.
- **Per-package READMEs**: Each package directory has its own `README.md` that is packed into the NuGet. The root `README.md` is a repo-level overview only.
- **Documentation**: Both `README.md` files (root + per-package) and `AGENTS.md` **must be updated** when features change.

## CI/CD & versioning

- **Versioning**: Automated via [release-please](https://github.com/googleapis/release-please). Each package has its own version tracked in `.release-please-manifest.json`. Never edit versions or `CHANGELOG.md` manually.
- **Tags**: Both packages use `"include-component-in-tag": true`, giving namespaced tags: `Dataverse.Extensions.DependencyInjection-vX.Y.Z` and `Dataverse.Extensions.HealthChecks-vX.Y.Z`.
- **Commit messages**: [Conventional Commits](https://www.conventionalcommits.org/). release-please tracks which package directory is touched and only bumps the relevant package.
- **CI** (`.github/workflows/ci.yml`): Builds and tests the full solution on every push/PR to `main`.
- **Release** (`.github/workflows/release.yml`): release-please opens/updates Release PRs per package. The `publish` job uses a **matrix strategy** gated on `releases_created` at the job level and `matrix.release_created` per step. Adding a new package requires two new output lines in the `release-please` job and one new matrix entry in `publish`.

## Release process

**Configuration**:

- `release-please-config.json` — one entry per package directory, `release-type: simple`, `include-component-in-tag: true`.
- `.release-please-manifest.json` — current released version per package directory. Maintained by release-please.

**Commit message rules**:

| Commit prefix | Version effect |
| --- | --- |
| `feat:` | minor bump |
| `fix:` | patch bump |
| `feat!:` / `BREAKING CHANGE:` footer | major bump |
| `docs:`, `chore:`, `test:`, `refactor:`, `ci:`, `build:` | no release |

Scopes are optional (e.g. `feat(healthchecks): ...`). Use **rebase-and-merge** to preserve individual commits when a PR contains multiple conventional commit types (e.g. a structural `chore:` alongside a `feat!:`). Use squash-merge only when all changes belong to a single conventional commit.

**Typical workflow**:

1. Push commits touching one or both package directories.
2. release-please opens/updates Release PRs for affected packages.
3. Merge the Release PR → tag(s) created.
4. Matrix `publish` job runs only for packages whose `release_created` flag is `true`.

## Key patterns

**Adding a new configuration option (DI package)**:
1. Add property to `DataverseClientOptions`
2. Add `.Validate()` in `ServiceCollectionExtensions` if required
3. Wire in `ServiceClientFactory` (`IOptions<T>` unkeyed, `IOptionsMonitor<T>` keyed)
4. Test registration and factory behavior
5. Update `Dataverse.Extensions.DependencyInjection/README.md` and `AGENTS.md`

**Adding a new package to the repo**:
1. Create `Dataverse.Extensions.<Name>/` and `Dataverse.Extensions.<Name>.Tests/` directories
2. Add both projects to `Dataverse.Extensions.slnx`
3. Add package entry to `release-please-config.json` with `include-component-in-tag: true`
4. Add version entry to `.release-please-manifest.json` (start at `0.0.0`)
5. Add two output lines to the `release-please` job in `.github/workflows/release.yml`
6. Add one matrix entry to the `publish` job in `.github/workflows/release.yml`
7. Update root `README.md` packages table and `AGENTS.md`

**Keyed registrations (DI package)**:
- Use `AddKeyedSingleton` / `AddKeyedScoped` — never reuse the unkeyed core path
- Use `IOptionsMonitor<T>.Get(key)` — never `IOptions<T>` for keyed registrations
- Keep unkeyed and keyed paths fully independent to avoid `Options.DefaultName` collisions
