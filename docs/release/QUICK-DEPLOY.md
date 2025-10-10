# RawRabbit v2.1.0 - Quick Deploy Reference

## 🚀 3-Step Deployment

### 1. Push Tag (30 seconds)
```bash
git push origin v2.1.0
```

### 2. Publish Packages (5-10 minutes)
```bash
dotnet nuget push nupkg/*.nupkg \
  --source https://api.nuget.org/v3/index.json \
  --api-key YOUR_API_KEY \
  --skip-duplicate
```

### 3. Create Release (2 minutes)
```bash
gh release create v2.1.0 \
  --title "RawRabbit v2.1.0 - .NET 9 Migration" \
  --notes-file docs/release/RELEASE-NOTES-v2.1.0.md \
  --target upgrade

gh release upload v2.1.0 \
  docs/release/RELEASE-NOTES-v2.1.0.md \
  docs/MIGRATION-GUIDE.md \
  CHANGELOG.md
```

## ✅ Verification

```bash
# Check tag
git ls-remote --tags origin v2.1.0

# Check packages
dotnet search RawRabbit --exact-match

# Check release
gh release view v2.1.0
```

## 📞 Need Help?

See `USER-DEPLOYMENT-CHECKLIST.md` for detailed instructions.

---
🤖 Generated with Claude Code
