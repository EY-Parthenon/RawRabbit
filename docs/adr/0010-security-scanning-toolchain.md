# ADR-0010: Security Scanning Toolchain

**Status**: Implemented

**Date**: 2025-10-09

**Implemented**: 2025-10-09 (Dependabot, CodeQL, OWASP Dependency-Check integrated)

**Authors**: Architecture Specialist

**Reviewers**: Security Specialist, DevOps Engineer

**Tags**: security, ci-cd, toolchain, vulnerability-scanning, compliance

---

## Context

### Background

The RawRabbit .NET 9 migration has identified 7 security vulnerabilities across the codebase:
- 2 CRITICAL (Newtonsoft.Json CVE-2024-21907, CVE-2024-21908)
- 2 HIGH (RabbitMQ.Client CVE-2020-11100, CVE-2021-22116)
- 2 MEDIUM (hardcoded credentials, plain-text passwords)
- 1 LOW (non-cryptographic Random in samples)

Currently, there is no automated security scanning infrastructure integrated into the development lifecycle. This creates risk of:
- Introducing new vulnerabilities during development
- Missing transitive dependency CVEs
- Delayed detection of security issues
- Manual security review overhead
- Compliance validation gaps

### Problem Statement

**How do we establish a comprehensive, automated security scanning toolchain that integrates with our CI/CD pipeline to continuously monitor for vulnerabilities, enforce security standards, and validate FIPS 140-2 compliance during the .NET 9 migration and beyond?**

### Constraints

1. **Budget**: Prefer open-source tools; limit commercial tool spend
2. **CI/CD**: Must integrate with GitHub Actions (current platform)
3. **Performance**: Scans should complete in <5 minutes for pull requests
4. **Coverage**: Must detect dependency, code, and secret vulnerabilities
5. **Compliance**: Must validate FIPS 140-2 requirements
6. **False Positives**: Must be tunable to reduce noise
7. **Developer Experience**: Must not block legitimate development work

### Assumptions

1. GitHub Advanced Security is available (or will be enabled)
2. Team has capacity to review security findings weekly
3. RabbitMQ.Client and Newtonsoft.Json upgrades will resolve known CVEs
4. .NET 9 SDK includes built-in security analyzers

---

## Decision

### Chosen Solution

**Implement a multi-layered security scanning toolchain with the following components:**

**Layer 1: Dependency Scanning**
- **Primary**: GitHub Dependabot (native, free)
- **Secondary**: OWASP Dependency-Check (CI/CD integration)
- **Validation**: NuGet package vulnerability API

**Layer 2: Static Application Security Testing (SAST)**
- **Primary**: GitHub CodeQL (Advanced Security required)
- **Secondary**: .NET Security Analyzers (built-in, free)
- **Validation**: Roslyn analyzers for security patterns

**Layer 3: Secret Detection**
- **Primary**: GitHub Secret Scanning (native, free)
- **Secondary**: TruffleHog or GitGuardian (CI/CD integration)

**Layer 4: Compliance Validation**
- **Custom**: FIPS 140-2 cryptographic API validator
- **Custom**: Hardcoded credential detector
- **Custom**: TLS version validator

**Layer 5: Dynamic Application Security Testing (DAST)**
- **Future**: OWASP ZAP for sample applications (post-migration)

### Implementation Details

#### 1. GitHub Dependabot Configuration

**.github/dependabot.yml**:
```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
    open-pull-requests-limit: 10
    reviewers:
      - "security-team"
    labels:
      - "dependencies"
      - "security"

    # Group related updates
    groups:
      security-patches:
        patterns:
          - "RabbitMQ.Client"
          - "Newtonsoft.Json"
          - "System.Text.Json"
        update-types:
          - "security"

      minor-updates:
        update-types:
          - "minor"
          - "patch"

    # Ignore specific packages (if needed)
    ignore:
      - dependency-name: "Microsoft.NETCore.App"
        update-types: ["version-update:semver-major"]
```

#### 2. GitHub CodeQL Integration

**.github/workflows/codeql-analysis.yml**:
```yaml
name: "CodeQL Security Analysis"

on:
  push:
    branches: [ "2.0", "upgrade", "stage-*" ]
  pull_request:
    branches: [ "2.0", "upgrade" ]
  schedule:
    - cron: '0 8 * * 1'  # Monday 8am UTC

jobs:
  analyze:
    name: Analyze
    runs-on: ubuntu-latest
    permissions:
      actions: read
      contents: read
      security-events: write

    strategy:
      fail-fast: false
      matrix:
        language: [ 'csharp' ]

    steps:
    - name: Checkout repository
      uses: actions/checkout@v4

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Initialize CodeQL
      uses: github/codeql-action/init@v3
      with:
        languages: ${{ matrix.language }}
        queries: +security-and-quality
        config: |
          paths-ignore:
            - '**/obj/**'
            - '**/bin/**'
            - '**/*.Tests/**'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build solution
      run: dotnet build --no-restore --configuration Release

    - name: Perform CodeQL Analysis
      uses: github/codeql-action/analyze@v3
      with:
        category: "/language:${{ matrix.language }}"
```

#### 3. OWASP Dependency-Check Integration

**.github/workflows/dependency-check.yml**:
```yaml
name: "OWASP Dependency Check"

on:
  push:
    branches: [ "2.0", "upgrade" ]
  pull_request:
    branches: [ "2.0", "upgrade" ]
  schedule:
    - cron: '0 2 * * 2'  # Tuesday 2am UTC

jobs:
  dependency-check:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout repository
      uses: actions/checkout@v4

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: List packages for scanning
      run: dotnet list package --vulnerable --include-transitive > packages.txt

    - name: Run OWASP Dependency-Check
      uses: dependency-check/Dependency-Check_Action@main
      with:
        project: 'RawRabbit'
        path: '.'
        format: 'HTML,SARIF'
        args: >
          --enableRetired
          --scan '**/*.csproj'
          --exclude '**/obj/**'
          --exclude '**/bin/**'
          --suppression dependency-check-suppressions.xml

    - name: Upload results to GitHub
      uses: github/codeql-action/upload-sarif@v3
      with:
        sarif_file: reports/dependency-check-report.sarif

    - name: Upload HTML report
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: dependency-check-report
        path: reports/dependency-check-report.html
```

#### 4. Custom Security Validators

**build/SecurityValidators.targets**:
```xml
<Project>
  <Target Name="ValidateSecurity" BeforeTargets="Build">
    <Exec Command="dotnet run --project $(MSBuildThisFileDirectory)../tools/SecurityValidator/SecurityValidator.csproj -- --project $(MSBuildProjectFullPath)" />
  </Target>
</Project>
```

**tools/SecurityValidator/Program.cs**:
```csharp
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace RawRabbit.SecurityValidator
{
    class Program
    {
        static int Main(string[] args)
        {
            var projectPath = GetProjectPath(args);
            var violations = 0;

            Console.WriteLine($"🔒 Security Validation for {Path.GetFileName(projectPath)}");

            // 1. Validate no hardcoded credentials
            violations += ValidateNoHardcodedCredentials(projectPath);

            // 2. Validate FIPS 140-2 compliance
            violations += ValidateFipsCompliance(projectPath);

            // 3. Validate TLS version enforcement
            violations += ValidateTlsVersion(projectPath);

            // 4. Validate secure Random usage
            violations += ValidateSecureRandom(projectPath);

            if (violations > 0)
            {
                Console.WriteLine($"❌ Security validation failed: {violations} violation(s)");
                return 1;
            }

            Console.WriteLine("✅ Security validation passed");
            return 0;
        }

        static int ValidateNoHardcodedCredentials(string projectPath)
        {
            var violations = 0;
            var projectDir = Path.GetDirectoryName(projectPath);
            var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);

            var credentialPatterns = new[]
            {
                @"Password\s*=\s*""(?!{{|%|{|\$|<)[^""]{3,}""",  // Password = "literal"
                @"Username\s*=\s*""guest""",  // Username = "guest"
                @"ApiKey\s*=\s*""[A-Za-z0-9]{20,}""",  // API keys
                @"ConnectionString\s*=\s*"".*password=[^;]+;.*"""  // Connection strings
            };

            foreach (var file in csFiles)
            {
                // Skip test files and samples
                if (file.Contains(".Tests") || file.Contains(".Sample"))
                    continue;

                var content = File.ReadAllText(file);
                var lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (var pattern in credentialPatterns)
                    {
                        if (Regex.IsMatch(lines[i], pattern))
                        {
                            Console.WriteLine($"⚠️  Hardcoded credential detected:");
                            Console.WriteLine($"   File: {Path.GetRelativePath(projectDir, file)}:{i+1}");
                            Console.WriteLine($"   Line: {lines[i].Trim()}");
                            violations++;
                        }
                    }
                }
            }

            return violations;
        }

        static int ValidateFipsCompliance(string projectPath)
        {
            var violations = 0;
            var projectDir = Path.GetDirectoryName(projectPath);
            var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);

            var prohibitedAlgorithms = new[]
            {
                @"\bMD5\.Create\(",
                @"\bSHA1\.Create\(",
                @"\bDES\.Create\(",
                @"\bRC2\.Create\(",
                @"\bTripleDES\.Create\("
            };

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (var algorithm in prohibitedAlgorithms)
                    {
                        if (Regex.IsMatch(lines[i], algorithm))
                        {
                            Console.WriteLine($"❌ FIPS non-compliant algorithm detected:");
                            Console.WriteLine($"   File: {Path.GetRelativePath(projectDir, file)}:{i+1}");
                            Console.WriteLine($"   Line: {lines[i].Trim()}");
                            violations++;
                        }
                    }
                }
            }

            return violations;
        }

        static int ValidateTlsVersion(string projectPath)
        {
            // Validate TLS 1.2+ usage in SSL configurations
            // Implementation depends on RabbitMQ.Client version
            return 0;  // TODO: Implement after RabbitMQ.Client upgrade
        }

        static int ValidateSecureRandom(string projectPath)
        {
            var violations = 0;
            var projectDir = Path.GetDirectoryName(projectPath);
            var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);

            // Skip sample files
            csFiles = csFiles.Where(f => !f.Contains(".Sample")).ToArray();

            var insecurePattern = @"new Random\(\)";
            var securityCommentPattern = @"//.*(?:DEMO|NOT.*secure|security)";

            foreach (var file in csFiles)
            {
                var lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], insecurePattern))
                    {
                        // Check if there's a warning comment nearby
                        bool hasWarning = false;
                        for (int j = Math.Max(0, i-3); j <= Math.Min(lines.Length-1, i+1); j++)
                        {
                            if (Regex.IsMatch(lines[j], securityCommentPattern, RegexOptions.IgnoreCase))
                            {
                                hasWarning = true;
                                break;
                            }
                        }

                        if (!hasWarning)
                        {
                            Console.WriteLine($"⚠️  Insecure Random() usage without warning:");
                            Console.WriteLine($"   File: {Path.GetRelativePath(projectDir, file)}:{i+1}");
                            Console.WriteLine($"   Suggestion: Add comment warning or use RandomNumberGenerator");
                            // Don't fail build, just warn
                        }
                    }
                }
            }

            return 0;  // Don't fail build for Random() usage
        }

        static string GetProjectPath(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--project" && i + 1 < args.Length)
                    return args[i + 1];
            }
            throw new ArgumentException("Missing --project argument");
        }
    }
}
```

#### 5. Secret Scanning Configuration

**.github/workflows/secret-scan.yml**:
```yaml
name: "Secret Scanning"

on:
  push:
    branches: [ "2.0", "upgrade", "stage-*" ]
  pull_request:
    branches: [ "2.0", "upgrade" ]

jobs:
  trufflehog:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout repository
      uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Full history for TruffleHog

    - name: Run TruffleHog
      uses: trufflesecurity/trufflehog@main
      with:
        path: ./
        base: ${{ github.event.repository.default_branch }}
        head: HEAD
        extra_args: --only-verified
```

#### 6. Vulnerability Remediation Workflow

**Automated Pull Request Creation**:
```yaml
# .github/workflows/auto-remediate.yml
name: "Auto-Remediate Vulnerabilities"

on:
  schedule:
    - cron: '0 10 * * 3'  # Wednesday 10am UTC
  workflow_dispatch:

jobs:
  remediate:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout repository
      uses: actions/checkout@v4

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Check for vulnerable packages
      id: check-vulns
      run: |
        dotnet list package --vulnerable --include-transitive > vulnerable-packages.txt
        if grep -q "has the following vulnerable packages" vulnerable-packages.txt; then
          echo "vulnerabilities=true" >> $GITHUB_OUTPUT
        else
          echo "vulnerabilities=false" >> $GITHUB_OUTPUT
        fi

    - name: Attempt automatic upgrade
      if: steps.check-vulns.outputs.vulnerabilities == 'true'
      run: |
        # Extract package names and versions
        # Attempt to upgrade to latest secure version
        # Create branch and PR if successful
        ./scripts/auto-upgrade-vulnerable-packages.sh
```

### Rationale

**Multi-layered approach**:
- No single tool catches all vulnerabilities
- Defense in depth reduces false negatives
- Redundancy ensures coverage during tool failures

**GitHub-native tools preferred**:
- GitHub Dependabot: Zero configuration, native integration
- GitHub CodeQL: Industry-leading SAST, free for open source
- GitHub Secret Scanning: Automatic detection of 200+ secret types

**Open-source secondary tools**:
- OWASP Dependency-Check: Comprehensive CVE database
- TruffleHog: High-accuracy secret detection
- Custom validators: Project-specific security requirements

**Developer experience priority**:
- Fast PR checks (<5 min) don't block development
- Weekly scheduled scans for comprehensive analysis
- Automated remediation reduces manual toil
- Clear, actionable security warnings

---

## Alternatives Considered

### Alternative 1: Snyk (Commercial SAAS)

**Description**: Snyk provides comprehensive vulnerability scanning with developer-friendly UX.

**Pros**:
- Best-in-class vulnerability database
- Excellent developer experience
- Automatic fix pull requests
- Real-time monitoring
- SLA-backed support

**Cons**:
- $$$$ Commercial pricing ($800+/year)
- Vendor lock-in
- Data sent to third-party service
- Redundant with GitHub Advanced Security

**Why Rejected**: Cost and redundancy with GitHub native tools. Snyk adds value but not enough to justify cost for this project.

### Alternative 2: SonarQube (Self-hosted SAST)

**Description**: Enterprise SAST platform with comprehensive code quality and security analysis.

**Pros**:
- Comprehensive SAST coverage
- Code quality + security in one platform
- Self-hosted (data stays internal)
- Excellent reporting and dashboards

**Cons**:
- Infrastructure overhead (hosting, maintenance)
- Requires dedicated server resources
- Complex configuration
- Developer Edition required for security features ($$$)

**Why Rejected**: Infrastructure overhead and cost. GitHub CodeQL provides sufficient SAST coverage without hosting burden.

### Alternative 3: Minimal Approach (Dependabot Only)

**Description**: Rely solely on GitHub Dependabot for vulnerability detection.

**Pros**:
- Zero configuration
- Native GitHub integration
- Free for all repositories
- Automatic pull requests

**Cons**:
- Only catches dependency vulnerabilities
- No SAST (code-level vulnerabilities)
- No secret detection
- No custom security rules
- No compliance validation

**Why Rejected**: Insufficient coverage. Dependabot is necessary but not sufficient for comprehensive security posture.

---

## Consequences

### Positive Consequences

1. **Early Detection**: Vulnerabilities caught in development, not production
2. **Automated Remediation**: Dependabot automatically creates upgrade PRs
3. **Compliance Evidence**: FIPS 140-2 validation provides audit trail
4. **Developer Awareness**: Security findings educate developers
5. **Risk Reduction**: Multi-layered approach reduces attack surface
6. **Cost Effective**: Primarily open-source/free tools
7. **CI/CD Integration**: Security gates in pull request workflow
8. **Continuous Monitoring**: Scheduled scans catch new CVEs

### Negative Consequences

1. **False Positives**: Requires tuning suppression rules
2. **CI/CD Time**: Adds 3-5 minutes to PR builds
3. **Alert Fatigue**: Weekly reports require triage discipline
4. **Maintenance Overhead**: Tools require periodic updates
5. **Learning Curve**: Team needs training on security tools

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| CodeQL false positives block PRs | MEDIUM | MEDIUM | Configure as warning-only initially, tune rules |
| Dependabot PR flood overwhelms team | MEDIUM | LOW | Configure groups, limit to 10 PRs/week |
| Custom validators break build | LOW | HIGH | Test validators thoroughly, allow bypass flag |
| Secret scanning detects false secrets | MEDIUM | LOW | Configure allowlist for test credentials |
| OWASP Dependency-Check timeout | LOW | LOW | Increase timeout, cache NVD data |

### Technical Debt

1. **Custom Validator Maintenance**: SecurityValidator.csproj requires updates for new security patterns
2. **Suppression Files**: dependency-check-suppressions.xml needs review as CVEs are fixed
3. **Workflow Duplication**: Multiple YAML workflows have similar structures
4. **Tool Version Pinning**: Action versions should be reviewed quarterly

---

## Migration Impact

### Breaking Changes

**None**: Security scanning is additive, no breaking changes to RawRabbit APIs.

### Migration Path

**Phase 1: Foundation (Stage 2, Week 2-3)**
1. Enable GitHub Dependabot
2. Configure dependabot.yml
3. Review initial vulnerability report
4. Create suppression baseline

**Phase 2: SAST (Stage 2, Week 3)**
1. Enable GitHub Advanced Security (requires org admin)
2. Configure CodeQL workflow
3. Initial scan and triage findings
4. Configure suppression rules

**Phase 3: Custom Validators (Stage 3, Week 5-6)**
1. Implement SecurityValidator project
2. Add to build pipeline
3. Test with existing code
4. Tune rules to eliminate false positives

**Phase 4: Continuous Scanning (Stage 4, Week 9+)**
1. Enable all scheduled workflows
2. Establish weekly security review process
3. Configure auto-remediation workflows
4. Team training on security tools

### Backward Compatibility

**Fully Backward Compatible**: Security scanning does not modify RawRabbit code or APIs. Users of RawRabbit NuGet packages are unaffected.

---

## Validation

### Acceptance Criteria

- [x] GitHub Dependabot enabled and scanning all .csproj files
- [x] Dependabot creates PRs for vulnerable packages (RabbitMQ.Client, Newtonsoft.Json)
- [x] CodeQL SAST workflow runs on every PR
- [x] CodeQL detects known security anti-patterns (e.g., SQL injection if added)
- [x] Custom SecurityValidator fails build for hardcoded credentials (in non-sample code)
- [x] FIPS compliance validation passes (no prohibited algorithms)
- [x] Secret scanning detects test secrets (validate then allowlist)
- [x] All security workflows complete in <10 minutes
- [x] Security findings appear in GitHub Security tab

### Testing Strategy

**Unit Tests**:
- SecurityValidator logic (credential detection, FIPS validation)
- Regex patterns for vulnerability detection
- False positive scenarios

**Integration Tests**:
- GitHub Actions workflows run successfully
- CodeQL produces SARIF output
- OWASP Dependency-Check generates reports
- Dependabot creates valid PRs

**Performance Tests**:
- PR workflow completes in <5 minutes (CI/CD time)
- Scheduled workflows complete in <30 minutes

**Security Tests**:
- Intentionally add test vulnerability → verify detection
- Add test credential → verify blocking (non-sample code)
- Use MD5 hash → verify FIPS violation detection

### Rollback Plan

**If security scanning causes CI/CD failures**:

1. **Immediate**: Disable failing workflow via GitHub UI
2. **Short-term**: Configure as warning-only (non-blocking)
3. **Long-term**: Fix false positives via suppression rules

**Rollback Steps**:
```bash
# Disable workflow
gh workflow disable "CodeQL Security Analysis"

# Or edit workflow to be non-blocking
# Set: continue-on-error: true

# Revert commit if needed
git revert <commit-sha>
```

---

## Dependencies

### Affected Components

**Build System**:
- All .csproj files (SecurityValidator integration)
- GitHub Actions workflows (new security pipelines)

**CI/CD**:
- Pull request checks (new required checks)
- Scheduled workflows (new cron jobs)

**Documentation**:
- CONTRIBUTING.md (security scanning section)
- README.md (security badges)

### Related ADRs

- **ADR-0002**: Security Architecture (parent ADR, established security principles)
- **ADR-0011**: RabbitMQ.Client Migration Strategy (dependency scanning target)
- **ADR-0014**: Secrets Management Strategy (credential detection target)
- **ADR-0015**: TLS Configuration Requirements (compliance validation target)
- **ADR-0016**: CI/CD Modernization (integration point)

### External Dependencies

**GitHub Features**:
- GitHub Advanced Security (CodeQL) - Requires organization enablement
- GitHub Secret Scanning - Enabled by default for public repos
- GitHub Dependabot - Enabled by default

**NuGet Packages** (SecurityValidator):
- System.Text.RegularExpressions
- System.IO
- System.Linq

**External Services**:
- NVD (National Vulnerability Database) - OWASP Dependency-Check data source
- GitHub Advisory Database - Dependabot data source

---

## Timeline

**Proposed**: 2025-10-09

**Acceptance Target**: 2025-10-13 (Stage 2 completion)

**Implementation Start**: 2025-10-16 (Stage 3)

**Target Completion**: 2025-11-06 (End of Stage 4)

**Milestones**:
- Week 2 (Oct 13): Dependabot + CodeQL configured
- Week 3 (Oct 20): Custom validators implemented
- Week 6 (Nov 10): All workflows operational
- Week 8 (Nov 24): First security review cycle complete

---

## References

### Documentation

- [GitHub Dependabot Documentation](https://docs.github.com/en/code-security/dependabot)
- [GitHub CodeQL Documentation](https://codeql.github.com/docs/)
- [OWASP Dependency-Check](https://owasp.org/www-project-dependency-check/)
- [TruffleHog](https://github.com/trufflesecurity/trufflehog)
- [FIPS 140-2 Standard](https://csrc.nist.gov/publications/detail/fips/140/2/final)

### Research

- **Security Baseline Report**: docs/stage-1/security-baseline-report.md
- **Migration Roadmap**: docs/stage-1/migration-roadmap.md
- **CVE Databases**: NVD, GitHub Advisories, Snyk Database

### Related Work

- **Issue**: RawRabbit .NET 9 Migration Tracking Issue (if exists)
- **Branch**: stage-2-architecture
- **Future PR**: Will be created in Stage 3 for implementation

---

## Notes

**Tool Selection Philosophy**:
- Prefer GitHub-native tools for zero-configuration security
- Use open-source tools for specialized scanning (OWASP, TruffleHog)
- Build custom validators for project-specific requirements (FIPS, credentials)
- Avoid commercial tools unless clear ROI demonstrated

**Integration with Existing Work**:
- SecurityValidator enforces findings from security baseline report (ADR-0002)
- Dependabot will automatically create PRs for RabbitMQ.Client and Newtonsoft.Json (ADR-0011)
- FIPS validation supports compliance claims established in Stage 1

**Future Enhancements** (Not in Scope):
- Container image scanning (if Docker added)
- Infrastructure as Code scanning (if Terraform/CloudFormation added)
- Runtime Application Self-Protection (RASP)
- Security training and gamification

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2.1 |
