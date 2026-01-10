# Stage 1: Frontend Build
FROM node:20.11.1-bookworm-slim AS frontend-build

WORKDIR /app

# Copy package files
COPY package.json yarn.lock .yarnrc ./
COPY frontend/package.json ./frontend/

# Install dependencies
RUN yarn install --frozen-lockfile --network-timeout 120000

# Copy frontend source
COPY frontend ./frontend

# Build frontend
RUN yarn run build --env production

# Stage 2: Backend Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS backend-build

WORKDIR /app

# Copy solution and project files
COPY src/*.sln ./src/
COPY src/Directory.Build.props ./src/
COPY src/Directory.Packages.props ./src/
COPY src/stylecop.json ./src/

# Copy all csproj files (restore layer - cached if no csproj changes)
COPY src/NzbDrone.Common/*.csproj ./src/NzbDrone.Common/
COPY src/NzbDrone.Core/*.csproj ./src/NzbDrone.Core/
COPY src/NzbDrone.Host/*.csproj ./src/NzbDrone.Host/
COPY src/NzbDrone.SignalR/*.csproj ./src/NzbDrone.SignalR/
COPY src/NzbDrone.Mono/*.csproj ./src/NzbDrone.Mono/
COPY src/NzbDrone.Windows/*.csproj ./src/NzbDrone.Windows/
COPY src/NzbDrone.Console/*.csproj ./src/NzbDrone.Console/
COPY src/NzbDrone.Update/*.csproj ./src/NzbDrone.Update/
COPY src/NzbDrone/*.csproj ./src/NzbDrone/
COPY src/Readarr.Api.V1/*.csproj ./src/Readarr.Api.V1/
COPY src/Readarr.Http/*.csproj ./src/Readarr.Http/
COPY src/ServiceHelpers/ServiceInstall/*.csproj ./src/ServiceHelpers/ServiceInstall/
COPY src/ServiceHelpers/ServiceUninstall/*.csproj ./src/ServiceHelpers/ServiceUninstall/

# Restore dependencies
RUN dotnet restore src/Readarr.sln

# Copy all source code
COPY src ./src
COPY Logo ./Logo

# Build for linux-x64
RUN dotnet msbuild -restore src/Readarr.sln \
    -p:Configuration=Release \
    -p:Platform=Posix \
    -p:RuntimeIdentifiers=linux-x64 \
    -t:PublishAllRids

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim

# Install runtime dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        mediainfo \
        sqlite3 && \
    rm -rf /var/lib/apt/lists/*

# Create app user and directories
RUN groupadd -r readarr && \
    useradd -r -g readarr -d /config -s /bin/bash readarr && \
    mkdir -p /config /books /app && \
    chown -R readarr:readarr /config /books /app

WORKDIR /app

# Copy backend artifacts from build stage
COPY --from=backend-build /app/_output ./

# Copy frontend artifacts from frontend build stage
COPY --from=frontend-build /app/_output/UI ./UI/

# Set ownership
RUN chown -R readarr:readarr /app

# Volumes
VOLUME ["/config", "/books"]

# Expose port
EXPOSE 8787

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8787/ping || exit 1

# Switch to non-root user
USER readarr

# Set environment defaults
ENV READARR__INSTANCE_NAME="Readarr" \
    READARR__BRANCH="develop" \
    READARR__LOG_LEVEL="info"

# Run Readarr
CMD ["/app/Readarr", "-nobrowser", "-data=/config"]
