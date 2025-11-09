# Developer Quick-Start Guide - RawRabbit 3.0 Code Migration

**Audience**: Developers who will complete the RawRabbit 3.0 code migration
**Estimated Time**: 21-32 days
**Prerequisites**: Senior .NET developer experience, RabbitMQ knowledge helpful

---

## TL;DR - What You Need to Do

1. **Set up .NET 8 SDK** environment
2. **Attempt to build** the solution (`dotnet build`)
3. **Fix ~60 files** for RabbitMQ.Client 6.x (follow guide)
4. **Fix ~8 files** for Polly 8.x (follow guide)
5. **Run tests** and fix failures (156+ tests)
6. **Validate** with integration tests and security scan

**Estimated Effort**: 21-32 days for experienced developer

---

## Phase 0: Environment Setup (1-2 hours)

### Step 1: Install .NET 8 SDK

**Windows**:
```powershell
# Download and install from:
https://dotnet.microsoft.com/download/dotnet/8.0

# Verify
dotnet --version  # Should show 8.0.x
```

**macOS**:
```bash
brew install dotnet-sdk

# Verify
dotnet --version
```

**Linux (Ubuntu)**:
```bash
wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Verify
dotnet --version
```

### Step 2: Clone and Inspect Repository

```bash
# Clone
git clone <repository-url>
cd RawRabbit

# Inspect structure
ls -la

# Key directories:
# - src/          : 25 source projects
# - test/         : 4 test projects
# - docs/         : All documentation
# - docs/adr/     : Architecture decisions
```

### Step 3: Set Up Docker RabbitMQ

```bash
# Start RabbitMQ for integration testing
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management

# Verify
docker ps | grep rabbitmq

# Access management UI: http://localhost:15672
# Credentials: guest/guest
```

### Step 4: Attempt Initial Build

```bash
# Restore NuGet packages
dotnet restore

# Attempt build (WILL FAIL - expected)
dotnet build 2>&1 | tee build-errors.txt

# Review errors
less build-errors.txt
```

**Expected**: Many compilation errors related to RabbitMQ.Client 6.x and Polly 8.x

---

## Phase 1: Understand the Codebase (4-8 hours)

### Read Documentation (Priority Order)

1. **[MODERNIZATION-STATUS.md](MODERNIZATION-STATUS.md)** (15 min)
   - Current status (45% complete)
   - What's done, what's remaining
   - Blockers and risks

2. **[RABBITMQ-CLIENT-6-MIGRATION.md](RABBITMQ-CLIENT-6-MIGRATION.md)** (1 hour)
   - ~60 files requiring changes
   - API mapping tables (5.x → 6.x)
   - Code examples (before/after)
   - File-by-file breakdown

3. **[POLLY-8-MIGRATION.md](POLLY-8-MIGRATION.md)** (30 min)
   - ~8 files requiring changes
   - Policy → ResiliencePipeline migration
   - Code examples

4. **[ADRs in docs/adr/](adr/)** (30 min)
   - Why .NET 8? (ADR-001)
   - Why RabbitMQ.Client 6.8.1? (ADR-002)
   - Why remove ZeroFormatter? (ADR-003)
   - Dependency strategy (ADR-004)
   - Versioning strategy (ADR-005)

5. **[CLAUDE.md](../CLAUDE.md)** (15 min)
   - Original project overview
   - Architecture explanation
   - Build and test instructions

### Explore the Codebase

```bash
# Count lines of code
find src -name "*.cs" | xargs wc -l

# Find RabbitMQ.Client usages
grep -r "IModel" src/ | wc -l
grep -r "IConnection" src/ | wc -l
grep -r "EventingBasicConsumer" src/ | wc -l

# Find Polly usages
grep -r "Policy" src/RawRabbit.Enrichers.Polly/ | wc -l
grep -r "IAsyncPolicy" src/ | wc -l

# Review key files
cat src/RawRabbit/Channel/ChannelFactory.cs
cat src/RawRabbit/Consumer/ConsumerFactory.cs
cat src/RawRabbit/Pipe/Middleware/BasicPublishMiddleware.cs
```

---

## Phase 2: Code Migration - RabbitMQ.Client 6.x (12-18 days)

### Week 1: Channel Management (3-5 days)

Follow **[RABBITMQ-CLIENT-6-MIGRATION.md](RABBITMQ-CLIENT-6-MIGRATION.md)** - Category 1

**Files** (priority order):
1. `src/RawRabbit/Channel/ChannelFactory.cs` ⚠️ CRITICAL
2. `src/RawRabbit/Channel/StaticChannelPool.cs`
3. `src/RawRabbit/Channel/DynamicChannelPool.cs`
4. `src/RawRabbit/Channel/AutoScalingChannelPool.cs`
5. `src/RawRabbit/Channel/ResilientChannelPool.cs`
6. Channel middleware (3 files)

**Key Issues**:
- `IRecoverable.Recovery` event signature
- Connection recovery pattern
- Channel lifetime management

**Workflow**:
```bash
# Create feature branch
git checkout -b feature/rabbitmq-6-channel-management

# Fix ChannelFactory.cs
code src/RawRabbit/Channel/ChannelFactory.cs

# Build and test incrementally
dotnet build src/RawRabbit/RawRabbit.csproj
dotnet test test/RawRabbit.Tests/

# Commit when tests pass
git add .
git commit -m "fix: Update ChannelFactory for RabbitMQ.Client 6.x"

# Repeat for each file
```

### Week 2: Consumer API (3-5 days)

Follow **[RABBITMQ-CLIENT-6-MIGRATION.md](RABBITMQ-CLIENT-6-MIGRATION.md)** - Category 2

**Files** (priority order):
1. `src/RawRabbit/Consumer/ConsumerFactory.cs` ⚠️ CRITICAL
2. Consumer middleware (7 files)
3. Subscription management (2 files)

**Key Issues**:
- `EventingBasicConsumer` constructor
- Consumer tag handling
- Message acknowledgment patterns

### Week 3: Publishing, Operations, Testing (4-6 days)

Follow **[RABBITMQ-CLIENT-6-MIGRATION.md](RABBITMQ-CLIENT-6-MIGRATION.md)** - Categories 3-6

**Files**:
- Publishing middleware (15 files)
- Topology management (5 files)
- DI & configuration (10 files)
- Test mocks and integration tests (5 files)

**Key Issues**:
- BasicPublish signature validation
- Queue/exchange declaration APIs
- Mock updates for tests

---

## Phase 3: Code Migration - Polly 8.x (3-5 days)

### Day 1: Core Plugin & Middleware (2 days)

Follow **[POLLY-8-MIGRATION.md](POLLY-8-MIGRATION.md)** - Categories 1-2

**Files**:
1. `src/RawRabbit.Enrichers.Polly/Middleware/PolicyMiddleware.cs`
2. 9 middleware wrappers (update `IAsyncPolicy` → `ResiliencePipeline`)
3. Services (ChannelFactory wrapper)

**Workflow**:
```bash
# Create feature branch
git checkout -b feature/polly-8-migration

# Update PolicyMiddleware
code src/RawRabbit.Enrichers.Polly/Middleware/PolicyMiddleware.cs

# Build incrementally
dotnet build src/RawRabbit.Enrichers.Polly/

# Run Polly tests
dotnet test test/RawRabbit.Enrichers.Polly.Tests/

# Commit when passing
git add .
git commit -m "feat: Update Polly enricher to 8.x API"
```

### Day 2-3: Update Tests (1 day)

**Files**:
- `test/RawRabbit.Enrichers.Polly.Tests/*`

**Tasks**:
- Update all test policies from Polly 5.x → 8.x
- Update assertions for new API
- Validate middleware behavior

---

## Phase 4: Testing & Validation (4-6 days)

### Day 1-2: Unit Tests (2 days)

```bash
# Run all unit tests
dotnet test --logger "console;verbosity=detailed"

# Target: 100% pass rate (156+ tests)

# If failures:
dotnet test --filter "FullyQualifiedName~ChannelFactory"
# Fix issues, re-run

# Generate coverage report (optional)
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover
```

### Day 3-4: Integration Tests (2 days)

```bash
# Ensure RabbitMQ is running
docker ps | grep rabbitmq

# Run integration tests
dotnet test test/RawRabbit.IntegrationTests/ \
  --logger "console;verbosity=detailed"

# Test scenarios:
# - Connection establishment
# - Channel creation and pooling
# - Message publishing end-to-end
# - Message consumption end-to-end
# - Request/response pattern
# - Connection recovery
```

### Day 5: Security Scan (1 day)

```bash
# Scan for vulnerable packages
dotnet list package --vulnerable --include-transitive

# Expected results:
# - Zero CRITICAL vulnerabilities
# - Zero HIGH vulnerabilities
# - Some MEDIUM/LOW (acceptable)

# Target security score: ≥45/100

# If issues found:
# - Update specific packages
# - Document exceptions (if unavoidable)
```

### Day 6: Performance Benchmarks (1 day)

```bash
# Run performance tests
dotnet run --project test/RawRabbit.PerformanceTest/ \
  -c Release

# Compare against baseline (if available)
# Target: No >10% regression

# Benchmarks:
# - Message publish throughput
# - Message consume throughput
# - Channel creation overhead
# - Connection recovery time
```

---

## Phase 5: Documentation & Release (2-3 days)

### Day 1: Final Documentation Review

```bash
# Review all documentation
cat CHANGELOG.md
cat MIGRATION-GUIDE.md
cat docs/MODERNIZATION-STATUS.md

# Update status to 100%
code docs/MODERNIZATION-STATUS.md

# Update README
cat README-3.0.md
# Consider replacing README.md with README-3.0.md
```

### Day 2: Prepare Release

```bash
# Create release branch
git checkout -b release/3.0.0

# Update version numbers (should already be 3.0.0)
grep -r "VersionPrefix" src/**/*.csproj

# Tag release
git tag -a v3.0.0 -m "RawRabbit 3.0.0 - .NET 8 Modernization"

# Build release packages
dotnet pack -c Release -o ./artifacts

# Verify NuGet packages
ls -lh artifacts/*.nupkg
```

### Day 3: Internal Validation

```bash
# Create test project using 3.0.0 packages
mkdir ../RawRabbit-Test
cd ../RawRabbit-Test
dotnet new console

# Add local NuGet source
dotnet nuget add source ../RawRabbit/artifacts

# Install RawRabbit 3.0.0
dotnet add package RawRabbit --version 3.0.0

# Write simple test
# - Connect to RabbitMQ
# - Publish message
# - Consume message
# - Verify end-to-end

# Run test
dotnet run

# If successful: Release is ready
```

---

## Common Issues & Solutions

### Issue 1: IRecoverable.Recovery Event Mismatch

**Error**:
```
Error CS1061: 'IRecoverable' does not contain a definition for 'Recovery'
```

**Solution**:
1. Check RabbitMQ.Client 6.8.1 documentation
2. Update event handler signature
3. Test connection recovery with Docker RabbitMQ

**Example Fix**:
```csharp
// Before (5.x)
EventHandler<EventArgs> completeTask = (sender, args) => { /* ... */ };
recoverable.Recovery += completeTask;

// After (6.x) - validate exact signature
// May be: EventHandler<RecoveryEventArgs> or similar
// Check RabbitMQ.Client 6.8.1 docs for correct type
```

### Issue 2: EventingBasicConsumer Constructor

**Error**:
```
Error CS1729: 'EventingBasicConsumer' does not contain a constructor that takes 1 argument
```

**Solution**:
1. Check RabbitMQ.Client 6.8.1 EventingBasicConsumer constructor
2. Update all `new EventingBasicConsumer(channel)` calls
3. May need additional parameters

### Issue 3: Polly Policy → ResiliencePipeline

**Error**:
```
Error CS0246: The type or namespace name 'IAsyncPolicy' could not be found
```

**Solution**:
```csharp
// Before (Polly 5.x)
IAsyncPolicy policy = context.Get<IAsyncPolicy>(PolicyKeys.BasicPublish);
await policy.ExecuteAsync(() => base.InvokeAsync(context, token));

// After (Polly 8.x)
ResiliencePipeline pipeline = context.Get<ResiliencePipeline>(PolicyKeys.BasicPublish);
await pipeline.ExecuteAsync(async ct => await base.InvokeAsync(context, ct), token);
```

### Issue 4: Tests Failing After Migration

**Symptoms**: Tests pass individually but fail in batch

**Solution**:
- Check for shared state (static variables)
- Ensure proper dispose/cleanup
- Run tests with `--no-parallel` flag
- Check RabbitMQ connection limits

---

## Workflow Best Practices

### Daily Routine

```bash
# 1. Start of day: Pull latest
git pull origin main

# 2. Create/switch to feature branch
git checkout -b feature/[component-name]

# 3. Make changes to 1-3 files max

# 4. Build incrementally
dotnet build [specific-project]

# 5. Run related tests
dotnet test [specific-test-project]

# 6. Commit when tests pass
git add .
git commit -m "fix: [description]"

# 7. Push regularly
git push origin feature/[component-name]

# 8. End of day: Update status doc
code docs/MODERNIZATION-STATUS.md
```

### Testing Strategy

**Incremental Testing** (Recommended):
```bash
# Test after each file change
dotnet test test/RawRabbit.Tests/ --filter "FullyQualifiedName~ChannelFactory"

# Test after each component
dotnet test test/RawRabbit.Tests/

# Full suite after each phase
dotnet test

# Integration tests after major changes
dotnet test test/RawRabbit.IntegrationTests/
```

**Continuous Integration** (If available):
- Set up CI pipeline to run tests on every commit
- Fail build if tests don't pass
- Track code coverage metrics

---

## Progress Tracking

### Daily Checklist

```markdown
## Day [N] - [Date]

### Completed
- [ ] Fixed [File1.cs] - [Issue description]
- [ ] Fixed [File2.cs] - [Issue description]
- [ ] Tests passing: [N/156]
- [ ] Build status: [Passing/Failing]

### Blockers
- [ ] [Blocker description]

### Next Steps
- [ ] Fix [NextFile.cs]
- [ ] Investigate [Issue]
```

### Weekly Status Update

Update `docs/MODERNIZATION-STATUS.md` weekly:
```markdown
## Week [N] Status

- **Completion**: [N]%
- **Files Updated**: [N/68]
- **Tests Passing**: [N/156]
- **Blockers**: [Description or "None"]
- **On Track**: Yes/No
```

---

## Getting Help

### Resources

**Official Documentation**:
- [RabbitMQ.Client 6.x Guide](https://www.rabbitmq.com/dotnet-api-guide.html)
- [Polly 8.x Documentation](https://www.pollydocs.org/)
- [.NET 8 Migration Guide](https://learn.microsoft.com/en-us/dotnet/core/migration/)

**Internal Documentation**:
- All files in `docs/` directory
- All ADRs in `docs/adr/`
- CHANGELOG.md and MIGRATION-GUIDE.md

**Community**:
- Stack Overflow (tag: `rabbitmq` + `rabbitmq-.net-client`)
- RabbitMQ Mailing List
- Polly Discussions

---

## Success Criteria

### Phase 2 Complete When:
- [ ] Solution builds with zero errors
- [ ] Zero compilation warnings in Release mode
- [ ] All 68 files updated (60 RabbitMQ.Client + 8 Polly)

### Phase 3 Complete When:
- [ ] 100% test pass rate (156+ tests)
- [ ] Integration tests passing
- [ ] Security score ≥45
- [ ] Zero CRITICAL/HIGH vulnerabilities
- [ ] Code coverage ≥80%

### Phase 4 Complete When:
- [ ] Documentation reviewed and updated
- [ ] Release packages built
- [ ] Internal validation complete
- [ ] Git tagged (v3.0.0)

### Project Complete When:
- [ ] All phases complete
- [ ] NuGet packages published (if applicable)
- [ ] Release notes announced
- [ ] Handoff to maintenance team

---

## Timeline Expectations

| Phase | Estimated | Your Progress |
|-------|-----------|---------------|
| Phase 0: Setup | 1-2 hours | [ ] |
| Phase 1: Learning | 4-8 hours | [ ] |
| Phase 2A: RabbitMQ.Client | 12-18 days | [ ] |
| Phase 2B: Polly | 3-5 days | [ ] |
| Phase 3: Testing | 4-6 days | [ ] |
| Phase 4: Release | 2-3 days | [ ] |
| **Total** | **21-32 days** | **[ ]** |

**Actual Calendar Time**: 5-7 weeks (accounting for interruptions, meetings, etc.)

---

## Final Checklist

### Before Starting
- [ ] Read all documentation (8 files, ~10,000 lines)
- [ ] .NET 8 SDK installed and verified
- [ ] Docker RabbitMQ running
- [ ] Repository cloned and inspected
- [ ] Initial build attempted and errors captured

### During Implementation
- [ ] Working in feature branches (not main)
- [ ] Committing frequently (after each file/component)
- [ ] Testing incrementally (not batched at end)
- [ ] Updating status docs weekly
- [ ] Asking for help when blocked

### Before Release
- [ ] 100% test pass rate achieved
- [ ] Security scan complete (≥45 score)
- [ ] Performance validated (no regressions)
- [ ] Documentation updated
- [ ] Release packages built and tested
- [ ] Git tagged

---

**Good luck!** The documentation is comprehensive. Follow the guides, test continuously, and you'll succeed.

**Questions?** Review the documentation first:
1. [RABBITMQ-CLIENT-6-MIGRATION.md](RABBITMQ-CLIENT-6-MIGRATION.md)
2. [POLLY-8-MIGRATION.md](POLLY-8-MIGRATION.md)
3. [MODERNIZATION-STATUS.md](MODERNIZATION-STATUS.md)

---

**Last Updated**: 2025-11-09
**Document Owner**: Migration Coordinator
**Target Audience**: Developer implementing Phase 2-4
