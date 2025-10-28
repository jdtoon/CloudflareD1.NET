# Security Policy

We take the security of CloudflareD1.NET and its users seriously. This document explains how to report vulnerabilities, handle secrets safely, and use our security-related tooling.

## Supported Versions

We support the latest minor release of each package and accept security reports for any currently published package version on NuGet. Please upgrade to the newest versions to receive the latest fixes.

## Reporting a Vulnerability

- Please email security reports to: security@jdtoon.dev
- Alternatively, open a private security advisory on GitHub (Security > Advisories > Report a vulnerability) for this repository.
- Do not create public issues for security problems.
- Provide as much detail as possible: affected package, version, PoC (if available), environment, and impact.
- We will acknowledge receipt within 3 business days and work with you on a coordinated fix and disclosure.

## Responsible Disclosure

- Please allow us reasonable time to investigate and release a fix before public disclosure.
- We will credit reporters in release notes if desired.

## Secrets Management

Never commit real secrets (API tokens, keys, passwords) to the repository.

- Configuration files with actual values (e.g., `appsettings.json`) should not be committed. The repo includes `appsettings.example.json` templates.
- Use environment variables or .NET User Secrets for development:
  - Environment variables: `CloudflareD1__AccountId`, `CloudflareD1__DatabaseId`, `CloudflareD1__ApiToken`
  - User Secrets: `dotnet user-secrets set "CloudflareD1:ApiToken" "..."`
- Our `.gitignore` excludes `**/appsettings.json`. If a file was previously committed, sanitize it by replacing values with placeholders.

## Logging Safety

- The library logs SQL and parameters at Debug level only. Avoid enabling Debug logs in production if queries may contain sensitive data.
- Authorization headers and tokens are never logged by the library.
- You can control log levels via your host's logging configuration to prevent sensitive payloads from being written.

## Dependency Security

- .NET Packages: We periodically run `dotnet list package --vulnerable` across the solution; please ensure your environment also enforces vulnerability checks.
- Docs Site (Node): We run `npm audit` within `/docs`; it currently reports zero vulnerabilities. Keep Node packages updated.

## Transport Security

- The library uses HTTPS by default for Cloudflare API calls (`https://api.cloudflare.com/client/v4/`).
- There is no facility to disable certificate validation.

## Contact

For any security-related questions, contact security@jdtoon.dev.
