# ADR-0019: API Versioning & Compatibility

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist (SPARC Stage 2)

**Reviewers**: TBD

**Tags**: migration, versioning, semver, breaking-changes, dotnet9

---

## Context

### Background

RawRabbit's current versioning and compatibility approach:
- Current version: v2.x (last release: 2.0.0)
- Semantic versioning used but not strictly enforced
- No formal deprecation policy
- Breaking changes introduced in minor versions historically
- Limited documentation of breaking changes
- All 32 packages versioned together (monolithic versioning)
- No migration guide for major version transitions

With the .NET 9 migration, we face:
- Significant breaking changes (sync API removal, ValueTask adoption)
- Need for deprecation period to ease user migration
- 32 NuGet packages that need coordinated versioning
- User expectations for clear upgrade paths
- Compliance with semantic versioning principles

### Problem Statement

How should we version RawRabbit packages during and after the .NET 9 migration to clearly communicate breaking changes, provide smooth upgrade paths, and maintain user trust while adhering to semantic versioning principles?

### Constraints

- **Semantic Versioning**: Must follow semver 2.0.0 principles strictly
- **User Impact**: Must minimize breaking changes where possible
- **Timeline**: Migration to .NET 9 requires v4.0 release
- **NuGet Compatibility**: Must work with NuGet package resolution
- **Documentation**: Must provide clear upgrade paths for each major version

### Assumptions

- Users understand semantic versioning basics (major.minor.patch)
- Most users are on v2.x currently (original RawRabbit)
- Users prefer gradual migration over big-bang changes
- Deprecation warnings help users prepare for breaking changes
- Clear communication reduces upgrade friction

---

## Decision

### Chosen Solution

**Implement strict semantic versioning with phased breaking changes:**

1. **Version 3.0 (Transition Release)** - .NET 8+ with deprecations
2. **Version 4.0 (Breaking Release)** - .NET 9+ with clean APIs
3. **Strict semver 2.0.0 compliance** for all future releases
4. **Formal deprecation policy** with 6-month minimum
5. **Comprehensive migration guides** for each major version
6. **Independent package versioning** (future) for flexibility

### Implementation Details

#### 1. Version Strategy Overview

```
v2.x (Current) → v3.0 (Transition) → v4.0 (Target)
       ↓                  ↓                 ↓
   .NET 4.5.2        .NET 8.0          .NET 9.0
   Mixed APIs      Deprecated APIs    Clean APIs
   Old patterns    + New patterns     New patterns only
```

#### 2. Version 3.0 - Transition Release

**Purpose**: Provide migration path with deprecation warnings

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Version>3.0.0</Version>
    <PackageVersion>3.0.0</PackageVersion>
    <AssemblyVersion>3.0.0.0</AssemblyVersion>
    <FileVersion>3.0.0.0</FileVersion>
  </PropertyGroup>
</Project>
```

**Changes in v3.0:**

```csharp
namespace RawRabbit
{
    public interface IBusClient : IDisposable, IAsyncDisposable
    {
        // ✅ NEW: Primary async APIs (using Task for v3.x compatibility)
        Task PublishAsync<T>(T message, CancellationToken ct = default);
        Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken ct = default);

        // ⚠️ DEPRECATED: Synchronous APIs (removed in v4.0)
        [Obsolete(
            "Synchronous Publish is deprecated and will be removed in v4.0. " +
            "Use PublishAsync instead.",
            error: false)] // Warning, not error
        void Publish<T>(T message);

        [Obsolete(
            "Synchronous Request is deprecated and will be removed in v4.0. " +
            "Use RequestAsync instead.",
            error: false)]
        TResponse Request<TRequest, TResponse>(TRequest request);
    }
}
```

**Deprecation Analyzer (Custom Roslyn Analyzer):**

```csharp
// RawRabbit.Analyzers/DeprecatedApiAnalyzer.cs
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DeprecatedApiAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "RAWRABBIT001",
        title: "Use async API instead of deprecated sync API",
        messageFormat: "'{0}' is deprecated and will be removed in v4.0. Use '{1}' instead.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Synchronous APIs are deprecated. Migrate to async/await patterns.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(
            AnalyzeInvocation,
            OperationKind.Invocation);
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // Check for deprecated sync methods
        if (method.Name is "Publish" or "Request" &&
            !method.Name.EndsWith("Async"))
        {
            var asyncName = $"{method.Name}Async";
            var diagnostic = Diagnostic.Create(
                Rule,
                invocation.Syntax.GetLocation(),
                method.Name,
                asyncName);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

#### 3. Version 4.0 - Breaking Release

**Purpose**: Clean, modern .NET 9 API surface

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Version>4.0.0</Version>
    <PackageVersion>4.0.0</PackageVersion>
    <AssemblyVersion>4.0.0.0</AssemblyVersion>
    <FileVersion>4.0.0.0</FileVersion>
  </PropertyGroup>
</Project>
```

**Changes in v4.0:**

```csharp
namespace RawRabbit
{
    public interface IBusClient : IAsyncDisposable
    {
        // ✅ BREAKING: ValueTask for performance (was Task in v3.x)
        ValueTask PublishAsync<T>(T message, CancellationToken ct = default);

        ValueTask<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken ct = default);

        // ✅ BREAKING: IAsyncEnumerable for streaming (new in v4.0)
        IAsyncEnumerable<T> SubscribeAsync<T>(
            string queueName,
            CancellationToken ct = default);

        // ❌ REMOVED: All synchronous methods deleted
        // ❌ REMOVED: IDisposable (only IAsyncDisposable remains)
    }
}
```

#### 4. Semantic Versioning Rules

**Strict adherence to semver 2.0.0:**

```
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]

Examples:
  4.0.0         - Major release (breaking changes)
  4.1.0         - Minor release (new features, backward compatible)
  4.1.1         - Patch release (bug fixes, backward compatible)
  4.0.0-alpha.1 - Pre-release (alpha)
  4.0.0-beta.2  - Pre-release (beta)
  4.0.0-rc.1    - Pre-release (release candidate)
```

**Version Bump Decision Matrix:**

| Change Type | Bump | Examples |
|------------|------|----------|
| Breaking change | MAJOR | Remove API, change signature, change behavior |
| New feature (backward compatible) | MINOR | Add new method, new optional parameter |
| Bug fix (backward compatible) | PATCH | Fix incorrect behavior, performance improvement |
| Documentation only | PATCH | README updates, code comments |
| Internal refactoring | PATCH | Internal changes, no API impact |

**Breaking Change Definition:**

A change is breaking if it:
1. Removes or renames public API members
2. Changes method signatures (parameters, return type)
3. Changes observable behavior in a way that could break existing code
4. Changes assembly strong name or namespace
5. Requires code changes in consuming applications

**NOT Breaking:**

1. Adding new public APIs
2. Adding optional parameters with defaults
3. Internal implementation changes
4. Performance improvements
5. Bug fixes that restore documented behavior

#### 5. Deprecation Policy

**Formal deprecation process:**

**Phase 1: Mark as Obsolete**
```csharp
[Obsolete(
    message: "This API is deprecated and will be removed in v{NEXT_MAJOR}. Use {REPLACEMENT} instead.",
    error: false)] // Warning only
public void DeprecatedMethod() { }
```

**Phase 2: Minimum Deprecation Period**
- **6 months minimum** between deprecation and removal
- Must span at least one minor version release
- Clearly documented in CHANGELOG and migration guide

**Phase 3: Removal**
```csharp
// Method completely removed in next major version
// No obsolete attribute - just gone
```

**Deprecation Communication:**
1. **NuGet Package Description**: "⚠️ Some APIs deprecated, see changelog"
2. **Compiler Warnings**: Obsolete attribute with message
3. **Release Notes**: "DEPRECATED" section
4. **Migration Guide**: Step-by-step replacement instructions
5. **Blog Post**: For major deprecations

#### 6. Breaking Changes Inventory (v2.x → v4.0)

**API Removals:**
```csharp
// ❌ REMOVED in v4.0 (deprecated in v3.0)
interface IBusClient
{
    void Publish<T>(T message);
    TResponse Request<TRequest, TResponse>(TRequest request);
    void Subscribe<T>(string queue, Action<T> handler);
}

// ❌ REMOVED: Synchronous disposal
public void Dispose()
```

**Signature Changes:**
```csharp
// v2.x / v3.x
Task PublishAsync<T>(T message);

// v4.0 (BREAKING: Task → ValueTask)
ValueTask PublishAsync<T>(T message, CancellationToken ct = default);
```

**Behavior Changes:**
```csharp
// v2.x / v3.x: Blocks on sync context
busClient.PublishAsync(msg).Wait(); // Could deadlock

// v4.0: Always uses ConfigureAwait(false)
await busClient.PublishAsync(msg); // Never deadlocks
```

**Namespace Changes:**
```csharp
// v2.x
using RawRabbit.Configuration;
using RawRabbit.Configuration.BasicPublish;

// v4.0 (BREAKING: Consolidated namespaces)
using RawRabbit.Configuration; // All configuration in one namespace
```

#### 7. Package Versioning Strategy

**Current: Monolithic Versioning (v2-v4)**
```
All 32 packages share same version:
- RawRabbit 4.0.0
- RawRabbit.Core 4.0.0
- RawRabbit.Operations.Publish 4.0.0
- ... (all 32 packages at 4.0.0)
```

**Future: Independent Versioning (v5+)**
```
Packages version independently:
- RawRabbit 5.0.0 (core APIs changed)
- RawRabbit.Core 5.0.0 (core infrastructure changed)
- RawRabbit.Operations.Publish 4.2.0 (no breaking changes)
- RawRabbit.DependencyInjection.Microsoft 3.1.5 (stable)
```

**Version Dependency Specifications:**
```xml
<!-- Core package -->
<PackageReference Include="RawRabbit.Core" Version="[4.0.0,5.0.0)" />
<!-- Allow any 4.x version, but not 5.x (breaking) -->

<!-- Stable extension -->
<PackageReference Include="RawRabbit.Operations.Publish" Version="4.*" />
<!-- Allow any 4.x version (backward compatible) -->
```

#### 8. Migration Guides

**Migration Guide Structure:**

```markdown
# RawRabbit v2.x → v3.0 Migration Guide

## Overview
- **Effort**: Low (2-4 hours for typical project)
- **Breaking Changes**: None (deprecations only)
- **Recommended Path**: Update now, fix warnings, prepare for v4.0

## Step 1: Update NuGet Packages
\`\`\`bash
dotnet add package RawRabbit --version 3.0.0
\`\`\`

## Step 2: Fix Compiler Warnings
\`\`\`csharp
// Before (v2.x)
busClient.Publish(message);

// After (v3.0)
await busClient.PublishAsync(message);
\`\`\`

## Step 3: Update Disposal Patterns
\`\`\`csharp
// Before (v2.x)
using (var client = CreateClient())
{
    // ...
}

// After (v3.0)
await using (var client = await CreateClientAsync())
{
    // ...
}
\`\`\`

## Common Migration Scenarios
[50+ code examples with before/after]

## Breaking Changes in Next Version (v4.0)
[Preview of upcoming changes]
```

### Rationale

**Why 3.0 transition release:**
- Gives users time to migrate gradually
- Compiler warnings guide migration (better than runtime errors)
- Reduces "surprise" breaking changes in v4.0
- Industry best practice (e.g., ASP.NET Core, Entity Framework)

**Why strict semver:**
- Clear communication of breaking changes
- Users can trust version numbers
- NuGet range specifications work correctly
- Industry standard, widely understood

**Why 6-month deprecation period:**
- Gives users time to plan migration
- Allows for LTS release overlap
- Balances user needs with development velocity
- Aligns with typical enterprise release cycles

**Why monolithic versioning (v2-v4):**
- Simpler during major migration (all packages move together)
- Easier for users to understand (no version matrix)
- Clearer communication of breaking changes
- Can transition to independent versioning later (v5+)

---

## Alternatives Considered

### Alternative 1: Big-Bang v3.0 with All Breaking Changes

**Description**: Skip transition release, go directly to v3.0 with all breaking changes.

**Pros**:
- Faster to implement (one release cycle)
- No need to maintain deprecated APIs
- Clean break from old patterns

**Cons**:
- Forces immediate migration for all users
- Higher risk of user frustration and abandonment
- No gradual migration path
- Surprise breaking changes hurt trust

**Why Rejected**: Too aggressive. Gradual migration with deprecation warnings is better for user experience and adoption.

### Alternative 2: Keep v2.x Forever (Never Break Compatibility)

**Description**: Maintain full backward compatibility, add new APIs alongside old.

**Pros**:
- Zero breaking changes, ever
- No migration required
- Happy users (in theory)

**Cons**:
- API surface grows indefinitely
- Technical debt accumulates
- Can't fix fundamental design issues
- Sync-over-async patterns persist
- Becomes unmaintainable over time

**Why Rejected**: Technical debt would cripple the library. Breaking changes are necessary for modernization.

### Alternative 3: Use Assembly Versioning for Breaking Changes

**Description**: Use AssemblyVersion changes instead of PackageVersion for breaking changes.

**Pros**:
- Strong-name signing forces recompilation
- .NET loader enforces compatibility

**Cons**:
- Confusing for users (package version ≠ assembly version)
- Doesn't work well with NuGet
- Makes dependency resolution harder
- Not industry standard for .NET libraries

**Why Rejected**: NuGet package versioning is the standard. Assembly versioning adds complexity without benefit.

---

## Consequences

### Positive Consequences

- **User Trust**: Clear communication of breaking changes builds trust
- **Smooth Migration**: Deprecation period allows gradual migration
- **Maintainability**: Clean v4.0 APIs reduce technical debt
- **Industry Alignment**: Semver is universal standard
- **Tooling Support**: NuGet, analyzers, and IDE understand semver

### Negative Consequences

- **Dual APIs**: v3.0 must maintain both old and new APIs (temporarily)
- **Deprecation Burden**: Must maintain deprecated code for 6 months
- **Documentation**: More documentation required (migration guides)
- **Support**: Must support multiple versions simultaneously

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Users resist migrating from v2.x | Medium | Medium | Clear value proposition, excellent migration guide |
| Breaking changes missed in inventory | Low | High | Thorough API review, automated API diff tools |
| v3.0 deprecation period too short | Low | Medium | Can extend if needed, monitor user feedback |
| Confusion between v3.0 and v4.0 | Medium | Low | Clear documentation, version comparison matrix |

### Technical Debt

- **Addressed**: Eliminates sync-over-async anti-patterns
- **Addressed**: Cleans up inconsistent APIs
- **Created**: Temporary (v3.0 only) - must maintain deprecated APIs for 6 months
- **Created**: Must document and track breaking changes meticulously

---

## Migration Impact

### Breaking Changes

**v2.x → v3.0 (None, deprecations only):**
- All v2.x code continues to work
- Compiler warnings for deprecated APIs
- No runtime breaking changes

**v3.0 → v4.0 (Major breaking changes):**
1. Removed all synchronous methods
2. Changed Task → ValueTask for hot paths
3. Removed IDisposable (IAsyncDisposable only)
4. Changed callback-based subscribe → IAsyncEnumerable
5. Consolidated namespaces

### Migration Path

**Phase 1: v2.x → v3.0 (Safe Upgrade)**

```bash
# Update packages
dotnet add package RawRabbit --version 3.0.0

# Build shows warnings (not errors)
dotnet build

# Warnings like:
# warning CS0618: 'IBusClient.Publish<T>(T)' is obsolete:
#   'Synchronous Publish is deprecated and will be removed in v4.0.
#    Use PublishAsync instead.'
```

**Phase 2: Fix Warnings (Prepare for v4.0)**

```csharp
// Example: Message Publishing
// Before (v2.x)
public void ProcessOrder(Order order)
{
    _busClient.Publish(order); // ⚠️ Warning in v3.0, ❌ Error in v4.0
}

// After (v3.0+)
public async Task ProcessOrderAsync(Order order)
{
    await _busClient.PublishAsync(order); // ✅ Works in v3.0 and v4.0
}

// Example: Request/Response
// Before (v2.x)
var response = _busClient.Request<GetUserRequest, GetUserResponse>(request);

// After (v3.0+)
var response = await _busClient.RequestAsync<GetUserRequest, GetUserResponse>(request);

// Example: Subscription
// Before (v2.x)
_busClient.Subscribe<OrderCreated>(order =>
{
    ProcessOrder(order);
});

// After (v4.0) - Changed to IAsyncEnumerable
await foreach (var order in _busClient.SubscribeAsync<OrderCreated>("orders"))
{
    await ProcessOrderAsync(order);
}
```

**Phase 3: Upgrade to v4.0 (Breaking)**

```bash
# Update packages
dotnet add package RawRabbit --version 4.0.0

# Build shows errors if any deprecated APIs still used
dotnet build

# Fix errors (all sync methods removed)
# Most fixes already done in Phase 2
```

### Backward Compatibility

**v3.0 Compatibility Matrix:**

| Code Written For | Works in v3.0 | Warnings |
|-----------------|---------------|----------|
| v2.x | ✅ Yes | ⚠️ Deprecation warnings for sync APIs |
| v3.0 (new APIs) | ✅ Yes | ✅ No warnings |

**v4.0 Compatibility Matrix:**

| Code Written For | Works in v4.0 | Notes |
|-----------------|---------------|-------|
| v2.x (sync APIs) | ❌ No | Must migrate to async APIs |
| v3.0 (async APIs, Task) | ⚠️ Mostly | ValueTask is mostly compatible with Task |
| v4.0 (async APIs, ValueTask) | ✅ Yes | Fully compatible |

**ValueTask Compatibility Note:**
```csharp
// v3.0 code using Task
Task<Response> oldMethod = client.RequestAsync<Req, Response>(req);
await oldMethod; // ✅ Works

// v4.0 returns ValueTask, but can be awaited same way
ValueTask<Response> newMethod = client.RequestAsync<Req, Response>(req);
await newMethod; // ✅ Works (no code change needed for await)

// Storing ValueTask in Task variable requires .AsTask()
Task<Response> task = newMethod.AsTask(); // Need explicit conversion
```

---

## Validation

### Acceptance Criteria

- [x] Semantic versioning policy documented
- [x] Deprecation policy documented (6-month minimum)
- [x] Breaking changes inventory completed (v2→v4)
- [x] Version 3.0 transition plan defined
- [x] Version 4.0 breaking release plan defined
- [x] Custom Roslyn analyzer design documented
- [ ] Migration guide v2→v3 written
- [ ] Migration guide v3→v4 written
- [ ] API diff tool integrated into CI
- [ ] All 32 packages updated with new versioning
- [ ] Release notes template created

### Testing Strategy

**API Compatibility Testing:**
```bash
# Use Microsoft.DotNet.ApiCompat for API diff analysis
dotnet tool install -g Microsoft.DotNet.ApiCompat

# Compare v2.x baseline with v3.0
apicompat -a v2.0.0/RawRabbit.dll -b v3.0.0/RawRabbit.dll

# Expected output: No breaking changes, new APIs added

# Compare v3.0 with v4.0
apicompat -a v3.0.0/RawRabbit.dll -b v4.0.0/RawRabbit.dll

# Expected output: Breaking changes listed (sync APIs removed, etc.)
```

**Roslyn Analyzer Testing:**
```csharp
// Test that analyzer detects deprecated API usage
[Fact]
public async Task Analyzer_DetectsDeprecatedPublish()
{
    var source = @"
using RawRabbit;

class Test {
    void Method(IBusClient client, object msg) {
        client.Publish(msg); // Should trigger RAWRABBIT001
    }
}";

    var expected = DiagnosticResult
        .CompilerWarning("RAWRABBIT001")
        .WithSpan(6, 9, 6, 32)
        .WithMessage("'Publish' is deprecated and will be removed in v4.0. Use 'PublishAsync' instead.");

    await VerifyAnalyzerAsync(source, expected);
}
```

**Migration Testing:**
```bash
# Create test project with v2.x APIs
dotnet new console -n MigrationTest
cd MigrationTest
dotnet add package RawRabbit --version 2.0.0

# Write code using v2.x APIs (sync methods)
# ... write test code ...

# Upgrade to v3.0, verify warnings appear
dotnet add package RawRabbit --version 3.0.0
dotnet build # Should show deprecation warnings

# Fix warnings, upgrade to v4.0
dotnet add package RawRabbit --version 4.0.0
dotnet build # Should compile successfully
```

### Rollback Plan

**If Critical Issues Found:**

1. **Phase 1 (v3.x)**: Extend deprecation period from 6 to 12 months
2. **Phase 2**: Un-deprecate APIs if user resistance is high (>50% negative feedback)
3. **Phase 3**: Release v3.5 with longer transition period
4. **Phase 4**: If complete rollback needed, maintain v3.x as LTS

**Rollback Triggers:**
- More than 50% of users report difficulty migrating
- Critical bugs in v4.0 that require API reversion
- Ecosystem incompatibility (major consumers can't upgrade)
- Security issues that require maintaining v3.x longer

---

## Dependencies

### Affected Components

**All 32 NuGet Packages:**
1. RawRabbit
2. RawRabbit.Core
3. RawRabbit.Channel
4. RawRabbit.Operations.Publish
5. ... (all 32 packages must follow version strategy)

**Supporting Projects:**
- RawRabbit.Analyzers (new - custom Roslyn analyzer)
- RawRabbit.ApiCompat (CI tool for API diff)
- RawRabbit.MigrationGuide (documentation project)

### Related ADRs

- [ADR-0001: Migration Strategy](./0001-migration-strategy.md) - Overall migration approach
- **ADR-0017: Async/Await Modernization** - Breaking changes detailed
- **ADR-0018: Test Framework Modernization** - Testing version changes
- **ADR-0020: Release & Deployment Strategy** - Release process

### External Dependencies

**NuGet Packages:**
- `Microsoft.CodeAnalysis.CSharp` (>= 4.9.0) - For Roslyn analyzer
- `Microsoft.DotNet.ApiCompat` (tool) - For API compatibility checks

**Tools:**
- API diff tools in CI pipeline
- NuGet package publishing automation
- Documentation generation (DocFX or similar)

---

## Timeline

**Proposed**: 2025-10-09

**Accepted**: TBD

**Implementation Start**: Stage 3 (Core Migration)

**Target Completion**: Stage 6 (Release & Documentation)

**Actual Completion**: TBD

**Milestones:**

**Q4 2025:**
- Week 1-2: Finalize breaking changes inventory
- Week 3-4: Develop Roslyn analyzer for deprecations
- Week 5-6: Implement v3.0 transition release
- Week 7: Alpha release of v3.0 for early feedback
- Week 8: Beta release of v3.0

**Q1 2026:**
- Month 1: v3.0 stable release
- Month 2-3: User migration period (6 months begins)
- Month 4: Collect feedback, write v4.0 migration guide

**Q2 2026:**
- Month 5-6: Implement v4.0 breaking changes
- Month 7: v4.0 alpha/beta releases

**Q3 2026:**
- Month 8: v4.0 RC release
- Month 9: v4.0 stable release (6 months after v3.0)

---

## References

### Documentation

- [Semantic Versioning 2.0.0](https://semver.org/)
- [Microsoft: Breaking Change Rules](https://learn.microsoft.com/en-us/dotnet/core/compatibility/)
- [NuGet Package Versioning](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning)
- [Roslyn Analyzer Development](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)

### Research

- [ASP.NET Core Breaking Changes Policy](https://github.com/dotnet/aspnetcore/blob/main/docs/Breaking-Changes.md)
- [Entity Framework Core Breaking Changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/breaking-changes)
- [JSON.NET Versioning Strategy](https://www.newtonsoft.com/json/help/html/VersioningStrategy.htm)

### Related Work

- Issue #XXX: Versioning and breaking changes tracking
- PR #XXX: Roslyn analyzer for deprecated APIs
- PR #XXX: API compatibility tests in CI

---

## Notes

**Version Numbering Clarity:**
- v3.0 = Transition (.NET 8, deprecated sync APIs, Task-based)
- v4.0 = Target (.NET 9, clean APIs, ValueTask-based)
- v5.0+ = Future (independent package versioning)

**Communication Strategy:**
1. **Blog Posts**: Announce v3.0 transition, explain v4.0 changes
2. **Release Notes**: Detailed changelog for each version
3. **Migration Guides**: Step-by-step instructions with code samples
4. **GitHub Discussions**: Community Q&A
5. **Twitter/Social**: Major version announcements

**Support Policy:**
- **v2.x**: Security fixes only (6 months after v3.0 release)
- **v3.x**: Full support (12 months after v4.0 release)
- **v4.x**: Current, full support

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2 |
