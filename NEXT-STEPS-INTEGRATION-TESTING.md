# RawRabbit 3.0 - Next Steps: Integration Testing Guide

**Date**: 2025-11-09
**Phase**: Testing & Validation
**Current Status**: 78% Complete
**Critical Path**: Integration Testing Required

---

## Executive Summary

The RawRabbit 3.0 code migration is functionally complete with 98% unit test pass rate. The remaining 3 test failures are recovery-related and require integration testing with a real RabbitMQ instance to validate that RabbitMQ.Client 6.x automatic recovery works correctly.

**What's Complete**:
- ✅ All 25 core projects build successfully
- ✅ 98% unit test pass rate (153+/156+ tests passing)
- ✅ RabbitMQ.Client 6.x API migration
- ✅ Polly 8.x API migration

**What Remains**:
- ⏳ Integration testing with RabbitMQ
- ⏳ Connection/channel recovery validation
- ⏳ Resolve 3 recovery unit test failures

---

## Integration Test Setup Required

### Prerequisites

1. **Docker Installation** (REQUIRED)
   - Docker is not currently available in the environment
   - Required for running RabbitMQ container
   - Installation: https://docs.docker.com/get-docker/

2. **RabbitMQ Container**
   - Official image: `rabbitmq:3-management`
   - Includes management UI on port 15672
   - AMQP on port 5672

3. **Integration Tests Project Fix**
   - The `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj` still references the removed ZeroFormatter project (line 20)
   - This must be fixed before integration tests can run

---

## Step-by-Step Integration Testing Guide

### Step 1: Fix IntegrationTests Project Reference

**File**: `test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj`

**Remove line 20**:
```xml
<ProjectReference Include="..\..\src\RawRabbit.Enrichers.ZeroFormatter\RawRabbit.Enrichers.ZeroFormatter.csproj" />
```

**Rebuild integration tests**:
```bash
dotnet build test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj --no-restore
```

---

### Step 2: Start RabbitMQ Container

**Option A: Using docker-compose (Recommended)**

Create `docker-compose.yml` in project root:
```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: rawrabbit-test
    ports:
      - "5672:5672"    # AMQP
      - "15672:15672"  # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
      RABBITMQ_DEFAULT_VHOST: /
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 10s
      retries: 5
```

**Start RabbitMQ**:
```bash
docker-compose up -d
```

**Wait for RabbitMQ to be ready**:
```bash
docker logs rawrabbit-test | grep "Server startup complete"
```

**Option B: Using docker run**
```bash
docker run -d \
  --name rawrabbit-test \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:3-management
```

---

### Step 3: Run Integration Tests

**Run all integration tests**:
```bash
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
  --logger "console;verbosity=normal"
```

**Expected Categories**:
- PublishAndSubscribe tests
- StateMachine tests
- Request/Response tests
- Retry/Recovery tests

**Monitor for failures**:
- Connection errors (RabbitMQ not ready)
- Recovery behavior failures
- Timeout issues

---

### Step 4: Manual Recovery Testing (CRITICAL)

This is the most important validation for RawRabbit 3.0 because we simplified recovery event handling.

**Test Script** (create `test-recovery.sh`):
```bash
#!/bin/bash

echo "=== RawRabbit 3.0 Recovery Test ==="

# 1. Start RabbitMQ
echo "Starting RabbitMQ..."
docker-compose up -d
sleep 10

# 2. Run integration tests
echo "Running integration tests (background)..."
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj &
TEST_PID=$!
sleep 5

# 3. Kill RabbitMQ mid-test
echo "Killing RabbitMQ to test recovery..."
docker stop rawrabbit-test
sleep 3

# 4. Restart RabbitMQ
echo "Restarting RabbitMQ..."
docker start rawrabbit-test
sleep 10

# 5. Wait for tests to complete
echo "Waiting for tests to complete..."
wait $TEST_PID
TEST_EXIT=$?

# 6. Report results
if [ $TEST_EXIT -eq 0 ]; then
    echo "✅ Recovery test PASSED - Tests continued after RabbitMQ restart"
else
    echo "❌ Recovery test FAILED - Tests failed after RabbitMQ restart"
fi

exit $TEST_EXIT
```

**Make executable and run**:
```bash
chmod +x test-recovery.sh
./test-recovery.sh
```

**What to Validate**:
1. Connections automatically recover after RabbitMQ restart
2. Channels automatically recover
3. Subscriptions are re-established
4. Message flow continues without manual intervention
5. No unhandled exceptions during recovery

---

### Step 5: Specific Recovery Scenarios

Create manual test cases for recovery scenarios:

#### Test Case 1: Connection Recovery
```csharp
// Manual test in test/RawRabbit.IntegrationTests/Recovery/ConnectionRecoveryTests.cs
[Fact]
public async Task Should_Recover_Connection_After_RabbitMQ_Restart()
{
    // Arrange
    var client = RawRabbitFactory.CreateSingleton();
    var messageReceived = new TaskCompletionSource<bool>();

    await client.SubscribeAsync<TestMessage>(msg =>
    {
        messageReceived.TrySetResult(true);
        return Task.FromResult(new Ack());
    });

    // Act - Publish before restart
    await client.PublishAsync(new TestMessage { Text = "Before restart" });
    await Task.Delay(1000);

    // ⚠️ MANUALLY STOP/START RABBITMQ HERE
    Console.WriteLine("Please stop and start RabbitMQ, then press any key...");
    Console.ReadKey();

    // Publish after restart
    await client.PublishAsync(new TestMessage { Text = "After restart" });

    // Assert
    var received = await Task.WhenAny(
        messageReceived.Task,
        Task.Delay(30000) // 30 second timeout
    ) == messageReceived.Task;

    Assert.True(received, "Message should be received after RabbitMQ restart");
}
```

#### Test Case 2: Channel Recovery
Similar structure but validates channel-level recovery

#### Test Case 3: Subscriber Recovery
Validates that subscriptions are automatically re-established

---

## Expected Integration Test Results

### If Automatic Recovery Works ✅
- All integration tests pass
- Connections recover automatically
- Channels recover automatically
- Subscriptions re-established
- Message flow continues

**Action**:
1. Update the 3 failing unit tests to match new automatic recovery behavior
2. Mark recovery unit tests as obsolete (testing old behavior)
3. Proceed to performance validation

### If Automatic Recovery Fails ❌
- Integration tests fail after RabbitMQ restart
- Connections/channels don't recover
- Subscriptions lost
- Message flow stops

**Action**:
1. Re-implement recovery event handling for RabbitMQ.Client 6.x API
2. Investigate RabbitMQ.Client 6.x recovery event API:
   - `IConnection.CallbackException`
   - `IConnection.ConnectionShutdown`
   - Check if `IRecoverable` interface still exists in 6.x
3. Update production code (ChannelFactory.cs, StaticChannelPool.cs)
4. Re-run integration tests

---

## RabbitMQ.Client 6.x Recovery API Research

### Questions to Answer
1. **Does `IRecoverable` interface still exist in RabbitMQ.Client 6.x?**
   - Check: `typeof(IConnection).GetInterfaces()`

2. **What recovery events are available?**
   - Check: `IConnection` event members
   - Look for: `RecoverySucceeded`, `ConnectionRecovered`, etc.

3. **Is automatic recovery enabled by default?**
   - Check: `ConnectionFactory.AutomaticRecoveryEnabled` default value

4. **What configuration is required for automatic recovery?**
   ```csharp
   var factory = new ConnectionFactory
   {
       AutomaticRecoveryEnabled = true,
       NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
       TopologyRecoveryEnabled = true
   };
   ```

### Where to Find Answers
1. **RabbitMQ.Client 6.x Documentation**:
   - https://www.rabbitmq.com/client-libraries/dotnet-api-guide
   - https://www.rabbitmq.com/client-libraries/dotnet#recovery

2. **GitHub RabbitMQ.Client Repository**:
   - https://github.com/rabbitmq/rabbitmq-dotnet-client
   - Check CHANGELOG for 6.x recovery changes

3. **Decompile RabbitMQ.Client 6.8.1**:
   ```bash
   dotnet add package ILSpy.CommandLine
   ilspycmd ~/.nuget/packages/rabbitmq.client/6.8.1/lib/net6.0/RabbitMQ.Client.dll \
     --type IRecoverable --type IConnection
   ```

---

## Recovery Testing Checklist

### Environment Setup
- [ ] Docker installed
- [ ] RabbitMQ container running
- [ ] Management UI accessible (http://localhost:15672, guest/guest)
- [ ] Integration tests project builds (ZeroFormatter reference removed)

### Integration Tests
- [ ] All PublishAndSubscribe tests pass
- [ ] All StateMachine tests pass
- [ ] All Request/Response tests pass
- [ ] All Retry tests pass

### Manual Recovery Testing
- [ ] Connection recovers after RabbitMQ restart
- [ ] Channels recover after RabbitMQ restart
- [ ] Subscriptions re-established after restart
- [ ] Message flow continues after restart
- [ ] No unhandled exceptions during recovery
- [ ] Performance acceptable after recovery

### Unit Test Resolution
- [ ] Decision made on recovery unit tests (skip/update/re-implement)
- [ ] If skipping: Tests marked with `[Fact(Skip = "Tests old recovery behavior")]`
- [ ] If updating: Tests modified to match RabbitMQ.Client 6.x behavior
- [ ] If re-implementing: Production code updated with 6.x recovery events
- [ ] 100% unit test pass rate achieved

---

## Timeline Estimate

### If Automatic Recovery Works (2-3 days)
- Day 1 (4 hours): Docker setup + integration test fixes + initial runs
- Day 2 (4 hours): Manual recovery testing + validation
- Day 3 (2 hours): Update/skip unit tests + final validation

### If Re-Implementation Required (4-6 days)
- Day 1 (4 hours): Docker setup + integration tests + identify failure
- Day 2 (6 hours): Research RabbitMQ.Client 6.x recovery API
- Day 3 (6 hours): Re-implement recovery event handling
- Day 4 (4 hours): Integration testing + validation
- Day 5 (2 hours): Update unit tests + final validation

---

## Success Criteria

### Minimum for Release
- ✅ All integration tests passing
- ✅ Manual recovery testing successful
- ✅ 95%+ unit test pass rate (recovery tests resolved)
- ✅ No critical/high security vulnerabilities
- ✅ Performance within 10% of RawRabbit 2.x

### Recommended for Release
- ✅ 100% unit test pass rate
- ✅ Load testing successful (1000+ messages/sec)
- ✅ 24-hour stability test passed
- ✅ Documentation complete and accurate

---

## Commands Quick Reference

### Docker
```bash
# Start RabbitMQ
docker-compose up -d

# Check logs
docker logs rawrabbit-test -f

# Stop RabbitMQ
docker stop rawrabbit-test

# Restart RabbitMQ
docker restart rawrabbit-test

# Remove container
docker rm -f rawrabbit-test
```

### Testing
```bash
# Run all tests
dotnet test

# Run integration tests only
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj

# Run with verbose output
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
  --logger "console;verbosity=detailed"

# Run specific test
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
  --filter "FullyQualifiedName~ConnectionRecovery"
```

### Build
```bash
# Build all
dotnet build --no-restore

# Build integration tests
dotnet build test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj --no-restore
```

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Docker not available | Use cloud-hosted RabbitMQ (CloudAMQP free tier) |
| Integration tests fail | Document failures, proceed with unit tests only (NOT recommended for production) |
| Recovery doesn't work | Re-implement with RabbitMQ.Client 6.x API (4-6 days) |
| Tests take too long | Run in parallel, use faster RabbitMQ config |
| RabbitMQ connection issues | Check firewall, verify ports, check Docker networking |

---

## Next Actions (Priority Order)

1. **IMMEDIATE**: Install Docker (if not available)
2. **IMMEDIATE**: Fix IntegrationTests project (remove ZeroFormatter reference)
3. **TODAY**: Start RabbitMQ container
4. **TODAY**: Run integration tests
5. **TODAY/TOMORROW**: Manual recovery testing
6. **BASED ON RESULTS**: Update unit tests OR re-implement recovery
7. **FINAL**: Performance + security validation

---

## Support Resources

### RabbitMQ.Client 6.x Documentation
- API Guide: https://www.rabbitmq.com/client-libraries/dotnet-api-guide
- Recovery Guide: https://www.rabbitmq.com/client-libraries/dotnet#recovery
- GitHub: https://github.com/rabbitmq/rabbitmq-dotnet-client

### Docker
- Installation: https://docs.docker.com/get-docker/
- RabbitMQ Image: https://hub.docker.com/_/rabbitmq

### RawRabbit 3.0 Documentation
- CODE-MIGRATION-COMPLETE.md - What was changed
- TEST-FAILURES-REPORT.md - Current test issues
- TESTING-STATUS.md - Current test status
- MIGRATION-GUIDE.md - Consumer upgrade guide

---

## Conclusion

The RawRabbit 3.0 migration is **functionally complete** at the code level. The critical remaining work is **validation that automatic recovery works correctly** with RabbitMQ.Client 6.x.

**Estimated Time to Production Ready**: 2-6 days (depending on recovery validation results)

**Critical Path**: Integration testing with RabbitMQ → Recovery validation → Unit test resolution

**Blocker**: Docker/RabbitMQ environment required

---

**Document Version**: 1.0
**Created**: 2025-11-09
**Next Update**: After integration testing complete
**Owner**: Development Team
