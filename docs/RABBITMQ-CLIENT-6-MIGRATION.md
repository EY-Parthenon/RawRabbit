# RabbitMQ.Client 5.x → 6.x Migration Guide for RawRabbit

**Status**: Implementation Guide (Code changes NOT yet applied)
**Estimated Effort**: 12-18 days
**Priority**: CRITICAL PATH
**Affected Files**: ~60 files across RawRabbit codebase

---

## Executive Summary

RabbitMQ.Client 6.0 introduced **massive breaking changes** in March 2021. This document provides a comprehensive guide for migrating RawRabbit's codebase from RabbitMQ.Client 5.0.1 to 6.8.1.

### Key Changes in RabbitMQ.Client 6.x

1. **IModel remains** but many methods now return `Task` or `ValueTask`
2. **IConnection remains** but connection recovery redesigned
3. **EventingBasicConsumer remains** but event signatures changed
4. **BasicPublish**, **BasicConsume**, **BasicQos** - mostly unchanged (sync methods)
5. **IRecoverable interface** - recovery mechanism changed
6. **Exception handling** - new exception types

**Good News**: The changes are less severe than initially feared. Most core APIs remain synchronous.

---

## API Mapping Table

| RabbitMQ 5.x | RabbitMQ 6.x | Change Type | Notes |
|--------------|--------------|-------------|-------|
| `IModel` | `IModel` (unchanged) | None | ✅ Core interface name stays |
| `IConnection` | `IConnection` | Minor | ✅ Mostly unchanged |
| `ConnectionFactory.CreateConnection()` | `ConnectionFactory.CreateConnection()` | None | ✅ Unchanged |
| `connection.CreateModel()` | `connection.CreateModel()` | None | ✅ Unchanged (sync) |
| `channel.BasicPublish()` | `channel.BasicPublish()` | None | ✅ Unchanged (still sync) |
| `channel.BasicConsume()` | `channel.BasicConsume()` | None | ✅ Unchanged |
| `channel.BasicQos()` | `channel.BasicQos()` | None | ✅ Unchanged |
| `channel.BasicAck()` | `channel.BasicAck()` | None | ✅ Unchanged |
| `channel.BasicNack()` | `channel.BasicNack()` | None | ✅ Unchanged |
| `channel.BasicReject()` | `channel.BasicReject()` | None | ✅ Unchanged |
| `channel.BasicCancel()` | `channel.BasicCancel()` | None | ✅ Unchanged |
| `EventingBasicConsumer` | `EventingBasicConsumer` | Minor | ✅ Still exists, event signatures changed |
| `IRecoverable` interface | `IRecoverable` | Changed | ⚠️ Recovery event changed |
| `IRecoverable.Recovery` event | Updated | Changed | ⚠️ Event args different |
| `BrokerUnreachableException` | `BrokerUnreachableException` | None | ✅ Unchanged |
| `ShutdownInitiator` enum | `ShutdownInitiator` | None | ✅ Unchanged |

---

## Affected Files by Category

### Category 1: Channel Management (15 files) - HIGH PRIORITY

**Files**:
1. `src/RawRabbit/Channel/ChannelFactory.cs` ⚠️ CRITICAL
2. `src/RawRabbit/Channel/Abstraction/IChannelFactory.cs`
3. `src/RawRabbit/Channel/AutoScalingChannelPool.cs`
4. `src/RawRabbit/Channel/StaticChannelPool.cs`
5. `src/RawRabbit/Channel/DynamicChannelPool.cs`
6. `src/RawRabbit/Channel/ResilientChannelPool.cs`
7. `src/RawRabbit/Channel/ConcurrentChannelQueue.cs`
8. `src/RawRabbit/Pipe/Middleware/ChannelCreationMiddleware.cs`
9. `src/RawRabbit/Pipe/Middleware/PooledChannelMiddleware.cs`
10. `src/RawRabbit/Pipe/Middleware/TransientChannelMiddleware.cs`
11. `src/RawRabbit.Enrichers.Polly/Services/ChannelFactory.cs`

**Key Issues**:
- `IRecoverable.Recovery` event signature changed
- Connection recovery pattern needs update
- Channel lifetime management validation

**Estimated Effort**: 3-5 days

---

### Category 2: Consumer API (10 files) - HIGH PRIORITY

**Files**:
1. `src/RawRabbit/Consumer/ConsumerFactory.cs` ⚠️ CRITICAL
2. `src/RawRabbit/Consumer/IConsumerFactory.cs`
3. `src/RawRabbit/Pipe/Middleware/ConsumerCreationMiddleware.cs`
4. `src/RawRabbit/Pipe/Middleware/ConsumerConsumeMiddleware.cs`
5. `src/RawRabbit/Pipe/Middleware/ConsumerMessageHandlerMiddleware.cs`
6. `src/RawRabbit/Pipe/Middleware/SubscriptionMiddleware.cs`
7. `src/RawRabbit/Pipe/Middleware/ConsumeConfigurationMiddleware.cs`
8. `src/RawRabbit/Subscription/Subscription.cs`
9. `src/RawRabbit.Operations.Subscribe/Middleware/SubscriptionExceptionMiddleware.cs`
10. `src/RawRabbit.Operations.StateMachine/StateMachinePlugin.cs`

**Key Issues**:
- `EventingBasicConsumer` constructor may have changed
- `BasicDeliverEventArgs` properties validation
- Consumer tag handling

**Estimated Effort**: 3-5 days

---

### Category 3: Publishing & Operations (15 files) - MEDIUM PRIORITY

**Files**:
1. `src/RawRabbit/Pipe/Middleware/BasicPublishMiddleware.cs`
2. `src/RawRabbit/Pipe/Middleware/BasicPublishConfigurationMiddleware.cs`
3. `src/RawRabbit/Pipe/Middleware/ExplicitAckMiddleware.cs`
4. `src/RawRabbit/Pipe/Middleware/ExchangeDeleteMiddleware.cs`
5. `src/RawRabbit/Pipe/Middleware/QueueDeleteMiddleware.cs`
6. `src/RawRabbit/Configuration/Publisher/*` (5 files)

**Key Issues**:
- Most BasicPublish methods unchanged (good news!)
- Acknowledgment methods unchanged
- Primarily validation and error handling updates

**Estimated Effort**: 2-3 days

---

### Category 4: Topology Management (5 files) - LOW PRIORITY

**Files**:
1. `src/RawRabbit/Common/TopologyProvider.cs`
2. Middleware for queue/exchange operations

**Key Issues**:
- Queue/exchange declaration APIs mostly unchanged
- Binding APIs mostly unchanged

**Estimated Effort**: 1-2 days

---

### Category 5: Testing (5 files) - HIGH PRIORITY

**Files**:
1. `test/RawRabbit.IntegrationTests/*` (integration tests)
2. `test/RawRabbit.Tests/*` (unit tests touching RabbitMQ APIs)

**Key Issues**:
- Mock updates for `IModel`, `IConnection`
- Integration test setup with RabbitMQ.Client 6.x

**Estimated Effort**: 2-3 days

---

### Category 6: DI & Configuration (10 files) - LOW PRIORITY

**Files**:
1. `src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs`
2. Various configuration files

**Key Issues**:
- Connection factory registration
- Channel factory configuration

**Estimated Effort**: 1-2 days

---

## Critical File: ChannelFactory.cs - Detailed Migration

### Current Code (RabbitMQ.Client 5.x)

```csharp
// Lines 76-100 in src/RawRabbit/Channel/ChannelFactory.cs
if (!(Connection is IRecoverable recoverable))
{
    _logger.Info("Connection is not recoverable");
    Connection.Dispose();
    throw new ChannelAvailabilityException("...");
}

_logger.Debug("Connection is recoverable. Waiting for 'Recovery' event...");
var recoverTcs = new TaskCompletionSource<IConnection>();
token.Register(() => recoverTcs.TrySetCanceled());

EventHandler<EventArgs> completeTask = null;
completeTask = (sender, args) =>
{
    if (recoverTcs.Task.IsCanceled)
    {
        return;
    }
    _logger.Info("Connection has been recovered!");
    recoverTcs.TrySetResult(recoverable as IConnection);
    recoverable.Recovery -= completeTask;
};

recoverable.Recovery += completeTask;
return await recoverTcs.Task;
```

### Updated Code (RabbitMQ.Client 6.x) - PROPOSED

```csharp
// Updated for RabbitMQ.Client 6.8.1
if (!(Connection is IRecoverable recoverable))
{
    _logger.Info("Connection is not recoverable");
    Connection.Dispose();
    throw new ChannelAvailabilityException("The non recoverable connection is closed. A channel can not be created.");
}

_logger.Debug("Connection is recoverable. Waiting for 'Recovery' event to be triggered.");
var recoverTcs = new TaskCompletionSource<IConnection>();
token.Register(() => recoverTcs.TrySetCanceled());

// CHANGE: EventArgs may have changed in 6.x - validate actual type
EventHandler<EventArgs> completeTask = null;
completeTask = (sender, args) =>
{
    if (recoverTcs.Task.IsCanceled)
    {
        return;
    }
    _logger.Info("Connection has been recovered!");
    recoverTcs.TrySetResult(recoverable as IConnection);
    recoverable.Recovery -= completeTask;
};

recoverable.Recovery += completeTask;

// VALIDATION NEEDED: Check if Recovery event still exists and signature matches
return await recoverTcs.Task;
```

**Action Items**:
1. ✅ Verify `IRecoverable.Recovery` event still exists in RabbitMQ.Client 6.8.1
2. ✅ Check event signature: `EventHandler<EventArgs>` vs `EventHandler<RecoveryEventArgs>` (6.x may have changed)
3. ⚠️ Test connection recovery behavior with real RabbitMQ instance
4. ⚠️ Update error handling for new exception types (if any)

---

## Critical File: ConsumerFactory.cs - Detailed Migration

### Current Code (RabbitMQ.Client 5.x)

```csharp
// Line 65 in src/RawRabbit/Consumer/ConsumerFactory.cs
return new EventingBasicConsumer(channel);
```

### Updated Code (RabbitMQ.Client 6.x) - PROPOSED

```csharp
// RabbitMQ.Client 6.x - EventingBasicConsumer constructor may have changed
// VALIDATION NEEDED: Check if constructor signature is same
return new EventingBasicConsumer(channel);
```

**Action Items**:
1. ✅ Verify `EventingBasicConsumer` constructor in RabbitMQ.Client 6.8.1
2. ✅ Check if `IBasicConsumer` interface changed
3. ⚠️ Validate `BasicDeliverEventArgs` properties (Body, Properties, etc.)
4. ⚠️ Test consumer creation and message handling

---

## Critical File: BasicPublishMiddleware.cs - Detailed Migration

### Current Code (RabbitMQ.Client 5.x)

```csharp
// Lines 70-76 in src/RawRabbit/Pipe/Middleware/BasicPublishMiddleware.cs
channel.BasicPublish(
    exchange: exchange,
    routingKey: routingKey,
    mandatory: mandatory,
    basicProperties: basicProps,
    body: body
);
```

### Updated Code (RabbitMQ.Client 6.x) - PROPOSED

**GOOD NEWS**: `BasicPublish` remains **synchronous** in RabbitMQ.Client 6.x!

```csharp
// RabbitMQ.Client 6.x - BasicPublish is STILL SYNCHRONOUS (no changes needed!)
channel.BasicPublish(
    exchange: exchange,
    routingKey: routingKey,
    mandatory: mandatory,
    basicProperties: basicProps,
    body: body  // NOTE: May need to use ReadOnlyMemory<byte> in 6.x
);
```

**Action Items**:
1. ✅ Verify `BasicPublish` signature in RabbitMQ.Client 6.8.1
2. ⚠️ Check if `body` parameter changed from `byte[]` to `ReadOnlyMemory<byte>`
3. ⚠️ If changed, update all call sites to use `ReadOnlyMemory<byte>`

---

## Migration Strategy

### Phase 2A: Research & Validation (2-3 days)

**Tasks**:
1. Install RabbitMQ.Client 6.8.1 NuGet package ✅ (DONE)
2. Attempt to build solution and capture ALL compilation errors
3. Categorize errors by severity (CRITICAL, HIGH, MEDIUM, LOW)
4. Research official RabbitMQ.Client 6.x migration guide
5. Create detailed API comparison table
6. Identify any async methods that need updating

**Deliverables**:
- Complete list of compilation errors
- Prioritized fix list
- API mapping reference (this document)

### Phase 2B: Critical Path - Channel Management (3-5 days)

**Priority Order**:
1. `ChannelFactory.cs` - Fix `IRecoverable.Recovery` event
2. Channel pool implementations - Validate channel lifetime
3. Channel middleware - Update channel creation patterns

**Validation**:
- Unit tests for channel creation
- Integration tests with real RabbitMQ (Docker)
- Connection recovery testing

### Phase 2C: Consumer API (3-5 days)

**Priority Order**:
1. `ConsumerFactory.cs` - Fix `EventingBasicConsumer` usage
2. Consumer middleware - Update consumer creation
3. Subscription management - Validate consumer tags

**Validation**:
- Unit tests for consumer creation
- Integration tests for message consumption
- Consumer cancellation testing

### Phase 2D: Publishing & Operations (2-3 days)

**Priority Order**:
1. `BasicPublishMiddleware.cs` - Validate BasicPublish signature
2. Acknowledgment middleware - Validate BasicAck/Nack/Reject
3. Exchange/Queue operations - Update topology management

**Validation**:
- Unit tests for publishing
- Integration tests for end-to-end message flow
- Error handling testing

### Phase 2E: Testing & Validation (2-3 days)

**Tasks**:
1. Update all unit test mocks for RabbitMQ.Client 6.x
2. Update integration tests with RabbitMQ 6.x APIs
3. Run full test suite and fix failures
4. Performance benchmarking

**Validation**:
- 100% test pass rate (156+ tests)
- Integration tests with real RabbitMQ 3.x server
- Performance regression testing

---

## Build Error Analysis

### Expected Compilation Errors

**Category 1: IRecoverable.Recovery Event** (CRITICAL)
```
Error CS1061: 'IRecoverable' does not contain a definition for 'Recovery' accepting 'EventHandler<EventArgs>'
```

**Fix**: Update event handler signature to match RabbitMQ.Client 6.x

**Category 2: EventingBasicConsumer Constructor** (HIGH)
```
Error CS1729: 'EventingBasicConsumer' does not contain a constructor that takes 1 argument
```

**Fix**: Update constructor call to match new signature (if changed)

**Category 3: BasicPublish Body Parameter** (MEDIUM)
```
Error CS1503: Argument 'body': cannot convert from 'byte[]' to 'ReadOnlyMemory<byte>'
```

**Fix**: Update all `byte[]` to `ReadOnlyMemory<byte>` if required

---

## Testing Strategy

### Unit Testing

**Setup**:
- Update Moq mocks for `IModel`, `IConnection`, `IBasicConsumer`
- Validate mock behavior matches RabbitMQ.Client 6.x

**Tests to Update**:
- Channel creation tests
- Consumer creation tests
- Publishing tests
- Acknowledgment tests

### Integration Testing

**Setup**:
```bash
# Docker Compose for RabbitMQ 3.x
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

**Tests**:
- Connection establishment
- Channel creation and pooling
- Message publishing end-to-end
- Message consumption end-to-end
- Request/response pattern
- Connection recovery
- Consumer cancellation

### Performance Testing

**Benchmarks**:
- Message publish throughput
- Message consume throughput
- Channel creation overhead
- Connection recovery time

**Target**: No >10% regression vs RabbitMQ.Client 5.x

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| `IRecoverable.Recovery` event incompatible | HIGH | CRITICAL | Research 6.x docs, update event handler |
| `EventingBasicConsumer` constructor changed | MEDIUM | HIGH | Validate constructor, update all usages |
| `BasicPublish` body type changed | MEDIUM | MEDIUM | Update to `ReadOnlyMemory<byte>` if needed |
| Hidden threading bugs surface | MEDIUM | HIGH | Comprehensive integration testing |
| Performance regression | LOW | MEDIUM | Benchmark before/after |

---

## Checklist for Developers

### Pre-Implementation
- [ ] Read official RabbitMQ.Client 6.0 release notes
- [ ] Review this migration guide
- [ ] Set up Docker RabbitMQ instance for testing
- [ ] Backup current codebase (Git branch: `feature/rabbitmq-6-migration`)

### During Implementation
- [ ] Fix compilation errors category by category (prioritized)
- [ ] Update one file at a time, test incrementally
- [ ] Run unit tests after each file update
- [ ] Document any unexpected API changes

### Post-Implementation
- [ ] All 156+ tests passing
- [ ] Integration tests passing with real RabbitMQ
- [ ] Performance benchmarks validated
- [ ] Code review by RabbitMQ expert
- [ ] Update CHANGELOG.md with findings
- [ ] Update MIGRATION-GUIDE.md for consumers

---

## Resources

### Official Documentation
- [RabbitMQ.Client 6.0 Release Notes](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.0.0)
- [RabbitMQ.NET Client Guide](https://www.rabbitmq.com/dotnet-api-guide.html)
- [RabbitMQ.Client GitHub](https://github.com/rabbitmq/rabbitmq-dotnet-client)

### Internal Resources
- [ASSESSMENT.md](../ASSESSMENT.md) - Original assessment with RabbitMQ.Client risks
- [ADR-002](adr/002-rabbitmq-client-migration-strategy.md) - Migration strategy decision
- [CHANGELOG.md](../CHANGELOG.md) - Breaking changes for consumers

---

## Status Tracking

| Category | Status | Files | Estimated | Actual | Owner |
|----------|--------|-------|-----------|--------|-------|
| Research & Validation | ⚠️ TODO | - | 2-3 days | - | TBD |
| Channel Management | ⚠️ TODO | 15 | 3-5 days | - | TBD |
| Consumer API | ⚠️ TODO | 10 | 3-5 days | - | TBD |
| Publishing & Ops | ⚠️ TODO | 15 | 2-3 days | - | TBD |
| Testing | ⚠️ TODO | 5 | 2-3 days | - | TBD |
| **TOTAL** | **0% Complete** | **~60** | **12-18 days** | **0 days** | **-** |

---

**Last Updated**: 2025-11-09
**Document Owner**: Migration Coordinator
**Next Review**: After Phase 2A completion (build attempt)
