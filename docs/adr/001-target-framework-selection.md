# ADR-001: Target Framework Selection

## Status

**Accepted** - 2025-11-09

## Context

RawRabbit 2.x targets `netstandard1.5` and `net451` (multi-targeting). Both frameworks are long past end-of-life:
- .NET Standard 1.5: EOL (multiple years ago)
- .NET Framework 4.5.1: EOL April 2016

Modern .NET has evolved significantly since 2018 with .NET Core 3.x, .NET 5, 6, 7, 8, and 9.

### Options Considered

1. **Option A: .NET 8 (LTS) only** ✅ SELECTED
   - Single target framework
   - LTS support until November 2026
   - Stable, production-ready
   - Strong ecosystem support

2. **Option B: .NET 9 (STS) only**
   - Latest features
   - Standard Term Support (STS) - only 18 months
   - Support ends May 2025
   - Less stable for long-term projects

3. **Option C: Multi-target (.NET 8 + .NET 9)**
   - Flexibility for consumers
   - Increased complexity
   - More testing required
   - Minimal benefit (most consumers on LTS)

4. **Option D: Continue multi-targeting (netstandard2.0 + net8.0)**
   - Backward compatibility with .NET Framework 4.6.1+
   - Increased complexity
   - Limits use of modern .NET features
   - Perpetuates legacy support burden

## Decision

**We will target .NET 8 (LTS) only** (`<TargetFramework>net8.0</TargetFramework>`).

### Rationale

1. **LTS Support**: .NET 8 is LTS (Long-Term Support) with support until November 2026, giving ~2 years of support runway
2. **Simplicity**: Single target framework reduces complexity, testing burden, and build times
3. **Modern Features**: Full access to .NET 8 runtime improvements, C# 12 language features, and modern BCL APIs
4. **Performance**: .NET 8 runtime provides 10-30% performance improvements over .NET Standard/Framework
5. **Security**: .NET 8 receives active security patches and updates
6. **Industry Standard**: Most enterprises have migrated or are migrating to .NET 6/8 LTS versions
7. **Breaking Change Acceptable**: This is already a major version (3.0.0) with breaking changes, so dropping legacy frameworks is appropriate

### Consequences

**Positive**:
- ✅ Simpler codebase (no conditional compilation)
- ✅ Faster builds (single target)
- ✅ Better performance (.NET 8 runtime optimizations)
- ✅ Modern language features (C# 12, nullable reference types)
- ✅ Easier maintenance (one framework to test)
- ✅ Smaller NuGet packages (no multi-targeting)

**Negative**:
- ❌ **Breaking Change**: Consumers on .NET Framework 4.x cannot upgrade
- ❌ **Breaking Change**: Consumers on .NET Standard 2.0 projects cannot upgrade
- ❌ Migration required for all downstream consumers
- ❌ No backward compatibility with .NET Framework

**Mitigation**:
- Document clearly in CHANGELOG.md and MIGRATION-GUIDE.md
- Use semantic versioning (3.0.0 major version bump)
- Provide comprehensive migration guide
- Keep RawRabbit 2.x available on NuGet for legacy consumers

## References

- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy)
- [.NET 8 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [.NET Standard Versions](https://learn.microsoft.com/en-us/dotnet/standard/net-standard)
