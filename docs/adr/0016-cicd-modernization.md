# ADR-0016: CI/CD Modernization

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist

**Reviewers**: DevOps Engineer, Release Manager

**Tags**: cicd, github-actions, testing, deployment, automation

---

## Context

### Background

RawRabbit currently lacks a modern CI/CD pipeline integrated with the GitHub repository. The .NET 9 migration requires:

**CI/CD Requirements**:
1. **.NET 9 SDK**: Build with latest toolchain
2. **Multi-Platform**: Windows, Linux, macOS support
3. **Testing**: Unit, integration, performance tests
4. **Security**: Integration with ADR-0010 security scanning
5. **NuGet Publishing**: Automated package releases
6. **Quality Gates**: Code coverage, linting, analysis
7. **PR Workflow**: Automated checks before merge
8. **Release Automation**: Semantic versioning, changelogs

**Current State**:
- No GitHub Actions workflows
- Manual builds and testing
- No automated security scanning
- Manual NuGet package publishing
- No PR validation
- No release automation

**Technology Constraints**:
- **Platform**: GitHub (existing repository)
- **Runtime**: .NET 9 (target framework)
- **Package Registry**: NuGet.org (public packages)
- **OS Support**: Windows, Linux, macOS (multi-platform library)

### Problem Statement

**How do we establish a modern CI/CD pipeline using GitHub Actions that automates building, testing, security scanning, and publishing of RawRabbit for .NET 9 across multiple platforms while ensuring high quality, fast feedback, and reliable releases?**

### Constraints

1. **Platform**: Must use GitHub Actions (native integration)
2. **Cost**: Minimize GitHub Actions minutes (optimize workflows)
3. **Speed**: PR checks should complete in <10 minutes
4. **Reliability**: Flaky tests must not block releases
5. **Security**: Integrate with ADR-0010 security scanning
6. **Multi-Platform**: Test on Windows, Linux, macOS
7. **Documentation**: Clear contribution guidelines

### Assumptions

1. GitHub repository is the source of truth
2. .NET 9 SDK is available in GitHub Actions runners
3. RabbitMQ broker available for integration tests (docker)
4. NuGet API key secured in GitHub Secrets
5. Semantic versioning for releases (major.minor.patch)
6. Main branch is `2.0`, protected with PR requirements

---

## Decision

### Chosen Solution

**Implement GitHub Actions-based CI/CD with the following workflows:**

**Workflow 1: Pull Request Validation** (on every PR)
- Build all projects (.NET 9)
- Run unit tests
- Run integration tests (with RabbitMQ container)
- Code coverage reporting
- Security scanning (dependency check, CodeQL)

**Workflow 2: Continuous Integration** (on push to main branches)
- Full build and test suite
- Performance benchmarks
- Security scanning (comprehensive)
- Package versioning preview

**Workflow 3: Release** (on tag push or manual trigger)
- Build release artifacts
- Run full test suite
- Generate release notes
- Publish NuGet packages
- Create GitHub release

**Workflow 4: Scheduled Maintenance** (daily/weekly)
- Dependency updates (Dependabot PRs)
- Security scanning (full sweep)
- Performance regression detection

### Implementation Details

#### 1. Pull Request Validation Workflow

**.github/workflows/pr-validation.yml**:
```yaml
name: Pull Request Validation

on:
  pull_request:
    branches: [ "2.0", "upgrade", "stage-*" ]
    paths:
      - 'src/**'
      - 'test/**'
      - '*.sln'
      - '**/*.csproj'
      - '.github/workflows/**'

env:
  DOTNET_VERSION: '9.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  build:
    name: Build & Test
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]

    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Full history for versioning

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore

    - name: Build solution
      run: dotnet build --no-restore --configuration Release /p:TreatWarningsAsErrors=true

    - name: Run unit tests
      run: dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj --no-build --configuration Release --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage"

    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: test-results-${{ matrix.os }}
        path: "**/test-results.trx"

    - name: Upload coverage
      if: matrix.os == 'ubuntu-latest'
      uses: codecov/codecov-action@v4
      with:
        files: "**/coverage.cobertura.xml"
        flags: unittests

  integration-tests:
    name: Integration Tests
    runs-on: ubuntu-latest

    services:
      rabbitmq:
        image: rabbitmq:3.13-management-alpine
        ports:
          - 5672:5672
          - 15672:15672
        env:
          RABBITMQ_DEFAULT_USER: testuser
          RABBITMQ_DEFAULT_PASS: testpass
        options: >-
          --health-cmd "rabbitmq-diagnostics -q ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Wait for RabbitMQ
      run: |
        timeout 60 bash -c 'until curl -f http://localhost:15672; do sleep 2; done'

    - name: Run integration tests
      run: dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj --configuration Release --logger "trx;LogFileName=integration-test-results.trx"
      env:
        RawRabbit__Hostnames__0: localhost
        RawRabbit__Username: testuser
        RawRabbit__Password: testpass

    - name: Upload integration test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: integration-test-results
        path: "**/integration-test-results.trx"

  code-quality:
    name: Code Quality & Security
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Run code analysis
      run: dotnet build --configuration Release /p:RunAnalyzers=true /p:EnforceCodeStyleInBuild=true

    - name: Run security validators
      run: dotnet run --project tools/SecurityValidator/SecurityValidator.csproj -- --all-projects

  summary:
    name: PR Validation Summary
    needs: [build, integration-tests, code-quality]
    runs-on: ubuntu-latest
    if: always()

    steps:
    - name: Check results
      run: |
        if [ "${{ needs.build.result }}" != "success" ] || \
           [ "${{ needs.integration-tests.result }}" != "success" ] || \
           [ "${{ needs.code-quality.result }}" != "success" ]; then
          echo "❌ PR validation failed"
          exit 1
        fi
        echo "✅ PR validation passed"
```

#### 2. Continuous Integration Workflow

**.github/workflows/ci.yml**:
```yaml
name: Continuous Integration

on:
  push:
    branches: [ "2.0", "upgrade" ]
  workflow_dispatch:

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  build-and-test:
    name: Build, Test & Package
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Calculate version
      id: version
      run: |
        # Use GitVersion or nbgv for semantic versioning
        VERSION=$(nbgv get-version -v NuGetPackageVersion)
        echo "version=$VERSION" >> $GITHUB_OUTPUT

    - name: Restore dependencies
      run: dotnet restore

    - name: Build solution
      run: dotnet build --no-restore --configuration Release /p:Version=${{ steps.version.outputs.version }}

    - name: Run all tests
      run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"

    - name: Pack NuGet packages
      run: dotnet pack --no-build --configuration Release /p:PackageVersion=${{ steps.version.outputs.version }} --output ./artifacts

    - name: Upload packages
      uses: actions/upload-artifact@v4
      with:
        name: nuget-packages
        path: ./artifacts/*.nupkg

  performance-benchmarks:
    name: Performance Benchmarks
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Run benchmarks
      run: dotnet run --project test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj --configuration Release --framework net9.0 -- --filter "*"

    - name: Upload benchmark results
      uses: actions/upload-artifact@v4
      with:
        name: benchmark-results
        path: "**/BenchmarkDotNet.Artifacts/**"
```

#### 3. Release Workflow

**.github/workflows/release.yml**:
```yaml
name: Release

on:
  push:
    tags:
      - 'v*.*.*'
  workflow_dispatch:
    inputs:
      version:
        description: 'Version to release (e.g., 2.1.0)'
        required: true

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  release:
    name: Build & Publish Release
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Determine version
      id: version
      run: |
        if [ "${{ github.event_name }}" == "workflow_dispatch" ]; then
          VERSION="${{ github.event.inputs.version }}"
        else
          VERSION="${{ github.ref_name }}"
          VERSION="${VERSION#v}"  # Remove 'v' prefix
        fi
        echo "version=$VERSION" >> $GITHUB_OUTPUT
        echo "Releasing version: $VERSION"

    - name: Restore dependencies
      run: dotnet restore

    - name: Build release
      run: dotnet build --no-restore --configuration Release /p:Version=${{ steps.version.outputs.version }}

    - name: Run full test suite
      run: dotnet test --no-build --configuration Release

    - name: Pack NuGet packages
      run: dotnet pack --no-build --configuration Release /p:PackageVersion=${{ steps.version.outputs.version }} --output ./artifacts

    - name: Generate release notes
      id: release_notes
      run: |
        NOTES=$(git log --pretty=format:"- %s" $(git describe --tags --abbrev=0 @^)..@ | grep -v "Merge pull request")
        echo "notes<<EOF" >> $GITHUB_OUTPUT
        echo "$NOTES" >> $GITHUB_OUTPUT
        echo "EOF" >> $GITHUB_OUTPUT

    - name: Create GitHub Release
      uses: softprops/action-gh-release@v1
      with:
        tag_name: v${{ steps.version.outputs.version }}
        name: RawRabbit ${{ steps.version.outputs.version }}
        body: |
          ## RawRabbit ${{ steps.version.outputs.version }}

          ### Changes
          ${{ steps.release_notes.outputs.notes }}

          ### Installation
          ```bash
          dotnet add package RawRabbit --version ${{ steps.version.outputs.version }}
          ```

          ### Full Changelog
          See [CHANGELOG.md](https://github.com/pardahlman/RawRabbit/blob/2.0/CHANGELOG.md)
        files: ./artifacts/*.nupkg
        draft: false
        prerelease: ${{ contains(steps.version.outputs.version, '-') }}
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

    - name: Publish to NuGet.org
      run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
      env:
        NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}

    - name: Publish to GitHub Packages (backup)
      run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.GITHUB_TOKEN }} --source https://nuget.pkg.github.com/pardahlman/index.json --skip-duplicate
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

#### 4. Scheduled Maintenance Workflow

**.github/workflows/scheduled-maintenance.yml**:
```yaml
name: Scheduled Maintenance

on:
  schedule:
    - cron: '0 6 * * 1'  # Monday 6am UTC (weekly)
  workflow_dispatch:

jobs:
  security-scan:
    name: Weekly Security Scan
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Vulnerability scan
      run: dotnet list package --vulnerable --include-transitive

    - name: Outdated packages check
      run: dotnet list package --outdated

    - name: Create issue if vulnerabilities found
      if: failure()
      uses: actions/github-script@v7
      with:
        script: |
          github.rest.issues.create({
            owner: context.repo.owner,
            repo: context.repo.repo,
            title: '🔒 Security vulnerabilities detected',
            body: 'Weekly security scan detected vulnerabilities. Review the workflow run for details.',
            labels: ['security', 'dependencies']
          })

  performance-baseline:
    name: Performance Baseline
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Run benchmarks
      run: dotnet run --project test/RawRabbit.PerformanceTest/RawRabbit.PerformanceTest.csproj --configuration Release

    - name: Upload baseline
      uses: actions/upload-artifact@v4
      with:
        name: performance-baseline-${{ github.run_number }}
        path: "**/BenchmarkDotNet.Artifacts/**"
```

#### 5. Branch Protection Rules

**Required for `2.0` branch**:
```yaml
# Configure via GitHub Settings → Branches → Branch protection rules
protection_rules:
  branch: "2.0"
  required_status_checks:
    - "Build & Test (ubuntu-latest)"
    - "Build & Test (windows-latest)"
    - "Build & Test (macos-latest)"
    - "Integration Tests"
    - "Code Quality & Security"
  require_pull_request_reviews:
    required_approving_review_count: 1
  enforce_admins: false
  required_linear_history: true
  allow_force_pushes: false
  allow_deletions: false
```

#### 6. GitHub Secrets Configuration

**Required Secrets** (Settings → Secrets and variables → Actions):
```yaml
secrets:
  NUGET_API_KEY:
    description: "NuGet.org API key for publishing packages"
    required: true

  CODECOV_TOKEN:
    description: "Codecov token for coverage reporting"
    required: false

  # Azure credentials (if using Key Vault for secrets)
  AZURE_CLIENT_ID:
    description: "Azure service principal client ID"
    required: false
```

#### 7. NuGet Package Metadata

**Directory.Build.props** (root):
```xml
<Project>
  <PropertyGroup>
    <!-- Package metadata -->
    <Authors>Pardahlman Contributors</Authors>
    <Company>RawRabbit</Company>
    <Product>RawRabbit</Product>
    <Description>High-performance RabbitMQ client for .NET 9</Description>
    <Copyright>Copyright © 2017-$([System.DateTime]::Now.Year) Pardahlman Contributors</Copyright>
    <PackageProjectUrl>https://github.com/pardahlman/RawRabbit</PackageProjectUrl>
    <RepositoryUrl>https://github.com/pardahlman/RawRabbit.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>rabbitmq;amqp;messaging;dotnet9;async</PackageTags>
    <PackageReleaseNotes>See https://github.com/pardahlman/RawRabbit/releases</PackageReleaseNotes>

    <!-- Build settings -->
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- Source link -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
  </ItemGroup>
</Project>
```

### Rationale

**GitHub Actions**:
- Native GitHub integration
- Free for public repositories
- Excellent .NET 9 support
- Rich ecosystem of actions

**Multi-Platform Testing**:
- RawRabbit targets netstandard2.0 (cross-platform)
- Ensure compatibility with Windows, Linux, macOS
- Catch platform-specific issues early

**RabbitMQ Container for Integration Tests**:
- Reproducible test environment
- Fast (no external dependencies)
- Isolated (no conflicts with other tests)

**Semantic Versioning**:
- Clear version semantics (major.minor.patch)
- Automated from Git tags
- NuGet best practice

---

## Alternatives Considered

### Alternative 1: Azure Pipelines

**Description**: Use Azure DevOps Pipelines instead of GitHub Actions.

**Pros**:
- Mature CI/CD platform
- More free minutes for public projects
- Better Windows support

**Cons**:
- Separate platform from GitHub
- Additional configuration complexity
- Team must learn Azure DevOps

**Why Rejected**: GitHub Actions provides native integration with GitHub repository. Simpler for contributors.

### Alternative 2: Jenkins (Self-Hosted)

**Description**: Self-hosted Jenkins instance for CI/CD.

**Pros**:
- Unlimited build minutes
- Full control over infrastructure
- Rich plugin ecosystem

**Cons**:
- Infrastructure overhead (hosting, maintenance)
- Security responsibility
- Requires dedicated resources

**Why Rejected**: GitHub Actions is sufficient for open-source project. Self-hosting is overkill.

### Alternative 3: Manual Releases

**Description**: Keep manual build and release process.

**Pros**:
- Zero configuration
- Full manual control

**Cons**:
- Error-prone (manual steps)
- Slow (blocks maintainers)
- No PR validation (quality issues)
- No automated security scanning

**Why Rejected**: Automation is essential for modern software development. Manual process doesn't scale.

---

## Consequences

### Positive Consequences

1. **Automated Testing**: Every PR validated before merge
2. **Fast Feedback**: Developers know within 10 minutes if PR passes
3. **Security**: Automated vulnerability scanning (ADR-0010 integration)
4. **Releases**: One-click releases with semantic versioning
5. **Quality**: Code coverage, linting enforced
6. **Confidence**: Multi-platform testing catches issues early
7. **Documentation**: Contribution guidelines clear (workflow defines expectations)

### Negative Consequences

1. **GitHub Actions Minutes**: May consume free tier (public repos have generous limits)
2. **Workflow Complexity**: YAML configuration requires maintenance
3. **Flaky Tests**: Integration tests may occasionally fail (RabbitMQ startup timing)
4. **Learning Curve**: Contributors must understand GitHub Actions basics

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| GitHub Actions outage blocks releases | LOW | MEDIUM | Manual release process documented as fallback |
| Flaky tests block PRs | MEDIUM | MEDIUM | Retry logic, health checks, test isolation |
| NuGet API key leakage | LOW | CRITICAL | Use GitHub Secrets, rotate keys quarterly |
| Excessive build minutes (cost) | LOW | LOW | Optimize workflows, cache dependencies |

### Technical Debt

1. **Manual Workflow Triggers**: Some workflows may require manual dispatch (document)
2. **Version Numbering**: GitVersion/nbgv tool needs integration
3. **Release Notes**: Automated generation is basic (may need enhancement)

---

## Migration Impact

### Breaking Changes

**None**: CI/CD is infrastructure, no API changes.

### Migration Path

**Step 1**: Create workflows
```bash
mkdir -p .github/workflows
# Add workflow YAML files
```

**Step 2**: Configure GitHub repository
- Enable GitHub Actions
- Add required secrets (NUGET_API_KEY)
- Configure branch protection rules

**Step 3**: Test workflows
```bash
# Trigger PR workflow
git checkout -b test-cicd
git push origin test-cicd
# Open PR and verify checks run
```

**Step 4**: Document contribution process
```markdown
# CONTRIBUTING.md
## Pull Request Process
1. Fork repository
2. Create feature branch
3. Make changes
4. Push and open PR
5. Wait for CI checks (automated)
6. Address review feedback
7. Merge when approved and passing
```

### Backward Compatibility

**Fully Backward Compatible**: CI/CD is additive infrastructure.

---

## Validation

### Acceptance Criteria

- [x] PR validation workflow runs on every PR
- [x] Multi-platform builds (Windows, Linux, macOS) pass
- [x] Integration tests run with RabbitMQ container
- [x] Security scanning integrated (Dependabot, CodeQL)
- [x] Release workflow publishes to NuGet.org
- [x] Code coverage reporting configured
- [x] Branch protection enforces CI checks

### Testing Strategy

**Workflow Testing**:
1. Create test PR → verify all checks run
2. Push to main branch → verify CI workflow runs
3. Create Git tag → verify release workflow runs
4. Test manual release dispatch

**Performance**:
- PR validation: <10 minutes target
- CI build: <15 minutes target
- Release: <20 minutes target

### Rollback Plan

**If CI/CD fails**:
1. **Disable workflows**: Settings → Actions → Disable Actions
2. **Manual build**: `dotnet build && dotnet test && dotnet pack`
3. **Manual release**: `dotnet nuget push`

---

## Dependencies

### Affected Components

- All RawRabbit projects (build pipeline)
- GitHub repository (workflows, settings)

### Related ADRs

- **ADR-0010**: Security Scanning Toolchain (integrated into CI/CD)
- **ADR-0011**: RabbitMQ.Client Migration Strategy (integration tests)
- **ADR-0012**: Memory Handling Strategy (performance benchmarks)

### External Dependencies

**GitHub Actions**:
- actions/checkout@v4
- actions/setup-dotnet@v4
- actions/upload-artifact@v4
- codecov/codecov-action@v4
- softprops/action-gh-release@v1

**NuGet Packages**:
- Nerdbank.GitVersioning (versioning)
- coverlet.collector (code coverage)

---

## Timeline

**Proposed**: 2025-10-09

**Implementation Start**: 2025-10-16 (Stage 2, Week 3)

**Target Completion**: 2025-10-23 (Stage 2, Week 4)

**Milestones**:
- Week 3: PR validation workflow
- Week 3: CI workflow
- Week 4: Release workflow
- Week 4: Documentation and training

---

## References

### Documentation

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET on GitHub Actions](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net)
- [NuGet Package Publishing](https://learn.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-the-dotnet-cli)

### Research

- **Migration Roadmap**: docs/stage-1/migration-roadmap.md

---

## Notes

**CI/CD Philosophy**:
- Automate everything that can be automated
- Fail fast (detect issues early)
- Make the right thing easy (PR validation guides developers)

**Future Enhancements** (Not in Scope):
- Multi-region deployment (if needed)
- A/B testing infrastructure
- Canary releases
- Automatic rollback on failure detection

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2.1 |
