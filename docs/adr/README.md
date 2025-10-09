# Architecture Decision Records (ADR)

This directory contains Architecture Decision Records for the RawRabbit .NET 9 upgrade project.

## What is an ADR?

An Architecture Decision Record (ADR) captures an important architectural decision made along with its context and consequences. ADRs help document the "why" behind technical choices, making it easier for future developers to understand the reasoning and to avoid revisiting settled decisions.

## ADR Process

### When to Create an ADR

Create an ADR when you need to make a decision about:
- **Major architectural changes** (e.g., changing core dependencies, migration strategies)
- **Security decisions** (e.g., credential management, encryption choices)
- **Breaking changes** (e.g., deprecated package removal, API changes)
- **Technology selection** (e.g., choosing between serializers, DI containers)
- **Cross-cutting concerns** (e.g., error handling strategy, logging approach)

### ADR Lifecycle

1. **Proposed**: Initial draft, under discussion
2. **Accepted**: Decision approved and ready for implementation
3. **Implemented**: Decision has been put into practice
4. **Deprecated**: Decision is no longer relevant
5. **Superseded**: Replaced by a newer ADR

### How to Write an ADR

1. **Copy the template**: Use `template.md` as your starting point
2. **Use the next available number**: ADR-0001, ADR-0002, etc.
3. **Choose a clear title**: Format: `ADR-XXXX-short-descriptive-title.md`
4. **Fill in all sections**: Don't skip sections - if not applicable, state "N/A"
5. **Be specific**: Include code examples, metrics, and concrete details
6. **Document alternatives**: Always document why alternatives were rejected
7. **Get review**: Have at least one other person review before marking "Accepted"

### ADR Numbering Scheme

```
ADR-XXXX-descriptive-title.md
```

- **XXXX**: Four-digit number (0001, 0002, etc.)
- **descriptive-title**: Lowercase with hyphens
- Examples:
  - `ADR-0001-target-framework-migration-strategy.md`
  - `ADR-0007-dependency-security-strategy.md`
  - `ADR-0008-zeroformatter-deprecation.md`

### Reserved ADR Numbers

The following ADR numbers are reserved for planned decisions:

- **ADR-0001**: Target Framework Migration Strategy (.NET 6/8/9)
- **ADR-0002**: Test Coverage Strategy
- **ADR-0003**: Serialization Strategy
- **ADR-0004**: Dependency Injection Strategy
- **ADR-0005**: Error Handling Strategy
- **ADR-0006**: Logging Strategy
- **ADR-0007**: Dependency Security Strategy
- **ADR-0008**: ZeroFormatter Deprecation
- **ADR-0009**: Ninject Deprecation Strategy
- **ADR-0010**: Security Scanning Toolchain
- **ADR-0011**: RabbitMQ.Client Migration Strategy
- **ADR-0012**: Memory Handling Strategy
- **ADR-0013**: Publisher Confirm Strategy
- **ADR-0014**: Secrets Management Strategy
- **ADR-0015**: TLS Configuration Requirements
- **ADR-0016**: CI/CD Modernization
- **ADR-0017**: Async/Await Modernization
- **ADR-0018**: Test Framework Modernization

## ADR Index

### Stage 1: Foundation & Security Audit

No ADRs created yet.

### Stage 2: Architecture & Design (Week 2-3)

Planned ADRs:
- ADR-0001: Target Framework Migration Strategy
- ADR-0002: Test Coverage Strategy
- ADR-0003: Serialization Strategy
- ADR-0004: Dependency Injection Strategy
- ADR-0005: Error Handling Strategy
- ADR-0006: Logging Strategy
- ADR-0007: Dependency Security Strategy
- ADR-0008: ZeroFormatter Deprecation
- ADR-0009: Ninject Deprecation Strategy
- ADR-0010: Security Scanning Toolchain
- ADR-0011: RabbitMQ.Client Migration Strategy
- ADR-0012: Memory Handling Strategy
- ADR-0013: Publisher Confirm Strategy
- ADR-0014: Secrets Management Strategy
- ADR-0015: TLS Configuration Requirements
- ADR-0016: CI/CD Modernization
- ADR-0017: Async/Await Modernization
- ADR-0018: Test Framework Modernization

### Stage 3+: Implementation ADRs

Additional ADRs will be created as needed during implementation.

## ADR Templates by Category

### Migration ADRs
Use standard template with emphasis on:
- Migration path
- Breaking changes
- Backward compatibility
- Rollback plan

### Security ADRs
Use standard template with emphasis on:
- Security risks
- Threat modeling
- Compliance requirements
- Validation strategy

### Performance ADRs
Use standard template with emphasis on:
- Performance benchmarks
- Resource utilization
- Scalability considerations
- Regression thresholds

## Finding ADRs

### By Status
```bash
# Find all accepted ADRs
grep -l "Status.*Accepted" docs/adr/ADR-*.md

# Find all proposed ADRs
grep -l "Status.*Proposed" docs/adr/ADR-*.md
```

### By Tag
```bash
# Find all security-related ADRs
grep -l "Tags.*security" docs/adr/ADR-*.md

# Find all migration-related ADRs
grep -l "Tags.*migration" docs/adr/ADR-*.md
```

### By Date
```bash
# Find ADRs from October 2025
grep -l "Date: 2025-10-" docs/adr/ADR-*.md
```

## ADR Best Practices

1. **Be Concise**: ADRs should be readable in 5-10 minutes
2. **Be Specific**: Include concrete examples and metrics
3. **Be Honest**: Document trade-offs and risks honestly
4. **Be Forward-Looking**: Consider future implications
5. **Be Timely**: Write ADRs before implementation, not after
6. **Be Collaborative**: Get feedback from multiple stakeholders
7. **Be Complete**: Fill in all sections of the template
8. **Be Consistent**: Follow the numbering and naming conventions

## Related Documentation

- [HISTORY.md](../HISTORY.md) - Work history tracking what was done and why
- [PLAN.md](../planning/PLAN.md) - Overall migration plan
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Documentation guidelines

## References

- [ADR GitHub Organization](https://adr.github.io/)
- [Michael Nygard's ADR post](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- [Architecture Decision Records in Action (ThoughtWorks)](https://www.thoughtworks.com/radar/techniques/lightweight-architecture-decision-records)
