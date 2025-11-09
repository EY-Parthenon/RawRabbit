# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for the RawRabbit 3.0 modernization project.

## Format

ADRs follow the [MADR 3.0.0 format](https://adr.github.io/madr/) (Markdown Architecture Decision Records).

## Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [ADR-001](001-target-framework-selection.md) | Target Framework Selection | Accepted | 2025-11-09 |
| [ADR-002](002-rabbitmq-client-migration-strategy.md) | RabbitMQ.Client Migration Strategy | Accepted | 2025-11-09 |
| [ADR-003](003-zeroformatter-removal.md) | ZeroFormatter Enricher Removal | Accepted | 2025-11-09 |
| [ADR-004](004-dependency-update-strategy.md) | Dependency Update Strategy | Accepted | 2025-11-09 |
| [ADR-005](005-versioning-strategy.md) | Versioning Strategy | Accepted | 2025-11-09 |
| [ADR-006](006-publisher-confirms-implementation.md) | Publisher Confirms Implementation Strategy | Accepted | 2025-11-09 |

## How to Create a New ADR

1. Copy the [MADR template](https://github.com/adr/madr/blob/main/template/adr-template.md)
2. Number it sequentially (e.g., `006-my-decision.md`)
3. Fill in all sections
4. Update this README index
5. Commit and push
