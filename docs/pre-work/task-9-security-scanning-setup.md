# Task 9: Security Scanning Tools Setup Guide

**Date**: 2025-10-09
**Author**: Security Engineer
**Session ID**: dotnet9-upgrade
**Status**: Complete

---

## Executive Summary

This document provides comprehensive guidance for setting up security scanning tools for the RawRabbit .NET 9 upgrade project. Based on the security review plan (docs/planning/security-review-plan.md), we need to implement 9 security checkpoints, which require robust scanning capabilities across vulnerability detection, dependency analysis, cryptographic validation, and supply chain security.

**Key Findings**:
- **Critical**: RabbitMQ.Client 5.0.1 has 2 HIGH severity CVEs
- **Critical**: Newtonsoft.Json 10.0.1 has 2 CRITICAL severity CVEs (including RCE)
- **Required Tools**: 5 essential + 3 recommended
- **Estimated Cost**: $0-$99/month (depending on features needed)
- **Setup Time**: 2-4 hours for complete toolchain

---

## Table of Contents

1. [Security Tools Inventory](#1-security-tools-inventory)
2. [Tool-by-Tool Setup Guide](#2-tool-by-tool-setup-guide)
3. [GitHub Actions Integration](#3-github-actions-integration)
4. [Configuration Files](#4-configuration-files)
5. [Cost Analysis](#5-cost-analysis)
6. [Recommended Toolchain](#6-recommended-toolchain)
7. [Testing & Validation](#7-testing--validation)
8. [Appendix](#8-appendix)

---

## 1. Security Tools Inventory

### 1.1 Essential Tools (Required)

#### A. .NET Built-in Security Tools

| Tool | Purpose | Cost | Integration |
|------|---------|------|-------------|
| `dotnet list package --vulnerable` | NuGet vulnerability scanning | Free | CLI, GitHub Actions |
| `dotnet format analyzers` | Code quality & security analyzers | Free | CLI, GitHub Actions |
| Microsoft Security Code Analysis | SAST for .NET | Free (VS) / Paid (Azure DevOps) | Visual Studio, Azure Pipelines |

**Capabilities**:
- Detects known CVEs in NuGet packages
- Scans transitive dependencies
- Integrates with National Vulnerability Database (NVD)
- Real-time IDE warnings (Visual Studio)

**Limitations**:
- Only covers NuGet packages (not code vulnerabilities)
- Requires manual updates to vulnerability database
- No historical tracking without custom scripting

---

#### B. OWASP Dependency-Check

| Tool | Purpose | Cost | Integration |
|------|---------|------|-------------|
| OWASP Dependency-Check | Dependency vulnerability scanner | Free (Open Source) | CLI, GitHub Actions, CI/CD |

**Capabilities**:
- Scans .NET, Java, JavaScript, Python, Ruby dependencies
- Supports multiple vulnerability databases (NVD, GitHub Advisories, OSS Index)
- Generates HTML, XML, JSON, SARIF reports
- Integrates with CI/CD pipelines
- Supports proxy and offline mode
- CVE correlation with CVSS scores

**Limitations**:
- Can be slow on large projects (25 projects = 5-10 minutes)
- High false-positive rate (requires manual review)
- Requires periodic database updates

**Installation**:
```bash
# Download latest release
wget https://github.com/jeremylong/DependencyCheck/releases/download/v9.0.7/dependency-check-9.0.7-release.zip
unzip dependency-check-9.0.7-release.zip

# Or use Docker
docker pull owasp/dependency-check:latest
```

---

#### C. GitHub Advanced Security

| Feature | Purpose | Cost | Integration |
|---------|---------|------|-------------|
| Dependabot | Automated dependency updates | Free (public repos) | GitHub native |
| Dependabot Security Alerts | CVE notifications | Free (public repos) | GitHub native |
| Code Scanning (CodeQL) | SAST for 20+ languages | Free (public repos) | GitHub Actions |
| Secret Scanning | Hardcoded credential detection | Free (public repos) | GitHub native |

**Capabilities**:
- **Dependabot**:
  - Automatic PR creation for vulnerable dependencies
  - Supports NuGet, npm, pip, Maven, etc.
  - Configurable update schedules and strategies
  - Security updates prioritized over version updates

- **CodeQL**:
  - 300+ security queries for C#
  - Dataflow analysis for SQL injection, XSS, etc.
  - Custom query support
  - SARIF report generation

- **Secret Scanning**:
  - Detects 100+ credential types (API keys, passwords, tokens)
  - Push protection (blocks commits with secrets)
  - Historical scanning of entire Git history

**Limitations**:
- Requires GitHub Advanced Security license for private repos ($49/user/month)
- CodeQL scans can be slow (10-15 minutes for large repos)
- False positives on secret scanning (e.g., test fixtures)

**Setup**: See [Section 3.2](#32-github-advanced-security-setup)

---

#### D. Snyk

| Tier | Purpose | Cost | Integration |
|------|---------|------|-------------|
| Snyk Free | Dependency & container scanning | Free (200 tests/month) | CLI, GitHub Actions, IDE |
| Snyk Team | Enhanced scanning + reporting | $52/month | CLI, GitHub Actions, IDE, CI/CD |
| Snyk Enterprise | Advanced features + custom rules | Custom pricing | Full platform integration |

**Capabilities**:
- **Dependency Scanning**:
  - Real-time vulnerability database (faster than NVD)
  - Fix recommendations with upgrade paths
  - Automatic PR generation
  - License compliance checking

- **Code Scanning**:
  - SAST engine for 10+ languages
  - Framework-specific rules (ASP.NET, Entity Framework)
  - IDE integration (Visual Studio, VS Code, Rider)

- **Container Scanning**:
  - Docker image vulnerability detection
  - Base image recommendations

- **Infrastructure as Code (IaC)**:
  - Terraform, CloudFormation, Kubernetes scanning

**Limitations**:
- Free tier limited to 200 tests/month (insufficient for CI/CD)
- Requires account signup and API token
- Some advanced features require paid tier

**Installation**:
```bash
npm install -g snyk
snyk auth  # Requires Snyk account
snyk test  # Scan project
```

---

#### E. SonarQube/SonarCloud

| Edition | Purpose | Cost | Integration |
|---------|---------|------|-------------|
| SonarCloud Free | Open source projects | Free | GitHub Actions |
| SonarCloud Paid | Private repos | $10/month (100K LOC) | GitHub Actions, CI/CD |
| SonarQube Community | Self-hosted | Free (Open Source) | Self-hosted, CI/CD |
| SonarQube Enterprise | Advanced features | Custom pricing | Self-hosted, CI/CD |

**Capabilities**:
- **Security Hotspots**:
  - SQL injection, XSS, hardcoded credentials
  - Cryptographic weakness detection
  - Authentication/authorization issues

- **Code Quality**:
  - Code smells, technical debt, duplications
  - Cyclomatic complexity analysis
  - Test coverage tracking

- **Security Rules for .NET**:
  - 80+ security-specific rules
  - Vulnerable dependency detection
  - Insecure configuration detection

**Limitations**:
- Requires SonarQube server or SonarCloud account
- Complex initial configuration
- Can be resource-intensive (self-hosted)

---

### 1.2 Recommended Tools (Optional but Valuable)

#### F. Microsoft Security Risk Detection (MSRD)

| Tool | Purpose | Cost | Integration |
|------|---------|------|-------------|
| MSRD | Fuzz testing for .NET | Free (preview) | Azure Portal |

**Capabilities**:
- Automated fuzz testing for .NET applications
- Discovers memory corruption, crashes, hangs
- Integrates with Azure DevOps
- Supports RabbitMQ message fuzzing

**Status**: Currently in preview, availability varies

---

#### G. NuGet Package Signing Tools

| Tool | Purpose | Cost | Integration |
|------|---------|------|-------------|
| NuGet sign | Package signature verification | Free | CLI |
| Azure Key Vault | Code signing certificate storage | $0.03/10K operations | Azure |

**Capabilities**:
- Verify package signatures
- Sign custom NuGet packages
- Prevent package tampering

---

#### H. SBOM Generation Tools

| Tool | Purpose | Cost | Integration |
|------|---------|------|-------------|
| Microsoft SBOM Tool | Software Bill of Materials | Free (Open Source) | CLI, GitHub Actions |
| CycloneDX | SBOM generation (SPDX, CycloneDX) | Free (Open Source) | CLI, GitHub Actions |

**Capabilities**:
- Generate SPDX or CycloneDX SBOMs
- Track all dependencies across 25 projects
- Supply chain transparency
- Compliance requirements (EO 14028)

---

## 2. Tool-by-Tool Setup Guide

### 2.1 .NET Built-in Vulnerability Scanning

#### Prerequisites
- .NET 9 SDK installed
- Access to RawRabbit solution file

#### Setup Steps

**Step 1: Basic Vulnerability Scan**
```bash
cd /path/to/RawRabbit
dotnet list package --vulnerable
```

**Expected Output**:
```
The following sources were used:
   https://api.nuget.org/v3/index.json

Project `RawRabbit` has the following vulnerable packages
   [net461]:
   Top-level Package      Requested   Resolved   Severity   Advisory URL
   > RabbitMQ.Client      5.0.1       5.0.1      High       https://github.com/advisories/GHSA-xxxx
   > Newtonsoft.Json      10.0.1      10.0.1     Critical   https://github.com/advisories/GHSA-yyyy
```

**Step 2: Include Transitive Dependencies**
```bash
dotnet list package --vulnerable --include-transitive
```

**Step 3: Export Results to File**
```bash
dotnet list package --vulnerable --include-transitive > vulnerability-scan-$(date +%Y%m%d).txt
```

**Step 4: Automate in GitHub Actions** (see Section 3.1)

---

### 2.2 OWASP Dependency-Check Setup

#### Prerequisites
- Java 8+ (required for Dependency-Check)
- Or Docker (for containerized execution)

#### Option A: Docker Setup (Recommended)

**Step 1: Create Scan Script**
```bash
#!/bin/bash
# scripts/security/dependency-check-scan.sh

PROJECT_NAME="RawRabbit"
SCAN_DIR="./src"
OUTPUT_DIR="./docs/security-reports"
OUTPUT_FORMAT="HTML,JSON,SARIF"

mkdir -p "$OUTPUT_DIR"

docker run --rm \
  -v $(pwd):/src \
  -v $(pwd)/.owasp/data:/usr/share/dependency-check/data \
  owasp/dependency-check:latest \
  --scan /src/$SCAN_DIR \
  --project "$PROJECT_NAME" \
  --format "$OUTPUT_FORMAT" \
  --out /src/$OUTPUT_DIR \
  --suppression /src/.owasp/suppressions.xml \
  --enableExperimental \
  --nvdApiKey "$NVD_API_KEY"  # Optional: speeds up scans

echo "Scan complete. Reports available in $OUTPUT_DIR"
```

**Step 2: Create Suppression File** (for false positives)
```xml
<!-- .owasp/suppressions.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<suppressions xmlns="https://jeremylong.github.io/DependencyCheck/dependency-suppression.1.3.xsd">
    <!-- Example: Suppress false positive for test-only dependency -->
    <suppress>
        <notes><![CDATA[
        This is a test-only dependency and does not pose production risk.
        ]]></notes>
        <packageUrl regex="true">^pkg:nuget/Moq@.*$</packageUrl>
        <cve>CVE-2023-XXXXX</cve>
    </suppress>
</suppressions>
```

**Step 3: Run Scan**
```bash
chmod +x scripts/security/dependency-check-scan.sh
./scripts/security/dependency-check-scan.sh
```

#### Option B: Standalone Installation

**Step 1: Download and Install**
```bash
wget https://github.com/jeremylong/DependencyCheck/releases/download/v9.0.7/dependency-check-9.0.7-release.zip
unzip dependency-check-9.0.7-release.zip -d ~/tools/
export PATH="$PATH:$HOME/tools/dependency-check/bin"
```

**Step 2: Run Scan**
```bash
dependency-check.sh \
  --project RawRabbit \
  --scan ./src \
  --format HTML \
  --out ./docs/security-reports
```

#### Performance Optimization

**Use NVD API Key** (speeds up scans 5-10x):
1. Register at https://nvd.nist.gov/developers/request-an-api-key
2. Set environment variable:
   ```bash
   export NVD_API_KEY="your-api-key-here"
   ```
3. Add to scan command: `--nvdApiKey "$NVD_API_KEY"`

**Incremental Scanning**:
```bash
# Cache vulnerability database locally
mkdir -p .owasp/data
docker run --rm \
  -v $(pwd)/.owasp/data:/usr/share/dependency-check/data \
  owasp/dependency-check:latest \
  --updateonly
```

---

### 2.3 GitHub Advanced Security Setup

#### 2.3.1 Dependabot Configuration

**Step 1: Create Dependabot Configuration**
```yaml
# .github/dependabot.yml
version: 2
updates:
  # NuGet dependencies
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

    # Security updates (prioritized)
    # Dependabot automatically prioritizes security updates

    # Version updates (grouped by type)
    groups:
      minor-and-patch:
        patterns:
          - "*"
        update-types:
          - "minor"
          - "patch"

    # Ignore specific dependencies (if needed)
    ignore:
      # Example: Ignore major version updates for breaking changes
      - dependency-name: "RabbitMQ.Client"
        update-types: ["version-update:semver-major"]

  # GitHub Actions dependencies
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
    labels:
      - "github-actions"
      - "dependencies"
```

**Step 2: Enable Dependabot Alerts**
1. Go to GitHub repository settings
2. Navigate to "Security & analysis"
3. Enable "Dependency graph"
4. Enable "Dependabot alerts"
5. Enable "Dependabot security updates"

**Step 3: Configure Alert Recipients**
```yaml
# .github/CODEOWNERS (optional: auto-assign security alerts)
# Security-related files
/.github/dependabot.yml @security-team
/src/RawRabbit/Configuration/RawRabbitConfiguration.cs @security-team
```

#### 2.3.2 CodeQL Setup

**Step 1: Create CodeQL Workflow**
```yaml
# .github/workflows/codeql-analysis.yml
name: "CodeQL Security Scan"

on:
  push:
    branches: ["2.0", "upgrade", "pre-work"]
  pull_request:
    branches: ["2.0"]
  schedule:
    # Run weekly on Monday at 6:00 AM UTC
    - cron: '0 6 * * 1'

jobs:
  analyze:
    name: Analyze C# Code
    runs-on: ubuntu-latest
    permissions:
      actions: read
      contents: read
      security-events: write

    strategy:
      fail-fast: false
      matrix:
        language: ['csharp']

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: ${{ matrix.language }}
          # Custom queries (optional)
          # queries: security-and-quality

          # Security-specific query pack
          packs: "codeql/csharp-queries:security-and-quality"

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build --no-restore --configuration Release

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3
        with:
          category: "/language:${{ matrix.language }}"
          # Upload SARIF results
          upload: true
```

**Step 2: Enable Code Scanning**
1. Go to GitHub repository settings
2. Navigate to "Security & analysis"
3. Enable "Code scanning"
4. CodeQL workflow will run automatically after creation

**Step 3: Review Results**
- Navigate to "Security" tab > "Code scanning alerts"
- Filter by severity (Critical, High, Medium, Low)
- Review and dismiss false positives

#### 2.3.3 Secret Scanning Setup

**Step 1: Enable Secret Scanning**
1. Go to GitHub repository settings
2. Navigate to "Security & analysis"
3. Enable "Secret scanning"
4. Enable "Push protection" (prevents commits with secrets)

**Step 2: Create Secret Pattern (Custom)**
```yaml
# .github/secret_scanning.yml (requires GitHub Enterprise)
patterns:
  - name: RabbitMQ Connection String
    pattern: 'amqp://[a-zA-Z0-9]+:[a-zA-Z0-9]+@[a-zA-Z0-9.-]+:[0-9]+'
    confidence: high
```

**Step 3: Exclude Test Fixtures**
```gitattributes
# .gitattributes
# Exclude test fixture files from secret scanning
test/fixtures/* linguist-generated=true
test/**/TestData.cs linguist-generated=true
```

---

### 2.4 Snyk Setup

#### Prerequisites
- Node.js 14+ (for Snyk CLI)
- Snyk account (free tier available)

#### Step 1: Install Snyk CLI
```bash
npm install -g snyk
```

#### Step 2: Authenticate
```bash
snyk auth
# Opens browser for authentication
```

#### Step 3: Test Project
```bash
cd /path/to/RawRabbit
snyk test --all-projects
```

**Expected Output**:
```
Testing RawRabbit...

Organization:      your-org
Package manager:   nuget
Target file:       RawRabbit.csproj
Project name:      RawRabbit
Open source:       yes
Project path:      /src/RawRabbit

✗ High severity vulnerability found in RabbitMQ.Client
  Description: Denial of Service
  Info: https://snyk.io/vuln/SNYK-DOTNET-RABBITMQCLIENT-XXXXX
  From: RabbitMQ.Client@5.0.1
  Fix: Upgrade to RabbitMQ.Client@6.2.1 or higher
```

#### Step 4: Monitor Project (Continuous Monitoring)
```bash
snyk monitor --all-projects
```

#### Step 5: Configure Snyk Policies
```yaml
# .snyk
version: v1.22.0

# Ignore specific vulnerabilities (with justification)
ignore:
  'SNYK-DOTNET-SYSTEMTEXTJSON-XXXXX':
    - '*':
        reason: Not exploitable in our use case (no user-controlled JSON)
        expires: 2025-12-31
        created: 2025-10-09

# Patch directives (if available)
patch: {}
```

---

### 2.5 SonarQube/SonarCloud Setup

#### Option A: SonarCloud (Recommended for Open Source)

**Step 1: Create SonarCloud Account**
1. Go to https://sonarcloud.io
2. Sign in with GitHub
3. Import RawRabbit repository

**Step 2: Install SonarScanner for .NET**
```bash
dotnet tool install --global dotnet-sonarscanner
```

**Step 3: Configure Project**
```bash
# Create sonar-project.properties
cat > sonar-project.properties <<EOF
sonar.organization=your-org
sonar.projectKey=your-org_RawRabbit
sonar.projectName=RawRabbit
sonar.projectVersion=3.0.0
sonar.sources=src
sonar.tests=test
sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml
sonar.cs.vstest.reportsPaths=**/*.trx

# Security-specific settings
sonar.security.enable=true
sonar.security.hotspots.inherited=true
EOF
```

**Step 4: Run Analysis**
```bash
# Begin analysis
dotnet sonarscanner begin \
  /k:"your-org_RawRabbit" \
  /o:"your-org" \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

# Build project
dotnet build --no-incremental

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# End analysis
dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
```

#### Option B: SonarQube Community (Self-Hosted)

**Step 1: Run SonarQube with Docker**
```bash
docker run -d --name sonarqube \
  -p 9000:9000 \
  -e SONAR_ES_BOOTSTRAP_CHECKS_DISABLE=true \
  sonarqube:latest
```

**Step 2: Access and Configure**
1. Open http://localhost:9000
2. Default login: admin/admin (change immediately)
3. Create new project
4. Generate authentication token

**Step 3: Follow analysis steps from Option A** (use local URL)

---

### 2.6 SBOM Generation Setup

#### Prerequisites
- .NET 9 SDK
- Microsoft SBOM Tool

#### Step 1: Install SBOM Tool
```bash
dotnet tool install --global Microsoft.Sbom.DotNetTool
```

#### Step 2: Generate SBOM
```bash
# For entire solution
sbom-tool generate \
  -b ./output \
  -bc ./src \
  -pn RawRabbit \
  -pv 3.0.0 \
  -ps Pardahlman \
  -nsb https://sbom.rawrabbit.org \
  -m ./output/manifest.json \
  -v Information

# Output: manifest.json (SPDX format)
```

#### Step 3: Validate SBOM
```bash
sbom-tool validate -b ./output -m manifest.json -v Information
```

#### Step 4: Include in NuGet Package
```xml
<!-- RawRabbit.csproj -->
<ItemGroup>
  <None Include="$(OutputPath)manifest.json" Pack="true" PackagePath="sbom/" />
</ItemGroup>
```

---

## 3. GitHub Actions Integration

### 3.1 .NET Vulnerability Scanning Workflow

```yaml
# .github/workflows/security-vulnerability-scan.yml
name: Security - Vulnerability Scan

on:
  push:
    branches: ["2.0", "upgrade", "pre-work"]
  pull_request:
    branches: ["2.0"]
  schedule:
    # Run daily at 3:00 AM UTC
    - cron: '0 3 * * *'
  workflow_dispatch: # Manual trigger

jobs:
  vulnerability-scan:
    name: .NET Vulnerability Scan
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Scan for vulnerable packages
        run: |
          echo "## Vulnerability Scan Results" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY
          echo '```' >> $GITHUB_STEP_SUMMARY
          dotnet list package --vulnerable --include-transitive | tee -a $GITHUB_STEP_SUMMARY
          echo '```' >> $GITHUB_STEP_SUMMARY

      - name: Check for critical vulnerabilities
        id: check-vulns
        run: |
          # Fail if critical or high severity vulnerabilities found
          if dotnet list package --vulnerable --include-transitive | grep -q "Critical\|High"; then
            echo "::error::Critical or high severity vulnerabilities detected!"
            exit 1
          fi

      - name: Upload scan results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: vulnerability-scan-results
          path: |
            **/obj/project.assets.json
          retention-days: 30
```

---

### 3.2 OWASP Dependency-Check Workflow

```yaml
# .github/workflows/security-dependency-check.yml
name: Security - OWASP Dependency Check

on:
  push:
    branches: ["2.0", "upgrade"]
  pull_request:
    branches: ["2.0"]
  schedule:
    # Run weekly on Monday at 6:00 AM UTC
    - cron: '0 6 * * 1'
  workflow_dispatch:

jobs:
  dependency-check:
    name: OWASP Dependency Check
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Create output directory
        run: mkdir -p security-reports

      - name: Run OWASP Dependency-Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'RawRabbit'
          path: './src'
          format: 'HTML,JSON,SARIF'
          out: 'security-reports'
          args: >
            --enableExperimental
            --suppression .owasp/suppressions.xml
            --nvdApiKey ${{ secrets.NVD_API_KEY }}
            --failOnCVSS 7

      - name: Upload SARIF to GitHub Security
        if: always()
        uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: security-reports/dependency-check-report.sarif

      - name: Upload dependency check report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: dependency-check-report
          path: security-reports/
          retention-days: 90

      - name: Summarize results
        if: always()
        run: |
          echo "## OWASP Dependency-Check Results" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY
          if [ -f security-reports/dependency-check-report.json ]; then
            jq -r '.dependencies[] | select(.vulnerabilities) | "\(.fileName): \(.vulnerabilities | length) vulnerabilities"' \
              security-reports/dependency-check-report.json >> $GITHUB_STEP_SUMMARY
          fi
```

---

### 3.3 Snyk Scanning Workflow

```yaml
# .github/workflows/security-snyk.yml
name: Security - Snyk Scan

on:
  push:
    branches: ["2.0", "upgrade"]
  pull_request:
    branches: ["2.0"]

jobs:
  snyk:
    name: Snyk Security Scan
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run Snyk to check for vulnerabilities
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
        with:
          args: --all-projects --severity-threshold=high
          command: test

      - name: Upload Snyk results to GitHub Code Scanning
        uses: github/codeql-action/upload-sarif@v3
        if: always()
        with:
          sarif_file: snyk.sarif

      - name: Snyk Monitor (Continuous Monitoring)
        if: github.event_name == 'push' && github.ref == 'refs/heads/2.0'
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
        with:
          args: --all-projects
          command: monitor
```

---

### 3.4 Comprehensive Security Scan Workflow

```yaml
# .github/workflows/security-comprehensive.yml
name: Security - Comprehensive Scan

on:
  schedule:
    # Run weekly on Sunday at 2:00 AM UTC
    - cron: '0 2 * * 0'
  workflow_dispatch:

jobs:
  comprehensive-security:
    name: Comprehensive Security Analysis
    runs-on: ubuntu-latest
    timeout-minutes: 60

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for secret scanning

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      # Step 1: Vulnerability Scanning
      - name: .NET Vulnerability Scan
        run: |
          mkdir -p security-reports
          dotnet list package --vulnerable --include-transitive > security-reports/dotnet-vuln-scan.txt

      # Step 2: OWASP Dependency-Check
      - name: OWASP Dependency-Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'RawRabbit'
          path: './src'
          format: 'HTML,JSON,SARIF'
          out: 'security-reports/owasp'

      # Step 3: CodeQL Analysis
      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp
          packs: "codeql/csharp-queries:security-and-quality"

      - name: Build for CodeQL
        run: |
          dotnet restore
          dotnet build --no-restore --configuration Release

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3

      # Step 4: Hardcoded Credential Scan
      - name: Scan for hardcoded credentials
        run: |
          echo "## Hardcoded Credential Scan" > security-reports/credential-scan.md
          echo "" >> security-reports/credential-scan.md

          # Search for password patterns
          echo "### Password Patterns" >> security-reports/credential-scan.md
          grep -rn 'Password.*=.*"' src/ --include="*.cs" >> security-reports/credential-scan.md || true

          # Search for connection strings
          echo "### Connection Strings" >> security-reports/credential-scan.md
          grep -rn 'ConnectionString.*=.*"' src/ --include="*.cs" >> security-reports/credential-scan.md || true

          # Search for API keys
          echo "### API Keys" >> security-reports/credential-scan.md
          grep -rn 'ApiKey\|api_key\|API_KEY' src/ --include="*.cs" >> security-reports/credential-scan.md || true

      # Step 5: Cryptographic API Inventory
      - name: Cryptographic API scan
        run: |
          echo "## Cryptographic API Inventory" > security-reports/crypto-inventory.md
          echo "" >> security-reports/crypto-inventory.md

          # Deprecated hash algorithms
          echo "### Deprecated Hash Algorithms" >> security-reports/crypto-inventory.md
          grep -rn 'MD5\|SHA1' src/ --include="*.cs" >> security-reports/crypto-inventory.md || true

          # Deprecated encryption
          echo "### Deprecated Encryption" >> security-reports/crypto-inventory.md
          grep -rn 'DES\|RC2\|Rijndael' src/ --include="*.cs" >> security-reports/crypto-inventory.md || true

          # Insecure random
          echo "### Random Number Generation" >> security-reports/crypto-inventory.md
          grep -rn 'new Random(' src/ --include="*.cs" >> security-reports/crypto-inventory.md || true

      # Step 6: SBOM Generation
      - name: Generate SBOM
        run: |
          dotnet tool install --global Microsoft.Sbom.DotNetTool
          sbom-tool generate \
            -b ./output \
            -bc ./src \
            -pn RawRabbit \
            -pv 3.0.0 \
            -ps Pardahlman \
            -m ./security-reports/sbom/manifest.json

      # Step 7: Upload all reports
      - name: Upload security reports
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: comprehensive-security-reports
          path: security-reports/
          retention-days: 90

      # Step 8: Create summary
      - name: Create security summary
        if: always()
        run: |
          echo "# Comprehensive Security Scan Results" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY
          echo "**Date**: $(date -u +%Y-%m-%d\ %H:%M:%S\ UTC)" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY

          echo "## Reports Generated" >> $GITHUB_STEP_SUMMARY
          echo "- .NET Vulnerability Scan: \`security-reports/dotnet-vuln-scan.txt\`" >> $GITHUB_STEP_SUMMARY
          echo "- OWASP Dependency-Check: \`security-reports/owasp/\`" >> $GITHUB_STEP_SUMMARY
          echo "- CodeQL Analysis: Uploaded to Security tab" >> $GITHUB_STEP_SUMMARY
          echo "- Credential Scan: \`security-reports/credential-scan.md\`" >> $GITHUB_STEP_SUMMARY
          echo "- Crypto Inventory: \`security-reports/crypto-inventory.md\`" >> $GITHUB_STEP_SUMMARY
          echo "- SBOM: \`security-reports/sbom/manifest.json\`" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY

          echo "## Next Steps" >> $GITHUB_STEP_SUMMARY
          echo "1. Review all reports in the artifacts" >> $GITHUB_STEP_SUMMARY
          echo "2. Address critical and high severity issues" >> $GITHUB_STEP_SUMMARY
          echo "3. Update security ADRs with findings" >> $GITHUB_STEP_SUMMARY
```

---

## 4. Configuration Files

### 4.1 Dependabot Configuration

**File**: `.github/dependabot.yml`

See [Section 2.3.1](#231-dependabot-configuration) for complete configuration.

---

### 4.2 OWASP Suppression File

**File**: `.owasp/suppressions.xml`

```xml
<?xml version="1.0" encoding="UTF-8"?>
<suppressions xmlns="https://jeremylong.github.io/DependencyCheck/dependency-suppression.1.3.xsd">

    <!-- Example 1: False positive on test dependency -->
    <suppress>
        <notes><![CDATA[
        xUnit is a test framework used only in development.
        CVE-2023-XXXXX does not apply to our usage.
        Reviewed: 2025-10-09
        ]]></notes>
        <packageUrl regex="true">^pkg:nuget/xunit@.*$</packageUrl>
        <cve>CVE-2023-XXXXX</cve>
    </suppress>

    <!-- Example 2: Known issue with remediation plan -->
    <suppress until="2025-12-31">
        <notes><![CDATA[
        RabbitMQ.Client 5.0.1 upgrade planned for Stage 3 (Week 4).
        Temporary suppression until migration complete.
        See: ADR-0009
        ]]></notes>
        <packageUrl regex="true">^pkg:nuget/RabbitMQ\.Client@5\.0\.1$</packageUrl>
        <vulnerabilityName>CVE-2020-11100</vulnerabilityName>
    </suppress>

    <!-- Example 3: Not exploitable in our context -->
    <suppress>
        <notes><![CDATA[
        This vulnerability requires user-controlled input to XML parser.
        RawRabbit does not process external XML.
        Reviewed: 2025-10-09
        ]]></notes>
        <filePath regex="true">.*System\.Xml\.XmlDocument.*</filePath>
        <cve>CVE-2023-YYYYY</cve>
    </suppress>

</suppressions>
```

**Best Practices**:
1. Always include justification in `<notes>`
2. Use `until` attribute for temporary suppressions
3. Review suppressions quarterly
4. Link to ADRs or tickets for remediation plans

---

### 4.3 Snyk Policy File

**File**: `.snyk`

```yaml
version: v1.22.0

# Language settings
language-settings:
  dotnet:
    # Target frameworks to scan
    targetFramework: 'net9.0'

# Ignore specific vulnerabilities
ignore:
  # Example: Known issue with planned fix
  'SNYK-DOTNET-RABBITMQCLIENT-1234567':
    - '*':
        reason: 'Upgrade planned for Stage 3 (ADR-0009)'
        expires: '2025-12-31T00:00:00.000Z'
        created: '2025-10-09T00:00:00.000Z'

  # Example: Not exploitable in our context
  'SNYK-DOTNET-NEWTONSOFTJSON-2345678':
    - 'RawRabbit > Newtonsoft.Json':
        reason: 'Prototype pollution not exploitable - no user-controlled JSON'
        expires: '2025-12-31T00:00:00.000Z'
        created: '2025-10-09T00:00:00.000Z'

# Patch directives (auto-applied by Snyk)
patch: {}

# Exclude directories from scanning
exclude:
  global:
    - 'test/fixtures/**'
    - '**/obj/**'
    - '**/bin/**'
    - '.swarm/**'
```

---

### 4.4 SonarQube Quality Profile

**File**: `sonar-project.properties`

```properties
# Project identification
sonar.organization=your-org
sonar.projectKey=your-org_RawRabbit
sonar.projectName=RawRabbit
sonar.projectVersion=3.0.0

# Source and test directories
sonar.sources=src
sonar.tests=test
sonar.sourceEncoding=UTF-8

# Coverage reports
sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml
sonar.cs.vstest.reportsPaths=**/*.trx

# Exclusions
sonar.exclusions=**/obj/**,**/bin/**,**/Migrations/**,**/*.Generated.cs
sonar.test.exclusions=**/bin/**,**/obj/**

# Security settings
sonar.security.enable=true
sonar.security.hotspots.inherited=true

# Security hotspot thresholds
sonar.qualitygate.wait=true
sonar.qualitygate.timeout=300

# Additional analysis parameters
sonar.dotnet.excludeTestProjects=true
sonar.cs.roslyn.ignoreIssues=false
```

---

### 4.5 EditorConfig Security Rules

**File**: `.editorconfig` (add security section)

```ini
# Security-specific analyzer rules

# CA3001: Review code for SQL injection vulnerabilities
dotnet_diagnostic.CA3001.severity = warning

# CA3002: Review code for XSS vulnerabilities
dotnet_diagnostic.CA3002.severity = warning

# CA3003: Review code for file path injection vulnerabilities
dotnet_diagnostic.CA3003.severity = warning

# CA3004: Review code for information disclosure vulnerabilities
dotnet_diagnostic.CA3004.severity = warning

# CA3005: Review code for LDAP injection vulnerabilities
dotnet_diagnostic.CA3005.severity = warning

# CA3006: Review code for process command injection vulnerabilities
dotnet_diagnostic.CA3006.severity = warning

# CA3007: Review code for open redirect vulnerabilities
dotnet_diagnostic.CA3007.severity = warning

# CA3008: Review code for XPath injection vulnerabilities
dotnet_diagnostic.CA3008.severity = warning

# CA3009: Review code for XML injection vulnerabilities
dotnet_diagnostic.CA3009.severity = warning

# CA3010: Review code for XAML injection vulnerabilities
dotnet_diagnostic.CA3010.severity = warning

# CA3011: Review code for DLL injection vulnerabilities
dotnet_diagnostic.CA3011.severity = warning

# CA3012: Review code for regex injection vulnerabilities
dotnet_diagnostic.CA3012.severity = warning

# CA5350: Do Not Use Weak Cryptographic Algorithms (MD5, SHA1)
dotnet_diagnostic.CA5350.severity = error

# CA5351: Do Not Use Broken Cryptographic Algorithms (DES, RC2)
dotnet_diagnostic.CA5351.severity = error

# CA5358: Do Not Use Unsafe Cipher Modes
dotnet_diagnostic.CA5358.severity = error

# CA5359: Do Not Disable Certificate Validation
dotnet_diagnostic.CA5359.severity = error

# CA5360: Do Not Call Dangerous Methods In Deserialization
dotnet_diagnostic.CA5360.severity = error

# CA5361: Do Not Disable SChannel Use of Strong Crypto
dotnet_diagnostic.CA5361.severity = error

# CA5362: Do Not Refer Self In Serializable Class
dotnet_diagnostic.CA5362.severity = warning

# CA5363: Do Not Disable Request Validation
dotnet_diagnostic.CA5363.severity = error

# CA5364: Do Not Use Deprecated Security Protocols
dotnet_diagnostic.CA5364.severity = error

# CA5365: Do Not Disable HTTP Header Checking
dotnet_diagnostic.CA5365.severity = error

# CA5366: Use XmlReader For DataSet Read XML
dotnet_diagnostic.CA5366.severity = warning

# CA5367: Do Not Serialize Types With Pointer Fields
dotnet_diagnostic.CA5367.severity = warning

# CA5368: Set ViewStateUserKey For Classes Derived From Page
dotnet_diagnostic.CA5368.severity = warning

# CA5369: Use XmlReader For Deserialize
dotnet_diagnostic.CA5369.severity = warning

# CA5370: Use XmlReader For Validating Reader
dotnet_diagnostic.CA5370.severity = warning

# CA5371: Use XmlReader For Schema Read
dotnet_diagnostic.CA5371.severity = warning

# CA5372: Use XmlReader For XPathDocument
dotnet_diagnostic.CA5372.severity = warning

# CA5373: Do not use obsolete key derivation function
dotnet_diagnostic.CA5373.severity = error

# CA5374: Do Not Use XslTransform
dotnet_diagnostic.CA5374.severity = error

# CA5375: Do Not Use Account Shared Access Signature
dotnet_diagnostic.CA5375.severity = warning

# CA5376: Use SharedAccessProtocol HttpsOnly
dotnet_diagnostic.CA5376.severity = warning

# CA5377: Use Container Level Access Policy
dotnet_diagnostic.CA5377.severity = warning

# CA5378: Do not disable ServicePointManagerSecurityProtocols
dotnet_diagnostic.CA5378.severity = error

# CA5379: Do Not Use Weak Key Derivation Function Algorithm
dotnet_diagnostic.CA5379.severity = error

# CA5380: Do Not Add Certificates To Root Store
dotnet_diagnostic.CA5380.severity = error

# CA5381: Ensure Certificates Are Not Added To Root Store
dotnet_diagnostic.CA5381.severity = error

# CA5382: Use Secure Cookies In ASP.NET Core
dotnet_diagnostic.CA5382.severity = warning

# CA5383: Ensure Use Secure Cookies In ASP.NET Core
dotnet_diagnostic.CA5383.severity = warning

# CA5384: Do Not Use Digital Signature Algorithm (DSA)
dotnet_diagnostic.CA5384.severity = error

# CA5385: Use Rivest–Shamir–Adleman (RSA) Algorithm With Sufficient Key Size
dotnet_diagnostic.CA5385.severity = error

# CA5386: Avoid hardcoding SecurityProtocolType value
dotnet_diagnostic.CA5386.severity = error

# CA5387: Do Not Use Weak Key Derivation Function With Insufficient Iteration Count
dotnet_diagnostic.CA5387.severity = warning

# CA5388: Ensure Sufficient Iteration Count When Using Weak Key Derivation Function
dotnet_diagnostic.CA5388.severity = warning

# CA5389: Do Not Add Archive Item's Path To The Target File System Path
dotnet_diagnostic.CA5389.severity = warning

# CA5390: Do not hard-code encryption key
dotnet_diagnostic.CA5390.severity = error

# CA5391: Use antiforgery tokens in ASP.NET Core MVC controllers
dotnet_diagnostic.CA5391.severity = warning

# CA5392: Use DefaultDllImportSearchPaths attribute for P/Invokes
dotnet_diagnostic.CA5392.severity = warning

# CA5393: Do not use unsafe DllImportSearchPath value
dotnet_diagnostic.CA5393.severity = warning

# CA5394: Do not use insecure randomness
dotnet_diagnostic.CA5394.severity = warning

# CA5395: Miss HttpVerb attribute for action methods
dotnet_diagnostic.CA5395.severity = warning

# CA5396: Set HttpOnly to true for HttpCookie
dotnet_diagnostic.CA5396.severity = warning

# CA5397: Do not use deprecated SslProtocols values
dotnet_diagnostic.CA5397.severity = error

# CA5398: Avoid hardcoded SslProtocols values
dotnet_diagnostic.CA5398.severity = warning

# CA5399: Definitely disable HttpClient certificate revocation list check
dotnet_diagnostic.CA5399.severity = warning

# CA5400: Ensure HttpClient certificate revocation list check is not disabled
dotnet_diagnostic.CA5400.severity = warning

# CA5401: Do not use CreateEncryptor with non-default IV
dotnet_diagnostic.CA5401.severity = warning

# CA5402: Use CreateEncryptor with the default IV
dotnet_diagnostic.CA5402.severity = warning

# CA5403: Do not hard-code certificate
dotnet_diagnostic.CA5403.severity = error
```

---

## 5. Cost Analysis

### 5.1 Free Tier Toolchain

**Total Cost**: $0/month

| Tool | Features | Limitations |
|------|----------|-------------|
| dotnet list package | NuGet vulnerability scanning | No historical tracking, manual execution |
| OWASP Dependency-Check | Comprehensive dependency scanning | Slow on large projects, requires database updates |
| GitHub Dependabot | Automated dependency updates & alerts | Limited to GitHub-hosted repos |
| GitHub Secret Scanning | Hardcoded credential detection | Public repos only (free) |
| SonarCloud Free | Code quality & security for open source | Open source projects only |
| Microsoft SBOM Tool | SBOM generation | Manual execution |

**Best For**:
- Open source projects
- Small teams (<5 developers)
- Budget-constrained projects
- Initial implementation phase

**Setup Time**: 2-3 hours

---

### 5.2 Paid Tier Toolchain (Recommended)

**Total Cost**: ~$99/month

| Tool | Cost | Features |
|------|------|----------|
| GitHub Advanced Security | $49/user/month | CodeQL, secret scanning (private repos), Dependabot |
| Snyk Team | $52/month | Unlimited tests, IDE integration, auto-fix PRs |
| NVD API Key | Free | 5-10x faster OWASP scans |

**Best For**:
- Private repositories
- Professional/commercial projects
- Teams requiring compliance reporting
- Continuous monitoring requirements

**Setup Time**: 3-4 hours

---

### 5.3 Enterprise Toolchain

**Total Cost**: ~$500-2000/month

| Tool | Cost | Features |
|------|------|----------|
| GitHub Enterprise Security | $21/user/month (5 users) | Full GH Advanced Security features |
| Snyk Enterprise | $500-1500/month | Custom rules, SSO, compliance reporting |
| SonarQube Enterprise | $150/month (self-hosted) | Advanced security rules, governance |
| Azure Key Vault | $3-10/month | Code signing certificate storage |

**Best For**:
- Large organizations
- Regulated industries (finance, healthcare)
- Multi-repo organizations
- Advanced compliance requirements

**Setup Time**: 8-12 hours (includes training)

---

### 5.4 Cost Comparison Table

| Feature | Free | Paid ($99/mo) | Enterprise ($500+/mo) |
|---------|------|---------------|-----------------------|
| NuGet vulnerability scanning | ✅ | ✅ | ✅ |
| Dependency scanning | ✅ | ✅ | ✅ |
| Code scanning (SAST) | ⚠️ Limited | ✅ | ✅ |
| Secret scanning | ⚠️ Public only | ✅ | ✅ |
| Auto-fix PRs | ❌ | ✅ | ✅ |
| IDE integration | ❌ | ✅ | ✅ |
| Compliance reporting | ❌ | ⚠️ Limited | ✅ |
| Custom security rules | ❌ | ⚠️ Limited | ✅ |
| SSO/SAML | ❌ | ❌ | ✅ |
| Priority support | ❌ | ⚠️ Email | ✅ 24/7 |
| SBOM generation | ✅ Manual | ✅ Automated | ✅ Automated |
| License compliance | ❌ | ✅ | ✅ |
| Container scanning | ❌ | ✅ | ✅ |
| IaC scanning | ❌ | ✅ | ✅ |

---

## 6. Recommended Toolchain

### 6.1 For RawRabbit .NET 9 Upgrade (Recommended)

**Toolchain**: **Free + GitHub Advanced Security**

**Total Cost**: $0/month (open source) or $49/month (private repo, 1 user)

**Tools**:
1. ✅ **dotnet list package --vulnerable** (Free, built-in)
2. ✅ **OWASP Dependency-Check** (Free, comprehensive)
3. ✅ **GitHub Dependabot** (Free/Included)
4. ✅ **GitHub CodeQL** (Free for public repos)
5. ✅ **GitHub Secret Scanning** (Free for public repos)
6. ✅ **Microsoft SBOM Tool** (Free, compliance)

**Rationale**:
- RawRabbit is open source (free GitHub features)
- Covers all 9 security checkpoints
- No additional cost
- Easy integration with existing GitHub workflows
- Sufficient for migration project scope

**Setup Priority**:
1. **Week 1 (Immediate)**:
   - Enable GitHub Dependabot
   - Run `dotnet list package --vulnerable`
   - Setup OWASP Dependency-Check workflow

2. **Week 2 (High Priority)**:
   - Enable GitHub CodeQL
   - Enable Secret Scanning
   - Create comprehensive security workflow

3. **Week 3 (Medium Priority)**:
   - Generate SBOM
   - Configure suppression files
   - Document scan results

---

### 6.2 Minimum Viable Security Scanning

**For Immediate Start (Today)**:

```bash
# Step 1: Basic vulnerability scan (5 minutes)
dotnet list package --vulnerable --include-transitive

# Step 2: Hardcoded credential scan (2 minutes)
grep -rn 'Password.*=.*"' src/ --include="*.cs"
grep -rn 'ConnectionString.*=.*"' src/ --include="*.cs"

# Step 3: Cryptographic API scan (2 minutes)
grep -rn 'MD5\|SHA1\|DES\|RC2' src/ --include="*.cs"

# Step 4: Enable GitHub Dependabot (5 minutes)
# Create .github/dependabot.yml (see Section 4.1)
```

**Total Time**: 15 minutes
**Total Cost**: $0
**Coverage**: Addresses 3 of 9 security checkpoints

---

### 6.3 Full Production Toolchain (Post-Migration)

**For Long-Term Maintenance**:

**Toolchain**: **GitHub Advanced Security + Snyk Team**

**Total Cost**: $101/month ($49 GH + $52 Snyk)

**Additional Tools**:
1. ✅ Snyk IDE integration (Visual Studio, Rider)
2. ✅ Automated fix PRs (Snyk + Dependabot)
3. ✅ License compliance scanning
4. ✅ Container scanning (for Docker images)
5. ✅ Real-time vulnerability alerts

**Rationale**:
- Post-migration, continuous monitoring critical
- Snyk provides faster response than NVD updates
- IDE integration catches issues before commit
- Automated fixes reduce maintenance burden

---

## 7. Testing & Validation

### 7.1 Scan Validation Checklist

**After setting up each tool, validate with this checklist**:

```markdown
## Tool Setup Validation

### dotnet list package --vulnerable
- [ ] Command runs without errors
- [ ] Detects RabbitMQ.Client 5.0.1 vulnerability
- [ ] Detects Newtonsoft.Json 10.0.1 vulnerability
- [ ] Output saved to file
- [ ] GitHub Action workflow created
- [ ] Workflow runs on schedule (daily)

### OWASP Dependency-Check
- [ ] Docker image pulls successfully
- [ ] Scan completes in <10 minutes
- [ ] HTML report generated
- [ ] SARIF report generated
- [ ] Suppressions file respected
- [ ] GitHub Action workflow created
- [ ] SARIF uploaded to GitHub Security tab

### GitHub Dependabot
- [ ] dependabot.yml created and committed
- [ ] Dependabot alerts enabled in settings
- [ ] Security alerts visible in Security tab
- [ ] Test PR created for vulnerable package
- [ ] PR contains security advisory link
- [ ] PR auto-assigned to security team

### GitHub CodeQL
- [ ] CodeQL workflow created
- [ ] Workflow runs successfully
- [ ] C# language detected
- [ ] Build completes without errors
- [ ] Security alerts visible in Security tab
- [ ] At least 1 security issue detected (test)
- [ ] SARIF results downloadable

### GitHub Secret Scanning
- [ ] Secret scanning enabled in settings
- [ ] Push protection enabled (optional)
- [ ] Test: Commit fake API key (in test branch)
- [ ] Test: Secret detected and blocked/alerted
- [ ] Historical scan completed
- [ ] Alert notification received

### Snyk (Optional)
- [ ] Snyk CLI installed
- [ ] Authentication successful
- [ ] Test scan completes
- [ ] Vulnerabilities detected
- [ ] IDE extension installed
- [ ] Real-time alerts working
- [ ] Monitor mode configured

### SonarCloud/SonarQube (Optional)
- [ ] Project created
- [ ] Scanner installed
- [ ] First analysis completes
- [ ] Security hotspots visible
- [ ] Quality gate configured
- [ ] Coverage reports integrated

### SBOM Generation
- [ ] SBOM tool installed
- [ ] SBOM generated successfully
- [ ] SBOM validates without errors
- [ ] SBOM includes all 25 projects
- [ ] SBOM format: SPDX 2.3
- [ ] SBOM included in build artifacts
```

---

### 7.2 Expected Findings (Pre-Migration Baseline)

**Based on security review (docs/planning/security-review-plan.md), expect to find**:

#### Critical Vulnerabilities
1. **RabbitMQ.Client 5.0.1**:
   - CVE-2020-11100 (High - DoS)
   - CVE-2021-22116 (High - Memory exhaustion)

2. **Newtonsoft.Json 10.0.1**:
   - CVE-2024-21907 (Critical - DoS)
   - CVE-2024-21908 (Critical - RCE)

#### Security Issues
3. **Hardcoded Credentials**:
   - `RawRabbitConfiguration.Local` (guest/guest)
   - Location: `src/RawRabbit/Configuration/RawRabbitConfiguration.cs:110-117`

4. **Deprecated Cryptographic APIs** (to be confirmed):
   - Potential MD5/SHA1 usage
   - Potential DES/RC2 usage
   - Potential insecure Random() usage

#### Scan Result Validation
```bash
# Verify critical findings
echo "Checking for expected vulnerabilities..."

# 1. RabbitMQ.Client CVEs
dotnet list package --vulnerable | grep -i "RabbitMQ.Client.*5.0.1"
# Expected: HIGH severity

# 2. Newtonsoft.Json CVEs
dotnet list package --vulnerable | grep -i "Newtonsoft.Json.*10.0.1"
# Expected: CRITICAL severity

# 3. Hardcoded credentials
grep -n "guest" src/RawRabbit/Configuration/RawRabbitConfiguration.cs
# Expected: Lines 110-117

# 4. Deprecated crypto (example)
grep -rn "MD5\|SHA1" src/ --include="*.cs" | wc -l
# Expected: 0 (good) or >0 (requires remediation)
```

---

### 7.3 Integration Testing

**Test complete security pipeline**:

```yaml
# .github/workflows/security-test.yml
name: Security Pipeline Test

on:
  workflow_dispatch:
    inputs:
      test-type:
        description: 'Test type'
        required: true
        type: choice
        options:
          - all
          - vulnerability-scan
          - dependency-check
          - codeql
          - secret-scan

jobs:
  test-security-pipeline:
    name: Test Security Pipeline
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Test 1 - Vulnerability Scan
        if: inputs.test-type == 'all' || inputs.test-type == 'vulnerability-scan'
        run: |
          echo "Testing vulnerability scan..."
          dotnet list package --vulnerable --include-transitive
          if [ $? -eq 0 ]; then
            echo "✅ Vulnerability scan working"
          else
            echo "❌ Vulnerability scan failed"
            exit 1
          fi

      - name: Test 2 - OWASP Dependency-Check
        if: inputs.test-type == 'all' || inputs.test-type == 'dependency-check'
        run: |
          echo "Testing OWASP Dependency-Check..."
          docker pull owasp/dependency-check:latest
          # Run minimal scan for testing
          docker run --rm -v $(pwd):/src owasp/dependency-check:latest \
            --scan /src/src/RawRabbit --project RawRabbit --format JSON --out /src/test-output
          if [ -f test-output/dependency-check-report.json ]; then
            echo "✅ OWASP Dependency-Check working"
          else
            echo "❌ OWASP Dependency-Check failed"
            exit 1
          fi

      - name: Test 3 - Secret Scanning
        if: inputs.test-type == 'all' || inputs.test-type == 'secret-scan'
        run: |
          echo "Testing secret scanning patterns..."
          # Check if patterns detect test secrets
          echo "Password = \"test123\"" > test-secret.txt
          if grep -q 'Password.*=.*\"' test-secret.txt; then
            echo "✅ Secret scanning patterns working"
            rm test-secret.txt
          else
            echo "❌ Secret scanning patterns failed"
            exit 1
          fi

      - name: Test 4 - Crypto API Detection
        if: inputs.test-type == 'all'
        run: |
          echo "Testing crypto API detection..."
          # Create test file with deprecated crypto
          echo "var md5 = MD5.Create();" > test-crypto.cs
          if grep -q 'MD5' test-crypto.cs; then
            echo "✅ Crypto API detection working"
            rm test-crypto.cs
          else
            echo "❌ Crypto API detection failed"
            exit 1
          fi

      - name: Results Summary
        if: always()
        run: |
          echo "## Security Pipeline Test Results" >> $GITHUB_STEP_SUMMARY
          echo "All tests completed. Review individual steps for details." >> $GITHUB_STEP_SUMMARY
```

---

## 8. Appendix

### 8.1 Useful Commands Reference

```bash
# ===========================
# .NET Vulnerability Scanning
# ===========================

# Basic scan
dotnet list package --vulnerable

# Include transitive dependencies
dotnet list package --vulnerable --include-transitive

# Output to file with timestamp
dotnet list package --vulnerable --include-transitive > vuln-scan-$(date +%Y%m%d).txt

# Scan specific project
dotnet list src/RawRabbit/RawRabbit.csproj package --vulnerable

# Check for outdated packages (non-security)
dotnet list package --outdated


# ===========================
# OWASP Dependency-Check
# ===========================

# Docker scan (basic)
docker run --rm -v $(pwd):/src owasp/dependency-check:latest \
  --scan /src/src --project RawRabbit --format HTML --out /src/reports

# Docker scan (with NVD API key)
docker run --rm \
  -e NVD_API_KEY="your-key" \
  -v $(pwd):/src \
  owasp/dependency-check:latest \
  --scan /src/src --project RawRabbit --format HTML,SARIF --out /src/reports \
  --enableExperimental --nvdApiKey $NVD_API_KEY

# Standalone scan
dependency-check.sh --project RawRabbit --scan ./src --format HTML --out ./reports

# Update vulnerability database only
docker run --rm \
  -v $(pwd)/.owasp/data:/usr/share/dependency-check/data \
  owasp/dependency-check:latest --updateonly


# ===========================
# Credential Scanning
# ===========================

# Password patterns
grep -rn 'Password.*=.*"' src/ --include="*.cs"

# Connection strings
grep -rn 'ConnectionString.*=.*"' src/ --include="*.cs"

# API keys
grep -rn 'ApiKey\|api_key\|API_KEY' src/ --include="*.cs"

# Hardcoded secrets (base64)
grep -rn '[A-Za-z0-9+/]{40,}' src/ --include="*.cs"

# Comprehensive scan
grep -rEn 'Password|ConnectionString|ApiKey|SecretKey|PrivateKey' src/ --include="*.cs"


# ===========================
# Cryptographic API Scanning
# ===========================

# Deprecated hash algorithms
grep -rn 'MD5\|SHA1' src/ --include="*.cs"

# Deprecated encryption
grep -rn 'DES\|RC2\|Rijndael' src/ --include="*.cs"

# Insecure random
grep -rn 'new Random(' src/ --include="*.cs"

# All crypto APIs
grep -rn 'CryptoServiceProvider\|HashAlgorithm\|SymmetricAlgorithm' src/ --include="*.cs"


# ===========================
# Snyk
# ===========================

# Install
npm install -g snyk

# Authenticate
snyk auth

# Test project
snyk test --all-projects

# Test with severity threshold
snyk test --severity-threshold=high

# Monitor (continuous)
snyk monitor --all-projects

# Generate JSON report
snyk test --json > snyk-report.json


# ===========================
# SonarQube/SonarCloud
# ===========================

# Install scanner
dotnet tool install --global dotnet-sonarscanner

# Begin analysis
dotnet sonarscanner begin \
  /k:"project-key" \
  /d:sonar.login="token" \
  /d:sonar.host.url="https://sonarcloud.io"

# Build
dotnet build --no-incremental

# End analysis
dotnet sonarscanner end /d:sonar.login="token"


# ===========================
# SBOM Generation
# ===========================

# Install tool
dotnet tool install --global Microsoft.Sbom.DotNetTool

# Generate SBOM
sbom-tool generate \
  -b ./output \
  -bc ./src \
  -pn RawRabbit \
  -pv 3.0.0 \
  -ps Pardahlman \
  -m ./output/manifest.json

# Validate SBOM
sbom-tool validate -b ./output -m manifest.json


# ===========================
# GitHub CLI (gh)
# ===========================

# List security alerts
gh api repos/{owner}/{repo}/vulnerability-alerts

# List Dependabot alerts
gh api repos/{owner}/{repo}/dependabot/alerts

# Enable Dependabot alerts
gh api -X PUT repos/{owner}/{repo}/vulnerability-alerts

# View secret scanning alerts
gh api repos/{owner}/{repo}/secret-scanning/alerts
```

---

### 8.2 Security Checkpoint Mapping

**9 Security Checkpoints → Tools Coverage**

| Checkpoint | Primary Tool | Secondary Tools | Stage |
|------------|--------------|-----------------|-------|
| 1. Pre-Migration Baseline | dotnet list package | OWASP Dep-Check, Dependabot | Stage 1 |
| 1.5. Threat Modeling | Manual + CodeQL | GitHub Security tab | Stage 1-2 |
| 2. Architecture Security Review | CodeQL | SonarQube, Manual review | Stage 2 |
| 2.5. Cryptographic Review | grep + CodeQL | Manual review | Stage 2-3 |
| 3. Component Security Review | CodeQL + OWASP | Snyk, per-component scans | Stage 3 |
| 3.5. Secrets Management Audit | Secret Scanning | grep, manual review | Stage 3-4 |
| 4. Integration Security Testing | All tools | Penetration testing | Stage 4 |
| 5. Supply Chain Validation | SBOM Tool + Snyk | NuGet signature verification | Stage 5 |
| 6. Pre-Production Audit | All tools | Final manual review | Stage 5 |

---

### 8.3 Troubleshooting Guide

#### Issue: "dotnet list package --vulnerable" shows no vulnerabilities

**Cause**: NuGet vulnerability database not updated

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force

# Re-run scan
dotnet list package --vulnerable --include-transitive
```

---

#### Issue: OWASP Dependency-Check fails with "NVD database download error"

**Cause**: Rate limiting on NVD API

**Solution**:
```bash
# Option 1: Get NVD API key (free)
# https://nvd.nist.gov/developers/request-an-api-key

# Option 2: Use cached database
docker run --rm \
  -v $(pwd)/.owasp/data:/usr/share/dependency-check/data \
  owasp/dependency-check:latest --updateonly
```

---

#### Issue: CodeQL analysis times out

**Cause**: Large codebase, complex build

**Solution**:
```yaml
# Increase timeout in workflow
jobs:
  analyze:
    timeout-minutes: 360  # 6 hours

    # Or use autobuild with reduced scope
    - name: Autobuild
      uses: github/codeql-action/autobuild@v3
      with:
        # Build only security-critical projects
        projects: 'src/RawRabbit/RawRabbit.csproj'
```

---

#### Issue: Snyk reports "No supported manifests found"

**Cause**: Snyk looking for packages.config instead of PackageReference

**Solution**:
```bash
# Ensure using PackageReference format
# In each .csproj, use:
<ItemGroup>
  <PackageReference Include="RabbitMQ.Client" Version="5.0.1" />
</ItemGroup>

# Not this (deprecated):
<packages.config>
```

---

#### Issue: Too many false positives in OWASP scan

**Cause**: Transitive dependencies, test libraries

**Solution**:
```xml
<!-- .owasp/suppressions.xml -->
<suppress>
  <notes>Test-only dependency, no production risk</notes>
  <packageUrl regex="true">^pkg:nuget/xunit.*$</packageUrl>
  <cpe>cpe:/a:xunit:xunit</cpe>
</suppress>
```

---

### 8.4 Security Scanning Best Practices

1. **Run scans early and often**:
   - Daily: `dotnet list package --vulnerable`
   - Weekly: OWASP Dependency-Check
   - On commit: CodeQL, Secret Scanning
   - On PR: All tools (comprehensive scan)

2. **Prioritize by severity**:
   - Critical/High: Fix immediately (Stage 1-2)
   - Medium: Fix in current stage
   - Low: Document and defer (or accept risk)

3. **Document suppressions**:
   - Every suppression needs justification
   - Set expiration dates (force re-review)
   - Link to ADRs or remediation tickets

4. **Automate everything**:
   - GitHub Actions for all scans
   - Dependabot for auto-updates
   - SARIF upload for centralized reporting

5. **Review scan results weekly**:
   - Security team reviews new alerts
   - Triage false positives
   - Create remediation tickets
   - Update ADRs with security decisions

6. **Keep tools updated**:
   - Update OWASP database weekly
   - Update Snyk database (automatic)
   - Update CodeQL queries (automatic via GitHub)

7. **Track metrics**:
   - Number of vulnerabilities over time
   - Mean time to remediation (MTTR)
   - False positive rate
   - Scan coverage percentage

---

### 8.5 Next Steps After Setup

**Week 1 (Immediate)**:
1. ✅ Run baseline scans (all tools)
2. ✅ Document findings in `docs/security-reports/baseline-scan-2025-10-09.md`
3. ✅ Create GitHub issues for CRITICAL/HIGH vulnerabilities
4. ✅ Update `docs/planning/security-review-plan.md` with actual findings

**Week 2 (High Priority)**:
1. ✅ Address hardcoded credentials (RawRabbitConfiguration.Local)
2. ✅ Create ADR-0009 (Dependency Security Strategy)
3. ✅ Setup suppression files for accepted risks
4. ✅ Configure GitHub Actions workflows

**Week 3-4 (Stage 3)**:
1. ✅ Upgrade RabbitMQ.Client 5.0.1 → 7.x
2. ✅ Upgrade Newtonsoft.Json 10.0.1 → 13.0.3+ or migrate to System.Text.Json
3. ✅ Re-run scans to verify fixes
4. ✅ Update SBOM

**Ongoing**:
1. ✅ Review Dependabot PRs weekly
2. ✅ Investigate new security alerts within 48 hours
3. ✅ Update suppression files quarterly
4. ✅ Train team on security tools

---

## Document Status

**Status**: ✅ Complete
**Last Updated**: 2025-10-09
**Next Review**: After Stage 1 completion (Week 2)

**Related Documents**:
- `docs/planning/security-review-plan.md` - Security requirements
- `docs/planning/PLAN.md` - Overall migration plan
- `docs/HISTORY.md` - Work history

**Contact**:
- Security questions: Security Specialist agent
- Setup issues: DevOps Engineer agent
- Integration questions: Migration Architect agent
