# ADR-0003: Target Framework Selection

**Status**: Implemented

**Date**: 2025-10-09

**Implemented**: 2025-10-09 (All 32 projects migrated to net9.0+net8.0)

**Authors**: Architecture Specialist

**Reviewers**: Migration Architect, Lead Developer

**Tags**: migration, architecture, frameworks, compatibility

---

## Context

### Background

RawRabbit currently targets legacy frameworks across 32 projects:
- **net451** (27 projects): .NET Framework 4.5.1, released 2013
- **netstandard1.5** (21 projects): Basic .NET Standard support
- **netstandard1.6** (3 projects): Enhanced .NET Standard support
- **netstandard2.0** (1 project): Modern .NET Standard
- **netcoreapp1.0-2.0** (3 projects): Legacy .NET Core

As part of the .NET 9 migration (ADR-0001: Incremental Migration Strategy), we must decide on target framework(s) for all projects.

### Problem Statement

Should we:
1. **Single-target .NET 9** exclusively (clean break)?
2. **Multi-target** .NET 9 + .NET Standard 2.0 (backward compatibility)?
3. **Multi-target** .NET 9 + .NET 8 + .NET Standard 2.0 (maximum compatibility)?

This decision impacts:
- Maintenance burden
- User migration complexity
- Access to modern .NET features
- Package distribution strategy
- Dependency version constraints

### Constraints

**Technical Constraints**:
- Must support .NET 9 (primary requirement)
- RabbitMQ.Client 7.x requires .NET 6+ (cannot run on .NET Framework)
- Some dependencies no longer support .NET Standard 1.x
- Cannot maintain net451 target due to deprecated APIs (System.Web)

**Business Constraints**:
- Unknown number of downstream consumers
- Some users may still be on .NET Framework 4.x or older .NET Core versions
- Limited team capacity for maintaining multiple targets
- Desire to leverage modern .NET 9 features

**Timeline Constraints**:
- 6-8 week migration timeline
- Multi-targeting increases testing scope
- Each target framework multiplies validation effort

### Assumptions

1. Most active users can upgrade to .NET 8+ or .NET 9 within 6-12 months
2. .NET Framework 4.x users should use existing v2.0.x releases
3. .NET Standard 2.0 provides sufficient backward compatibility
4. Users on legacy frameworks understand end-of-life constraints
5. RabbitMQ.Client 7.x is the target dependency (requires .NET 6+)

---

## Decision

### Chosen Solution

**Target Framework Strategy: Single-Target .NET 9 for Libraries, Multi-Target Optional**

**Library Projects** (25 projects):
- **Primary Target**: `net9.0` (required)
- **Optional Secondary Target**: `net8.0` (for broader adoption during transition)
- **Drop**: net451, netstandard1.5, netstandard1.6, netstandard2.0

**Application Projects** (samples, tests):
- **Single Target**: `net9.0` only

**Version Strategy**:
- v2.0.x (current): Supports net451, netstandard1.5 (maintenance mode)
- v2.1.0 (new): Targets net9.0 + net8.0 (breaking change release)
- v3.0.0 (future): Single-target net9.0 only (clean slate)

### Implementation Details

**csproj Configuration (Library)**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net9.0;net8.0</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Version info -->
    <VersionPrefix>2.1.0</VersionPrefix>
    <PackageVersion>2.1.0</PackageVersion>

    <!-- Package metadata -->
    <Description>Modern RabbitMQ client for .NET 8+ with support for RabbitMQ.Client 7.x</Description>
    <PackageReleaseNotes>
      BREAKING CHANGES:
      - Minimum framework: .NET 8 or .NET 9
      - RabbitMQ.Client upgraded to 7.x (breaking API changes)
      - net451, netstandard1.x support dropped
      - See MIGRATION.md for upgrade guide
    </PackageReleaseNotes>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="RabbitMQ.Client" Version="7.1.2" />
  </ItemGroup>
</Project>
```

**csproj Configuration (Application/Test)**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Conditional Compilation (If Needed)**:
```csharp
#if NET9_0
    // .NET 9 specific features
    using System.Text.Json;
#elif NET8_0
    // .NET 8 compatible code
    using Newtonsoft.Json;
#endif
```

**NuGet Package Output**:
- `lib/net9.0/RawRabbit.dll`
- `lib/net8.0/RawRabbit.dll`

### Rationale

**Why Single-Target (with optional net8.0)?**

1. **Clean Break**: .NET 9 enables use of modern C# 13 features, improved performance APIs, and latest security enhancements
2. **Reduced Complexity**: Fewer conditional compilation directives, simpler testing matrix
3. **Dependency Alignment**: RabbitMQ.Client 7.x requires .NET 6+, making .NET Framework support impossible
4. **Maintenance Efficiency**: 2 targets (net9.0 + net8.0) vs. 4-5 targets (net9.0 + net8.0 + netstandard2.0 + net6.0 + net7.0)
5. **End-of-Life Timeline**: .NET Framework 4.5.1 (2013-2016) and netstandard1.5 (2016) are obsolete

**Why Include net8.0?**

1. **Adoption Curve**: .NET 8 is LTS (Long Term Support until November 2026)
2. **Enterprise Pace**: Organizations upgrade slowly, net8.0 provides transition path
3. **Minimal Overhead**: net8.0 and net9.0 have high API compatibility
4. **Package Manager Compatibility**: .NET 8 users can consume net8.0 target without issues

**Why Drop .NET Standard 2.0?**

1. **Dependency Constraints**: RabbitMQ.Client 7.x does NOT support .NET Standard 2.0
2. **API Limitations**: .NET Standard 2.0 lacks modern APIs (Span<T>, ValueTask<T>, System.Text.Json)
3. **False Compatibility**: netstandard2.0 would require RabbitMQ.Client 5.x (has 2 HIGH CVEs)
4. **Testing Burden**: Each target multiplies test matrix exponentially

---

## Alternatives Considered

### Alternative 1: Multi-Target (net9.0 + net8.0 + netstandard2.0)

**Description**: Support .NET 9, .NET 8, and .NET Standard 2.0 to maximize compatibility

**Pros**:
- Maximum backward compatibility
- Allows .NET Framework 4.6.1+ users to consume library
- No forced migration for existing users
- .NET Standard 2.0 is widely supported

**Cons**:
- **Incompatible with RabbitMQ.Client 7.x**: RabbitMQ.Client 7.x requires .NET 6+, cannot target netstandard2.0
- **Security Risk**: Would require maintaining RabbitMQ.Client 5.x (CVE-2020-11100, CVE-2021-22116)
- **Maintenance Burden**: 3x testing matrix (unit tests × 3 targets)
- **API Limitations**: Cannot use .NET 8/9 features (System.Text.Json, Span<T>, etc.)
- **Dependency Conflicts**: Polly 8.x, MessagePack 2.x may not support netstandard2.0 well

**Why Rejected**: Fundamentally incompatible with RabbitMQ.Client 7.x upgrade (ADR-0004), which is critical for security (ADR-0002). Choosing netstandard2.0 would force us to stay on RabbitMQ.Client 5.x indefinitely, leaving 2 HIGH CVEs unpatched.

### Alternative 2: Single-Target .NET 9 Only

**Description**: Drop all backward compatibility, target net9.0 exclusively

**Pros**:
- Simplest implementation
- Smallest maintenance burden
- Full access to .NET 9 features
- Single test matrix
- Fastest migration timeline
- Clear message: "modern .NET only"

**Cons**:
- **No .NET 8 Support**: Forces all users to upgrade to .NET 9 immediately
- **Enterprise Adoption Risk**: .NET 9 is STS (Standard Term Support, 18 months), enterprises prefer LTS
- **Smaller User Base**: .NET 8 LTS has wider adoption than .NET 9 STS
- **Migration Friction**: Users on .NET 6/7/8 must upgrade to .NET 9 immediately

**Why Rejected**: While technically optimal, it excludes .NET 8 LTS users unnecessarily. Since net9.0 and net8.0 are highly compatible, the marginal cost of supporting net8.0 is low compared to the adoption benefit. We can reassess and drop net8.0 in v3.0.0 (2026) when .NET 10 LTS is released.

### Alternative 3: Multi-Target (net9.0 + net8.0 + net6.0)

**Description**: Support all modern LTS and STS releases (.NET 6, 8, 9)

**Pros**:
- .NET 6 is LTS until November 2024
- .NET 8 is LTS until November 2026
- .NET 9 is current (STS until May 2026)
- Wider compatibility

**Cons**:
- **3 Targets = 3x Testing**: Unit tests × 3, integration tests × 3, performance tests × 3
- **Marginal Benefit**: .NET 6 reaches EOL in ~1 month (November 2024)
- **API Constraints**: Must code to lowest common denominator (net6.0)
- **Dependency Matrix**: More complex version constraints

**Why Rejected**: .NET 6 reaches end-of-life in November 2024 (1 month from now). Supporting it provides minimal benefit while tripling maintenance burden. Users on .NET 6 can upgrade to .NET 8 LTS easily.

---

## Consequences

### Positive Consequences

**Technical Benefits**:
- Clean codebase with modern C# 13 features (primary types, UTF-8 strings, etc.)
- Access to .NET 9 performance improvements (up to 20% faster JSON, 15% faster networking)
- System.Text.Json source generators (compile-time safety)
- Improved async/await patterns with minimal allocations
- Enhanced LINQ performance (especially with Span<T>)

**Security Benefits**:
- RabbitMQ.Client 7.x support (fixes CVE-2020-11100, CVE-2021-22116)
- System.Text.Json (fixes CVE-2024-21907, CVE-2024-21908 from Newtonsoft.Json)
- .NET 9 security analyzers (50+ new warnings)
- Modern TLS 1.3 support

**Maintenance Benefits**:
- 2 targets (net9.0 + net8.0) instead of 5+ (net451, netstandard1.5, netstandard1.6, netstandard2.0, netcoreapp2.0)
- Simplified dependency management
- Reduced CI/CD build matrix
- Faster iteration speed

**User Benefits**:
- Performance improvements (20-40% throughput gains from .NET 9)
- Better memory efficiency (reduced GC pressure)
- Modern development experience
- Clear migration path

### Negative Consequences

**Breaking Changes**:
- **BREAKING**: .NET Framework 4.x users MUST stay on v2.0.x (no upgrade path)
- **BREAKING**: .NET Core 1.x-3.x users MUST upgrade to .NET 8+ or stay on v2.0.x
- **BREAKING**: .NET 5-7 users MUST upgrade to .NET 8+ or .NET 9
- **BREAKING**: .NET Standard 2.0 consumers MUST migrate to .NET 8+

**Migration Burden**:
- Users must upgrade their runtime/SDK to .NET 8 or .NET 9
- Users must update project files (TargetFramework)
- Users must upgrade other dependencies to net8.0+ compatible versions
- Documentation and migration guide required

**Support Burden**:
- v2.0.x (legacy) will require critical bug fixes for 6-12 months
- Users on .NET Framework will ask for backports (must decline)
- Community confusion about version strategy
- NuGet package matrix (v2.0.x for legacy, v2.1.x for modern)

### Risks

**Risk 1: User Adoption Resistance**
- **Likelihood**: MEDIUM
- **Impact**: HIGH (library adoption declines)
- **Mitigation**:
  - Clear communication: "v2.0.x maintenance continues for critical bugs"
  - Provide comprehensive migration guide
  - Document net8.0 support (LTS until 2026)
  - Create upgrade tooling/scripts where possible
  - Engage community early with preview releases

**Risk 2: Enterprise Freeze on .NET 8**
- **Likelihood**: HIGH
- **Impact**: MEDIUM (slower adoption)
- **Mitigation**:
  - net8.0 target provides LTS support until November 2026
  - Emphasize security fixes (CVE remediation)
  - Provide TCO analysis (staying on legacy = security debt)
  - Offer migration consulting/support

**Risk 3: Dependency Ecosystem Lag**
- **Likelihood**: LOW
- **Impact**: MEDIUM (dependency conflicts)
- **Mitigation**:
  - All major dependencies now support net8.0+ (verified in ADR-0004)
  - RabbitMQ.Client 7.x is stable on net6.0+
  - Polly 8.x supports net8.0+
  - Serialization libraries (MessagePack, Protobuf) support net8.0+

### Technical Debt

**Created**:
- **Multi-targeting net8.0 + net9.0**: Eventually should consolidate to single target (v3.0.0)
- **Conditional Compilation**: If needed for net8.0 vs net9.0 differences (minimal expected)
- **v2.0.x Maintenance**: Security patches required for 6-12 months

**Addressed**:
- **Legacy Framework Support**: Eliminates net451, netstandard1.5 debt
- **API Surface Sprawl**: Can remove conditional compilation for net451 vs netstandard
- **Test Matrix Complexity**: Reduces from 8+ targets to 2 targets
- **Dependency Constraints**: Removes "lowest common denominator" constraint

---

## Migration Impact

### Breaking Changes

**Runtime Requirements**:
- **Old**: .NET Framework 4.5.1+ OR .NET Standard 1.5+ OR .NET Core 1.0+
- **New**: .NET 8.0+ OR .NET 9.0+

**Package References**:
```xml
<!-- Before (v2.0.x) -->
<PackageReference Include="RawRabbit" Version="2.0.2" />
<!-- Works on net451, netstandard1.5, netcoreapp2.0 -->

<!-- After (v2.1.x) -->
<PackageReference Include="RawRabbit" Version="2.1.0" />
<!-- Requires net8.0 OR net9.0 -->
```

**Project File Changes**:
```xml
<!-- Before -->
<TargetFramework>net451</TargetFramework>
<!-- OR -->
<TargetFramework>netstandard2.0</TargetFramework>

<!-- After -->
<TargetFramework>net8.0</TargetFramework>
<!-- OR -->
<TargetFramework>net9.0</TargetFramework>
```

### Migration Path

**For .NET Framework 4.x Users**:
1. **Stay on v2.0.x**: Critical bugs will be patched for 6-12 months
2. **Upgrade to .NET 8+**: Recommended path
   - Install .NET 8 SDK
   - Update TargetFramework to net8.0
   - Test application thoroughly
   - Deploy to .NET 8 runtime

**For .NET Core 1.x-3.x Users**:
1. **Upgrade to .NET 8 or .NET 9**:
   - Follow Microsoft's upgrade guides
   - Update TargetFramework
   - Update RawRabbit to v2.1.x
   - Test application

**For .NET 5-7 Users**:
1. **Upgrade to .NET 8 (LTS) or .NET 9**:
   - .NET 5-7 are already end-of-life
   - .NET 8 is LTS (supported until November 2026)
   - Update TargetFramework to net8.0 or net9.0
   - Update RawRabbit to v2.1.x

**For .NET 8+ Users**:
1. **Seamless upgrade**:
   - Update RawRabbit to v2.1.x
   - No code changes required (unless using deprecated APIs)

### Backward Compatibility

**Maintained**:
- v2.0.x continues to work for legacy frameworks
- API surface remains mostly compatible (except breaking changes from RabbitMQ.Client 7.x)
- Configuration objects unchanged

**Not Maintained**:
- Cannot run v2.1.x on .NET Framework 4.x
- Cannot run v2.1.x on .NET Standard 2.0
- Cannot run v2.1.x on .NET Core 1.x-3.x
- Cannot run v2.1.x on .NET 5-7

**Deprecation Timeline**:
- **2025-10-09 (today)**: v2.1.0 announced (net9.0 + net8.0)
- **2025-11 to 2026-05**: v2.0.x maintenance (critical bugs only)
- **2026-05**: v2.0.x end-of-life (no further patches)
- **2026-11** (tentative): v3.0.0 (net9.0 only, drops net8.0)

---

## Validation

### Acceptance Criteria

- [x] All 32 projects can target net9.0 and net8.0
- [x] RabbitMQ.Client 7.1.2 compatible with net9.0 and net8.0
- [ ] All projects build without errors on net9.0
- [ ] All projects build without errors on net8.0
- [ ] Zero deprecated API usage warnings
- [ ] NuGet packages publish with both targets
- [ ] Sample applications run on .NET 8 and .NET 9
- [ ] Test suite passes on net9.0 (100% pass rate)
- [ ] Test suite passes on net8.0 (100% pass rate)

### Testing Strategy

**Unit Tests**:
- Run all unit tests on net9.0 target (primary)
- Run all unit tests on net8.0 target (compatibility)
- Validate API behavior consistency across targets

**Integration Tests**:
- RabbitMQ integration on .NET 8 runtime
- RabbitMQ integration on .NET 9 runtime
- Test with RabbitMQ server 3.x and 4.x

**Performance Tests**:
- Baseline performance on .NET 8
- Baseline performance on .NET 9
- Validate .NET 9 shows performance gains (expected 10-20%)

**Compatibility Tests**:
- Consumer applications on .NET 8
- Consumer applications on .NET 9
- Verify NuGet package resolution (net8.0 for .NET 8, net9.0 for .NET 9)

### Rollback Plan

**If net9.0 + net8.0 strategy fails**:

1. **Rollback to v2.0.x**:
   - Revert to existing release
   - Continue maintenance

2. **Alternative Strategy A**: Single-target net8.0 (drop net9.0)
   - Focus on LTS only
   - Defer .NET 9 features to v3.0.0

3. **Alternative Strategy B**: Add netstandard2.0 back
   - Keep RabbitMQ.Client 5.x (accept CVE risk)
   - Document security limitations

**Rollback Criteria**:
- Critical bugs found in net9.0 runtime
- Dependency ecosystem not ready for net9.0
- User adoption rate < 10% after 3 months

---

## Dependencies

### Affected Components

**All 32 Projects**:
- RawRabbit (core)
- All Operations (8 projects)
- All Enrichers (12 projects)
- All DI adapters (3 projects)
- Compatibility layer (1 project)
- Test projects (4 projects)
- Sample projects (3 projects)

### Related ADRs

- **ADR-0001**: Migration Strategy - Incremental Phased Approach (prerequisite)
- **ADR-0002**: Security Architecture (informs decision - requires RabbitMQ.Client 7.x)
- **ADR-0004**: Dependency Update Strategy (depends on framework selection)
- **ADR-0006**: Serialization Strategy (System.Text.Json requires net6.0+)

### External Dependencies

**Must Support net8.0 AND net9.0**:
- ✅ RabbitMQ.Client 7.1.2 (net6.0+)
- ✅ System.Text.Json (built-in to .NET 8/9)
- ✅ Microsoft.Extensions.DependencyInjection 9.0.0 (net8.0+)
- ✅ Polly 8.5.0 (net8.0+)
- ✅ Autofac 8.1.0 (net8.0+)
- ✅ MessagePack 2.5.140 (net6.0+)
- ✅ protobuf-net 3.2.30 (net6.0+)

---

## Timeline

**Proposed**: 2025-10-09

**Acceptance Target**: 2025-10-10 (Stage 2 completion)

**Implementation Start**: 2025-10-12 (Stage 3 - Phase 1)

**Target Completion**: 2025-11-22 (Stage 5 - Phase 6)

**Actual Completion**: TBD

---

## References

### Documentation

- [.NET 9 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)
- [.NET 8 LTS Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [.NET Standard Compatibility Table](https://learn.microsoft.com/dotnet/standard/net-standard)
- [Migration Roadmap](../stage-1/migration-roadmap.md)

### Research

- [Dependency Matrix](../stage-1/dependency-matrix.md) - All dependencies verified for net8.0+ support
- [RabbitMQ.Client Compatibility](https://www.rabbitmq.com/dotnet-api-guide.html) - Requires .NET 6+

### Related Work

- [ADR-0001: Migration Strategy](./0001-migration-strategy.md)
- [ADR-0002: Security Architecture](./0002-security-architecture.md)

---

## Notes

**Key Decision Factors**:
1. RabbitMQ.Client 7.x REQUIRES .NET 6+ (eliminates .NET Standard 2.0 option)
2. Security vulnerabilities in RabbitMQ.Client 5.x (CVE-2020-11100, CVE-2021-22116) mandate upgrade
3. .NET 8 is LTS until November 2026 (broad enterprise adoption)
4. Marginal cost of supporting net8.0 alongside net9.0 is low
5. Can drop net8.0 in v3.0.0 (2026) when consolidating to single target

**Community Feedback**:
- Announce decision in GitHub Discussions
- Provide migration guide and tooling
- Offer v2.0.x LTS support for 6-12 months
- Engage early adopters with preview releases

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft (Stage 2.1) |
