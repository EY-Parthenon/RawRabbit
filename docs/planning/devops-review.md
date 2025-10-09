# DevOps Review: .NET 9 Upgrade Plan

**Reviewer**: DevOps Engineer
**Date**: 2025-10-09
**Document**: /home/laird/src/EYP/RawRabbit/docs/PLAN.md
**Focus**: CI/CD, Build Infrastructure, Deployment Strategy

---

## Executive Summary

The current .NET 9 upgrade plan has **CRITICAL DevOps gaps** that will create significant deployment risks. CI/CD is scheduled too late (Stage 7, Week 9-10), missing opportunities for continuous validation throughout the migration. The plan lacks essential infrastructure standardization, dependency management strategy, and rollback procedures.

**Overall Assessment**: NEEDS MAJOR REVISION

**Critical Issues**: 5
**Infrastructure Gaps**: 8
**Deployment Risks**: 6

---

## 1. CI/CD Pipeline: Stage 7.2 is TOO LATE

### Current Plan Problem

**Stage 7.2 (Week 9-10)**: "Update CI/CD pipelines for .NET 9"

**Risk Level**: CRITICAL - Red Flag

### Why This is a Problem

1. **No Continuous Validation**: Migration code (Stages 3-6) won't be validated in CI/CD
2. **Late Discovery of Issues**: Build/packaging problems discovered in final weeks
3. **Quality Assurance Gap**: Manual testing only, no automated build gates
4. **Integration Risk**: 25 packages built locally without CI validation
5. **No Safety Net**: Failed builds won't block broken commits

### Recommended Timeline Shift

CI/CD should be **Stage 1.5** (Week 2), right after foundation setup:

```
CURRENT (WRONG):
  Stage 1: Foundation
  Stage 2: Architecture
  Stage 3-6: Migration (no CI/CD)
  Stage 7: CI/CD setup <-- TOO LATE!

RECOMMENDED (CORRECT):
  Stage 1: Foundation
  Stage 1.5: CI/CD Setup <-- MOVE HERE
  Stage 2: Architecture
  Stage 3-6: Migration (validated by CI/CD)
  Stage 7: Deployment preparation
```

### What CI/CD Should Do Early

**Week 2 Implementation**:
1. Create .NET 9 build pipeline
2. Multi-platform test matrix (Windows/Linux/macOS)
3. Automated NuGet package generation
4. Dependency vulnerability scanning
5. Test report publication
6. Branch protection rules

---

## 2. Build Infrastructure Changes for .NET 9

### SDK Version Requirements

**Critical Infrastructure Change**:

```yaml
# BEFORE (.NET Standard 1.5 / .NET Framework 4.5.1)
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '2.1.x'  # Old SDK for .NET Standard 1.5

# AFTER (.NET 9)
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0.x'  # .NET 9 SDK required
```

### Build Agent Requirements

**Existing Infrastructure** (AppVeyor):
- Windows Server 2012 R2 (Visual Studio 2015)
- Single platform (Windows only)
- PowerShell-based build scripts
- **PROBLEM**: AppVeyor image outdated for .NET 9

**Required Infrastructure**:
- GitHub Actions (recommended) or Azure Pipelines
- Multi-OS support: `ubuntu-latest`, `windows-latest`, `macos-latest`
- .NET 9 SDK pre-installed
- RabbitMQ container support (Docker)

### Recommended Migration

**From**: `/home/laird/src/EYP/RawRabbit/.build/appveyor.yml`
**To**: `/home/laird/src/EYP/RawRabbit/.github/workflows/dotnet-build.yml`

**Why GitHub Actions?**:
- Already using GitHub (workflows exist: `claude.yml`, `claude-code-review.yml`)
- Native Docker support for RabbitMQ
- Free for public repositories
- Multi-platform matrix builds
- Better secret management

---

## 3. NuGet Package Strategy: 25 Separate Packages

### Current Plan Gap

**Stage 7.2**: "Configure NuGet package generation"

**Missing**:
- Package versioning strategy during migration
- Dependency version consistency across 25 projects
- Pre-release package distribution
- Package validation automation

### Critical Versioning Issues

**Problem**: 25 projects, each needs coordinated versioning

Current approach (manual):
```xml
<VersionPrefix>2.0.0</VersionPrefix>  <!-- Hardcoded in each .csproj -->
```

**Risks**:
1. **Version Mismatches**: Core 2.0.0, but Operations.Publish 1.9.9
2. **Dependency Conflicts**: Project A references Core 2.0.0, Project B references Core 2.0.1
3. **Manual Errors**: Forget to update version in 1 of 25 projects

### Recommended Solution: Centralized Versioning

**Create**: `/home/laird/src/EYP/RawRabbit/Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <!-- Centralized version for all packages -->
    <VersionPrefix>3.0.0</VersionPrefix>
    <VersionSuffix Condition="'$(GITHUB_REF)' != 'refs/heads/main'">alpha-$(GITHUB_RUN_NUMBER)</VersionSuffix>

    <!-- Common package metadata -->
    <Authors>pardahlman;enrique-avalon</Authors>
    <PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/pardahlman/RawRabbit</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- .NET 9 target -->
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Benefits**:
- Single source of truth for versions
- Automated alpha/beta versioning from CI/CD
- Consistent metadata across all packages
- Prevents version drift

### Package Distribution Strategy

**Pre-Release Channels**:

1. **Alpha Builds** (upgrade branch)
   - Version: `3.0.0-alpha-{build-number}`
   - Publish to: GitHub Packages or private NuGet feed
   - Consumers: Internal testing only

2. **Beta Builds** (beta branch)
   - Version: `3.0.0-beta.1`, `3.0.0-beta.2`
   - Publish to: NuGet.org (unlisted)
   - Consumers: Early adopters

3. **Release Candidate** (rc branch)
   - Version: `3.0.0-rc.1`
   - Publish to: NuGet.org (listed)
   - Consumers: Public beta testing

4. **Production** (main branch)
   - Version: `3.0.0`
   - Publish to: NuGet.org (listed)
   - Consumers: Everyone

---

## 4. Multi-Platform Testing Automation

### Current Plan Gap

**Stage 6.1**: "Build succeeds on Windows, Linux, macOS"

**Problem**: Manual testing mentioned, no automation strategy

### Required Test Matrix

**Platforms** (all must pass):
- Windows (latest)
- Linux (ubuntu-latest)
- macOS (latest)

**RabbitMQ Versions**:
- 3.11.x (current production)
- 3.12.x (latest)
- 3.13.x (preview) - optional

**Test Types**:
- Unit tests (all platforms)
- Integration tests (require RabbitMQ)
- Performance benchmarks (Linux only)
- Package installation tests (validate NuGet packages work)

### Recommended CI/CD Implementation

**File**: `/home/laird/src/EYP/RawRabbit/.github/workflows/dotnet-ci.yml`

```yaml
name: .NET 9 CI/CD

on:
  push:
    branches: [upgrade, main]
  pull_request:
    branches: [upgrade, main]

jobs:
  build-and-test:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        rabbitmq-version: ['3.11', '3.12']
    runs-on: ${{ matrix.os }}

    services:
      rabbitmq:
        image: rabbitmq:${{ matrix.rabbitmq-version }}-management
        ports:
          - 5672:5672
          - 15672:15672
        options: >-
          --health-cmd "rabbitmq-diagnostics -q ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore RawRabbit.sln

      - name: Build solution
        run: dotnet build RawRabbit.sln --configuration Release --no-restore

      - name: Run unit tests
        run: dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj --no-build --verbosity normal --logger "trx;LogFileName=unit-tests.trx"

      - name: Run integration tests
        run: dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj --no-build --verbosity normal --logger "trx;LogFileName=integration-tests.trx"
        env:
          RabbitMQ__Host: localhost
          RabbitMQ__Port: 5672

      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results (${{ matrix.os }}, RabbitMQ ${{ matrix.rabbitmq-version }})
          path: "**/*.trx"
          reporter: dotnet-trx
          fail-on-error: true

      - name: Upload test reports to docs/test
        if: always()
        run: |
          mkdir -p docs/test
          cp **/*.trx docs/test/

      - name: Publish artifacts
        uses: actions/upload-artifact@v4
        with:
          name: test-results-${{ matrix.os }}-rabbitmq-${{ matrix.rabbitmq-version }}
          path: docs/test/

  package:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/upgrade' || github.ref == 'refs/heads/main'

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Pack all projects
        run: |
          dotnet pack RawRabbit.sln \
            --configuration Release \
            --output ./artifacts \
            -p:VersionSuffix=alpha-${{ github.run_number }}

      - name: Publish packages to GitHub Packages
        run: |
          dotnet nuget push "./artifacts/*.nupkg" \
            --source https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json \
            --api-key ${{ secrets.GITHUB_TOKEN }} \
            --skip-duplicate

      - name: Upload NuGet packages
        uses: actions/upload-artifact@v4
        with:
          name: nuget-packages
          path: ./artifacts/*.nupkg
```

**Key Features**:
- Multi-platform matrix (Windows/Linux/macOS)
- RabbitMQ service container (Linux only, use local for Windows/macOS)
- Test results published to docs/test (as required)
- Automated NuGet packaging
- Pre-release versioning (alpha-{build-number})

---

## 5. Staging Environment: Inadequately Defined

### Current Plan

**Stage 8.1**: "Staged Rollout"
- Alpha Release (internal)
- Beta Release (early adopters)
- Release Candidate
- Production Release

**Problem**: No infrastructure details

### What's Missing?

1. **Alpha Environment**:
   - Where is it hosted?
   - How do internal testers access packages?
   - What RabbitMQ version?
   - Monitoring/telemetry setup?

2. **Beta Environment**:
   - Public NuGet feed or private?
   - How to collect feedback?
   - Issue tracking integration?

3. **Production Release**:
   - NuGet.org publication process
   - API key management
   - Release approval gates

### Recommended Staging Strategy

**Alpha Stage** (Week 10):
- **Infrastructure**: GitHub Packages (private feed)
- **Version**: `3.0.0-alpha-{build-number}`
- **Access**: Internal team only (GitHub org members)
- **Testing**: Manual testing with sample applications
- **Duration**: 1 week

**Beta Stage** (Week 11):
- **Infrastructure**: NuGet.org (unlisted packages)
- **Version**: `3.0.0-beta.1`, `3.0.0-beta.2`
- **Access**: Early adopters (announced on GitHub Discussions)
- **Testing**: Community feedback via GitHub Issues
- **Duration**: 2 weeks

**Release Candidate** (Week 12):
- **Infrastructure**: NuGet.org (listed packages)
- **Version**: `3.0.0-rc.1`
- **Access**: Public
- **Testing**: Production-like scenarios
- **Duration**: 1 week (feature freeze)

**Production** (Week 13):
- **Infrastructure**: NuGet.org (stable feed)
- **Version**: `3.0.0`
- **Access**: Everyone
- **Testing**: Post-release monitoring

---

## 6. Docker Standardization: Currently Optional

### Current Plan Issue

**Stage 7.2**: "Create Docker images (if applicable)"

**Problem**: "If applicable" is ambiguous for testing infrastructure

### Why Docker is NOT Optional

**Required for**:
1. **Integration Tests**: RabbitMQ instance needed
2. **Consistent Testing**: Same RabbitMQ version across platforms
3. **CI/CD Automation**: GitHub Actions service containers
4. **Local Development**: Easy setup for contributors

**Current State**:
- AppVeyor uses PowerShell to install RabbitMQ
- Manual installation required for local development
- Inconsistent RabbitMQ versions across environments

### Recommended Docker Strategy

**1. RabbitMQ Test Container**

**Create**: `/home/laird/src/EYP/RawRabbit/docker-compose.test.yml`

```yaml
version: '3.8'

services:
  rabbitmq-3.11:
    image: rabbitmq:3.11-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5

  rabbitmq-3.12:
    image: rabbitmq:3.12-management
    ports:
      - "5673:5672"
      - "15673:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5
```

**Usage**:
```bash
# Start RabbitMQ for testing
docker-compose -f docker-compose.test.yml up -d

# Run integration tests
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj

# Stop RabbitMQ
docker-compose -f docker-compose.test.yml down
```

**2. Optional: Application Container** (for samples)

```dockerfile
# /home/laird/src/EYP/RawRabbit/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and restore
COPY RawRabbit.sln .
COPY src/ src/
COPY test/ test/
COPY sample/ sample/
RUN dotnet restore RawRabbit.sln

# Build
RUN dotnet build RawRabbit.sln -c Release --no-restore

# Run tests
RUN dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj --no-build -c Release

# Package
FROM build AS package
RUN dotnet pack src/RawRabbit/RawRabbit.csproj -c Release -o /artifacts

# Final image with sample app
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /src/sample/RawRabbit.ConsoleApp.Sample/bin/Release/net9.0/ .
ENTRYPOINT ["dotnet", "RawRabbit.ConsoleApp.Sample.dll"]
```

---

## 7. Rollback Strategy: MISSING

### Current Plan Gap

**NO rollback strategy defined anywhere in PLAN.md**

**This is a CRITICAL omission.**

### What Could Go Wrong?

1. **NuGet Package Issue**: Published package has critical bug
2. **Breaking Change**: Consumers can't upgrade
3. **Performance Regression**: .NET 9 slower than expected
4. **RabbitMQ Incompatibility**: Works in testing, fails in production
5. **Dependency Conflict**: New RabbitMQ.Client breaks existing code

### Recommended Rollback Procedures

**Scenario 1: Pre-Release Bug (Alpha/Beta)**

**Action**:
- Unlist broken package from NuGet.org
- Publish patched version with incremented suffix (`3.0.0-beta.2`)
- Announce issue in GitHub Discussions
- No major impact (pre-release users expect instability)

**Rollback Time**: 1-2 hours

**Scenario 2: Production Critical Bug**

**Action**:
1. **Immediate**: Unlist 3.0.0 from NuGet.org
2. **Short-term** (24 hours):
   - Publish 3.0.1 with fix, OR
   - Publish 2.0.1 patch (maintain 2.x branch)
3. **Long-term**:
   - Post-mortem ADR documenting issue
   - Additional testing for similar scenarios

**Rollback Time**: 24 hours (critical path)

**Scenario 3: Cannot Complete Migration**

**Action**:
1. Abandon `upgrade` branch
2. Continue maintaining 2.x branch (.NET Standard 1.5)
3. Create `dotnet-standard-2.1` branch (intermediate upgrade)
4. Delay .NET 9 migration 6-12 months

**Decision Point**: End of Stage 3 (Core Migration)

**Scenario 4: Performance Regression**

**Action**:
1. Identify bottleneck with performance profiling
2. Optimize code or configuration
3. If unfixable: Document performance impact in migration guide
4. Users can choose to stay on 2.x

**Rollback Time**: N/A (performance is a documented limitation)

### Required: Version 2.x Maintenance Branch

**Create**: `2.x-maintenance` branch before starting upgrade

**Purpose**:
- Security patches for existing users
- Critical bug fixes
- Allows users to stay on 2.x if 3.0 upgrade not feasible

**Lifespan**: 12 months after 3.0.0 release

---

## 8. Dependency Management: Version Conflicts

### Current Plan Issue

**Stage 2.1**: "Plan NuGet package upgrade strategy"
**Stage 3.1**: "Update RabbitMQ.Client to 7.x"

**Problem**: No strategy for ensuring version consistency across 25 projects

### Dependency Conflict Scenarios

**Scenario A: Direct Version References**

Project A:
```xml
<PackageReference Include="RabbitMQ.Client" Version="7.0.0" />
```

Project B:
```xml
<PackageReference Include="RabbitMQ.Client" Version="7.1.0" />
```

**Result**: Runtime version conflict

**Scenario B: Transitive Dependencies**

```
RawRabbit.Operations.Publish -> RawRabbit 3.0.0 -> RabbitMQ.Client 7.0.0
RawRabbit.Enrichers.Polly -> RawRabbit 3.0.1 -> RabbitMQ.Client 7.1.0
```

**Result**: Consumer project gets unpredictable version

### Recommended Solution: Central Package Management

**Create**: `/home/laird/src/EYP/RawRabbit/Directory.Packages.props`

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core dependencies - SINGLE version across ALL projects -->
    <PackageVersion Include="RabbitMQ.Client" Version="7.0.0" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

    <!-- Optional: System.Text.Json (alternative to Newtonsoft.Json) -->
    <PackageVersion Include="System.Text.Json" Version="9.0.0" />

    <!-- Enricher-specific dependencies -->
    <PackageVersion Include="Polly" Version="8.5.0" />
    <PackageVersion Include="protobuf-net" Version="3.2.30" />
    <PackageVersion Include="MessagePack" Version="2.5.187" />
    <PackageVersion Include="ZeroFormatter" Version="1.6.4" />

    <!-- DI adapters -->
    <PackageVersion Include="Autofac" Version="8.1.0" />
    <PackageVersion Include="Ninject" Version="3.3.6" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />

    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="Moq" Version="4.20.72" />
  </ItemGroup>
</Project>
```

**Update all .csproj files**:

```xml
<!-- BEFORE -->
<PackageReference Include="RabbitMQ.Client" Version="5.0.1" />

<!-- AFTER (version centralized) -->
<PackageReference Include="RabbitMQ.Client" />
```

**Benefits**:
- Single source of truth for dependency versions
- Prevents version conflicts across 25 projects
- Easy to update all projects at once
- Enforced by build system

---

## Summary: Critical DevOps Improvements

### DevOps Practices That Are Well-Defined

- Staged rollout approach (Alpha -> Beta -> RC -> Production)
- Multi-platform testing requirement (Windows/Linux/macOS)
- Test coverage target (90%+)
- Security audit checkpoints
- Documentation requirements

### Infrastructure Gaps with Specific Recommendations

| Gap | Current State | Recommendation | Timeline |
|-----|---------------|----------------|----------|
| **CI/CD Timing** | Stage 7 (Week 9-10) | **Stage 1.5 (Week 2)** | Move up 7 weeks |
| **Build Infrastructure** | AppVeyor (outdated) | GitHub Actions | Week 2 |
| **Package Versioning** | Manual per-project | `Directory.Build.props` | Week 2 |
| **Dependency Management** | Uncoordinated | `Directory.Packages.props` | Week 2 |
| **Docker Strategy** | "If applicable" | **REQUIRED** for testing | Week 2 |
| **Rollback Plan** | Missing | Document all scenarios | Week 2 |
| **Staging Infrastructure** | Vague | Define GitHub Packages + NuGet.org | Week 10 |
| **Multi-platform Testing** | Manual | Automated CI matrix | Week 2 |

### Critical Deployment Risks

**Red (Critical)**:
1. CI/CD setup in Stage 7 (too late, no continuous validation)
2. No rollback strategy (what if production release fails?)
3. No dependency version management (25 projects, version chaos)

**Yellow (High)**:
1. Docker "if applicable" (should be mandatory for RabbitMQ testing)
2. Manual multi-platform testing (should be automated)
3. No SDK version standardization (local vs. CI mismatch)

**Orange (Medium)**:
1. Staging infrastructure undefined (where/how to publish alpha/beta?)
2. Test report automation missing (manual copy to docs/test?)
3. Package validation not automated (did we test installing the package?)

### Required CI/CD Pipeline Changes (with Timing)

**Week 2 (Stage 1.5): Initial Setup** CRITICAL
- Create `.github/workflows/dotnet-ci.yml`
- Multi-platform build matrix (Windows/Linux/macOS)
- RabbitMQ service containers
- Automated test execution
- Test result publishing to docs/test
- Branch protection rules

**Week 2 (Stage 1.5): Package Infrastructure** CRITICAL
- Create `Directory.Build.props` (centralized versioning)
- Create `Directory.Packages.props` (dependency management)
- Create `docker-compose.test.yml` (RabbitMQ testing)
- Configure GitHub Packages (alpha package feed)

**Week 3-9 (Stages 3-6): Continuous Validation**
- All commits automatically built/tested
- Test results published after each run
- Package generation on `upgrade` branch
- Security scanning on every build

**Week 10 (Stage 7): Pre-Production**
- Add NuGet.org publishing workflow (manual approval gate)
- Add release notes generation
- Add GitHub Release creation
- Add deployment smoke tests

**Week 10-12 (Stage 8): Production Release**
- Publish to NuGet.org (manual trigger)
- Create Git tag (automated)
- Generate changelog (automated)
- Announce release (manual)

### Infrastructure Standardization Recommendations

**1. Adopt GitHub Actions** (replace AppVeyor)
- Already using GitHub for code
- Better Docker support
- Multi-platform matrix builds
- Free for public repos

**2. Centralized Configuration**
- `Directory.Build.props` - versions, metadata
- `Directory.Packages.props` - dependency versions
- `.editorconfig` - code style
- `global.json` - .NET SDK version

**3. Docker Standardization**
- `docker-compose.test.yml` - RabbitMQ for testing
- CI uses same Docker images as local development
- Consistent testing environment

**4. Automated Testing**
- Unit tests on every commit
- Integration tests with RabbitMQ containers
- Performance benchmarks on main branch
- Test results published to docs/test/

**5. Package Management**
- GitHub Packages for alpha/beta builds
- NuGet.org for RC/production
- Automated versioning (semver)
- Package validation tests

---

## Recommended Plan Revisions

### CRITICAL CHANGE 1: Move CI/CD to Stage 1.5

**Before**:
```
Stage 1: Foundation (Week 1-2)
Stage 2: Architecture (Week 2-3)
Stage 3-6: Migration (Week 3-9)
Stage 7: CI/CD Setup (Week 9-10)  <-- TOO LATE
```

**After**:
```
Stage 1: Foundation (Week 1-2)
Stage 1.5: CI/CD & Infrastructure (Week 2)  <-- ADD THIS
Stage 2: Architecture (Week 2-3)
Stage 3-6: Migration (Week 3-9, validated by CI/CD)
Stage 7: Deployment Preparation (Week 9-10)
```

### CRITICAL CHANGE 2: Add Stage 1.5 Tasks

**New Stage 1.5: CI/CD & Infrastructure Setup (Week 2)**

**Agent**: DevOps Engineer

**Tasks**:
- [ ] Create GitHub Actions workflow (`.github/workflows/dotnet-ci.yml`)
- [ ] Setup multi-platform matrix (Windows/Linux/macOS)
- [ ] Configure RabbitMQ service containers (Docker)
- [ ] Create centralized version management (`Directory.Build.props`)
- [ ] Create centralized dependency management (`Directory.Packages.props`)
- [ ] Setup branch protection rules (require CI to pass)
- [ ] Configure GitHub Packages (alpha package feed)
- [ ] Create `docker-compose.test.yml` for local development
- [ ] Document rollback procedures
- [ ] Create `2.x-maintenance` branch

**Deliverables**:
- Working CI/CD pipeline
- Automated multi-platform testing
- Centralized configuration files
- Rollback documentation
- `docs/adr/0008-cicd-infrastructure.md`

### CRITICAL CHANGE 3: Update Stage 7.2

**Before**:
```
7.2 Build & Packaging
- [ ] Update CI/CD pipelines for .NET 9
- [ ] Configure NuGet package generation
- [ ] Test package installation scenarios
- [ ] Validate package metadata
- [ ] Create Docker images (if applicable)
```

**After**:
```
7.2 Deployment Preparation
- [ ] Add NuGet.org publishing workflow (manual approval)
- [ ] Create release notes generation script
- [ ] Configure GitHub Release automation
- [ ] Add deployment smoke tests
- [ ] Finalize staging environment (GitHub Packages -> NuGet.org)
- [ ] Test rollback procedures
```

### RECOMMENDED CHANGE 4: Add Dependency Management to Stage 2

**Stage 2.1 Architecture Design**

**Add to Key Decisions**:
```
4. **Dependency Version Management**:
   - Use Directory.Packages.props for centralized versions
   - Pin all transitive dependencies
   - Single source of truth for dependency versions
```

**Add Task**:
- [ ] Audit all 25 projects for dependency versions
- [ ] Create dependency upgrade matrix (current -> target)
- [ ] Identify potential version conflicts
- [ ] Plan migration order to avoid conflicts

---

## Conclusion

The current .NET 9 upgrade plan has **significant DevOps gaps** that will create deployment risks if not addressed early. The most critical issue is CI/CD setup scheduled for Stage 7 (Week 9-10), which is far too late.

**Key Recommendations**:

1. MOVE CI/CD to Stage 1.5 (Week 2) - CRITICAL
2. ADD centralized version and dependency management - CRITICAL
3. STANDARDIZE on GitHub Actions (replace AppVeyor) - CRITICAL
4. REQUIRE Docker for RabbitMQ testing (not optional) - HIGH
5. DOCUMENT rollback procedures before starting migration - HIGH
6. AUTOMATE multi-platform testing (not manual) - MEDIUM
7. DEFINE staging infrastructure details - MEDIUM

**Without these changes**, the migration will face:
- Late discovery of build/packaging issues
- Version conflicts across 25 packages
- No automated validation during migration
- High risk production deployment
- No clear rollback path

**Timeline Impact**: These recommendations add 1 week (Stage 1.5) but reduce overall risk and prevent late-stage surprises that could add 2-4 weeks of rework.

---

**Approval Status**: REQUIRES REVISION
**Next Steps**: Update PLAN.md with Stage 1.5 and revised Stage 7.2
**ADR Required**: Yes - `docs/adr/0008-cicd-infrastructure.md`
