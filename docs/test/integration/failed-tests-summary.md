# Failed Integration Tests - Quick Reference

**Test Run:** Stage 6 - 2025-10-09
**Total Failures:** 19 of 57 tests (33.3%)

---

## Critical Priority Failures (Must Fix First)

### 1. Message Sequence Operations (0% Pass Rate)

**Test:** `Should_Work_For_Concurrent_Sequences`
- **File:** MessageSequence/MessageSequenceTests.cs:216
- **Error:** System.TimeoutException - Unable to complete sequences in 10 seconds
- **Impact:** HIGH - Core messaging pattern broken
- **Root Cause:** Async/threading issue with .NET 9
- **Fix Effort:** 4-6 hours

**Test:** `Should_Create_Chain_With_Publish_When_And_Complete`
- **File:** MessageSequence/* (location TBD)
- **Error:** Chain creation failing
- **Impact:** HIGH - Message workflow orchestration broken
- **Root Cause:** Related to sequence coordination issue
- **Fix Effort:** 2-3 hours

### 2. BasicGet Operations (0% Pass Rate)

**All 3 tests fail with same error:**
- `Should_Be_Able_To_Get_Message`
- `Should_Be_Able_To_Get_BasicGetResult_Message`
- `Should_Be_Able_To_Get_BasicGetResult_When_Queue_IsEmpty`

**Common Error:**
```
PRECONDITION_FAILED - inequivalent arg 'auto_delete' for queue 'basicmessage'
in vhost '/': received 'false' but current is 'true'
```

- **File:** GetOperation/BasicGetTests.cs:20, 47, 73
- **Impact:** HIGH - Queue management broken
- **Root Cause:** Queue state not cleaned between tests
- **Fix:** Use unique queue names per test or aggressive cleanup
- **Fix Effort:** 1-2 hours

---

## High Priority Failures (Fix Next)

### 3. Acknowledgement Mechanisms

**Test:** `Should_Be_Able_To_Return_Ack_From_Subscriber_With_Context`
- **File:** PublishAndSubscribe/AcknowledgementSubscribeTests.cs:77
- **Error:** PublishConfirmException - Broker did not send acknowledgement within 1s
- **Impact:** MEDIUM - Publish confirms timing out
- **Root Cause:** Timeout configuration or callback registration
- **Fix Effort:** 2 hours

**Test:** `Should_Be_Able_To_Retry_Multiple_Times`
- **File:** PublishAndSubscribe/AcknowledgementSubscribeTests.cs:369
- **Error:** Assert.Equal() Failure - Expected: 1, Actual: 0
- **Impact:** MEDIUM - Retry counter not incrementing
- **Root Cause:** Retry middleware not executing
- **Fix Effort:** 2-3 hours

**Test:** `Should_Be_Able_To_Return_Retry`
- **Error:** Retry mechanism not functioning
- **Impact:** MEDIUM
- **Fix Effort:** 1-2 hours

**Test:** `Should_Be_Able_To_Return_Nack_With_Requeue`
- **Error:** Nack with requeue not working
- **Impact:** MEDIUM
- **Fix Effort:** 1-2 hours

**Test:** `Should_Be_Able_To_Auto_Ack`
- **Error:** Auto-acknowledgement config issue
- **Impact:** MEDIUM
- **Fix Effort:** 1 hour

---

## Medium Priority Failures (Fix Later)

### 4. Context Forwarding

**Test:** `Should_Forward_On_Pub_Sub`
- **Duration:** 4s (timeout)
- **Impact:** MEDIUM - Context propagation in pub/sub
- **Fix Effort:** 2 hours

**Test:** `Should_Forward_For_Rpc`
- **Duration:** 657ms
- **Impact:** MEDIUM - Context propagation in RPC
- **Fix Effort:** 2 hours

**Test:** `Should_Override_With_Explicit_Context_On_Pub_Sub`
- **Duration:** 237ms
- **Impact:** MEDIUM - Explicit context override
- **Fix Effort:** 1 hour

**Test:** `Should_Forward_Context_For_Pub_Sub_And_Rpc`
- **Duration:** 274ms
- **Impact:** MEDIUM - Cross-operation context
- **Fix Effort:** 1-2 hours

**Test:** `Shoud_Create_Context_From_Supplied_Factory_Method`
- **Duration:** 128ms
- **Impact:** LOW - Custom context factory
- **Fix Effort:** 1 hour

---

## Low Priority Failures (Fix If Time Permits)

### 5. Client Resolution & Configuration

**Test:** `Should_Be_Able_To_Publish_Message_From_Resolved_Client`
- **Duration:** 263ms
- **Impact:** LOW - DI integration
- **Fix Effort:** 1 hour

**Test:** `Should_Be_Able_To_Subscribe_To_Generic_Message`
- **Duration:** 1s
- **Impact:** LOW - Generic type handling
- **Fix Effort:** 1-2 hours

**Test:** `Should_Be_Able_To_Override_Custom_Suffix`
- **Duration:** 1s
- **Impact:** LOW - Config override
- **Fix Effort:** 1 hour

**Test:** `Should_Honor_Task_Cancellation`
- **Duration:** 765ms
- **Impact:** LOW - Cancellation token handling
- **Fix Effort:** 1 hour

---

## Debugging Action Plan

### Phase 1: Quick Wins (2-3 hours)
1. Fix BasicGet queue isolation issue
   - Generate unique queue names using GUIDs
   - Add cleanup in test teardown
2. Increase acknowledgement timeouts temporarily
   - Change from 1s to 5s to test if timing-related

### Phase 2: Core Functionality (6-8 hours)
3. Debug message sequence timeouts
   - Add verbose logging to sequence coordination
   - Review async state machine behavior in .NET 9
   - Check for deadlocks or race conditions
4. Fix acknowledgement mechanisms
   - Verify RabbitMQ.Client 7.0 callback signatures
   - Add diagnostic logging to ack middleware
   - Test retry counter incrementing

### Phase 3: Context & Polish (4-6 hours)
5. Fix context forwarding
   - Review middleware pipeline changes
   - Verify context propagation in .NET 9
6. Fix remaining edge cases
   - Generic message handling
   - Cancellation token support
   - DI integration

### Total Estimated Fix Time: 12-17 hours

---

## Testing Strategy

### Before Fixing
1. Run single failing test in isolation
2. Enable DEBUG logging for RabbitMQ client
3. Attach debugger and set breakpoints
4. Review .NET 9 migration guide for breaking changes

### After Fixing
1. Run fixed test in isolation (must pass)
2. Run entire test suite (ensure no regressions)
3. Run 3 times to verify stability
4. Document fix in HISTORY.md

### Validation Criteria
- All tests must pass 3 consecutive runs
- No new timeouts introduced
- Pass rate must be ≥95%
- Average test execution time ≤500ms (excluding long-running tests)

---

## Environment Notes

### Current Test Environment
- RabbitMQ: 3.12.14 (healthy)
- .NET: 9.0
- RabbitMQ.Client: 7.0.0
- xUnit: Latest

### Known Good Configuration
- From Stage 4: 112/112 tests passed
- May be different test suite or environment

### Recommended Debug Tools
1. RabbitMQ Management UI (http://localhost:15672)
2. Visual Studio Debugger with async debugging enabled
3. BenchmarkDotNet for performance profiling
4. Seq or Serilog for structured logging

---

## References

- Full test output: `/home/laird/src/EYP/RawRabbit/docs/test/integration/stage-6-integration-tests.txt`
- Detailed report: `/home/laird/src/EYP/RawRabbit/docs/test/integration/stage-6-integration-report.md`
- History entry: `/home/laird/src/EYP/RawRabbit/docs/HISTORY.md`

**Generated:** 2025-10-09T23:16:00Z
**Agent:** QA Tester (dotnet9-upgrade session)
