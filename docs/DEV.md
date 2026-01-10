# Readarr Development Guide

## Prerequisites

### Required

- **.NET 8 SDK** (8.0.404 or later)
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify: `dotnet --version` should show 8.0.x

- **Node.js** 20.11.1 (managed via nvm or volta)
  - Download: https://nodejs.org/
  - Verify: `node --version` should show v20.11.1

- **Yarn** 1.22.19
  - Install: `npm install -g yarn@1.22.19`
  - Verify: `yarn --version` should show 1.22.19

### Optional

- **Docker** & **Docker Compose** (for containerized development/deployment)
- **Visual Studio 2022** or **JetBrains Rider** (for IDE development)
- **VS Code** with C# extension

## Quick Start

### 1. Clone Repository

```bash
git clone https://github.com/maddefientist/Readarr.git
cd Readarr
```

### 2. Install Dependencies

#### Backend (.NET 8)
```bash
dotnet restore src/Readarr.sln
```

#### Frontend (Node/Yarn)
```bash
yarn install --frozen-lockfile --network-timeout 120000
```

### 3. Build

#### Full Build (Backend + Frontend)
```bash
./build.sh --all
```

#### Backend Only
```bash
./build.sh --backend
```

#### Frontend Only
```bash
./build.sh --frontend
```

## Build Script Options

The `build.sh` script supports several flags:

- `--backend` - Build .NET backend only
- `--frontend` - Build React frontend only
- `--packages` - Create distribution packages
- `--installer` - Build Windows installer (requires Inno Setup)
- `--lint` - Run code linters (ESLint, Stylelint)
- `--all` - Build everything

### Runtime Identifiers

Specify target platform with `-r` or `--runtime`:

```bash
./build.sh --backend -r linux-x64
./build.sh --backend -r win-x64
./build.sh --backend -r osx-arm64
```

Available RIDs:
- `win-x64`, `win-x86` (Windows)
- `linux-x64`, `linux-musl-x64`, `linux-arm64`, `linux-musl-arm64`, `linux-arm`, `linux-musl-arm` (Linux)
- `osx-x64`, `osx-arm64` (macOS)
- `freebsd-x64` (FreeBSD - optional)

## Running Tests

### All Tests
```bash
dotnet test src/Readarr.sln
```

### Unit Tests Only
```bash
./test.sh Unit
```

### Integration Tests Only
```bash
./test.sh Integration
```

### Automation Tests
```bash
./test.sh Automation
```

## Running Readarr

### From Build Output
```bash
cd _output
./Readarr  # Linux/macOS
Readarr.exe  # Windows
```

### Default URLs
- Web UI: http://localhost:8787
- API: http://localhost:8787/api/v1

## Project Structure

```
Readarr/
├── src/                        # C# backend source
│   ├── NzbDrone.Core/          # Core business logic
│   ├── Readarr.Api.V1/         # REST API v1
│   ├── NzbDrone.Host/          # ASP.NET Core host
│   ├── NzbDrone.Common/        # Shared utilities
│   └── *.Test/                 # Test projects
├── frontend/                   # React/TypeScript frontend
│   ├── src/                    # Frontend source
│   └── build/                  # Webpack config
├── _output/                    # Build output
├── _tests/                     # Test packages
├── _artifacts/                 # Distribution packages
├── build.sh                    # Main build script
├── test.sh                     # Test runner
├── global.json                 # .NET SDK version
└── .nvmrc                      # Node version
```

## Technology Stack

### Backend
- **.NET 8** (LTS through Nov 2026)
- **ASP.NET Core 8** - Web framework
- **Dapper** - Micro-ORM
- **SQLite / PostgreSQL** - Databases
- **FluentMigrator** - Database migrations
- **DryIoc** - Dependency injection
- **NLog** - Logging

### Frontend
- **React 17.0.2**
- **TypeScript 5.1.6**
- **Webpack 5** - Build tool
- **Redux** - State management

## Troubleshooting

### .NET SDK Not Found
```bash
# Check SDK version
dotnet --version

# List installed SDKs
dotnet --list-sdks

# Should see 8.0.404 (or later 8.0.x)
```

### Node Version Mismatch
```bash
# Using nvm
nvm install
nvm use

# Using volta (auto-switches based on package.json)
volta install node@20.11.1
```

### Yarn Install Fails
```bash
# Clear cache and retry
yarn cache clean
yarn install --frozen-lockfile --network-timeout 120000
```

### Build Fails with "Platform not supported"
Make sure you're targeting a valid runtime identifier:
```bash
dotnet --info  # Check current RID
./build.sh --backend -r <your-rid>
```

## IDE Setup

### Visual Studio 2022
1. Open `src/Readarr.sln`
2. Set `Readarr.Console` or `Readarr.Host` as startup project
3. Build and Run (F5)

### JetBrains Rider
1. Open `src/Readarr.sln`
2. Run configurations auto-generated
3. Run/Debug as needed

### VS Code
1. Install **C# Dev Kit** extension
2. Open workspace root
3. Use integrated terminal for build commands

## Contributing

1. Create a feature branch from `develop`
2. Make changes
3. Run linters: `./build.sh --lint`
4. Run tests: `dotnet test src/Readarr.sln`
5. Commit with clear messages
6. Push and create PR against `develop`

## Next Steps

- [Deployment Guide](DEPLOYMENT.md) - Docker deployment
- [Operations Guide](OPERATIONS.md) - Production operations
- [Architecture Overview](../README.md) - System architecture
