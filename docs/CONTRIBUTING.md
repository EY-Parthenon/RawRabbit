# Contributing to RawRabbit Documentation

This guide explains how to document changes, decisions, and progress during the RawRabbit .NET 9 upgrade project.

## Documentation Philosophy

Good documentation:
1. **Explains WHY, not just WHAT**: Document the reasoning behind decisions
2. **Is Written for Future Readers**: Assume the reader has no context
3. **Is Updated Immediately**: Document as you work, not after
4. **Is Specific**: Include file paths, line numbers, metrics, and examples
5. **Is Honest**: Document failures and trade-offs, not just successes

## Documentation Structure

```
docs/
├── HISTORY.md              # What was done, why, and impact
├── CONTRIBUTING.md         # This file
├── adr/                    # Architecture Decision Records
│   ├── README.md           # ADR process and index
│   ├── template.md         # ADR template
│   └── ADR-XXXX-*.md       # Individual ADRs
├── planning/               # Planning documents (historical)
│   └── ...
├── stage-1/                # Stage-specific documentation
├── test/                   # Test reports
│   ├── unit/               # Unit test reports
│   ├── integration/        # Integration test reports
│   ├── performance/        # Performance benchmarks
│   └── security/           # Security scans
└── pre-work/               # Pre-work task documentation
```

## Documentation Types

### 1. Work History (HISTORY.md)

**Purpose**: Record ALL work completed with context and impact

**When to Update**: IMMEDIATELY after completing any task or making any change

**Format**:
```markdown
## YYYY-MM-DD - [Brief Title]

### What was changed
- [Specific changes made]
- [Files created/modified]
- [Commands executed]

### Why it was changed
[Explain the reasoning, problem being solved, or requirement being met]

### Impact on the codebase
- [Technical impact]
- [Timeline impact]
- [Dependencies affected]

### Rationale
[Deeper explanation of why this approach was chosen]

---
```

**Example**:
```markdown
## 2025-10-09 - Stage 1.4: Documentation Infrastructure Setup

### What was changed
- Created docs/adr/ directory with ADR template
- Created docs/stage-1/ for stage-specific docs
- Created docs/test/ structure for test reports
- Established documentation standards

### Why it was changed
Proper documentation infrastructure ensures traceability throughout migration

### Impact on the codebase
- ADR process established
- Test reporting standardized
- Documentation guidelines available

---
```

**Guidelines**:
- ✅ Write entries immediately after completing work
- ✅ Be specific (include file paths, line numbers, metrics)
- ✅ Include both positive and negative outcomes
- ✅ Link to related documents
- ❌ Don't batch entries (write as you go)
- ❌ Don't use vague language ("improved", "fixed")
- ❌ Don't skip failures or setbacks

### 2. Architecture Decision Records (ADR)

**Purpose**: Document significant architectural decisions with context and consequences

**When to Create**: Before making major architectural, security, or breaking change decisions

**Process**:
1. Copy `docs/adr/template.md`
2. Name: `ADR-XXXX-descriptive-title.md` (use next available number)
3. Status: Start as "Proposed"
4. Fill in ALL sections (if not applicable, write "N/A" with explanation)
5. Get review from at least one other person
6. Update status to "Accepted" after review
7. Update index in `docs/adr/README.md`

**Key Sections**:
- **Context**: What is the situation requiring a decision?
- **Decision**: What did we decide to do?
- **Alternatives Considered**: What else did we evaluate? Why reject it?
- **Consequences**: What are the positive/negative impacts?
- **Migration Impact**: Breaking changes? Migration path?
- **Validation**: How do we know the decision was successful?

**Guidelines**:
- ✅ Write ADRs BEFORE implementing the decision
- ✅ Document alternatives honestly (including why they were rejected)
- ✅ Include concrete examples and code snippets
- ✅ Update status as decision progresses (Proposed → Accepted → Implemented)
- ✅ Link to related ADRs
- ❌ Don't write ADRs after the fact
- ❌ Don't skip the "Alternatives Considered" section
- ❌ Don't leave status as "Proposed" forever

**Reserved ADR Numbers**: See `docs/adr/README.md` for reserved numbers

### 3. Test Reports

**Purpose**: Document test execution results for validation and tracking

**When to Create**: After running tests during any stage

**Location**: `docs/test/[unit|integration|performance|security]/`

**Naming Convention**:
- Unit: `unit-test-YYYY-MM-DD-[description].md`
- Integration: `integration-test-YYYY-MM-DD-[component].md`
- Performance: `performance-YYYY-MM-DD-[framework]-[description].md`
- Security: `security-scan-YYYY-MM-DD-[tool].md`

**Guidelines**:
- ✅ Include summary metrics (pass/fail counts, coverage, duration)
- ✅ Document failed tests with reproduction steps
- ✅ Compare to previous runs (regressions, improvements)
- ✅ Link to GitHub issues for failures
- ✅ Archive reports (don't overwrite previous reports)
- ❌ Don't skip documenting failures
- ❌ Don't include only raw output (add analysis)

See `docs/test/README.md` for detailed report formats.

### 4. Stage-Specific Documentation

**Purpose**: Organize deliverables and documentation for each stage

**Location**: `docs/stage-1/`, `docs/stage-2/`, etc.

**Contents**:
- Stage deliverables
- Stage-specific analysis
- Implementation notes
- Migration guides

**Guidelines**:
- ✅ Create stage directory when starting the stage
- ✅ Move completed work to stage directory
- ✅ Link from HISTORY.md to stage documentation
- ❌ Don't mix stages (keep stage-1 separate from stage-2)

## Documentation Workflow

### Starting a New Task

1. **Check HISTORY.md**: Review recent entries for context
2. **Check ADRs**: See if there are relevant decisions
3. **Check Planning Docs**: Review `docs/planning/PLAN.md` for task requirements

### During Work

1. **Take Notes**: Document what you're doing as you go
2. **Document Findings**: Write down discoveries, issues, solutions
3. **Create ADRs**: If making architectural decisions
4. **Update Tests**: Document test results

### Completing a Task

1. **Update HISTORY.md**: Write entry immediately
2. **Update ADRs**: Update status if related to ADR
3. **Create Test Reports**: If tests were run
4. **Link Documents**: Reference related documents
5. **Commit Documentation**: Commit documentation with code changes

## Writing Style Guidelines

### Be Specific

❌ **Bad**: "Fixed the issue"
✅ **Good**: "Fixed deadlock in TopologyProvider.cs:317 by replacing GetAwaiter().GetResult() with async/await"

❌ **Bad**: "Improved performance"
✅ **Good**: "Reduced message publish latency from 150μs to 95μs (37% improvement) by caching channel instances"

### Be Honest

❌ **Bad**: "Completed all tasks successfully"
✅ **Good**: "Completed 8/10 tasks. Tasks 9-10 blocked by missing RabbitMQ.Client documentation"

❌ **Bad**: "Migration complete"
✅ **Good**: "Migration complete with 2 known issues: ZeroFormatter tests fail (deprecated), Polly middleware needs refactoring"

### Provide Context

❌ **Bad**: "Updated package"
✅ **Good**: "Updated RabbitMQ.Client from 5.0.1 to 7.1.2 to fix CVE-2020-11100 (TLS validation bypass)"

❌ **Bad**: "Created ADR"
✅ **Good**: "Created ADR-0008: ZeroFormatter Deprecation to document removal of unmaintained serializer that blocks .NET 9 compatibility"

### Use Active Voice

❌ **Bad**: "The package was updated"
✅ **Good**: "Updated the package"

❌ **Bad**: "Tests were run"
✅ **Good**: "Ran tests"

### Include Metrics

❌ **Bad**: "Many tests passed"
✅ **Good**: "247/250 tests passed (98.8%)"

❌ **Bad**: "Performance improved"
✅ **Good**: "Publish throughput increased from 1,200 msg/s to 1,850 msg/s (54% improvement)"

## Git Commit Messages

### Format

```
Brief summary (50 characters or less)

More detailed explanation (wrap at 72 characters):
- Bullet points are fine
- Use imperative mood ("Add feature" not "Added feature")
- Reference GitHub issues (#123)
- Link to ADRs (ADR-0008)

Relates to ADR-XXXX
Fixes #123
```

### Examples

✅ **Good**:
```
Add ADR-0008: ZeroFormatter deprecation strategy

Documents decision to deprecate ZeroFormatter in v3.0 due to:
- Package archived in 2018, no .NET Core 3.0+ support
- No .NET 9 compatibility
- Superior alternatives available (MemoryPack 10x faster)

Includes migration guide and timeline for removal.

Relates to ADR-0008
```

❌ **Bad**:
```
Fixed stuff
```

❌ **Bad**:
```
Updated files
```

## Review Checklist

Before considering documentation complete:

### HISTORY.md
- [ ] Entry added immediately after completing work
- [ ] "What was changed" section is specific (file paths, metrics)
- [ ] "Why it was changed" section explains reasoning
- [ ] "Impact on the codebase" section documents consequences
- [ ] Entry uses consistent format with previous entries

### ADRs (if applicable)
- [ ] ADR created BEFORE implementation
- [ ] All sections filled in (or "N/A" with explanation)
- [ ] Alternatives documented with rejection reasons
- [ ] Code examples included where relevant
- [ ] Status updated correctly (Proposed/Accepted/Implemented)
- [ ] Index in `docs/adr/README.md` updated

### Test Reports (if applicable)
- [ ] Summary metrics included (pass/fail, coverage, duration)
- [ ] Failed tests documented with reproduction steps
- [ ] Comparison to previous runs included
- [ ] GitHub issues created for failures
- [ ] Report follows naming convention

### General
- [ ] All file paths are absolute (not relative)
- [ ] All links are valid
- [ ] No TODO or FIXME comments left
- [ ] Spelling and grammar checked
- [ ] Code examples are tested and working

## Common Mistakes to Avoid

1. **Batching documentation**: Write as you go, not at the end
2. **Vague language**: Use specific metrics and file paths
3. **Missing context**: Explain why, not just what
4. **Undocumented failures**: Document setbacks and blockers
5. **Outdated links**: Keep documentation synchronized
6. **Copy-paste errors**: Review before committing
7. **Inconsistent formatting**: Follow templates and examples
8. **Missing test reports**: Document all test runs
9. **ADRs after the fact**: Write ADRs before implementation
10. **Incomplete entries**: Fill in all sections

## Getting Help

If you're unsure about documentation:

1. **Check examples**: Look at existing HISTORY.md entries or ADRs
2. **Use templates**: Follow `docs/adr/template.md` structure
3. **Ask for review**: Get feedback before finalizing
4. **Iterate**: Documentation can be improved over time

## Related Resources

- [HISTORY.md](./HISTORY.md) - Work history format and examples
- [docs/adr/README.md](./adr/README.md) - ADR process and index
- [docs/adr/template.md](./adr/template.md) - ADR template
- [docs/test/README.md](./test/README.md) - Test report formats
- [docs/planning/PLAN.md](./planning/PLAN.md) - Overall migration plan

## Quick Reference

### Update HISTORY.md
```bash
# Edit HISTORY.md
nano docs/HISTORY.md

# Add entry at the top (after title, before previous entries)
# Use format:
## YYYY-MM-DD - [Title]
### What was changed
### Why it was changed
### Impact on the codebase
---
```

### Create ADR
```bash
# Copy template
cp docs/adr/template.md docs/adr/ADR-0019-my-decision.md

# Edit ADR
nano docs/adr/ADR-0019-my-decision.md

# Update index
nano docs/adr/README.md
```

### Create Test Report
```bash
# Run tests
dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Create report
nano docs/test/unit/unit-test-2025-10-09-stage-1.md

# Follow format in docs/test/README.md
```

---

**Remember**: Documentation is not optional. It is a critical part of the migration process that ensures traceability, knowledge transfer, and project success.
