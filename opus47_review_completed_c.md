# Opus 4.7 Review -- Completed Items (Part C)

Continuation of opus47_review_completed_b.md. Items resolved/decided in Part C are logged here as they are closed out of opus47_review.md.

---

## C1. Mixed TFMs (CompressionLzma net9.0) -- ALREADY ADDRESSED (stale)

Review claimed `CompressionLzma/src/CompressionLzma/CompressionLzma.csproj:4` targets `net9.0`
while everything else is `net10.0`. Verified: the csproj now targets `net10.0` (line 4). All
13 csproj files in the repo target `net10.0`. No action needed -- the finding is stale.

---

## C1. Stale dep: System.ComponentModel.Annotations 5.0.0 in Api.csproj -- DONE (removed)

`Api.csproj` carried an explicit `<PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />`.
The only consumers are `[Required]` attributes in `Api/Entities/Credentials.cs` and
`Api/Entities/Prototypes/NamedEntity.cs`, whose types (`System.ComponentModel.DataAnnotations`)
ship in the net10.0 shared framework. Removed the package reference. Api project builds clean
(0 errors / 1 warning).

---

## C1. Heavy/dead client deps: NuGet.Packaging + NuGet.Protocol + CodeGeneration.Design -- DONE (removed)

`BlazorServer.csproj` referenced `NuGet.Packaging 7.6.0` and `NuGet.Protocol 7.6.0`. Grep
confirmed ZERO usages of `NuGet.*` anywhere in the client (.cs/.razor). Removing the direct
7.6.0 references initially SURFACED two new `NU1901` vulnerability warnings: a transitive
vulnerable `NuGet.Packaging`/`NuGet.Protocol 6.12.1` pulled in by
`Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2`. So the direct 7.6.0 refs had been
acting as an (accidental) transitive pin away from the vulnerable version.

Investigated `CodeGeneration.Design`: it is a design-time scaffolding tool
(`dotnet aspnet-codegenerator`) with no scaffolding usage anywhere in the repo (no build
scripts, no code). However, removing it broke the build because it was the *only* transitive
provider of `Newtonsoft.Json`, which a single file -- `Shared/FormDataDisplayUnit.razor` (the
unit Export modal) -- depended on (`JsonConvert.SerializeObject`, `IOptions<JsonSerializerSettings>`).

Resolution: converted `FormDataDisplayUnit.razor` to System.Text.Json (see entry below), then
removed all three packages (`NuGet.Packaging`, `NuGet.Protocol`,
`Microsoft.VisualStudio.Web.CodeGeneration.Design`). Client builds clean (0 errors / 12 warnings,
back to baseline; the transient NU1901 warnings are gone since nothing pulls the vulnerable
transitive `NuGet.*` anymore).

---

## C1 (related). FormDataDisplayUnit export modal: Newtonsoft -> System.Text.Json -- DONE (code change)

`Shared/FormDataDisplayUnit.razor` was the lone Newtonsoft.Json consumer in the entire client.
Its Export button serialized a `Unit` for manual save/import. The rest of the app (and the
`DataExporter`/`DataImporter` tools) uses System.Text.Json with the canonical Faemiyah options
(`ConfigurationUtilities.GetJsonSerializerOptions` / `ConfigureJsonSerializerOptions`:
`JsonStringEnumConverter`, ignore-nulls). The injected `IOptions<JsonSerializerSettings>` was
never even registered in DI (resolved to a default Newtonsoft instance).

Change: inject the already-DI-registered `IOptions<JsonSerializerOptions>` (per the user's
direction to use the DI-defined options rather than constructing options locally) and serialize
with `JsonSerializer.Serialize(_unit, _jsonSerializerOptions.Value)`. Removed `@using Newtonsoft.Json`.

Behavior change to verify in-browser: enums now serialize as STRINGS and null properties are
OMITTED (matching `DataExporter`/`DataImporter`). The previous Newtonsoft default wrote enums as
NUMBERS, which would not have round-tripped correctly through `DataImporter`
(`JsonStringEnumConverter`). Net effect is a consistency/correctness fix for manual unit export.

Build: client 0 errors / 12 warnings.

---

## C1. No global.json / RollForward -- NO-ACTION (by design)

Considered pinning the SDK via a root `global.json`. User builds exclusively inside containers
based on `dotnet:latest` (the `infra/sdk` image), so the build environment is already controlled
and intentionally tracks the latest SDK. Pinning an SDK version would conflict with that
floating-latest container strategy. No action -- environment reproducibility is handled by the
container base image rather than global.json.
---

## C1. Mixed package family versions / no Directory.Packages.props -- NO-ACTION

The flagged "mismatch" (`Microsoft.Orleans.* 10.1.0` vs `Microsoft.Extensions.* / SignalR.Client 10.0.8`)
is expected: Orleans versions on its own release cadence independently of the .NET runtime
libraries, so 10.1.0 and 10.0.8 co-existing is correct, not a real version conflict.

Central Package Management (a root `Directory.Packages.props`) was considered to consolidate the
duplicated versions across the 13 projects, but declined: the repo builds across multiple
contexts (separate slnx solutions plus per-project Dockerfiles), and the user did not want to
churn every project/Dockerfile setup to relocate versions into a central props file. The
`<Version>` of the Api/Common packages is also managed by the checked-in BuildPipeline rollversion
tool, which edits the csproj `<Version>` property directly. No action.
---

## C1. Custom NuGets checked in (CustomNugets/*.nupkg) -- NO-ACTION (inaccurate finding)

Verified `git ls-files CustomNugets` returns 0 tracked files -- the `CustomNugets` folder is a
local build-output cache (regenerated by `build.bat`), not committed to the repo. The review's
"repo bloat" claim is inaccurate. No action.
---

## C1. Compiled BuildPipeline binaries committed -- NO-ACTION (intentional)

`BuildPipeline/BuildPipeline.{dll,exe,runtimeconfig.json}` are tracked in git with no source. It is
the author's personal rollversion helper (invoked by `build_rollversion.bat`) that increments the
`<Version>` in `Api.csproj`/`Common.csproj`. `build.bat` itself documents it as "only for the
author's own use." It exists precisely to avoid editing the NuGet version by hand across 450+
version rolls. Kept as-is by user decision. No action.

---

## C1. No vulnerability / dependency scanning -- DEFERRED to C3 (CI)

No `dotnet list package --vulnerable`, Dependabot, Renovate, or CodeQL. This is a CI/automation
concern that belongs with C3 (".github/workflows is empty"). Tracked there rather than as a
separate C1 package item. (Note: the NU1901 transitive vulnerability that the dead-NuGet.* cleanup
briefly surfaced has already been eliminated by removing CodeGeneration.Design.)

C1 is now fully closed.
---

## C2. Project file / Directory.Build.props -- PARTIAL DONE + dispositions

DONE (code changes):
- Centralized the shared package/assembly metadata (`Authors=Warma`, `Company=Faemiyah`,
  `Product=BtDamageResolver`, `Copyright`, `RepositoryUrl`, `PackageProjectUrl`) into the root
  `Directory.Build.props`. Removed the duplicated copies from `Api.csproj` and `Common.csproj`.
  `CompressionLzma.csproj` keeps its own `Authors`/`Copyright`/`Product` (Igor Pavlov / 7-Zip),
  which override the props, so the third-party package metadata is unchanged.
- Fixed the stale `<Copyright>Faemiyah 2020</Copyright>` -> `Faemiyah 2020-2026` (now in one place).
- Removed the redundant `Condition="$(MSBuildProjectExtension) == '.csproj'"` on the SonarAnalyzer
  PackageReference (all projects are .csproj). SonarAnalyzer retained (user values it).
- Dropped `CompressionLzma.csproj`'s local `<RepositoryUrl>.../`</RepositoryUrl>` (trailing slash)
  so it inherits the canonical no-slash URL from props.
Verified: main solution builds 0 errors; generated Api nuspec shows authors/projectUrl/copyright
(2020-2026)/repository correctly inherited; LZMA nuspec keeps Igor Pavlov authors/copyright with
canonical URLs. CompressionLzma solution builds 0 errors.

NO-ACTION (per user stance -- avoid build friction / analyzer noise; net10.0 TFM already implies
the language version):
- `<Nullable>enable</Nullable>` -- user: NO (would surface hundreds of NRT warnings; major refactor).
- `<ImplicitUsings>enable</ImplicitUsings>` -- user: NO.
- `<LangVersion>latest</LangVersion>` -- redundant: the TFM (net10.0) already selects C# 14;
  `latest` could even opt into preview behavior.
- `<TreatWarningsAsErrors>`, `<AnalysisMode>`/`<AnalysisLevel>`, `<EnforceCodeStyleInBuild>`,
  `<GenerateDocumentationFile>` -- declined (noise / CS1591 doc spam / would block builds).
- Per-project `<None Update>` JSON boilerplate; `Silo.csproj` GC properties (work fine as project
  props); `BlazorServer.csproj` `<None Include>` vs `Update` -- low value, left as-is.

DEFERRED to C5 (Tests):
- `Tests.csproj` references only `Actors.csproj` (cannot test other projects without rebuild).
- `Tests.csproj` lacks `coverlet.collector`/`coverlet.msbuild` (no coverage instrumentation).

C2 closed (Tests items tracked under C5).