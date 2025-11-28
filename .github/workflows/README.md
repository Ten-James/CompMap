# GitHub Actions Workflows

This directory contains the CI/CD workflows for TenJames.CompMap.

## Workflows Overview

### 🔧 CI (`ci.yml`)
**Purpose:** Continuous integration - ensures code quality on every change

**Triggers:**
- Pull requests (opened, synchronized, reopened)
- Pushes to `main` branch

**Steps:**
1. Checkout code
2. Setup .NET 10
3. Restore dependencies
4. Build solution in Release mode
5. Run all tests (unit + integration)

---

### 🚀 Release (`release.yml`)
**Purpose:** Create new releases with automatic version bumping

**Trigger:** Manual workflow dispatch from GitHub Actions UI

**Inputs:**
- `version_bump`: Choice of `patch`, `minor`, or `major`
- `prerelease`: Boolean flag for prerelease versions

**Process:**
1. **Bump and Tag Job:**
   - Reads current version from `TenJames.CompMap.csproj`
   - Bumps version according to selected type:
     - `patch`: 0.1.6 → 0.1.7
     - `minor`: 0.1.6 → 0.2.0
     - `major`: 0.1.6 → 1.0.0
   - Updates version in csproj
   - Creates commit: `chore: bump version to X.Y.Z`
   - Creates and pushes tag: `vX.Y.Z`
   - Pushes commit to main branch

2. **Build and Release Job:**
   - Checks out code at the new tag
   - Restores, builds, and tests
   - Packs NuGet package with correct version
   - Publishes to NuGet.org (if `NUGET_API_KEY` is configured)
   - Generates release notes from git commits
   - Creates GitHub release with:
     - Auto-generated changelog
     - Installation instructions
     - NuGet package as artifact

**Usage:**
```
1. Go to: GitHub → Actions → Release
2. Click: "Run workflow"
3. Select branch: main
4. Choose version bump: patch/minor/major
5. Set prerelease: true/false
6. Click: "Run workflow"
```

---

### 📦 Publish on Tag (`publish-on-tag.yml`)
**Purpose:** Automatic publishing when version tags are pushed

**Trigger:** Push of tags matching `v*.*.*` pattern

**Use Cases:**
- Manual tag creation: `git tag v0.1.7 && git push origin v0.1.7`
- Fallback if release workflow fails
- External CI/CD integration

**Process:**
1. Extracts version from tag
2. Builds and tests solution
3. Warns if csproj version doesn't match tag
4. Packs with tag version (overrides csproj)
5. Publishes to NuGet.org
6. Creates GitHub release with changelog

---

## Secrets Required

### `NUGET_API_KEY`
- **Required for:** Publishing to NuGet.org
- **Setup:** GitHub → Settings → Secrets and variables → Actions → New repository secret
- **Get key from:** https://www.nuget.org/account/apikeys
- **Scope:** Push packages to nuget.org

If not configured, packages are built and attached to releases but not published to NuGet.

---

## Release Checklist

### Before Release:
- [ ] All tests passing on main branch
- [ ] CHANGELOG or commit messages are descriptive
- [ ] Breaking changes documented (for major versions)
- [ ] `NUGET_API_KEY` secret configured (for NuGet publish)

### Creating a Release:
1. **Via GitHub UI (Recommended):**
   - Actions → Release → Run workflow
   - Select version bump type
   - Monitor workflow progress

2. **Via Manual Tag:**
   ```bash
   # Update version in csproj if needed
   vim TenJames.CompMap/TenJames.CompMap/TenJames.CompMap.csproj

   # Create and push tag
   git tag v0.1.7
   git push origin v0.1.7
   ```

### After Release:
- [ ] Verify package on NuGet.org
- [ ] Test installation: `dotnet add package TenJames.CompMap --version X.Y.Z`
- [ ] Check GitHub release page for artifacts
- [ ] Update documentation if needed

---

## Troubleshooting

### Release workflow fails to push
**Problem:** Protected branch rules prevent bot from pushing

**Solution:**
- Go to: Settings → Branches → Branch protection rules
- Allow: "Allow specified actors to bypass required pull requests"
- Add: `github-actions[bot]`

### NuGet publish skipped
**Problem:** `NUGET_API_KEY` not configured

**Solution:**
- Get API key from https://www.nuget.org/account/apikeys
- Add as secret: Settings → Secrets → New repository secret

### Version conflict
**Problem:** Tag version doesn't match csproj version

**Solution:**
- Use release workflow (automatically syncs)
- Or manually update csproj before creating tag

---

## Workflow Dependencies

```
┌─────────────────┐
│   Developer     │
│  Pushes Code    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   CI Workflow   │ ◄── Pull Requests
│  (ci.yml)       │ ◄── Push to main
└─────────────────┘


┌─────────────────┐
│    Release      │
│  Workflow       │ ◄── Manual trigger
│ (release.yml)   │
└────────┬────────┘
         │
         ├─► Bumps version
         ├─► Creates commit
         ├─► Creates tag ─────┐
         └─► Builds & publishes │
                                │
                                │
┌───────────────────────────────┘
│
▼
┌─────────────────┐
│  Publish on     │
│     Tag         │ ◄── Tag pushed
│(publish-on-tag) │     (manual or auto)
└─────────────────┘
```
