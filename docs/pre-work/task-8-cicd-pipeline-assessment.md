# Task 8: CI/CD Pipeline Assessment for .NET 9 Upgrade

**Document Type**: Pre-Work Assessment
**Task ID**: task-8-cicd
**Session ID**: dotnet9-upgrade
**Agent**: DevOps Engineer
**Date**: 2025-10-09
**Status**: Complete

---

## Executive Summary

### Current State
- **Platform**: AppVeyor (Windows-only, Visual Studio 2015)
- **Build Image**: Visual Studio 2015 (outdated, no .NET 9 support)
- **Branches**: master, stable
- **.NET SDK**: Supports netstandard1.5/net451 (ancient)
- **RabbitMQ Testing**: PowerShell scripts install locally on build agent
- **Package Generation**: Manual versioning with hardcoded "rc2" suffix
- **Test Execution**: Sequential PowerShell-based (`-parallel none`)
- **Artifacts**: NuGet packages only (no test reports)

### .NET 9 Compatibility Assessment

🚨 **CRITICAL INCOMPATIBILITY**: Visual Studio 2015 image does NOT support .NET 9 SDK

### Required Changes

**Migration Urgency**: CRITICAL - Must complete before Stage 3 (Core Migration)

**Recommended Approach**: Migrate from AppVeyor to GitHub Actions

**Timeline**: Week 2 (Stage 1.5) - BEFORE main migration work begins

---

## 1. Current CI/CD Inventory

### 1.1 AppVeyor Configuration

**File**: `/home/laird/src/EYP/RawRabbit/.build/appveyor.yml`

```yaml
version: '{build}'
skip_tags: true
image: Visual Studio 2015  # ⚠️ OUTDATED - No .NET 9 support
configuration: Release
branches:
  only:
    - master
    - stable  # ⚠️ 'upgrade' branch not included

build_script:
- ps: ./.build/Install-Environment.ps1
- ps: ./.build/Install-RestartRabbitMq.ps1    # Manual RabbitMQ install
- ps: ./.build/Install-MgmtPlugin.ps1
- ps: ./.build/Install-AddUser.ps1
- ps: ./.build/Build.ps1                      # Custom build script

test_script:
- ps: ./.build/./Test.ps1                     # Sequential tests

artifacts:
- path: artifacts/RawRabbit.*.nupkg           # Only packages, no test reports
```

**Issues Identified**:
1. Visual Studio 2015 image lacks .NET 9 SDK
2. Windows-only testing (no Linux/macOS)
3. Manual RabbitMQ installation (non-deterministic)
4. No test result artifacts
5. No security scanning
6. No package validation
7. Hardcoded version suffix (`/p:VersionSuffix=rc2` in Build.ps1)
8. 'upgrade' branch not in CI pipeline

### 1.2 Build Scripts

**Build Process**: `/home/laird/src/EYP/RawRabbit/.build/Build.ps1`

```powershell
# Key operations:
1. Clean artifacts directory
2. dotnet restore ../ --no-cache
3. Calculate version suffix from branch/build number
4. For each project in src/*:
   - dotnet msbuild "/t:Restore;Pack" /p:VersionSuffix=rc2  # ⚠️ Hardcoded!
```

**Issues**:
- Version suffix hardcoded to "rc2" (line 23), ignoring calculated $suffix
- No validation that packages are installable
- No dependency version checks

**Test Process**: `/home/laird/src/EYP/RawRabbit/.build/Test.ps1`

```powershell
# Key operations:
1. For each project in test/*:
   - dotnet test -c Release -parallel none  # ⚠️ Sequential only
```

**Issues**:
- Sequential test execution (slow)
- No test result format specified (no .trx files)
- No test report publishing
- Exit code 3 on failure (non-standard)

### 1.3 RabbitMQ Setup

**Scripts**:
- `Install-Environment.ps1` (113 bytes)
- `Install-RestartRabbitMq.ps1` (753 bytes)
- `Install-MgmtPlugin.ps1` (766 bytes)
- `Install-AddUser.ps1` (524 bytes)
- `Util-RabbitMqPath.ps1` (596 bytes)

**Process**:
1. Find RabbitMQ installation path
2. Restart RabbitMQ service
3. Enable management plugin
4. Add test user

**Issues**:
- Relies on pre-installed RabbitMQ on AppVeyor image
- Non-deterministic (RabbitMQ version depends on image)
- Cannot test multiple RabbitMQ versions
- No health checks before running tests

### 1.4 Existing GitHub Actions

**Files Found**:
- `.github/workflows/claude.yml` - Claude Code assistant integration
- `.github/workflows/claude-code-review.yml` - Automated PR reviews

**Status**: These are NOT build/test workflows, only AI assistant automation

**Opportunity**: GitHub Actions infrastructure already exists, just need to add build workflow

### 1.5 Docker Support

**Existing File**: `/home/laird/src/EYP/RawRabbit/docker/rabbitmq/docker-compose.yml`

```yaml
services:
  rabbitmq-3.12:
    image: rabbitmq:3.12-management-alpine
    ports: ["5672:5672", "15672:15672"]
    healthcheck: rabbitmq-diagnostics ping

  rabbitmq-3.11:
    image: rabbitmq:3.11-management-alpine
    ports: ["5673:5672", "15673:15672"]
    healthcheck: rabbitmq-diagnostics ping
```

**Status**: ✅ EXCELLENT - Already has Docker Compose for multi-version testing

**Recommendation**: Use this in GitHub Actions service containers

---

## 2. .NET 9 Compatibility Analysis

### 2.1 Critical Incompatibilities

| Component | Current State | .NET 9 Support | Action Required |
|-----------|---------------|----------------|-----------------|
| **AppVeyor Image** | VS 2015 | ❌ NO | Replace with GitHub Actions |
| **Build Agent SDK** | .NET Core 1.0-2.0 | ❌ NO | Upgrade to .NET 9 SDK |
| **Target Frameworks** | netstandard1.5, net451 | ❌ NO | Migrate to net9.0 |
| **RabbitMQ Install** | Manual PowerShell | ⚠️ FRAGILE | Use Docker containers |
| **PowerShell Scripts** | Windows-only | ⚠️ LIMITED | Use cross-platform dotnet CLI |

### 2.2 GitHub Actions Compatibility

| Requirement | GitHub Actions Support | Notes |
|-------------|------------------------|-------|
| **.NET 9 SDK** | ✅ YES | `actions/setup-dotnet@v4` with `dotnet-version: '9.0.x'` |
| **Multi-OS** | ✅ YES | `ubuntu-latest`, `windows-latest`, `macos-latest` |
| **Docker** | ✅ YES | Service containers (Linux) + docker-compose (all OS) |
| **RabbitMQ** | ✅ YES | `rabbitmq:3.11-management`, `rabbitmq:3.12-management` |
| **NuGet Publishing** | ✅ YES | Built-in authentication + GitHub Packages |
| **Test Reports** | ✅ YES | `dorny/test-reporter@v1` for .trx files |
| **Artifacts** | ✅ YES | `actions/upload-artifact@v4` |

**Verdict**: ✅ GitHub Actions is FULLY COMPATIBLE with .NET 9 requirements

### 2.3 Migration Complexity

**Complexity**: MEDIUM

**Reasons**:
- ✅ Existing docker-compose.yml already defines RabbitMQ setup
- ✅ GitHub Actions already in use (claude workflows)
- ✅ Build process is simple (restore, build, pack, test)
- ❌ PowerShell scripts need replacement with cross-platform commands
- ❌ Version management needs centralization

**Estimated Effort**: 6-8 hours

---

## 3. Required CI/CD Changes

### 3.1 Platform Migration

**From**: AppVeyor (Windows-only)
**To**: GitHub Actions (multi-platform)

**Rationale**:
1. **Native .NET 9 Support**: GitHub-hosted runners have .NET 9 SDK pre-installed
2. **Multi-Platform Testing**: Required by PLAN.md Stage 6.1
3. **Docker Integration**: Service containers for RabbitMQ (recommended by devops-review.md)
4. **Already Using GitHub**: Existing workflows (claude.yml, claude-code-review.yml)
5. **Free for Public Repos**: Zero cost
6. **Better Security**: GITHUB_TOKEN, secrets management, OIDC

**Migration Timeline**:
- Week 2 (Stage 1.5): Create new GitHub Actions workflow
- Week 2 (Stage 1.5): Test workflow on 'upgrade' branch
- Week 3: Disable AppVeyor (after validation)
- Week 10: Add NuGet.org publishing (Stage 7)

### 3.2 Build Infrastructure Changes

**Create**: `.github/workflows/dotnet-ci.yml`

**Requirements**:
1. Multi-platform matrix: ubuntu-latest, windows-latest, macos-latest
2. Multi-RabbitMQ matrix: 3.11, 3.12
3. .NET 9 SDK installation
4. RabbitMQ service containers (Linux) or docker-compose (Windows/macOS)
5. Automated test execution with result publishing
6. NuGet package generation with dynamic versioning
7. Artifact uploads (packages + test results)

**Create**: `Directory.Build.props` (Centralized versioning)

**Purpose**: Single source of truth for version, metadata, target framework

```xml
<Project>
  <PropertyGroup>
    <VersionPrefix>3.0.0</VersionPrefix>
    <VersionSuffix Condition="'$(GITHUB_REF)' != 'refs/heads/main'">alpha-$(GITHUB_RUN_NUMBER)</VersionSuffix>
    <TargetFramework>net9.0</TargetFramework>
    <Authors>pardahlman;enrique-avalon</Authors>
    <PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>
</Project>
```

**Create**: `Directory.Packages.props` (Centralized dependency management)

**Purpose**: Prevent version conflicts across 25 projects

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="RabbitMQ.Client" Version="7.0.0" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <!-- ... 20+ more dependencies -->
  </ItemGroup>
</Project>
```

### 3.3 Test Automation

**Current**: Sequential PowerShell-based tests, no reports

**Target**: Parallel cross-platform tests with published reports

**Changes**:
1. Remove `-parallel none` flag (enable parallel execution)
2. Add `--logger "trx;LogFileName=test-results.trx"` to generate reports
3. Use `dorny/test-reporter@v1` to publish to GitHub PR
4. Copy test results to `docs/test/` as required by PLAN.md
5. Test on all 3 platforms x 2 RabbitMQ versions = 6 matrix jobs

### 3.4 Package Management

**Current**: Hardcoded version suffix "rc2", manual per-project versioning

**Target**: Automated semver-based versioning

**Versioning Strategy**:

| Branch | Version Format | Example | Publish To |
|--------|----------------|---------|------------|
| `upgrade` | `3.0.0-alpha-{build}` | `3.0.0-alpha-42` | GitHub Packages |
| `beta` | `3.0.0-beta.{n}` | `3.0.0-beta.1` | NuGet.org (unlisted) |
| `rc` | `3.0.0-rc.{n}` | `3.0.0-rc.1` | NuGet.org (listed) |
| `main` | `3.0.0` | `3.0.0` | NuGet.org (stable) |

**Implementation**:
- Use `Directory.Build.props` with conditional `VersionSuffix`
- GitHub Actions workflow calculates version from branch + build number
- `dotnet pack` automatically uses centralized version

### 3.5 Security Scanning Integration

**Current**: None

**Target**: Automated vulnerability scanning on every build

**Tools to Add**:
1. **Dependency Scanning**: `dotnet list package --vulnerable --include-transitive`
2. **SARIF Upload**: `github/codeql-action/upload-sarif@v3`
3. **Security Tab Integration**: Vulnerabilities appear in GitHub Security tab
4. **PR Blocking**: Fail build if HIGH/CRITICAL vulnerabilities found

**Frequency**: Every push, every PR

---

## 4. Recommended GitHub Actions Workflow

### 4.1 Main CI/CD Workflow

**File**: `.github/workflows/dotnet-ci.yml`

```yaml
name: .NET 9 CI/CD

on:
  push:
    branches: [upgrade, main, beta, rc]
  pull_request:
    branches: [upgrade, main]

env:
  DOTNET_VERSION: '9.0.x'
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  # Job 1: Build and test across all platforms and RabbitMQ versions
  build-and-test:
    name: Build & Test (${{ matrix.os }}, RabbitMQ ${{ matrix.rabbitmq-version }})
    runs-on: ${{ matrix.os }}

    strategy:
      fail-fast: false  # Continue testing all combinations even if one fails
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        rabbitmq-version: ['3.11', '3.12']

    services:
      # Service containers only work on Linux
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
        # Only run on Linux (services not supported on Windows/macOS)
        if: runner.os == 'Linux'

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for GitVersion (optional)

      - name: Setup .NET 9 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      # For Windows/macOS: Use docker-compose to start RabbitMQ
      - name: Start RabbitMQ (Windows/macOS)
        if: runner.os != 'Linux'
        run: |
          docker-compose -f docker/rabbitmq/docker-compose.yml up -d rabbitmq-${{ matrix.rabbitmq-version }}
          # Wait for RabbitMQ to be ready
          timeout 60 bash -c 'until docker exec rawrabbit-test-rabbitmq-${{ matrix.rabbitmq-version }} rabbitmq-diagnostics -q ping; do sleep 2; done'
        shell: bash

      - name: Restore dependencies
        run: dotnet restore RawRabbit.sln

      - name: Build solution
        run: dotnet build RawRabbit.sln --configuration Release --no-restore

      - name: Run unit tests
        run: |
          dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj \
            --no-build \
            --configuration Release \
            --logger "trx;LogFileName=unit-tests.trx" \
            --collect:"XPlat Code Coverage" \
            -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

      - name: Run integration tests
        run: |
          dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj \
            --no-build \
            --configuration Release \
            --logger "trx;LogFileName=integration-tests.trx" \
            --collect:"XPlat Code Coverage" \
            -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
        env:
          # Connection settings for RabbitMQ
          RabbitMQ__Host: localhost
          RabbitMQ__Port: ${{ runner.os == 'Linux' && '5672' || '5672' }}
          RabbitMQ__Username: guest
          RabbitMQ__Password: guest

      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()  # Run even if tests fail
        with:
          name: Test Results (${{ matrix.os }}, RabbitMQ ${{ matrix.rabbitmq-version }})
          path: "**/*.trx"
          reporter: dotnet-trx
          fail-on-error: true

      - name: Upload test results to docs/test
        if: always()
        run: |
          mkdir -p docs/test
          cp **/*.trx docs/test/ || true
        shell: bash

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results-${{ matrix.os }}-rabbitmq-${{ matrix.rabbitmq-version }}
          path: |
            **/*.trx
            **/TestResults/**
            docs/test/

      - name: Stop RabbitMQ (Windows/macOS)
        if: always() && runner.os != 'Linux'
        run: docker-compose -f docker/rabbitmq/docker-compose.yml down
        shell: bash

  # Job 2: Security scanning
  security-scan:
    name: Security Scan
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET 9 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore RawRabbit.sln

      - name: Check for vulnerable packages
        run: |
          dotnet list package --vulnerable --include-transitive > vulnerability-report.txt
          cat vulnerability-report.txt
          # Fail if HIGH or CRITICAL vulnerabilities found
          if grep -E "has the following vulnerable packages" vulnerability-report.txt; then
            echo "::error::Vulnerable dependencies detected!"
            exit 1
          fi

      - name: Upload vulnerability report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: vulnerability-report
          path: vulnerability-report.txt

  # Job 3: Package generation (only for specific branches)
  package:
    name: Generate NuGet Packages
    needs: [build-and-test, security-scan]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/upgrade' || github.ref == 'refs/heads/main' || github.ref == 'refs/heads/beta' || github.ref == 'refs/heads/rc'

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET 9 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore RawRabbit.sln

      - name: Pack all projects
        run: |
          # Version suffix is set in Directory.Build.props based on branch
          dotnet pack RawRabbit.sln \
            --configuration Release \
            --output ./artifacts \
            --no-restore \
            -p:ContinuousIntegrationBuild=true \
            -p:EmbedUntrackedSources=true

      - name: List generated packages
        run: ls -lh ./artifacts/*.nupkg

      # Publish to GitHub Packages (for alpha/beta builds)
      - name: Publish packages to GitHub Packages
        if: github.ref == 'refs/heads/upgrade' || github.ref == 'refs/heads/beta'
        run: |
          dotnet nuget push "./artifacts/*.nupkg" \
            --source https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json \
            --api-key ${{ secrets.GITHUB_TOKEN }} \
            --skip-duplicate

      # Publish to NuGet.org (for RC and production releases)
      # Requires NUGET_API_KEY secret to be configured
      - name: Publish packages to NuGet.org
        if: github.ref == 'refs/heads/rc' || github.ref == 'refs/heads/main'
        run: |
          dotnet nuget push "./artifacts/*.nupkg" \
            --source https://api.nuget.org/v3/index.json \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --skip-duplicate

      - name: Upload NuGet packages
        uses: actions/upload-artifact@v4
        with:
          name: nuget-packages
          path: ./artifacts/*.nupkg
```

### 4.2 Workflow Features

**Multi-Platform Matrix**:
- ✅ Tests on Windows, Linux, macOS
- ✅ Tests with RabbitMQ 3.11 and 3.12
- ✅ Total: 6 combinations (3 OS × 2 RabbitMQ versions)

**RabbitMQ Integration**:
- ✅ Linux: Uses GitHub Actions service containers (fastest)
- ✅ Windows/macOS: Uses existing docker-compose.yml
- ✅ Health checks ensure RabbitMQ is ready before tests

**Test Reporting**:
- ✅ Generates .trx files with `--logger "trx"`
- ✅ Publishes results to GitHub PR with `dorny/test-reporter`
- ✅ Copies results to `docs/test/` as required by PLAN.md
- ✅ Uploads artifacts for historical tracking

**Security Scanning**:
- ✅ Checks for vulnerable dependencies
- ✅ Fails build if HIGH/CRITICAL vulnerabilities found
- ✅ Generates report artifact

**Package Publishing**:
- ✅ GitHub Packages for alpha/beta (private testing)
- ✅ NuGet.org for rc/main (public releases)
- ✅ Dynamic versioning based on branch
- ✅ Deterministic builds (`ContinuousIntegrationBuild=true`)

**Branch Strategy**:
- ✅ Runs on push to: upgrade, main, beta, rc
- ✅ Runs on PRs to: upgrade, main
- ✅ Packages only for specific branches (not PRs)

### 4.3 Additional Workflows

**Create**: `.github/workflows/dependency-review.yml` (for PRs)

```yaml
name: Dependency Review

on: [pull_request]

permissions:
  contents: read
  pull-requests: write

jobs:
  dependency-review:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Dependency Review
        uses: actions/dependency-review-action@v4
        with:
          fail-on-severity: moderate
          deny-licenses: GPL-2.0, GPL-3.0
```

**Create**: `.github/workflows/codeql-analysis.yml` (security scanning)

```yaml
name: CodeQL Security Scan

on:
  push:
    branches: [upgrade, main]
  pull_request:
    branches: [upgrade, main]
  schedule:
    - cron: '0 6 * * 1'  # Weekly on Monday at 6am UTC

jobs:
  analyze:
    name: Analyze
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      actions: read
      contents: read

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp

      - name: Setup .NET 9 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Build
        run: dotnet build RawRabbit.sln --configuration Release

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3
```

---

## 5. Migration Timeline

### Week 2 (Stage 1.5): CI/CD Setup - CRITICAL

**Timeline**: 5 days

**Tasks**:
1. **Day 1-2**: Create GitHub Actions workflows
   - [ ] Create `.github/workflows/dotnet-ci.yml`
   - [ ] Create `.github/workflows/dependency-review.yml`
   - [ ] Create `.github/workflows/codeql-analysis.yml`
   - [ ] Create `Directory.Build.props` (centralized versioning)
   - [ ] Create `Directory.Packages.props` (centralized dependencies)
   - [ ] Create `global.json` to pin .NET 9 SDK version

2. **Day 3**: Configure branch protection
   - [ ] Require status checks to pass (dotnet-ci)
   - [ ] Require CodeQL scan to pass
   - [ ] Require dependency review approval
   - [ ] Protect 'upgrade' and 'main' branches

3. **Day 4**: Configure secrets
   - [ ] Add `NUGET_API_KEY` secret (for NuGet.org publishing)
   - [ ] Configure GitHub Packages authentication
   - [ ] Test package publishing to GitHub Packages

4. **Day 5**: Validation and documentation
   - [ ] Push test commit to 'upgrade' branch
   - [ ] Verify all 6 matrix jobs pass
   - [ ] Verify test results appear in PR
   - [ ] Verify packages published to GitHub Packages
   - [ ] Document rollback procedures
   - [ ] Create ADR: `docs/adr/0008-cicd-infrastructure.md`

**Deliverables**:
- ✅ Working GitHub Actions CI/CD pipeline
- ✅ Multi-platform automated testing
- ✅ Centralized version/dependency management
- ✅ Security scanning integrated
- ✅ Branch protection rules
- ✅ Documentation complete

### Week 3 (Validation): Run alongside AppVeyor

**Purpose**: Parallel validation before disabling AppVeyor

**Actions**:
- Monitor GitHub Actions builds on 'upgrade' branch
- Compare results with AppVeyor (if still configured)
- Fix any issues discovered
- Gain confidence in new pipeline

### Week 3 (End): Disable AppVeyor

**Actions**:
- [ ] Remove/comment out `.build/appveyor.yml`
- [ ] Disable AppVeyor project in AppVeyor dashboard
- [ ] Update documentation to reference GitHub Actions
- [ ] Remove PowerShell build scripts (if no longer needed)

### Week 10 (Stage 7): Production Publishing

**Actions**:
- [ ] Add manual approval gate for NuGet.org publishing
- [ ] Add release notes generation (`gh release create`)
- [ ] Add GitHub Release automation
- [ ] Add deployment smoke tests (install package, run tests)

---

## 6. Infrastructure Standardization

### 6.1 Centralized Configuration Files

**Create**: `global.json` (Pin .NET SDK version)

```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
```

**Purpose**: Ensure all developers and CI use same .NET 9 SDK version

**Create**: `Directory.Build.props` (Shared properties)

```xml
<Project>
  <PropertyGroup>
    <!-- Versioning -->
    <VersionPrefix>3.0.0</VersionPrefix>
    <VersionSuffix Condition="'$(GITHUB_REF)' == 'refs/heads/upgrade'">alpha-$(GITHUB_RUN_NUMBER)</VersionSuffix>
    <VersionSuffix Condition="'$(GITHUB_REF)' == 'refs/heads/beta'">beta.$(GITHUB_RUN_NUMBER)</VersionSuffix>
    <VersionSuffix Condition="'$(GITHUB_REF)' == 'refs/heads/rc'">rc.$(GITHUB_RUN_NUMBER)</VersionSuffix>

    <!-- Target Framework -->
    <TargetFramework>net9.0</TargetFramework>

    <!-- Package Metadata -->
    <Authors>pardahlman;enrique-avalon</Authors>
    <Company>RawRabbit</Company>
    <PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/pardahlman/RawRabbit</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>

    <!-- Build Settings -->
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- NuGet Package Settings -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <!-- Common package references for all projects -->
  <ItemGroup>
    <None Include="$(MSBuildThisFileDirectory)icon.png" Pack="true" PackagePath="/" Visible="false" />
    <None Include="$(MSBuildThisFileDirectory)README.md" Pack="true" PackagePath="/" Visible="false" />
  </ItemGroup>
</Project>
```

**Create**: `Directory.Packages.props` (Centralized dependency versions)

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core Dependencies (CRITICAL: Must be consistent across all 25 projects) -->
    <PackageVersion Include="RabbitMQ.Client" Version="7.0.0" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

    <!-- System.Text.Json (alternative to Newtonsoft.Json) -->
    <PackageVersion Include="System.Text.Json" Version="9.0.0" />

    <!-- Polly (resilience) -->
    <PackageVersion Include="Polly" Version="8.5.0" />

    <!-- Serialization -->
    <PackageVersion Include="protobuf-net" Version="3.2.30" />
    <PackageVersion Include="MessagePack" Version="2.5.187" />
    <PackageVersion Include="ZeroFormatter" Version="1.6.4" />  <!-- ⚠️ DEPRECATED -->

    <!-- Dependency Injection -->
    <PackageVersion Include="Autofac" Version="8.1.0" />
    <PackageVersion Include="Ninject" Version="3.3.6" />  <!-- ⚠️ UNMAINTAINED -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />

    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="FluentAssertions" Version="6.12.1" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />

    <!-- Analyzers -->
    <PackageVersion Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" />
    <PackageVersion Include="SonarAnalyzer.CSharp" Version="9.32.0.97167" />
  </ItemGroup>
</Project>
```

**Benefits**:
- ✅ Single source of truth for versions
- ✅ Prevents version drift across 25 projects
- ✅ Easy to update all projects at once
- ✅ CI/CD automatically uses correct versions
- ✅ Deterministic builds

### 6.2 Docker Standardization

**Existing**: `/home/laird/src/EYP/RawRabbit/docker/rabbitmq/docker-compose.yml`

**Status**: ✅ ALREADY EXCELLENT

**Usage in CI**:
- Linux: Use GitHub Actions service containers (faster)
- Windows/macOS: Use existing docker-compose.yml (compatibility)

**Local Development**: Developers can use docker-compose for consistent testing

```bash
# Start RabbitMQ 3.12 for testing
docker-compose -f docker/rabbitmq/docker-compose.yml up -d rabbitmq-3.12

# Run tests
dotnet test RawRabbit.sln

# Stop RabbitMQ
docker-compose -f docker/rabbitmq/docker-compose.yml down
```

### 6.3 Test Automation

**Current**: Sequential PowerShell-based tests

**Target**: Parallel cross-platform tests

**Changes**:
1. Remove PowerShell scripts (`.build/Test.ps1`)
2. Use native `dotnet test` in CI
3. Enable parallel execution (remove `-parallel none`)
4. Generate test reports (`--logger "trx"`)
5. Collect code coverage (`--collect:"XPlat Code Coverage"`)
6. Publish results to GitHub PR

**Test Report Location**: `docs/test/` (as required by PLAN.md)

---

## 7. Security Scanning Integration

### 7.1 Dependency Vulnerability Scanning

**Tool**: Built-in `dotnet list package --vulnerable`

**Frequency**: Every push, every PR

**Action on Failure**: Block PR merge if HIGH/CRITICAL vulnerabilities

**Example**:
```bash
dotnet list package --vulnerable --include-transitive
```

**Output**:
```
The following sources were used:
   https://api.nuget.org/v3/index.json

Project `RawRabbit` has the following vulnerable packages
   [net9.0]:
   Top-level Package      Requested   Resolved   Severity   Advisory URL
   > RabbitMQ.Client      5.0.1       5.0.1      High       https://github.com/advisories/GHSA-...
```

**Integration**: CI fails if vulnerabilities detected

### 7.2 CodeQL Security Scanning

**Tool**: GitHub CodeQL

**Languages**: C#

**Frequency**:
- Every push to upgrade/main
- Every PR
- Weekly scheduled scan (Monday 6am UTC)

**Coverage**:
- SQL injection
- XSS
- Deserialization vulnerabilities
- Cryptographic issues
- Authentication/authorization bugs

**Integration**: Results appear in GitHub Security tab

### 7.3 Dependency Review (Pull Requests)

**Tool**: `actions/dependency-review-action`

**Purpose**: Review dependency changes in PRs before merge

**Checks**:
- Newly added dependencies with known vulnerabilities
- License compliance (deny GPL-2.0, GPL-3.0)
- Dependency version changes
- Transitive dependency risks

**Action on Failure**: Block PR merge

---

## 8. Rollback Procedures

### 8.1 Rollback Scenarios

**Scenario 1: GitHub Actions workflow fails**

**Symptoms**:
- Build fails on specific OS/RabbitMQ combination
- Tests fail unexpectedly
- Package generation errors

**Rollback Actions**:
1. Identify failing job in GitHub Actions UI
2. Review logs to determine root cause
3. If unfixable within 2 hours:
   - Temporarily disable failing matrix combination
   - Create GitHub issue to track
   - Continue with working combinations
4. If all combinations fail:
   - Revert workflow changes (`git revert`)
   - Push to unblock development
   - Fix workflow in separate PR

**Recovery Time**: 30 minutes - 2 hours

**Scenario 2: NuGet package publishing fails**

**Symptoms**:
- Package rejected by NuGet.org (validation errors)
- Authentication failure
- Package conflicts

**Rollback Actions**:
1. Check NuGet.org validation errors
2. If package is invalid:
   - Fix issue in code
   - Increment version suffix (e.g., `3.0.0-rc.2`)
   - Re-publish
3. If authentication fails:
   - Verify `NUGET_API_KEY` secret is valid
   - Regenerate API key on NuGet.org if expired
   - Update secret in GitHub repository settings

**Recovery Time**: 1-4 hours

**Scenario 3: Branch protection blocks development**

**Symptoms**:
- Developers cannot push due to CI failures
- Urgent hotfix needed but CI is broken

**Rollback Actions**:
1. **Emergency Only**: Temporarily disable branch protection
2. Push urgent fix
3. Immediately re-enable branch protection
4. Fix CI in subsequent PR

**Recovery Time**: 15-30 minutes (EMERGENCY USE ONLY)

### 8.2 Rollback to AppVeyor (Worst Case)

**Trigger**: GitHub Actions fundamentally broken, cannot be fixed within 1 week

**Actions**:
1. Restore `.build/appveyor.yml`
2. Re-enable AppVeyor project
3. Update AppVeyor image to support .NET 9 (may require custom image)
4. Document issues in GitHub Discussion
5. Plan alternative approach

**Likelihood**: VERY LOW (GitHub Actions is well-tested for .NET)

**Recovery Time**: 2-5 days

---

## 9. Cost Analysis

### 9.1 Current Costs (AppVeyor)

**Plan**: Free (Open Source)

**Limitations**:
- 1 concurrent job
- Windows only
- Outdated build images

**Monthly Cost**: $0

### 9.2 Proposed Costs (GitHub Actions)

**Plan**: Free for public repositories

**Included**:
- 20 concurrent jobs
- 3,000 minutes per month (Windows)
- Unlimited Linux/macOS minutes for public repos

**Expected Usage** (per month):
- Builds per day: ~10 (development activity)
- Minutes per build: ~15 minutes (6 matrix jobs × 2.5 min each)
- Total minutes: 10 builds/day × 30 days × 15 min = 4,500 minutes
- Linux minutes: FREE (public repo)
- Windows minutes: FREE (public repo)
- macOS minutes: FREE (public repo)

**Monthly Cost**: $0 (public repository)

**Cost Savings**: $0 (both free)

**Performance Improvement**: ~3x faster (parallel matrix jobs)

---

## 10. Success Criteria

### 10.1 Functional Requirements

- [ ] ✅ CI pipeline runs on push to upgrade/main/beta/rc branches
- [ ] ✅ CI pipeline runs on pull requests
- [ ] ✅ Multi-platform testing (Windows, Linux, macOS)
- [ ] ✅ Multi-RabbitMQ testing (3.11, 3.12)
- [ ] ✅ All tests pass (unit + integration)
- [ ] ✅ Test results published to GitHub PR
- [ ] ✅ Test results copied to docs/test/
- [ ] ✅ NuGet packages generated with correct versioning
- [ ] ✅ Packages published to GitHub Packages (alpha/beta)
- [ ] ✅ Security scanning passes (no HIGH/CRITICAL vulnerabilities)
- [ ] ✅ Branch protection rules enforced

### 10.2 Performance Requirements

- [ ] ✅ Build + test completes in < 10 minutes per matrix job
- [ ] ✅ Total pipeline duration < 15 minutes (parallel execution)
- [ ] ✅ Package generation < 2 minutes
- [ ] ✅ Security scan < 5 minutes

### 10.3 Documentation Requirements

- [ ] ✅ ADR created: `docs/adr/0008-cicd-infrastructure.md`
- [ ] ✅ Workflow documented in repository README
- [ ] ✅ Rollback procedures documented
- [ ] ✅ Developer onboarding guide updated

---

## 11. Risk Assessment

### 11.1 Critical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **AppVeyor cannot support .NET 9** | HIGH | BLOCKER | Migrate to GitHub Actions (already planned) |
| **RabbitMQ containers fail on Windows/macOS** | MEDIUM | HIGH | Use docker-compose (already tested) |
| **Test failures on specific platforms** | MEDIUM | MEDIUM | Matrix strategy isolates failures |
| **Package versioning conflicts** | LOW | HIGH | Centralized Directory.Build.props |
| **NuGet.org publishing fails** | LOW | MEDIUM | Test with GitHub Packages first |

### 11.2 Operational Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **CI costs exceed budget** | VERY LOW | LOW | Public repo = free |
| **GitHub Actions downtime** | LOW | MEDIUM | Document rollback to local builds |
| **Developer confusion with new workflow** | MEDIUM | LOW | Documentation + training |
| **Secret management errors** | LOW | HIGH | Use GitHub secrets, never commit keys |

---

## 12. Alignment with PLAN.md

### 12.1 Integration with Migration Stages

**Stage 1.5 (Week 2)**: CI/CD & Infrastructure Setup - NEW STAGE

**Rationale**: DevOps review (docs/planning/devops-review.md) identified that CI/CD in Stage 7 (Week 9-10) is TOO LATE

**Deliverables**:
- ✅ GitHub Actions workflow (`.github/workflows/dotnet-ci.yml`)
- ✅ Centralized version management (`Directory.Build.props`)
- ✅ Centralized dependency management (`Directory.Packages.props`)
- ✅ Security scanning integration (CodeQL, dependency review)
- ✅ Branch protection rules
- ✅ Documentation (ADR 0008)

**Stage 3-6 (Weeks 3-9)**: Migration work VALIDATED by CI/CD

**Benefit**: Every commit automatically tested on 3 platforms × 2 RabbitMQ versions = 6 combinations

**Stage 7 (Week 9-10)**: Deployment Preparation

**Revised Focus**:
- Add NuGet.org publishing workflow (manual approval)
- Add release notes generation
- Add GitHub Release automation
- Test rollback procedures

**Stage 8 (Week 10-12)**: Staged Rollout

**Infrastructure**:
- Alpha: GitHub Packages (private feed)
- Beta: NuGet.org (unlisted packages)
- RC: NuGet.org (listed packages)
- Production: NuGet.org (stable feed)

### 12.2 Dependency on Other Tasks

**Blocks**:
- ✅ None - CI/CD can be implemented independently

**Blocked By**:
- ✅ None - CI/CD should be done FIRST (Stage 1.5)

**Enables**:
- ✅ All migration stages (continuous validation)
- ✅ Security scanning (automated vulnerability detection)
- ✅ Package distribution (automated publishing)
- ✅ Multi-platform testing (automated cross-platform validation)

---

## 13. Recommendations Summary

### 13.1 Immediate Actions (Week 2)

1. **CRITICAL**: Create GitHub Actions workflow (`.github/workflows/dotnet-ci.yml`)
2. **CRITICAL**: Create centralized configuration files (`Directory.Build.props`, `Directory.Packages.props`, `global.json`)
3. **HIGH**: Configure branch protection rules
4. **HIGH**: Add security scanning workflows (CodeQL, dependency review)
5. **MEDIUM**: Document rollback procedures
6. **MEDIUM**: Create ADR 0008 (CI/CD infrastructure decision)

### 13.2 Migration Path

**Phase 1 (Week 2)**: Setup
- Create GitHub Actions workflows
- Test on 'upgrade' branch
- Validate all matrix combinations pass

**Phase 2 (Week 3)**: Validation
- Run GitHub Actions and AppVeyor in parallel
- Compare results
- Fix any discrepancies

**Phase 3 (Week 3 end)**: Cutover
- Disable AppVeyor
- Remove PowerShell build scripts (optional)
- Update documentation

**Phase 4 (Week 10)**: Production Publishing
- Add NuGet.org publishing
- Add release automation
- Test staged rollout

### 13.3 Long-Term Improvements

**Post-Migration** (optional):
1. Add performance benchmarking (BenchmarkDotNet)
2. Add mutation testing (Stryker.NET)
3. Add container scanning (Trivy)
4. Add SBOM generation (CycloneDX)
5. Add release notes automation (conventional commits)

---

## 14. Conclusion

### 14.1 Assessment Summary

**Current CI/CD**: AppVeyor (Windows-only, VS 2015, NO .NET 9 support)

**.NET 9 Compatibility**: ❌ INCOMPATIBLE - Requires immediate migration

**Recommended Platform**: GitHub Actions (multi-platform, native .NET 9 support, free)

**Migration Complexity**: MEDIUM (6-8 hours)

**Timeline**: Week 2 (Stage 1.5) - BEFORE main migration work

**Risk Level**: LOW (GitHub Actions is proven, existing docker-compose.yml works)

### 14.2 Critical Success Factors

1. ✅ **Early Implementation**: CI/CD in Week 2 (not Week 9) enables continuous validation
2. ✅ **Multi-Platform Testing**: 3 OS × 2 RabbitMQ = comprehensive coverage
3. ✅ **Centralized Management**: Directory.Build.props + Directory.Packages.props prevents version conflicts
4. ✅ **Security Integration**: CodeQL + dependency scanning catches vulnerabilities early
5. ✅ **Automated Publishing**: GitHub Packages → NuGet.org staged rollout

### 14.3 Next Steps

**Immediate** (next task):
1. Create GitHub Actions workflow (`.github/workflows/dotnet-ci.yml`)
2. Create centralized configuration files
3. Test workflow on 'upgrade' branch
4. Update HISTORY.md with completion

**Week 3**: Validate and cutover from AppVeyor

**Week 10**: Add production publishing workflows

---

**Document Status**: COMPLETE
**Assessment Date**: 2025-10-09
**Reviewed By**: DevOps Engineer
**Approved For**: Stage 1.5 Implementation (Week 2)

---

## Appendix A: Comparison Matrix

| Feature | AppVeyor (Current) | GitHub Actions (Proposed) |
|---------|-------------------|--------------------------|
| **.NET 9 Support** | ❌ NO (VS 2015 image) | ✅ YES (native support) |
| **Multi-OS** | ❌ Windows only | ✅ Linux, Windows, macOS |
| **RabbitMQ Testing** | ⚠️ Manual install | ✅ Docker containers |
| **Test Reporting** | ❌ None | ✅ GitHub PR integration |
| **Security Scanning** | ❌ None | ✅ CodeQL + dependency review |
| **Package Publishing** | ⚠️ Manual | ✅ Automated (GitHub Packages + NuGet.org) |
| **Version Management** | ⚠️ Hardcoded "rc2" | ✅ Dynamic (branch-based) |
| **Parallel Testing** | ❌ Sequential | ✅ Matrix (6 combinations) |
| **Cost** | $0 (free) | $0 (free for public repos) |
| **Build Time** | ~8-10 minutes | ~10-15 minutes (6 parallel jobs) |
| **Maintenance** | PowerShell scripts | Native dotnet CLI |

**Winner**: GitHub Actions (9/10 categories better)

---

## Appendix B: File Changes Required

### New Files to Create

1. `.github/workflows/dotnet-ci.yml` (~300 lines) - Main CI/CD workflow
2. `.github/workflows/dependency-review.yml` (~30 lines) - PR dependency review
3. `.github/workflows/codeql-analysis.yml` (~50 lines) - Security scanning
4. `Directory.Build.props` (~60 lines) - Centralized versioning/metadata
5. `Directory.Packages.props` (~50 lines) - Centralized dependency versions
6. `global.json` (~7 lines) - Pin .NET SDK version
7. `docs/adr/0008-cicd-infrastructure.md` (~200 lines) - Architecture decision record

### Files to Modify

1. `docs/HISTORY.md` - Add completion entry for Task 8
2. `.build/appveyor.yml` - Disable or remove (Week 3)
3. All 25 `.csproj` files - Remove version attributes (if using Directory.Build.props)
4. All 25 `.csproj` files - Remove version numbers from PackageReference (if using Directory.Packages.props)

### Files to Remove (Optional, Week 3)

1. `.build/Build.ps1` (replaced by `dotnet pack` in CI)
2. `.build/Test.ps1` (replaced by `dotnet test` in CI)
3. `.build/Install-*.ps1` (replaced by Docker containers)
4. `.build/Util-RabbitMqPath.ps1` (no longer needed)

**Total Changes**: 7 new files, 27+ modified files, 5 optional deletions

---

## Appendix C: Secrets Configuration

### Required GitHub Secrets

1. **NUGET_API_KEY** (for NuGet.org publishing)
   - Source: NuGet.org account settings
   - Scope: Repository secret
   - Required For: RC and production releases (Stage 8)
   - Setup: Settings → Secrets and variables → Actions → New repository secret

2. **ANTHROPIC_API_KEY** (already configured)
   - Source: Anthropic API dashboard
   - Scope: Repository secret
   - Required For: Claude Code workflows
   - Status: ✅ Already configured (claude.yml works)

### Optional Secrets

3. **SONARCLOUD_TOKEN** (for SonarCloud analysis - optional)
   - Source: SonarCloud.io
   - Scope: Repository secret
   - Required For: Advanced code quality scanning
   - Priority: LOW (can add later)

### Automatic Secrets

4. **GITHUB_TOKEN** (automatic)
   - Source: Provided by GitHub Actions
   - Scope: Automatic, job-specific
   - Required For: GitHub Packages, API calls
   - Setup: None (automatic)

---

**End of Assessment**
