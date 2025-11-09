# ADR-005: Versioning Strategy

## Status

**Accepted** - 2025-11-09

## Context

RawRabbit 2.x used version `2.0.0` (with RC releases like `2.0.0-rc5`). The modernization introduces significant breaking changes:

1. **Framework**: .NET Standard 1.5 / .NET Framework 4.5.1 → .NET 8 (drops legacy support)
2. **Dependencies**: 29 packages updated, many with breaking changes
3. **Features**: ZeroFormatter enricher removed
4. **APIs**: RabbitMQ.Client 6.x requires extensive code changes

### Semantic Versioning (SemVer) Reminder

**Format**: `MAJOR.MINOR.PATCH`
- **MAJOR**: Incompatible API changes (breaking changes)
- **MINOR**: Backwards-compatible new functionality
- **PATCH**: Backwards-compatible bug fixes

### Options Considered

1. **Option A: Bump to 3.0.0** ✅ SELECTED
   - Indicates major breaking changes
   - Follows semantic versioning
   - Clear signal to consumers
   - Industry standard practice

2. **Option B: Bump to 2.1.0** (minor version)
   - Minimizes perceived change
   - **INCORRECT** - violates SemVer (breaking changes)
   - Confuses consumers
   - Hides migration burden

3. **Option C: Bump to 2.0.1** (patch version)
   - **INCORRECT** - violates SemVer (breaking changes)
   - Extremely misleading
   - Could break production deployments
   - Unacceptable

4. **Option D: Jump to 4.0.0 or higher**
   - Skips version number
   - No technical reason
   - Confusing for users

## Decision

**We will version RawRabbit as 3.0.0**

### Version Bump Strategy

**All projects updated**:
```xml
<!-- BEFORE -->
<VersionPrefix>2.0.0</VersionPrefix>

<!-- AFTER -->
<VersionPrefix>3.0.0</VersionPrefix>
```

**Exception**: `RawRabbit.Compatibility.Legacy` uses `<Version>3.0.0-alpha</Version>` to indicate continued experimental status.

### Rationale

1. **Semantic Versioning Compliance**: Major version bump required for breaking changes
2. **Clear Communication**: Version 3.x signals "expect breaking changes"
3. **Ecosystem Expectations**: .NET ecosystem follows SemVer strictly
4. **NuGet Best Practices**: NuGet tooling understands SemVer
5. **Backwards Incompatibility**: Prevents accidental upgrades (NuGet won't auto-update major versions)

### What Constitutes "Breaking" in this Release

#### ✅ **BREAKING CHANGES** (require major version bump):

1. **Target Framework Change**:
   - Consumers on .NET Framework 4.5.1 CANNOT upgrade
   - Consumers on .NET Standard 1.5 CANNOT upgrade
   - **Impact**: 100% of .NET Framework users

2. **Removed Package**:
   - `RawRabbit.Enrichers.ZeroFormatter` no longer exists
   - **Impact**: Any code using `.UseZeroFormatter()` fails to compile

3. **Dependency Breaking Changes**:
   - RabbitMQ.Client 6.x has breaking APIs
   - Polly 8.x has breaking APIs
   - Autofac 8.x has breaking APIs
   - **Impact**: Custom code using these APIs will break

4. **API Surface Changes** (future):
   - When RabbitMQ.Client 6.x code changes are complete, public APIs may change
   - **Impact**: Unknown until code migration complete

#### ❌ **NOT BREAKING** (would allow minor version bump):

1. **Internal Implementation Changes**: Middleware pipeline internals (if changed)
2. **Performance Improvements**: Faster execution without API changes
3. **Bug Fixes**: Fixing existing broken behavior (patch-worthy)
4. **New Features**: Adding new enrichers (minor-worthy)
5. **Documentation**: README, CHANGELOG, migration guides

### Consequences

**Positive**:
- ✅ **Clear Communication**: Developers immediately understand this is a breaking release
- ✅ **Prevents Accidents**: NuGet won't auto-upgrade (major versions require explicit opt-in)
- ✅ **SemVer Compliance**: Follows industry standards
- ✅ **Search/Discovery**: Users can search for "RawRabbit 3" migration guides
- ✅ **Parallel Installs**: NuGet allows RawRabbit 2.x and 3.x side-by-side

**Negative**:
- ❌ **Migration Required**: Users cannot upgrade without code changes
- ❌ **Perceived Instability**: Some orgs avoid ".0" releases (can mitigate with 3.0.1 patch)
- ❌ **Documentation Fragmentation**: Must maintain 2.x and 3.x docs separately

### Version Lifecycle Plan

**RawRabbit 2.x** (frozen):
- No new features
- Security patches only (if critical)
- Remains on NuGet indefinitely
- Branch: `2.0` (for hotfixes)

**RawRabbit 3.x** (active):
- Target: .NET 8+
- Active development and maintenance
- Regular updates (3.0.1, 3.0.2, etc.)
- Branch: `main` or `3.0`

**Future RawRabbit 4.x** (speculative):
- Potential changes:
  - Drop .NET 8, target .NET 9+ only
  - Migrate to System.Text.Json
  - RabbitMQ.Client 7.x
  - Modern C# 13+ features

### Pre-Release Versioning (Not Used)

We considered pre-release tags but decided against them:

**Not Used**:
- `3.0.0-alpha.1` - Too early to release
- `3.0.0-beta.1` - Implies feature-incomplete
- `3.0.0-rc.1` - Implies near-ready (we're not)

**Reasoning**: The current state has incomplete code migration (RabbitMQ.Client 6.x code NOT updated). Publishing any version (even alpha) would mislead users. We should complete code migration before ANY release.

### Release Checklist (Future)

Before publishing 3.0.0 to NuGet:

- [ ] All code migrations complete (RabbitMQ.Client 6.x, Polly 8.x)
- [ ] 100% test pass rate (156+ tests)
- [ ] Integration tests passing with real RabbitMQ
- [ ] Performance benchmarks validate no regression
- [ ] CHANGELOG.md complete
- [ ] MIGRATION-GUIDE.md complete
- [ ] All ADRs written
- [ ] README.md updated
- [ ] NuGet package metadata updated
- [ ] Git tag: `v3.0.0`
- [ ] Release notes published
- [ ] Documentation website updated (if exists)

## Versioning for Individual Packages

RawRabbit has 25+ NuGet packages (one per project). All will be versioned identically:

| Package | Version |
|---------|---------|
| RawRabbit | 3.0.0 |
| RawRabbit.Operations.Publish | 3.0.0 |
| RawRabbit.Operations.Subscribe | 3.0.0 |
| RawRabbit.Enrichers.Polly | 3.0.0 |
| ... (all others) | 3.0.0 |
| RawRabbit.Enrichers.ZeroFormatter | (Not published) |

**Consistency Rationale**:
- All packages tested together
- All packages released together
- Easier version management
- Clearer for consumers

**Alternative Not Used**: Independent versioning (e.g., RawRabbit 3.0.0, Polly enricher 1.2.0)
- More complex
- Harder to track compatibility
- Not worth the effort

## Implementation

**Files Modified**:
- 24 source .csproj files: `<VersionPrefix>2.0.0</VersionPrefix>` → `<VersionPrefix>3.0.0</VersionPrefix>`
- 1 source .csproj file: `<Version>2.0.0-alpha</Version>` → `<Version>3.0.0-alpha</Version>` (Compatibility.Legacy)
- 4 test .csproj files: No version (not published)

**Status**: ✅ COMPLETE - All version numbers updated

## References

- [Semantic Versioning 2.0.0](https://semver.org/)
- [NuGet Package Versioning](https://docs.microsoft.com/en-us/nuget/concepts/package-versioning)
- [.NET Library Versioning](https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/versioning)
- [Breaking Change Rules](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/breaking-change-rules.md)
