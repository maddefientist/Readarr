# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 0.4.x   | :white_check_mark: |
| < 0.4.0 | :x:                |

## Reporting a Vulnerability

DO NOT open a public GitHub issue for security vulnerabilities.

### Reporting Process

1. Create a private security advisory via GitHub
2. GitHub Security Advisory: https://github.com/maddefientist/Readarr/security/advisories/new

### Response Timeline

- Acknowledgment: Within 48 hours
- Initial Assessment: Within 1 week
- Critical fixes: 7-14 days
- High severity: 14-30 days

## Security Best Practices

### Authentication
- Enable authentication in production
- Use strong passwords
- Change default API keys
- Rotate keys periodically

### Network Security
- Run behind reverse proxy with HTTPS
- Restrict to trusted networks
- Use firewall rules

### Updates
- Keep Readarr updated
- Monitor security advisories
- Test updates in staging

## Security Features

- API key authentication
- Form-based authentication
- Input validation
- Parameterized queries
- CORS protection
- Dependency scanning
- Container scanning

Last Updated: 2026-01-09
