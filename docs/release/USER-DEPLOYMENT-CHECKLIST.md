# RawRabbit v2.1.0 - User Deployment Checklist

## 🎯 Quick Start - 3 Steps to Release

This checklist guides you through the final deployment steps for RawRabbit v2.1.0.

---

## ✅ Step 1: Review and Push Git Tag

### 1.1 Review Tag Locally
```bash
# View tag details
git tag -l -n20 v2.1.0

# Verify tag points to correct commit
git show v2.1.0 --no-patch

# Check what's included since last tag
git log v2.0.0..v2.1.0 --oneline
```

### 1.2 Push Tag to Remote
```bash
# Push the tag
git push origin v2.1.0

# Verify tag on remote
git ls-remote --tags origin v2.1.0
```

**Expected Result**: Tag v2.1.0 visible on GitHub in the "Tags" section

---

## ✅ Step 2: Publish NuGet Packages

### 2.1 Prerequisites
- [ ] NuGet API key configured
- [ ] Verify all packages built (should be 26 .nupkg files)

```bash
# Check package count
ls nupkg/*.nupkg | wc -l
# Should output: 26
```

### 2.2 Test Publish (Optional but Recommended)
```bash
# Test with local feed first
dotnet nuget push nupkg/*.nupkg --source local-feed
```

### 2.3 Publish to NuGet.org
```bash
# Publish all packages at once
dotnet nuget push nupkg/*.nupkg \
  --source https://api.nuget.org/v3/index.json \
  --api-key YOUR_API_KEY_HERE \
  --skip-duplicate

# OR publish individually (more control)
for package in nupkg/*.nupkg; do
  dotnet nuget push "$package" \
    --source https://api.nuget.org/v3/index.json \
    --api-key YOUR_API_KEY_HERE
  echo "Published: $package"
done
```

### 2.4 Verify Publication
1. Visit https://www.nuget.org/packages/RawRabbit/
2. Confirm v2.1.0 is listed
3. Check all 26 packages are available
4. Verify package metadata is correct

**Expected Result**: All 26 packages visible on NuGet.org with version 2.1.0

---

## ✅ Step 3: Create GitHub Release

### Option A: Using GitHub CLI (Recommended)

#### 3.1 Create Draft Release
```bash
gh release create v2.1.0 \
  --title "RawRabbit v2.1.0 - .NET 9 Migration" \
  --notes-file docs/release/RELEASE-NOTES-v2.1.0.md \
  --draft \
  --target upgrade
```

#### 3.2 Upload Release Assets
```bash
gh release upload v2.1.0 \
  docs/release/RELEASE-NOTES-v2.1.0.md \
  docs/MIGRATION-GUIDE.md \
  CHANGELOG.md
```

#### 3.3 Review Draft
```bash
# View draft in browser
gh release view v2.1.0 --web
```

#### 3.4 Publish Release
```bash
# After review, publish
gh release edit v2.1.0 --draft=false
```

### Option B: Using GitHub Web UI

#### 3.1 Navigate to Release Page
1. Go to: https://github.com/EY-Parthenon/RawRabbit/releases/new
2. Or click "Releases" → "Draft a new release"

#### 3.2 Configure Release
- **Tag**: v2.1.0
- **Target**: upgrade (branch)
- **Release title**: RawRabbit v2.1.0 - .NET 9 Migration

#### 3.3 Add Release Notes
Copy content from `docs/release/RELEASE-NOTES-v2.1.0.md`

#### 3.4 Attach Files
Upload these files:
- [ ] docs/release/RELEASE-NOTES-v2.1.0.md
- [ ] docs/MIGRATION-GUIDE.md
- [ ] CHANGELOG.md

#### 3.5 Save as Draft
Click "Save draft" to review before publishing

#### 3.6 Review and Publish
1. Review all content and assets
2. Click "Publish release"

**Expected Result**: Release visible at https://github.com/EY-Parthenon/RawRabbit/releases

---

## 🎉 Post-Release Actions

### Immediate (Within 1 Hour)
- [ ] Verify release is visible on GitHub
- [ ] Confirm packages are downloadable from NuGet
- [ ] Test `dotnet add package RawRabbit --version 2.1.0`
- [ ] Pin release announcement in GitHub Discussions

### Short-term (First 24 Hours)
- [ ] Monitor GitHub issues for v2.1.0 tags
- [ ] Check NuGet download statistics
- [ ] Respond to community questions
- [ ] Update README badges with v2.1.0

### Long-term (First Week)
- [ ] Track adoption metrics
- [ ] Collect migration feedback
- [ ] Plan patch release if needed (v2.1.1)
- [ ] Update roadmap based on community input

---

## 🚨 Troubleshooting

### Issue: Git Tag Push Fails
```bash
# Check for existing tag on remote
git ls-remote --tags origin v2.1.0

# If exists, delete and recreate (careful!)
git push --delete origin v2.1.0
git push origin v2.1.0
```

### Issue: NuGet Package Already Exists
- NuGet doesn't allow overwriting versions
- If package already exists, you'll need to increment to v2.1.1
- Use `--skip-duplicate` flag to skip already-published packages

### Issue: GitHub Release Creation Fails
- Verify tag v2.1.0 exists on remote: `git ls-remote --tags origin v2.1.0`
- Ensure you have repository write permissions
- Try creating as draft first, then publish after review

### Issue: Package Not Appearing on NuGet
- NuGet indexing can take 5-15 minutes
- Check NuGet.org package page directly
- Verify API key has publish permissions
- Check for any error messages during push

---

## 📊 Success Verification

Run these commands to verify successful deployment:

```bash
# 1. Verify git tag on remote
git ls-remote --tags origin v2.1.0

# 2. Check GitHub release
gh release view v2.1.0

# 3. Verify NuGet package availability
dotnet search RawRabbit --exact-match

# 4. Test package installation
mkdir /tmp/test-rawrabbit-2.1.0
cd /tmp/test-rawrabbit-2.1.0
dotnet new console
dotnet add package RawRabbit --version 2.1.0
```

**All checks should pass** ✅

---

## 📞 Support & Communication

### If You Encounter Issues
1. Check troubleshooting section above
2. Review `/docs/release/stage-8-release-strategy.md` for detailed procedures
3. Consult rollback procedures if needed
4. Create GitHub issue with `release:v2.1.0` label

### Communication Channels
- **GitHub Issues**: Technical problems and bugs
- **GitHub Discussions**: Community questions and feedback
- **Release Page**: Official announcements and updates

---

## 🎯 Summary

**Deployment Checklist**:
- [ ] Step 1: Push git tag v2.1.0 to remote
- [ ] Step 2: Publish 26 NuGet packages to NuGet.org
- [ ] Step 3: Create and publish GitHub release

**Estimated Time**: 15-30 minutes
**Difficulty**: Easy (well-documented procedures)
**Risk Level**: Low (comprehensive testing completed)

**When complete**: RawRabbit v2.1.0 will be publicly available! 🎉

---

🤖 Generated with Claude Code - User Deployment Guide
📅 Guide Date: 2025-10-09
✅ Status: READY FOR EXECUTION
