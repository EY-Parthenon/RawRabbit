# Phase 1: BasicGet Queue Isolation Fix

## Problem Description

All 3 BasicGet integration tests were failing with identical PRECONDITION_FAILED errors:

### Initial Error (Queues)
```
PRECONDITION_FAILED - inequivalent arg 'auto_delete' for queue 'basicmessage'
in vhost '/': received 'false' but current is 'true'
```

### Secondary Error (Exchanges)
```
PRECONDITION_FAILED - inequivalent arg 'durable' for exchange 'rawrabbit.integrationtests.testmessages'
in vhost '/': received 'false' but current is 'true'
```

**Root Cause**: Tests were reusing the same queue and exchange names ("basicmessage" and "rawrabbit.integrationtests.testmessages") across multiple test runs. When RabbitMQ resources from a previous test run persisted with different parameters, subsequent test runs would fail attempting to declare the same resources with conflicting parameters.

## Affected Tests

1. `Should_Be_Able_To_Get_Message` (GetOperation/BasicGetTests.cs:12)
2. `Should_Be_Able_To_Get_BasicGetResult_Message` (GetOperation/BasicGetTests.cs:39)
3. `Should_Be_Able_To_Get_BasicGetResult_When_Queue_IsEmpty` (GetOperation/BasicGetTests.cs:67)

## Solution Implemented

**Strategy**: Option A - Unique Resource Names (RECOMMENDED)

Generated unique queue and exchange names per test using `Guid.NewGuid()` to ensure complete isolation between test runs.

### Code Changes

**File**: `/home/laird/src/EYP/RawRabbit/test/RawRabbit.IntegrationTests/GetOperation/BasicGetTests.cs`

#### Before (Example from Test 1):
```csharp
var message = new BasicMessage {Prop = "Get me, get it?"};
var conventions = new NamingConventions();
var exchangeName = conventions.ExchangeNamingConvention(message.GetType());
TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), exchangeName, ...);

await client.PublishAsync(message, ...);
var ackable = await client.GetAsync<BasicMessage>();
```

#### After (Example from Test 1):
```csharp
var message = new BasicMessage {Prop = "Get me, get it?"};
var conventions = new NamingConventions();
// Use unique queue and exchange names to avoid PRECONDITION_FAILED errors from state conflicts between test runs
var testId = System.Guid.NewGuid().ToString();
var queueName = $"{conventions.QueueNamingConvention(message.GetType())}-{testId}";
var exchangeName = $"{conventions.ExchangeNamingConvention(message.GetType())}-{testId}";
TestChannel.QueueDeclare(queueName, true, false, false, null);
TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
TestChannel.QueueBind(queueName, exchangeName, ...);

await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
var ackable = await client.GetAsync<BasicMessage>(cfg => cfg.FromQueue(queueName));
```

### Key Changes Applied to All 3 Tests:

1. **Generate unique test ID**: `var testId = System.Guid.NewGuid().ToString();`
2. **Append GUID to queue names**: `var queueName = $"{conventions.QueueNamingConvention(message.GetType())}-{testId}";`
3. **Append GUID to exchange names**: `var exchangeName = $"{conventions.ExchangeNamingConvention(message.GetType())}-{testId}";`
4. **Update GetAsync calls**: Explicitly specify queue name with `cfg.FromQueue(queueName)` for tests that use generic `GetAsync<T>()`
5. **Add explanatory comments**: Document why unique names are used

## Test Results

### Before Fix
```
Total tests: 3
     Passed: 0
     Failed: 3
 Total time: N/A
```

**Errors**: All 3 tests failed with PRECONDITION_FAILED errors

### After Fix
```
Total tests: 3
     Passed: 3
     Failed: 0
 Total time: 1.7947 Seconds
```

**Success**: 100% pass rate (0% → 100%)

## Benefits of Solution

1. **Guaranteed Isolation**: Each test run creates unique resources, eliminating conflicts
2. **No Cleanup Required**: Tests don't depend on cleanup from previous runs
3. **Parallel Safe**: Tests can run in parallel without interference
4. **Simple**: Minimal code changes, easy to understand and maintain
5. **Fast**: Cleanup not required, tests complete quickly

## Trade-offs

**Cons**:
- Leaves test queues and exchanges in RabbitMQ after test completion
- Over time, many orphaned resources will accumulate in RabbitMQ

**Mitigation**:
- Acceptable for test environments
- Can implement periodic cleanup script if needed
- Could add IDisposable teardown in future if resource cleanup becomes critical

## Verification

Run tests with:
```bash
~/.dotnet/dotnet test test/RawRabbit.IntegrationTests/ --filter "FullyQualifiedName~BasicGetTests" --logger "console;verbosity=detailed"
```

Expected: All 3 tests pass in < 2 seconds

## Impact

- **Test Suite Progress**: Phase 1 complete (3/3 BasicGet tests passing)
- **Overall Progress**: Improved from 0% to 100% for BasicGet test category
- **Next Steps**: Proceed to Phase 2 test fixes

## Notes

- This pattern should be applied to other tests experiencing PRECONDITION_FAILED errors
- The fix is backward compatible and doesn't affect production code
- Tests now follow best practice of test isolation and independence
