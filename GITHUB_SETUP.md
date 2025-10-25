# GitHub Repository Setup Guide

## Overview
This guide walks you through the complete setup process for the CloudflareD1.NET repository on GitHub.

## 1. Repository Settings

### Basic Settings
- ✅ Repository created: https://github.com/jdtoon/CloudflareD1.NET
- ✅ Visibility: Public
- ✅ Default branch: `master`

### Branch Protection
Recommended settings for the `master` branch:

1. Go to **Settings** → **Branches** → **Add branch protection rule**
2. Branch name pattern: `master`
3. Enable:
   - ✅ Require pull request reviews before merging
   - ✅ Require status checks to pass before merging
   - ✅ Require branches to be up to date before merging
   - ✅ Include administrators (optional but recommended)

## 2. GitHub Actions Secrets

The CI/CD workflow requires a NuGet API key to publish packages.

### Creating a NuGet API Key

1. Go to https://www.nuget.org/
2. Sign in with your account
3. Click your username → **API Keys**
4. Click **Create**
5. Configure:
   - **Key Name**: `CloudflareD1.NET GitHub Actions`
   - **Expiration**: 365 days (or longer)
   - **Scopes**: Select "Push new packages and package versions"
   - **Glob Pattern**: `CloudflareD1.NET`
6. Click **Create**
7. **IMPORTANT**: Copy the API key immediately (you won't see it again!)

### Adding the Secret to GitHub

1. Go to your repository: https://github.com/jdtoon/CloudflareD1.NET
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Configure:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Paste the API key you copied from NuGet.org
5. Click **Add secret**

## 3. GitHub Pages (Documentation)

To host your Docusaurus documentation on GitHub Pages:

### Option A: Using GitHub Actions (Recommended)

1. Create `.github/workflows/deploy-docs.yml`:

```yaml
name: Deploy Documentation

on:
  push:
    branches:
      - master
    paths:
      - 'docs/**'
      - '.github/workflows/deploy-docs.yml'

permissions:
  contents: read
  pages: write
  id-token: write

jobs:
  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: docs/package-lock.json
      
      - name: Install dependencies
        run: npm ci
        working-directory: ./docs
      
      - name: Build docs
        run: npm run build
        working-directory: ./docs
      
      - name: Setup Pages
        uses: actions/configure-pages@v4
      
      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: ./docs/build
      
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

2. Enable GitHub Pages:
   - Go to **Settings** → **Pages**
   - Source: **GitHub Actions**

### Option B: Manual Deployment

```bash
cd docs
npm run build
npm run serve  # Test locally first

# Deploy (requires write access)
USE_SSH=true npm run deploy
```

Your documentation will be available at: https://jdtoon.github.io/CloudflareD1.NET/

## 4. Repository Topics

Add relevant topics to make your repo discoverable:

1. Go to **About** (top right of repo page)
2. Click the gear icon ⚙️
3. Add topics:
   - `cloudflare`
   - `cloudflare-d1`
   - `dotnet`
   - `csharp`
   - `database`
   - `sqlite`
   - `nuget`
   - `d1-database`
   - `serverless`
   - `edge-computing`

## 5. Repository Details

Update the **About** section:
- **Description**: `.NET adapter for Cloudflare D1 database with local SQLite support`
- **Website**: `https://jdtoon.github.io/CloudflareD1.NET/`
- ✅ Include in the home page
- Add the topics listed above

## 6. Issue Templates

Create issue templates for better bug reports and feature requests:

1. Go to **Settings** → **Features** → **Issues** → **Set up templates**
2. Add:
   - **Bug report** template
   - **Feature request** template
   - **Documentation improvement** template

## 7. Pull Request Template

Create `.github/PULL_REQUEST_TEMPLATE.md`:

```markdown
## Description
<!-- Describe your changes in detail -->

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
<!-- Describe the tests you ran to verify your changes -->

## Checklist
- [ ] My code follows the project's style guidelines
- [ ] I have performed a self-review of my code
- [ ] I have commented my code where necessary
- [ ] I have updated the documentation accordingly
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
```

## 8. GitHub Discussions (Optional)

Enable Discussions for community engagement:
1. Go to **Settings** → **Features**
2. Enable **Discussions**
3. Categories to create:
   - 💡 Ideas
   - 🙏 Q&A
   - 📣 Announcements
   - 💬 General

## 9. Verify CI/CD Pipeline

After setting up the `NUGET_API_KEY` secret:

1. Make a small change (e.g., update CHANGELOG.md)
2. Commit and push to `master` branch
3. Go to **Actions** tab
4. Watch the "Build and Publish" workflow
5. Verify:
   - ✅ Build succeeds
   - ✅ Tests pass
   - ✅ NuGet package publishes (on master branch only)

## 10. NuGet Package Verification

After the first successful CI/CD run:

1. Go to https://www.nuget.org/packages/CloudflareD1.NET/
2. Verify:
   - ✅ Package is listed
   - ✅ Description is correct
   - ✅ License is MIT
   - ✅ Repository link points to GitHub
   - ✅ Documentation link works

## 11. Social Sharing

Update the social preview image:
1. Go to **Settings** → **Social preview**
2. Upload an image (recommended: 1280x640px)
3. Create one with:
   - CloudflareD1.NET logo/text
   - ".NET adapter for Cloudflare D1"
   - "Local development with SQLite"

## Security

### Enable Security Features
1. Go to **Settings** → **Security**
2. Enable:
   - ✅ Dependency graph
   - ✅ Dependabot alerts
   - ✅ Dependabot security updates
   - ✅ Secret scanning
   - ✅ Code scanning (GitHub Advanced Security)

### Create Security Policy
Already included in `SECURITY.md`, but verify it's visible:
- Go to **Security** tab
- Should see "Security policy" section

## Verification Checklist

Before announcing your package:

- [ ] All CI/CD secrets configured
- [ ] GitHub Actions workflow runs successfully
- [ ] NuGet package published and accessible
- [ ] GitHub Pages documentation deployed
- [ ] Repository topics added
- [ ] Branch protection enabled
- [ ] Issue templates created
- [ ] README.md has correct links
- [ ] CHANGELOG.md is up to date
- [ ] Security features enabled
- [ ] License file present (MIT)
- [ ] All sample projects build and run
- [ ] Docusaurus site loads correctly

## Next Steps

1. **Announce the Release**:
   - Create a GitHub Release for v1.0.0
   - Share on relevant communities:
     - r/dotnet
     - r/cloudflare
     - Twitter/X with #dotnet #cloudflare
     - .NET Discord servers
     - DEV.to blog post

2. **Monitor**:
   - Watch for issues
   - Respond to discussions
   - Review pull requests
   - Check NuGet download stats

3. **Improve**:
   - Add more examples based on user feedback
   - Expand documentation
   - Create video tutorials (optional)
   - Build a sample app showcase

## Support

If you encounter issues during setup:
1. Check GitHub Actions logs for detailed error messages
2. Verify secrets are correctly named (`NUGET_API_KEY`)
3. Ensure NuGet API key has correct permissions
4. Test builds locally first: `dotnet build && dotnet test`

---

**Repository**: https://github.com/jdtoon/CloudflareD1.NET  
**Documentation**: https://jdtoon.github.io/CloudflareD1.NET/  
**NuGet Package**: https://www.nuget.org/packages/CloudflareD1.NET/
