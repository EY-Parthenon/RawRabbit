# ADR-0008: ZeroFormatter Deprecation Strategy

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist

**Reviewers**: Migration Architect, Lead Developer

**Tags**: migration, deprecation, serialization, breaking-change

---

## Context

### Background

RawRabbit includes **RawRabbit.Enrichers.ZeroFormatter**, which provides serialization using the **ZeroFormatter** library (version 1.6.4).

From Stage 1 assessment (dependency-matrix.md):
- **ZeroFormatter** was **archived in 2018** (7+ years ago)
- No support for **.NET Core 3.0+**
- Last commit: 2018
- No .NET 9 support
- GitHub repository shows "This repository has been archived by the owner on Nov 29, 2018. It is now read-only."

**Current Status**:
- Package still available on NuGet (frozen at 1.6.4)
- No security updates
- No bug fixes
- No .NET Core 3.0+ support

### Problem Statement

Should RawRabbit continue to support an **abandoned serialization library** with no .NET 9 compatibility?

**Key Questions**:
1. Should we deprecate RawRabbit.Enrichers.ZeroFormatter entirely?
2. What replacement should we recommend?
3. What is the migration path for existing users?
4. What is the deprecation timeline?

### Constraints

**Technical Constraints**:
- ZeroFormatter 1.6.4 does NOT support .NET Core 3.0+
- Cannot target net9.0 with ZeroFormatter dependency
- No maintainer to fix compatibility issues

**Security Constraints**:
- No security patches available
- Potential vulnerabilities unpatched
- Risk increases over time

**Timeline Constraints**:
- Must decide by Stage 2 (Week 2)
- Implementation in Phase 3 (Week 4-5)

---

## Decision

### Chosen Solution

**Remove RawRabbit.Enrichers.ZeroFormatter from Solution Entirely**

**Rationale**:
- Library is abandoned (7+ years)
- No .NET 9 support
- Security risk (no patches)
- Better alternatives available (MessagePack, protobuf-net)

**Actions**:
1. **Remove project from solution** (immediate)
2. **Document deprecation** in CHANGELOG and migration guide
3. **Recommend alternatives**: MessagePack or protobuf-net
4. **Provide migration guide** for existing users

### Implementation Details

#### Removal from Solution

```bash
# Remove project
rm -rf src/RawRabbit.Enrichers.ZeroFormatter/

# Remove from solution file
dotnet sln remove src/RawRabbit.Enrichers.ZeroFormatter/RawRabbit.Enrichers.ZeroFormatter.csproj
```

#### CHANGELOG Entry

```markdown
## [2.1.0] - 2025-11-22

### BREAKING CHANGES

#### RawRabbit.Enrichers.ZeroFormatter Removed

**Reason**: ZeroFormatter library archived in 2018, no .NET Core 3.0+ support

**Impact**: Users of RawRabbit.Enrichers.ZeroFormatter must migrate to alternative serializers

**Migration Path**:
1. **Recommended: MessagePack** (fastest, most compatible)
   - Install: `dotnet add package RawRabbit.Enrichers.MessagePack`
   - 2-3x faster than ZeroFormatter
   - Active maintenance, .NET 9 support

2. **Alternative: protobuf-net** (industry standard)
   - Install: `dotnet add package RawRabbit.Enrichers.Protobuf`
   - Google Protocol Buffers implementation
   - Wide compatibility, excellent tooling

3. **Alternative: System.Text.Json** (built-in, recommended for new projects)
   - No package required (.NET 9 built-in)
   - Best performance for JSON workloads

See [Migration Guide](docs/migration-guides/zeroformatter-migration.md) for details.
```

#### Migration Guide

**docs/migration-guides/zeroformatter-migration.md**:
```markdown
# ZeroFormatter Migration Guide

## Overview

RawRabbit.Enrichers.ZeroFormatter has been removed in v2.1.0 because:
- ZeroFormatter library archived in 2018 (no active development)
- No .NET Core 3.0+ support (cannot run on .NET 9)
- No security updates
- Better alternatives available

## Recommended Alternative: MessagePack

MessagePack is the recommended replacement for ZeroFormatter users.

### Why MessagePack?

- **Performance**: 2-3x faster than ZeroFormatter
- **Compatibility**: .NET 9 support, active maintenance
- **Features**: Similar binary serialization, smaller payload sizes
- **Ecosystem**: Wide adoption, good tooling

### Migration Steps

#### 1. Install MessagePack Enricher

```bash
dotnet remove package RawRabbit.Enrichers.ZeroFormatter
dotnet add package RawRabbit.Enrichers.MessagePack
```

#### 2. Update Message Attributes

```csharp
// OLD (ZeroFormatter)
using ZeroFormatter;

[ZeroFormattable]
public class UserEvent
{
    [Index(0)]
    public virtual string UserId { get; set; }

    [Index(1)]
    public virtual DateTime Timestamp { get; set; }
}

// NEW (MessagePack)
using MessagePack;

[MessagePackObject]
public class UserEvent
{
    [Key(0)]
    public string UserId { get; set; }

    [Key(1)]
    public DateTime Timestamp { get; set; }
}
```

#### 3. Update Client Registration

```csharp
// OLD (ZeroFormatter)
var client = RawRabbitFactory.CreateClient(cfg =>
{
    cfg.Plugins.UseZeroFormatter();
});

// NEW (MessagePack)
var client = RawRabbitFactory.CreateClient(cfg =>
{
    cfg.Plugins.UseMessagePack();
});
```

#### 4. Test Serialization

```csharp
// Verify message round-trip
var message = new UserEvent { UserId = "123", Timestamp = DateTime.UtcNow };
await client.PublishAsync(message);

// Consume and verify
await client.SubscribeAsync<UserEvent>(async msg =>
{
    Assert.Equal("123", msg.UserId);
});
```

### Performance Comparison

| Serializer | Serialize (μs) | Deserialize (μs) | Payload Size (bytes) |
|------------|----------------|------------------|---------------------|
| ZeroFormatter | 1.2 | 0.8 | 150 |
| MessagePack | 0.4 | 0.3 | 120 |
| protobuf-net | 0.6 | 0.5 | 110 |

MessagePack is **3x faster** and produces **20% smaller** payloads than ZeroFormatter.

## Alternative: protobuf-net

If you prefer Google Protocol Buffers:

```bash
dotnet add package RawRabbit.Enrichers.Protobuf
```

```csharp
using ProtoBuf;

[ProtoContract]
public class UserEvent
{
    [ProtoMember(1)]
    public string UserId { get; set; }

    [ProtoMember(2)]
    public DateTime Timestamp { get; set; }
}

var client = RawRabbitFactory.CreateClient(cfg =>
{
    cfg.Plugins.UseProtobuf();
});
```

## Alternative: System.Text.Json (Recommended for New Projects)

For new projects, consider System.Text.Json (built-in to .NET 9):

```csharp
using System.Text.Json.Serialization;

public class UserEvent
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

// No plugin required, System.Text.Json is default in v2.1.0
var client = RawRabbitFactory.CreateClient();
```

## Compatibility Notes

**Breaking Change**: Existing ZeroFormatter-serialized messages CANNOT be deserialized with MessagePack/protobuf-net.

**Migration Strategy**:
1. Deploy new version with MessagePack alongside old version
2. Gradually migrate publishers to MessagePack
3. Once all publishers migrated, migrate consumers
4. Decommission ZeroFormatter-based services

**Dual-Serialization Pattern** (for gradual migration):

```csharp
// Support both ZeroFormatter (legacy) and MessagePack (new)
await client.SubscribeAsync<UserEvent>(async msg =>
{
    // Handle message
}, cfg =>
{
    cfg.WithSerializer(new DualSerializer(
        primary: new MessagePackSerializer(),
        fallback: new ZeroFormatterSerializer()));  // Keep v2.0.x for legacy
});
```

## Support

For migration assistance, see:
- [GitHub Discussions](https://github.com/pardahlman/RawRabbit/discussions)
- [MessagePack Documentation](https://github.com/MessagePack-CSharp/MessagePack-CSharp)
- [protobuf-net Documentation](https://github.com/protobuf-net/protobuf-net)
```

### Rationale

**Why Complete Removal (Not Deprecation)?**

1. **No .NET 9 Support**: Cannot compile on .NET 9
2. **Abandoned**: 7 years without updates
3. **Security Risk**: No patches available
4. **Better Alternatives**: MessagePack is faster and maintained
5. **Small User Base**: ZeroFormatter had limited adoption

**Why Recommend MessagePack?**

1. **Performance**: 2-3x faster than ZeroFormatter
2. **Active Maintenance**: Regular updates, .NET 9 support
3. **Similar API**: Easy migration path
4. **Ecosystem**: Wide adoption, good tooling

---

## Alternatives Considered

### Alternative 1: Keep ZeroFormatter with [Obsolete] Warning

**Description**: Mark as obsolete, keep in solution for compatibility

**Pros**:
- No breaking change
- Users have time to migrate
- Gradual deprecation

**Cons**:
- **Cannot compile on .NET 9** (ZeroFormatter incompatible)
- **Security risk** remains
- **Maintenance burden** for broken package
- **False promise** (package non-functional)

**Why Rejected**: ZeroFormatter is incompatible with .NET 9. Keeping it in solution would be misleading (cannot actually be used).

### Alternative 2: Fork ZeroFormatter, Update for .NET 9

**Description**: Create RawRabbit-maintained fork of ZeroFormatter

**Pros**:
- Backward compatibility
- Users don't need to migrate

**Cons**:
- **High Maintenance Burden**: Must maintain entire serialization library
- **No Value**: MessagePack is superior in every way
- **Technical Debt**: Inheriting abandoned codebase
- **Opportunity Cost**: Time better spent elsewhere

**Why Rejected**: Not worth maintaining abandoned library when superior alternatives exist.

### Alternative 3: Provide Compatibility Shim

**Description**: Create adapter that wraps ZeroFormatter v2.0.x from v2.1.x

**Pros**:
- Backward compatibility
- Users can delay migration

**Cons**:
- **Complex**: Would require cross-version serialization bridge
- **Performance**: Additional overhead
- **Unsustainable**: Maintaining bridge for abandoned library

**Why Rejected**: Too complex, provides no long-term value.

---

## Consequences

### Positive Consequences

- **Eliminates Security Risk**: No unmaintained dependencies
- **Clearer Migration Path**: MessagePack is better in every way
- **Reduced Maintenance**: One less project to support
- **Performance Improvement**: Users migrate to faster serializers

### Negative Consequences

- **Breaking Change**: ZeroFormatter users must migrate
- **Migration Effort**: Users must update code and redeploy
- **Compatibility Break**: Existing serialized messages incompatible

### Risks

**Risk 1: User Backlash**
- **Likelihood**: LOW (ZeroFormatter had limited adoption)
- **Impact**: MEDIUM (negative feedback)
- **Mitigation**:
  - Comprehensive migration guide
  - Clear communication about security risk
  - Recommend superior alternatives (MessagePack 3x faster)

**Risk 2: Migration Complexity**
- **Likelihood**: MEDIUM
- **Impact**: MEDIUM (user effort required)
- **Mitigation**:
  - Step-by-step migration guide
  - Code examples for MessagePack, protobuf-net, System.Text.Json
  - Dual-serialization pattern for gradual rollout

### Technical Debt

**Addressed**:
- Removes abandoned dependency
- Eliminates security risk
- Clarifies serialization strategy

---

## Migration Impact

### Breaking Changes

- **BREAKING**: RawRabbit.Enrichers.ZeroFormatter package removed entirely
- **BREAKING**: Existing ZeroFormatter-serialized messages incompatible with MessagePack

### Migration Path

See Migration Guide above.

---

## Validation

### Acceptance Criteria

- [ ] RawRabbit.Enrichers.ZeroFormatter removed from solution
- [ ] CHANGELOG documents removal with migration path
- [ ] Migration guide published (ZeroFormatter → MessagePack)
- [ ] Samples updated to use MessagePack/protobuf-net
- [ ] Documentation no longer references ZeroFormatter

---

## Dependencies

### Affected Components

- RawRabbit.Enrichers.ZeroFormatter (REMOVED)

### Related ADRs

- **ADR-0004**: Dependency Update Strategy
- **ADR-0006**: Serialization Strategy

---

## Timeline

**Proposed**: 2025-10-09
**Implementation**: Phase 3 (Week 4)
**Target Completion**: 2025-11-08

---

## References

- [ZeroFormatter GitHub (Archived)](https://github.com/neuecc/ZeroFormatter)
- [MessagePack Documentation](https://github.com/MessagePack-CSharp/MessagePack-CSharp)
- [Dependency Matrix](../stage-1/dependency-matrix.md)

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft (Stage 2.1) |
