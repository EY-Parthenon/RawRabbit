# CRITICAL ISSUE: Hanging Test Execution

**Issue ID:** TEST-HANG-001
**Severity:** CRITICAL (Blocking)
**Date Identified:** 2025-10-09
**Status:** OPEN - Requires Investigation

## Problem Statement

Test suite execution hangs and does not complete, preventing full test coverage measurement required by ADR-0005 (80% coverage target).

## Symptoms

1. Test execution starts normally
2. ~26 tests execute successfully
3. Execution hangs at specific test: `Should_Wait_For_Connection_To_Recover_Before_Returning_Channel`
4. No timeout occurs despite 10-minute session timeout configured
5. Process must be manually terminated

## Confirmed Working Tests

**Total Passing:** 26/35 tests (74% completion rate before hang)

- ✅ All ConnectionStringParserTests (15/15)
- ✅ All NamingConventionsTests (5/5)
- ✅ DynamicChannelPoolTests (3/3)
- ✅ Partial ChannelPoolTests (3+/8)
- ✅ Partial ChannelFactoryTests (1/4)

## Problem Tests

### Primary Suspect
**Test:** `RawRabbit.Tests.Channel.ChannelFactoryTests.Should_Wait_For_Connection_To_Recover_Before_Returning_Channel`
**Location:** `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs` (line 107-134)

**Code Analysis:**
```csharp
// Line 126: Blocking wait with timeout
connection.RecoverySucceeded += (sender, args) => resetEvent.Set();
await channelFactory.ConnectAsync();
Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(10));
    recoverable.Raise(c => c.Recovery += null, new EventArgs());
}).ConfigureAwait(false);
resetEvent.Wait(TimeSpan.FromSeconds(1));  // PROBLEM: Blocks thread
```

**Root Cause Hypothesis:**
1. `ManualResetEvent.Wait()` blocks the thread
2. Event handler may not fire due to mock timing issues
3. Async event raising may not work correctly with mocks
4. Test times out at thread level, not at test framework level

### Other Potentially Affected Tests

**Status:** UNKNOWN (not reached during execution)

- `Should_Throw_Exception_If_Connection_Is_Closed_By_Lib_But_Is_Not_Recoverable` (may have passed, unclear)
- `Should_Return_Channel_From_Connection` (may have passed, unclear)
- Remaining ChannelPoolTests (5-8 tests not yet executed)

## Technical Root Causes

### 1. Async/Await Anti-Pattern
**File:** `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs:126`

```csharp
// PROBLEM: Blocking call in async test
resetEvent.Wait(TimeSpan.FromSeconds(1));
```

**Issue:**
- Uses `ManualResetEvent.Wait()` which blocks the thread
- Should use `ManualResetEventSlim.Wait()` with async pattern or `TaskCompletionSource`
- Blocking wait prevents proper async/await flow

**Recommended Fix:**
```csharp
// SOLUTION: Use TaskCompletionSource
var tcs = new TaskCompletionSource<bool>();
connection.RecoverySucceeded += (sender, args) => tcs.SetResult(true);
await channelFactory.ConnectAsync();
Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(10));
    recoverable.Raise(c => c.Recovery += null, new EventArgs());
}).ConfigureAwait(false);
await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));  // Modern async wait
```

### 2. Mock Event Handler Timing
**File:** `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs:112-133`

**Issue:**
- Event handler setup: `connection.RecoverySucceeded +=`
- Event raised via Moq: `recoverable.Raise(c => c.Recovery += null, new EventArgs())`
- Mismatch between event names: `RecoverySucceeded` vs `Recovery`
- 10ms delay may not be sufficient for event propagation

**Recommended Fix:**
- Verify correct event name mapping
- Increase delay to 50-100ms or use synchronization primitive
- Add diagnostic logging to confirm event fires

### 3. RabbitMQ.Client 5.2.0 Compatibility Change
**File:** `src/RawRabbit/Channel/ChannelFactory.cs:35-36`

**Recent Change:**
```csharp
// OLD (6.x+ API):
// Connection = ConnectionFactory.CreateConnection(ClientConfig.Hostnames, ClientProvidedName);

// NEW (5.2.0 compatibility):
Connection = ConnectionFactory.CreateConnection(ClientConfig.Hostnames);
```

**Impact:**
- Removed `ClientProvidedName` parameter (not available in 5.2.0)
- May affect connection lifecycle events
- Tests may need updated mock expectations

### 4. Variable Naming Typo
**File:** `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs`

**Issue:** Variable named `connectionFactroy` (typo) throughout tests
**Impact:** Low (cosmetic), but indicates recent mass edits
**Recommendation:** Rename to `connectionFactory` for clarity

## Reproduction Steps

```bash
# 1. Navigate to project root
cd /home/laird/src/EYP/RawRabbit

# 2. Run specific test
~/.dotnet/dotnet test test/RawRabbit.Tests/ \
  --filter "FullyQualifiedName~Should_Wait_For_Connection_To_Recover_Before_Returning_Channel" \
  --logger "console;verbosity=diagnostic"

# Expected: Test hangs after ~1 second, requires Ctrl+C

# 3. Run all tests to observe hang
~/.dotnet/dotnet test test/RawRabbit.Tests/ --no-build --verbosity normal

# Expected: Hangs after 26 tests, requires Ctrl+C
```

## Investigation Commands

```bash
# Check for other blocking patterns
grep -rn "\.Wait(" test/RawRabbit.Tests/Channel/
grep -rn "\.Result" test/RawRabbit.Tests/Channel/
grep -rn "Task.Run" test/RawRabbit.Tests/Channel/

# Find all async test methods
grep -rn "public async Task" test/RawRabbit.Tests/Channel/

# Check mock event patterns
grep -rn "recoverable.Raise" test/RawRabbit.Tests/
grep -rn "Recovery" test/RawRabbit.Tests/Channel/
```

## Recommended Fixes

### Fix 1: Replace ManualResetEvent with TaskCompletionSource

**File:** `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs:107-134`

```csharp
[Fact]
public async Task Should_Wait_For_Connection_To_Recover_Before_Returning_Channel()
{
    /* Setup */
    var channel = new Mock<IModel>();
    var connectionFactory = new Mock<IConnectionFactory>();  // Fixed typo
    var connection = new Mock<IConnection>();
    var recoverable = connection.As<IRecoverable>();

    connectionFactory
        .Setup(c => c.CreateConnection(It.IsAny<IList<string>>()))
        .Returns(connection.Object);

    connection
        .Setup(c => c.CreateModel())
        .Returns(channel.Object);

    connection
        .SetupSequence(c => c.IsOpen)
        .Returns(false)
        .Returns(true);

    var channelFactory = new ChannelFactory(connectionFactory.Object, RawRabbitConfiguration.Local);

    /* Test */
    var tcs = new TaskCompletionSource<bool>();

    connection.RecoverySucceeded += (sender, args) =>
    {
        tcs.TrySetResult(true);
    };

    await channelFactory.ConnectAsync();

    // Trigger recovery after short delay
    _ = Task.Run(async () =>
    {
        await Task.Delay(50);  // Increased from 10ms
        recoverable.Raise(c => c.Recovery += null, new EventArgs());
    });

    // Wait for event with timeout
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    var recovered = await tcs.Task.WaitAsync(cts.Token);

    Assert.True(recovered, "Connection should have recovered");

    var actualChannel = await channelFactory.CreateChannelAsync();
    Assert.Equal(channel.Object, actualChannel);
}
```

### Fix 2: Add Per-Test Timeout Attribute

```csharp
[Fact(Timeout = 5000)]  // 5-second timeout
public async Task Should_Wait_For_Connection_To_Recover_Before_Returning_Channel()
{
    // ... test implementation
}
```

### Fix 3: Add Diagnostic Logging

```csharp
// Add to test to understand flow
var tcs = new TaskCompletionSource<bool>();
connection.RecoverySucceeded += (sender, args) =>
{
    Console.WriteLine("Recovery event fired!");
    tcs.TrySetResult(true);
};

Console.WriteLine("Starting ConnectAsync...");
await channelFactory.ConnectAsync();
Console.WriteLine("ConnectAsync complete");

Console.WriteLine("Triggering recovery...");
// ... recovery trigger code
```

## Workaround for Immediate Progress

### Option 1: Skip Hanging Test
```bash
# Run all tests except problematic one
~/.dotnet/dotnet test test/RawRabbit.Tests/ \
  --filter "FullyQualifiedName!~Should_Wait_For_Connection_To_Recover_Before_Returning_Channel"
```

### Option 2: Set Per-Test Timeout
Add to `test/RawRabbit.Tests/xunit.runner.json`:
```json
{
  "longRunningTestSeconds": 5
}
```

This will mark tests as "long-running" after 5 seconds (but may not actually kill them).

## Impact Assessment

**Blocking:**
- ✅ Test configuration: COMPLETE
- ❌ Full test suite execution: BLOCKED
- ❌ Test coverage measurement: BLOCKED
- ❌ ADR-0005 compliance validation: BLOCKED

**Non-Blocking:**
- ✅ Build: Working (tests not required for build)
- ✅ Partial testing: 26/35 tests confirmed working
- ✅ Code quality: No compile errors

## Next Steps

### Priority 1: Fix Hanging Test (1-2 hours)
1. Apply Fix 1 (TaskCompletionSource pattern)
2. Apply Fix 2 (timeout attribute)
3. Test individual test in isolation
4. Verify fix doesn't break test intent

### Priority 2: Run Full Test Suite (30 minutes)
1. Execute all tests with fixed code
2. Collect execution metrics
3. Verify all 35 tests complete

### Priority 3: Measure Coverage (15 minutes)
1. Run with code coverage collection
2. Generate coverage report
3. Compare against 80% target

### Priority 4: Document Results (15 minutes)
1. Update test results documentation
2. Record coverage metrics
3. Update HISTORY.md

## References

- **Test File:** `test/RawRabbit.Tests/Channel/ChannelFactoryTests.cs`
- **Source File:** `src/RawRabbit/Channel/ChannelFactory.cs`
- **ADR:** `docs/adr/ADR-0005-test-coverage-standards.md`
- **Full Report:** `docs/test/unit/full-suite-results-2025-10-09.md`
- **Test Discovery:** `docs/test/unit/test-discovery-2025-10-09.txt`
- **Test Output:** `docs/test/unit/full-test-run-2025-10-09.txt`

## Owner

**Current:** Unassigned
**Recommended:** Developer with async/await expertise
**Estimated Effort:** 2-3 hours total

---

**Document Created:** 2025-10-09
**Last Updated:** 2025-10-09
**Status:** OPEN - Awaiting Fix
