# ADR-003: ZeroFormatter Enricher Removal

## Status

**Accepted** - 2025-11-09

## Context

RawRabbit 2.x includes `RawRabbit.Enrichers.ZeroFormatter`, which provides serialization using the ZeroFormatter library.

### ZeroFormatter Status

- **Last Update**: 2017 (8+ years ago)
- **NuGet Version**: 1.6.4 (frozen since 2017)
- **GitHub Activity**: Repository archived/unmaintained
- **Compatibility**: Does NOT support modern .NET
- **.NET Standard 1.6**: Last supported framework
- **Security**: 8 years of unpatched vulnerabilities
- **Community**: No active development or support

### Current Usage

The enricher allows users to opt-in to ZeroFormatter serialization:
```csharp
services.AddRawRabbit(cfg => cfg
    .UseZeroFormatter() // Enricher plugin
);
```

**Impact Analysis**:
- Small user base (ZeroFormatter is niche)
- MessagePack and Protobuf are superior alternatives
- Removing it is a **breaking change** for any users relying on it

### Options Considered

1. **Option A: Remove entirely** ✅ SELECTED
   - Clean break from abandoned dependency
   - Simplifies codebase
   - Breaking change documented
   - Users must migrate to MessagePack or Protobuf

2. **Option B: Update to maintained alternative (e.g., MemoryPack)**
   - MemoryPack is modern, high-performance
   - API incompatible with ZeroFormatter
   - Still a breaking change (different serialization format)
   - Adds maintenance burden for niche feature

3. **Option C: Keep but mark deprecated**
   - Perpetuates dependency on abandoned library
   - Security risk
   - Doesn't work with .NET 8 anyway
   - Defers inevitable removal

4. **Option D: Fork and maintain ZeroFormatter ourselves**
   - Massive effort (entire serialization library)
   - Not core to RawRabbit's mission
   - Low ROI (small user base)

## Decision

**We will remove RawRabbit.Enrichers.ZeroFormatter entirely**

### What was removed:
- Project: `src/RawRabbit.Enrichers.ZeroFormatter/`
- Files: ~5 C# files
- NuGet Package: `RawRabbit.Enrichers.ZeroFormatter`
- Dependency: `ZeroFormatter 1.6.4`

### Rationale

1. **Abandoned Dependency**: ZeroFormatter hasn't been updated since 2017 (8+ years)
2. **Security Risk**: 8 years of unpatched vulnerabilities
3. **.NET 8 Incompatibility**: Doesn't support modern .NET frameworks
4. **Superior Alternatives Exist**:
   - MessagePack-CSharp 2.x (actively maintained, high performance)
   - protobuf-net 3.x (actively maintained, widely adopted)
5. **Small User Base**: ZeroFormatter is niche; most users use JSON or Protobuf
6. **Clean Break**: Major version 3.0.0 is the right time for breaking changes
7. **Reduces Maintenance Burden**: One less enricher to maintain

### Consequences

**Positive**:
- ✅ Eliminates security risk from abandoned dependency
- ✅ Reduces codebase complexity
- ✅ One less package to maintain
- ✅ Cleaner dependency tree

**Negative**:
- ❌ **BREAKING CHANGE**: Users of `.UseZeroFormatter()` will experience compilation errors
- ❌ Users must migrate serialization format (data incompatibility)
- ❌ Requires code changes and possibly data migration for affected users

**Migration Path for Affected Users**:

Users have two options:

### Option 1: Migrate to MessagePack (Recommended)

**Why MessagePack**:
- High performance (similar to ZeroFormatter)
- Actively maintained
- .NET 8 compatible
- Mature ecosystem

**Code Changes**:
```csharp
// BEFORE (RawRabbit 2.x)
services.AddRawRabbit(cfg => cfg
    .UseZeroFormatter()
);

// AFTER (RawRabbit 3.0)
services.AddRawRabbit(cfg => cfg
    .UseMessagePack() // Switch enricher
);
```

**NuGet Packages**:
```xml
<!-- Remove -->
<PackageReference Include="RawRabbit.Enrichers.ZeroFormatter" Version="2.0.0" />

<!-- Add -->
<PackageReference Include="RawRabbit.Enrichers.MessagePack" Version="3.0.0" />
```

**Data Migration**:
- Old messages serialized with ZeroFormatter CANNOT be deserialized with MessagePack
- Options:
  1. Drain old queues before upgrading
  2. Run dual consumers temporarily (one on 2.x, one on 3.0)
  3. Accept message loss if acceptable

### Option 2: Migrate to Protobuf

**Why Protobuf**:
- Strong schema definition
- Cross-platform standard
- Excellent performance
- Actively maintained

**Code Changes**:
```csharp
// BEFORE (RawRabbit 2.x)
services.AddRawRabbit(cfg => cfg
    .UseZeroFormatter()
);

// AFTER (RawRabbit 3.0)
services.AddRawRabbit(cfg => cfg
    .UseProtobuf() // Switch enricher
);
```

**NuGet Packages**:
```xml
<!-- Remove -->
<PackageReference Include="RawRabbit.Enrichers.ZeroFormatter" Version="2.0.0" />

<!-- Add -->
<PackageReference Include="RawRabbit.Enrichers.Protobuf" Version="3.0.0" />
```

### Option 3: Stay on RawRabbit 2.x (Not Recommended)

If migrating serialization is too complex:
- Stay on RawRabbit 2.0.x
- Accept security risks and .NET Framework limitations
- Plan migration to MassTransit or other modern alternatives

## Communication Plan

**CHANGELOG.md**:
- ✅ Listed under "Removed Features" section
- ✅ Clear warning with migration options

**MIGRATION-GUIDE.md**:
- ✅ Dedicated section on ZeroFormatter removal
- ✅ Step-by-step migration instructions
- ✅ Code examples for MessagePack and Protobuf
- ✅ Data migration strategies

**NuGet Package**:
- RawRabbit.Enrichers.ZeroFormatter 2.x remains on NuGet (no deletion)
- No 3.0 version published (implicit removal)

## Rollback Plan

If removal causes unexpected issues:
1. **Cannot Rollback**: Removal is permanent for 3.0
2. **Alternative**: Users can stay on RawRabbit 2.x if ZeroFormatter is critical
3. **Community Fork**: If demand exists, community can fork and maintain separately

## Implementation Checklist

- [x] Remove `src/RawRabbit.Enrichers.ZeroFormatter/` directory
- [x] Remove from solution file (`RawRabbit.sln`)
- [x] Document removal in CHANGELOG.md
- [x] Document migration path in MIGRATION-GUIDE.md
- [x] Update README.md to remove ZeroFormatter references (if any)
- [ ] Announce in release notes
- [ ] Monitor feedback post-release

## References

- [ZeroFormatter GitHub (archived)](https://github.com/neuecc/ZeroFormatter)
- [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp)
- [protobuf-net](https://github.com/protobuf-net/protobuf-net)
- [MemoryPack (modern alternative)](https://github.com/Cysharp/MemoryPack)
