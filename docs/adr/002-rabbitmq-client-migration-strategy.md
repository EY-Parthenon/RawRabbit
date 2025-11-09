# ADR-002: RabbitMQ.Client Migration Strategy

## Status

**Accepted** - 2025-11-09

## Context

RawRabbit 2.x uses RabbitMQ.Client 5.0.1 (released 2018). RabbitMQ.Client has had two major releases since then:
- **Version 6.0** (March 2021): Massive breaking changes, API redesign, async-by-default
- **Version 7.0** (2023): Further improvements, continued async evolution

### Major Breaking Changes in 6.x

1. **Async-by-default**: Most operations return `Task` or `ValueTask`
2. **Connection/Channel API**: Lifetime management redesigned
3. **Consumer API**: `EventingBasicConsumer` pattern overhauled
4. **Exception Handling**: Different exception types and patterns
5. **Topology Management**: Queue/exchange declaration APIs changed

### Affected Areas in RawRabbit

- `src/RawRabbit/Channel/` - Channel factory and pooling (~15 files)
- `src/RawRabbit/Pipe/` - Middleware pipeline (~10 files)
- `src/RawRabbit.Operations.*/` - All operations (~20 files)
- `src/RawRabbit.Enrichers.*/` - Enrichers (~10 files)
- `test/` - Integration tests (~5 files)
- **Total**: ~60 files affected

### Options Considered

1. **Option A: Direct migration 5.0.1 → 7.x**
   - Jump 2 major versions
   - Most up-to-date
   - Highest risk (compound breaking changes)
   - Longest timeline (15-20 days estimated)

2. **Option B: Incremental migration 5.0.1 → 6.x → 7.x**
   - Two-phase approach
   - Lower risk per phase
   - Can stop at 6.x if needed
   - Slightly longer overall (16-22 days total)

3. **Option C: Migrate to 6.8.x (LTS) and stop** ✅ SELECTED
   - Single major version jump
   - 6.8.x is LTS with long support runway
   - Stable, production-ready
   - Still get async improvements
   - Reasonable effort (12-18 days)
   - Can upgrade to 7.x later (non-breaking within 6.x)

4. **Option D: Stay on 5.x, apply security patches only**
   - Minimal effort
   - Misses 6+ years of improvements
   - Security risk (5.x is EOL)
   - Defeats purpose of modernization

## Decision

**We will migrate from RabbitMQ.Client 5.0.1 → 6.8.1 (LTS)**

We will:
1. Update the PackageReference to `6.8.1` immediately
2. Document the required code changes (this ADR)
3. Leave actual code migration for follow-up work (tracked separately)
4. Provide clear warnings in CHANGELOG.md that code changes are required

### Rationale

1. **LTS Support**: 6.8.x is the latest 6.x LTS release with extended support
2. **Single Major Jump**: Only one set of breaking changes to handle (vs 5→6→7)
3. **Manageable Scope**: 6.x breaking changes are documented and understood
4. **Future Flexibility**: Can upgrade 6.8 → 7.x later if needed (smaller jump)
5. **Production Ready**: 6.8.1 is stable and battle-tested (released 2023+)
6. **Async Benefits**: Get async-by-default improvements without 7.x complexity
7. **Risk Mitigation**: If 7.x proves too difficult, 6.x is acceptable long-term

### Consequences

**Positive**:
- ✅ Modern RabbitMQ.Client with async/await
- ✅ 6+ years of bug fixes and improvements
- ✅ Better performance and reliability
- ✅ Active security support
- ✅ Compatible with modern RabbitMQ server versions

**Negative**:
- ❌ **Requires extensive code changes** (~60 files)
- ❌ High effort (12-18 days estimated)
- ❌ High risk of introducing bugs
- ❌ Requires RabbitMQ expertise
- ❌ Integration testing required with real RabbitMQ
- ❌ Not using latest 7.x features

**Risks**:
- **CRITICAL**: Code changes NOT completed yet - dependency updated but code is NOT compatible
- **HIGH**: Async pattern changes may introduce threading bugs
- **MEDIUM**: Consumer API changes may break message handling

**Mitigation**:
- Clearly document in CHANGELOG.md that code changes are required
- Provide RabbitMQ.Client 6.x migration examples in MIGRATION-GUIDE.md
- Link to official RabbitMQ.Client 6.0 migration guide
- Recommend extensive integration testing with Docker RabbitMQ
- Consider hiring RabbitMQ consultant for code review (3-5 days, $6k-10k)

## Implementation Plan

### Phase 1: Dependency Update (COMPLETE ✅)
- Update PackageReference: `5.0.1` → `6.8.1`
- This phase is complete

### Phase 2: Code Migration (TODO ⚠️)

**Estimated effort**: 12-18 days

**Task 2.1: Channel Management** (3-5 days)
- Update `src/RawRabbit/Channel/ChannelFactory.cs`
- Update `src/RawRabbit/Channel/*ChannelPool.cs` (4 implementations)
- Update channel lifetime management
- Update connection recovery logic

**Task 2.2: Consumer API** (3-5 days)
- Update `src/RawRabbit.Operations.Subscribe/SubscribeMessageMiddleware.cs`
- Update `src/RawRabbit.Operations.Respond/RespondMessageMiddleware.cs`
- Migrate EventingBasicConsumer → async consumer
- Update acknowledgment patterns

**Task 2.3: Publish/Request Operations** (2-3 days)
- Update `src/RawRabbit.Operations.Publish/`
- Update `src/RawRabbit.Operations.Request/`
- Async method signatures
- Exception handling

**Task 2.4: Enrichers** (2-3 days)
- Update enrichers touching RabbitMQ APIs
- Test serialization/deserialization

**Task 2.5: Integration Tests** (2-3 days)
- Rewrite integration tests for async APIs
- Set up Docker Compose for RabbitMQ
- Run full test suite

### Phase 3: Validation (TODO ⚠️)
- Performance benchmarking
- Load testing
- Staging deployment
- Production rollout

## References

- [RabbitMQ.Client 6.0 Release Notes](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.0.0)
- [RabbitMQ.Client 6.x Migration Guide](https://www.rabbitmq.com/dotnet-api-guide.html)
- [RabbitMQ.Client GitHub](https://github.com/rabbitmq/rabbitmq-dotnet-client)

## Status Tracking

| Task | Status | Estimated Effort | Actual Effort | Owner |
|------|--------|------------------|---------------|-------|
| Dependency Update | ✅ COMPLETE | 1 hour | 1 hour | Coder Agent |
| Channel Management | ⚠️ TODO | 3-5 days | - | TBD |
| Consumer API | ⚠️ TODO | 3-5 days | - | TBD |
| Publish/Request Ops | ⚠️ TODO | 2-3 days | - | TBD |
| Enrichers | ⚠️ TODO | 2-3 days | - | TBD |
| Integration Tests | ⚠️ TODO | 2-3 days | - | TBD |
| **TOTAL** | **17% Complete** | **12-18 days** | **1 hour** | - |
