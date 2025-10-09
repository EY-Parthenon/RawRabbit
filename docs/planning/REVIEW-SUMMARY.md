# .NET 9 Migration Plan Review - Executive Summary

**Date**: 2025-10-09
**Reviewer**: Migration Architect
**Plan Version**: 1.0
**Status**: ⚠️ APPROVE WITH CRITICAL MODIFICATIONS

---

## Quick Verdict

The migration plan is **fundamentally sound** with excellent structure, but requires **critical corrections** before execution. Timeline is achievable with proper parallel execution, but should be extended by 2 weeks for safety.

**Recommendation**: ✅ PROCEED after incorporating the 6 critical fixes below.

---

## 6 Critical Issues (Must Fix Before Starting)

### 1. ❌ INCORRECT: Dependency Migration Order

**Problem**: Plan shows MessageSequence migrating in Week 5-6 alongside basic operations, but it actually depends on 5 other components.

**Fix**: MessageSequence must migrate in **Tier 3 (Week 5-6)** AFTER:
- GlobalExecutionId (Tier 1)
- Operations.Publish (Tier 1)
- MessageContext.Subscribe (Tier 2)
- Operations.StateMachine (Tier 2)
- Operations.Tools (Tier 1)

**Impact**: Could block entire Stage 4 if not corrected.

---

### 2. ⚠️ MISSING: ZeroFormatter Deprecation Decision

**Problem**: ZeroFormatter was archived in 2018, does NOT support .NET Core 3.0+.

**Fix**: Decide in Stage 2:
- Option A: Deprecate entirely (recommended)
- Option B: Find active fork
- Option C: Implement alternative

**Required**: ADR 0008 - ZeroFormatter Deprecation Strategy

---

### 3. ⚠️ MISSING: Ninject Status Verification

**Problem**: Ninject last updated 2017, .NET 9 compatibility unknown.

**Fix**: Research in Stage 1, decide in Stage 2:
- If incompatible: Deprecate, provide migration guide to Autofac/MS.DI
- If compatible: Update and continue support

**Required**: ADR 0009 - Ninject Deprecation (if needed)

---

### 4. ⚠️ MISSING: RabbitMQ.Client 7.x Breaking Changes

**Problem**: Jumping from 5.0.1 → 7.x (major version) with unknown API changes.

**Fix**: Research in Stage 1 Week 1:
- Review RabbitMQ.Client 7.x release notes
- Test basic operations with .NET 9
- Document breaking changes
- Create compatibility strategy

**Required**: ADR 0011 - RabbitMQ.Client 7.x Compatibility

---

### 5. ❌ BLOCKER: .NET 9 SDK Not Installed

**Problem**: Development machine doesn't have .NET 9 SDK installed.

**Fix**: Install before Stage 1 starts:
```bash
# Download and install .NET 9 SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 9.0.100
```

---

### 6. ⚠️ MISSING: 5 Additional ADRs

**Problem**: Plan identifies 5-7 ADRs, but analysis reveals 12 critical decisions.

**Fix**: Add to Stage 2 deliverables:
- ADR 0008: ZeroFormatter Deprecation
- ADR 0009: Ninject Deprecation
- ADR 0010: ASP.NET Core Migration
- ADR 0011: RabbitMQ.Client 7.x Compatibility
- ADR 0012: JSON Serializer Strategy (Newtonsoft vs System.Text.Json)

---

## Timeline Recommendation

| Version | Duration | Assessment |
|---------|----------|------------|
| **Original Plan** | 10-12 weeks | Optimistic, no buffer |
| **Recommended** | **12-14 weeks** | Realistic with buffer |
| **Conservative** | 14-16 weeks | Safe, accounts for unknowns |

**Justification for +2 weeks**:
- Additional ADR work (+0.5 weeks)
- MessageSequence complexity (+0.5 weeks)
- RabbitMQ.Client 7.x unknowns (+0.5 weeks)
- Deprecation handling (+0.5 weeks)

---

## Corrected Migration Order

### Week 3: Core Foundation
- RawRabbit (Core)
- RabbitMQ.Client 5.0.1 → 7.x
- Setup RabbitMQ Docker

### Week 3.5-4: Tier 1 (9 projects, parallel)
**Operations**: Publish, Subscribe, Request, Respond, Get, Tools
**Enrichers**: MessageContext, Attributes, GlobalExecutionId, QueueSuffix, Polly, RetryLater

### Week 4.5-5.5: Tier 2 (3 projects)
- MessageContext.Subscribe (depends on Subscribe)
- MessageContext.Respond (depends on Respond)
- Operations.StateMachine (depends on Subscribe)

### Week 5-6: Tier 3 (Complex)
- **MessageSequence** (5 dependencies - needs extra time)
- HttpContext (ASP.NET Core migration)
- Serialization enrichers (Protobuf, MessagePack, ZeroFormatter decision)

### Week 7: Tier 4 (DI Adapters)
- ServiceCollection, Autofac, Ninject (if keeping)

---

## Multi-Targeting Strategy Decision

**RECOMMENDED**: Single-target .NET 9 (Option A)

### Option A: Single Target `.NET 9` ✅
**Pros**:
- Clean break, removes technical debt
- Enables .NET 9 optimizations
- Simpler migration

**Cons**:
- Breaking change for users
- No backward compatibility

### Option B: Multi-Target `.NET 9 + .NET Standard 2.1`
**Pros**:
- Backward compatible
- Gradual migration

**Cons**:
- Complex conditional compilation
- .NET Standard 2.1 already obsolete (2019)
- Delays modernization

**Justification for Option A**:
1. Current targets (netstandard1.5, net451) are severely outdated
2. Major version upgrade justifies clean break
3. RabbitMQ.Client 7.x likely requires modern runtime

**Required**: ADR 0003 - Target Framework Selection (already planned)

---

## Breaking Changes Checklist

### Confirmed Breaking Changes
- ✅ .NET Framework 4.5.1 → .NET 9 (if single-target)
- ✅ RabbitMQ.Client 5.x → 7.x API changes
- ✅ ASP.NET Classic (System.Web) → ASP.NET Core
- ✅ ZeroFormatter likely deprecated
- ✅ Ninject adapter possibly deprecated

### Potential Breaking Changes (Requires Review)
- ⚠️ Polly 5.x → 8.x async patterns
- ⚠️ Newtonsoft.Json API changes (unlikely)
- ⚠️ Reflection API updates
- ⚠️ Cryptography API replacements

**Required Document**: `docs/BREAKING-CHANGES.md` (not in current plan)

---

## Immediate Actions (Before Stage 1)

### Priority 1 - Blockers 🚨
1. ✅ Install .NET 9 SDK
2. ✅ Research RabbitMQ.Client 5→7 breaking changes
3. ✅ Verify ZeroFormatter .NET 9 compatibility
4. ✅ Check Ninject maintenance status

### Priority 2 - Planning 📋
1. Update PLAN.md with corrected dependency order
2. Add 5 missing ADRs to Stage 2
3. Setup Docker RabbitMQ test environment
4. Create visual dependency graph

**Timeline**: Complete within 1 week before Stage 1 kickoff

---

## Risk Assessment

| Risk | Severity | Probability | Mitigation |
|------|----------|-------------|------------|
| RabbitMQ.Client 7.x breaking changes | 🔴 High | 80% | Early testing, compatibility shims |
| ZeroFormatter incompatibility | 🟡 Medium | 90% | Deprecation plan, user migration guide |
| MessageSequence dependency chain | 🟡 Medium | 30% | Corrected migration order (done) |
| Timeline overrun | 🟡 Medium | 40% | Add 2-week buffer (recommended) |
| .NET 9 API incompatibilities | 🟢 Low | 20% | Well-documented upgrade path |

---

## Plan Strengths ✅

1. Excellent multi-stage structure (8 stages)
2. Clear agent allocation (6 roles)
3. Comprehensive documentation (ADRs, HISTORY, test reports)
4. Security checkpoints at each stage
5. Phased rollout (alpha → beta → RC → production)
6. Parallel execution strategy

---

## Modified Success Criteria

### Add to Technical Criteria
- ✅ All deprecated dependencies documented
- ✅ RabbitMQ.Client 7.x validated
- ✅ Test projects migrated concurrently
- ✅ Docker test infrastructure automated

### Add to Documentation Criteria
- ✅ `BREAKING-CHANGES.md` complete
- ✅ `DEPRECATED.md` created
- ✅ Dependency graph visualization
- ✅ RabbitMQ upgrade guide
- ✅ Troubleshooting guide

---

## Approval Checklist

Before proceeding to Stage 1:

- [ ] .NET 9 SDK installed and verified
- [ ] Corrected dependency order incorporated into PLAN.md
- [ ] 5 additional ADRs added to Stage 2 deliverables
- [ ] RabbitMQ.Client 7.x research completed
- [ ] ZeroFormatter status verified
- [ ] Ninject status verified
- [ ] Docker RabbitMQ environment documented
- [ ] Timeline extended to 12-14 weeks
- [ ] Visual dependency graph created
- [ ] BREAKING-CHANGES.md template created

---

## Final Recommendation

**APPROVE PLAN with the following conditions**:

1. ✅ Incorporate corrected dependency migration order
2. ✅ Extend timeline to 12-14 weeks (add 2-week buffer)
3. ✅ Complete 4 immediate research tasks (RabbitMQ, ZeroFormatter, Ninject, .NET SDK)
4. ✅ Add 5 missing ADRs to Stage 2
5. ✅ Add test project migration to component stages
6. ✅ Create BREAKING-CHANGES.md and DEPRECATED.md

**Next Steps**:
1. Review team approves modifications
2. Update PLAN.md to v1.1
3. Complete immediate research tasks
4. Begin Stage 1 - Foundation & Assessment

---

**Reviewed by**: Migration Architect
**Date**: 2025-10-09
**Next Review**: After Stage 1 completion

---

## Quick Reference Files

- **Full Review**: `/home/laird/src/EYP/RawRabbit/docs/PLAN-REVIEW.md`
- **Original Plan**: `/home/laird/src/EYP/RawRabbit/docs/PLAN.md`
- **Dependency Graph**: `/home/laird/src/EYP/RawRabbit/docs/dependency-graph.mermaid`
- **This Summary**: `/home/laird/src/EYP/RawRabbit/docs/REVIEW-SUMMARY.md`
