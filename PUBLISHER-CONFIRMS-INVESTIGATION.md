# Publisher Confirms Investigation - RabbitMQ.Client 6.x Migration

## Problem Statement
After migrating from RabbitMQ.Client 5.x to 6.8.1, all integration tests involving publisher confirms fail with timeout errors:
```
PublishConfirmException: The broker did not send a publish acknowledgement for message 1 within 0:00:01.
```

## Key Discovery: RabbitMQ.Client 6.x Works Correctly

A standalone test using RabbitMQ.Client 6.8.1 directly **SUCCEEDS**:
```csharp
channel.BasicAcks += (sender, args) => { ... };
channel.ConfirmSelect();
channel.BasicPublish(...);
// Result: BasicAcks fired! SUCCESS: Ack received = True
```

This proves that RabbitMQ.Client 6.x publisher confirms work correctly. The issue is specific to RawRabbit's middleware implementation.

## Critical Bugs Fixed

### 1. Race Condition in Lock Objects (`PublishAcknowledgeMiddleware.cs`)
**Location**: Lines 66, 124

**Problem**: 
- `EnableAcknowledgement()` was locking on the `channel` object (line 124)
- Message publishing was locking on a different `channelLock` object (line 78)
- These are DIFFERENT objects, providing NO mutual exclusion

**Impact**: Handler registration and message publishing could execute concurrently, causing the ack to arrive before the handler was registered.

**Fix Applied**:
```csharp
// Before:
protected virtual void EnableAcknowledgement(IModel channel, CancellationToken token)
{
    _exclusive.Execute(channel, c => { ... }, token);  // Locks on channel
}

// After:
protected virtual void EnableAcknowledgement(IModel channel, object channelLock, CancellationToken token)
{
    _exclusive.Execute(channelLock, _ => { ... }, token);  // Locks on channelLock (same as publish)
}
```

### 2. Task.Run Cancellation Token Bug
**Location**: Line 176 (originally line 151)

**Problem**: BasicAcks event handler used operation-scoped cancellation token:
```csharp
Task.Run(() => { ... }, token);  // token is scoped to single publish operation
```

**Impact**: On pooled channels, after the first publish operation completes:
- Token is cancelled
- Task.Run refuses to execute
- All subsequent acks are ignored

**Fix Applied**:
```csharp
Task.Run(() => { ... });  // No token - handler must persist for channel lifetime
```

### 3. Thread-Safety Issues
**Location**: Lines 31-32, 116

**Problem**:
- `ConfirmsDictionary` was a regular `Dictionary` (not thread-safe)
- `GetChannelDictionary()` used `ContainsKey` + `Add` pattern (race condition)

**Fix Applied**:
```csharp
// Changed from Dictionary to ConcurrentDictionary
protected static ConcurrentDictionary<IModel, ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>> ConfirmsDictionary = ...

// Use atomic GetOrAdd pattern
protected virtual ConcurrentDictionary<ulong, TaskCompletionSource<ulong>> GetChannelDictionary(IModel channel)
{
    return ConfirmsDictionary.GetOrAdd(channel, c => new ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>());
}
```

### 4. Missing Handler Registration Tracking
**Location**: Line 34, 127

**Problem**: No mechanism to detect duplicate handler registration on pooled channels.

**Fix Applied**:
```csharp
protected static ConcurrentDictionary<IModel, bool> HandlersRegistered = new ConcurrentDictionary<IModel, bool>();

// In EnableAcknowledgement:
if (!HandlersRegistered.TryAdd(channel, true))
{
    return; // Handler already registered
}
```

## Remaining Issue

Despite all fixes, tests still fail. This indicates an additional problem not yet identified.

### Hypothesis: ConfirmSelect() Ordering Problem

There may be a logic ordering issue in `EnableAcknowledgement()`:

**Current Code Flow**:
1. Check if handler is already registered
2. If yes, `return` early
3. Call `ConfirmSelect()` (only if handler not registered)
4. Register handler

**Problem**: On pooled channels' second use, the early `return` prevents `ConfirmSelect()` from being checked/called.

**Potential Fix**: Move `ConfirmSelect()` check BEFORE handler registration check:
```csharp
_exclusive.Execute(channelLock, _ =>
{
    // ALWAYS ensure publisher confirms are enabled
    if (!PublishAcknowledgeEnabled(channel))
    {
        channel.ConfirmSelect();
    }
    
    // THEN check if handler needs registration
    if (!HandlersRegistered.TryAdd(channel, true))
    {
        return; // Handler already registered, but confirms are enabled
    }
    
    // Register handler (first time only)
    channel.BasicAcks += ...
});
```

## Files Modified

- `/src/RawRabbit.Operations.Publish/Middleware/PublishAcknowledgeMiddleware.cs`
  - Fixed race condition in locking (lines 66, 121, 124)
  - Removed cancellation token from Task.Run (line 176)
  - Made ConfirmsDictionary thread-safe (lines 31-32)
  - Added HandlersRegistered tracking (line 34)
  - Fixed GetChannelDictionary race condition (line 116)
  - Added comprehensive diagnostic logging (lines 46-93, 123-150)

- `/test/RawRabbit.IntegrationTests/PublisherConfirms/MinimalPublisherConfirmsTest.cs` (NEW)
  - Created minimal reproduction test with detailed logging
  
## Next Steps

1. **Fix ConfirmSelect() ordering** - Ensure it's called regardless of handler registration status
2. **Enable and analyze diagnostic logs** - The comprehensive logging added should reveal the exact execution flow
3. **Investigate middleware pipeline** - Examine how PooledChannelMiddleware interacts with PublishAcknowledgeMiddleware
4. **Test with single-use channels** - Determine if issue is specific to pooled channels
5. **Compare with RabbitMQ.Client 5.x** - Review what changed in event delivery mechanism

## Test Status

- ✅ Standalone RabbitMQ.Client 6.x test: **PASSES**
- ❌ RawRabbit minimal test: **FAILS** (timeout)
- ❌ RawRabbit integration tests: **FAIL** (timeout)

## Conclusion

Multiple critical bugs have been identified and fixed:
1. Race condition in locking mechanism
2. Cancellation token preventing handler execution on pooled channels
3. Thread-safety issues in shared dictionaries
4. Missing handler registration tracking

These fixes are correct and necessary, but insufficient to resolve the issue completely. The problem appears to be multi-faceted, requiring additional investigation with the diagnostic logging now in place.
