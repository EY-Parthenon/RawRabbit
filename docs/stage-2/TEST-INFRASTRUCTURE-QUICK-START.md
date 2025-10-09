# Test Infrastructure Quick Start Guide

**Stage 2.3 Deliverable** | **Date**: 2025-10-09 | **Author**: QA Engineer

This quick start guide provides immediate instructions for using the RawRabbit test infrastructure created in Stage 2.3.

---

## Prerequisites

- Docker and Docker Compose installed
- .NET 9 SDK installed
- Bash shell (Linux/macOS/WSL on Windows)

---

## Quick Start: Run Tests Locally

### 1. Start Default Test Environment

```bash
# Start single RabbitMQ instance (most common)
bash scripts/start-test-environment.sh

# Or using Docker Compose directly
docker-compose up -d rabbitmq
```

**Access**:
- AMQP: `localhost:5672`
- Management UI: `http://localhost:15672` (guest/guest)

### 2. Run Unit Tests

```bash
# Run all unit tests
dotnet test --filter "Category!=Integration"

# Run with coverage
dotnet test --filter "Category!=Integration" \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

### 3. Run Integration Tests

```bash
# Ensure RabbitMQ is running
bash scripts/start-test-environment.sh

# Run integration tests
dotnet test --filter "Category=Integration"
```

### 4. Stop Test Environment

```bash
# Stop and remove containers
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

---

## Advanced Scenarios

### SSL/TLS Testing

```bash
# Generate certificates (first time only)
bash test/certificates/generate-test-certs.sh

# Start SSL-enabled RabbitMQ
bash scripts/start-test-environment.sh ssl

# Run SSL integration tests
dotnet test --filter "Category=SSL"
```

**Access**:
- AMQPS: `localhost:5671`
- Management UI: `https://localhost:15671` (guest/guest)

### RabbitMQ Version Compatibility Testing

```bash
# Start both RabbitMQ 3.11 and 3.12
bash scripts/start-test-environment.sh compatibility

# Run tests against RabbitMQ 3.11
export RABBITMQ_PORT=5673
dotnet test --filter "Category=Integration"

# Run tests against RabbitMQ 3.12
export RABBITMQ_PORT=5672
dotnet test --filter "Category=Integration"
```

**Access**:
- RabbitMQ 3.12: `localhost:5672` → `http://localhost:15672`
- RabbitMQ 3.11: `localhost:5673` → `http://localhost:15673`

### Cluster Testing (High Availability)

```bash
# Start 3-node RabbitMQ cluster
bash scripts/start-test-environment.sh cluster

# Run cluster tests
dotnet test --filter "Category=Cluster"

# View cluster status
docker exec rawrabbit-cluster-node1 rabbitmqctl cluster_status
```

**Access**:
- Node 1: `localhost:5674` → `http://localhost:15674`
- Node 2: `localhost:5675` → `http://localhost:15675`
- Node 3: `localhost:5676` → `http://localhost:15676`

### Performance Benchmarks

```bash
# Start RabbitMQ
bash scripts/start-test-environment.sh

# Run benchmarks
cd benchmark/RawRabbit.Benchmarks
dotnet run -c Release --framework net9.0

# Compare across frameworks
dotnet run -c Release --runtimes net6.0 net8.0 net9.0
```

### All Environments (Comprehensive Testing)

```bash
# Start all test environments
bash scripts/start-test-environment.sh all

# Run full test suite
dotnet test --configuration Release
```

---

## Test Categories

Organize tests using xUnit traits:

```csharp
// Unit test (no external dependencies)
[Fact]
public void MyUnitTest() { }

// Integration test (requires RabbitMQ)
[Fact]
[Trait("Category", "Integration")]
public async Task MyIntegrationTest() { }

// SSL test (requires SSL-enabled RabbitMQ)
[Fact]
[Trait("Category", "SSL")]
public async Task MySSLTest() { }

// Cluster test (requires clustered RabbitMQ)
[Fact]
[Trait("Category", "Cluster")]
public async Task MyClusterTest() { }

// Performance benchmark
[Benchmark]
public async Task MyBenchmark() { }
```

---

## Coverage Reports

### Generate HTML Coverage Report

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Install ReportGenerator (first time only)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:"Html;Badges"

# Open report
open ./coverage/report/index.html  # macOS
xdg-open ./coverage/report/index.html  # Linux
start ./coverage/report/index.html  # Windows
```

### Check Coverage Thresholds

```bash
# Extract coverage percentage
COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' ./coverage/report/Cobertura.xml)
COVERAGE_PCT=$(echo "$COVERAGE * 100" | bc)

echo "Coverage: $COVERAGE_PCT%"

# Verify meets threshold (75%)
if (( $(echo "$COVERAGE_PCT < 75" | bc -l) )); then
  echo "❌ Coverage $COVERAGE_PCT% is below 75% threshold"
else
  echo "✅ Coverage $COVERAGE_PCT% meets 75% threshold"
fi
```

---

## Troubleshooting

### RabbitMQ Won't Start

**Problem**: Container exits immediately

```bash
# Check logs
docker logs rawrabbit-test-rabbitmq

# Common issues:
# 1. Port already in use
netstat -an | grep 5672  # Check if port is occupied
docker-compose down      # Stop existing containers

# 2. Corrupted data volume
docker-compose down -v   # Remove volumes
docker-compose up -d     # Restart fresh
```

### Tests Can't Connect to RabbitMQ

**Problem**: Connection refused errors

```bash
# Wait for RabbitMQ to be ready
docker exec rawrabbit-test-rabbitmq rabbitmq-diagnostics -q ping

# If not ready, wait and retry (takes ~10 seconds)
for i in {1..30}; do
  if docker exec rawrabbit-test-rabbitmq rabbitmq-diagnostics -q ping; then
    echo "RabbitMQ ready!"
    break
  fi
  sleep 1
done
```

### SSL Certificate Errors

**Problem**: Certificate validation failed

```bash
# Regenerate certificates
cd test/certificates
bash generate-test-certs.sh

# Verify certificates
openssl verify -CAfile ca-cert.pem server-cert.pem
openssl verify -CAfile ca-cert.pem client-cert.pem

# Restart SSL container
docker-compose down
docker-compose --profile ssl up -d rabbitmq-ssl
```

### Test Discovery Issues

**Problem**: Tests not found by `dotnet test`

```bash
# Build first
dotnet build

# Then run tests
dotnet test --no-build

# Verbose output for debugging
dotnet test --verbosity detailed
```

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      rabbitmq:
        image: rabbitmq:3.12-management
        ports:
          - 5672:5672
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
          dotnet-version: 9.0.x

      - name: Run tests
        run: dotnet test --configuration Release
```

### Local CI Simulation

```bash
# Simulate CI environment locally
docker-compose up -d rabbitmq
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
docker-compose down
```

---

## Quick Reference

### Common Commands

| Task | Command |
|------|---------|
| Start default environment | `bash scripts/start-test-environment.sh` |
| Start SSL environment | `bash scripts/start-test-environment.sh ssl` |
| Start cluster | `bash scripts/start-test-environment.sh cluster` |
| Generate SSL certs | `bash test/certificates/generate-test-certs.sh` |
| Run unit tests | `dotnet test --filter "Category!=Integration"` |
| Run integration tests | `dotnet test --filter "Category=Integration"` |
| Run with coverage | `dotnet test --collect:"XPlat Code Coverage"` |
| Stop environment | `docker-compose down` |
| Clean volumes | `docker-compose down -v` |

### Port Mapping

| Service | AMQP Port | Management Port | Profile |
|---------|-----------|-----------------|---------|
| RabbitMQ 3.12 (default) | 5672 | 15672 | default |
| RabbitMQ 3.11 (LTS) | 5673 | 15673 | compatibility |
| RabbitMQ SSL | 5671 | 15671 | ssl |
| Cluster Node 1 | 5674 | 15674 | cluster |
| Cluster Node 2 | 5675 | 15675 | cluster |
| Cluster Node 3 | 5676 | 15676 | cluster |

### Coverage Targets

| Component | Target | Priority |
|-----------|--------|----------|
| Overall | 75%+ | REQUIRED |
| RawRabbit (Core) | 80%+ | CRITICAL |
| Operations.* | 70%+ | HIGH |
| Enrichers.* | 60%+ | MEDIUM |
| DependencyInjection.* | 50%+ | MEDIUM |

### Performance Thresholds

| Metric | BLOCKER | WARNING |
|--------|---------|---------|
| Mean execution time | +20% | - |
| P95 latency | +25% | - |
| Throughput | -15% | - |
| Memory allocations | - | +30% |
| Gen2 collections | - | +50% |

---

## Next Steps

1. **Review Test Strategy**: Read `docs/stage-2/test-strategy.md` for comprehensive details
2. **Set Up Environment**: Run `bash scripts/start-test-environment.sh` to start RabbitMQ
3. **Run Tests**: Execute `dotnet test` to verify setup
4. **Check Coverage**: Generate coverage report to establish baseline
5. **Explore Profiles**: Try different profiles (ssl, cluster) for specific testing needs

---

## Support

- **Full Documentation**: `docs/stage-2/test-strategy.md`
- **Docker Config**: `docker-compose.yml`
- **SSL/TLS Guide**: `test/certificates/README.md`
- **Test Standards**: `docs/test/README.md`

---

**Quick Start Guide Complete** ✅

Ready to run tests! For detailed information, see the comprehensive test strategy document.
