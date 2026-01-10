# Readarr Deployment Guide

## Docker Deployment (Recommended)

### Quick Start

1. **Clone and configure:**
   ```bash
   git clone https://github.com/maddefientist/Readarr.git
   cd Readarr
   cp .env.example .env
   ```

2. **Edit `.env` file:**
   ```bash
   nano .env
   ```
   Set at least:
   - `BOOKS_PATH` - Where your books are stored
   - `DOWNLOADS_PATH` - Where downloads are saved
   - `TZ` - Your timezone (e.g., `America/New_York`)

3. **Start Readarr:**
   ```bash
   docker compose up -d
   ```

4. **Access the UI:**
   - Open: http://localhost:8787
   - Complete initial setup wizard

### Configuration Options

#### Using SQLite (Default)

SQLite is used by default with no additional configuration needed. Database files are stored in the `/config` volume.

**Pros:**
- Zero configuration
- Simple backup (copy database file)
- Good for most users

**Cons:**
- Not ideal for high-concurrency scenarios
- Limited to single-node deployments

#### Using PostgreSQL

For better performance or multi-node setups, use PostgreSQL:

1. **Edit `docker-compose.yml`:**
   Uncomment the `postgres` service section

2. **Edit `.env`:**
   ```env
   POSTGRES_USER=readarr
   POSTGRES_PASSWORD=your_secure_password
   POSTGRES_MAINDB=readarr-main
   ```

3. **Edit `docker-compose.yml`:**
   Uncomment PostgreSQL environment variables in the `readarr` service

4. **Start services:**
   ```bash
   docker compose up -d
   ```

**Pros:**
- Better performance
- Supports concurrent operations
- Production-ready

**Cons:**
- Requires additional container
- More complex backup/restore

### Volume Management

#### Default Volumes

| Volume | Purpose | Location |
|--------|---------|----------|
| `readarr-config` | App config, database, logs | `/config` |
| `books` | Your book library | `/books` |
| `downloads` | Download client output | `/downloads` |

#### Custom Volume Paths

Edit `.env` to use host paths instead of Docker volumes:

```env
# Use absolute paths
BOOKS_PATH=/mnt/media/books
DOWNLOADS_PATH=/mnt/downloads
```

Then update `docker-compose.yml`:
```yaml
volumes:
  - ./config:/config  # Use local directory
  - ${BOOKS_PATH}:/books
  - ${DOWNLOADS_PATH}:/downloads
```

### Networking

#### Default Port
- Readarr: `8787`

#### Custom Port
Edit `.env`:
```env
READARR_PORT=9999
```

#### Reverse Proxy

##### Nginx
```nginx
location /readarr {
    proxy_pass http://localhost:8787;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_redirect off;
}
```

Set in `.env`:
```env
READARR__URL_BASE=/readarr
```

##### Traefik
Add labels to `docker-compose.yml`:
```yaml
labels:
  - "traefik.enable=true"
  - "traefik.http.routers.readarr.rule=Host(`readarr.example.com`)"
  - "traefik.http.routers.readarr.entrypoints=websecure"
  - "traefik.http.routers.readarr.tls.certresolver=letsencrypt"
  - "traefik.http.services.readarr.loadbalancer.server.port=8787"
```

### Health Checks

Built-in health check monitors:
- Web server responsiveness
- Database connectivity

**Check status:**
```bash
docker compose ps
```

**Manual health check:**
```bash
curl http://localhost:8787/ping
```

Expected response:
```json
{"status": "OK"}
```

## Production Deployment

### System Requirements

**Minimum:**
- CPU: 2 cores
- RAM: 512MB
- Storage: 100MB (app) + database size

**Recommended:**
- CPU: 4 cores
- RAM: 2GB
- Storage: SSD for database

### Security Best Practices

1. **Change default credentials** on first login

2. **Use API key authentication:**
   ```env
   READARR__API_KEY=your-secure-api-key
   ```

3. **Enable authentication:**
   ```env
   READARR__AUTH_METHOD=Forms
   READARR__AUTH_REQUIRED=Enabled
   READARR__USERNAME=admin
   READARR__PASSWORD=secure_password
   ```

4. **Run behind reverse proxy** with HTTPS

5. **Restrict network access:**
   ```yaml
   # docker-compose.yml
   ports:
     - "127.0.0.1:8787:8787"  # Only localhost
   ```

6. **Use secrets for sensitive data:**
   ```bash
   echo "my_password" | docker secret create postgres_password -
   ```

### Backup & Restore

#### Backup

**SQLite:**
```bash
# Stop Readarr
docker compose stop readarr

# Backup config volume
docker run --rm -v readarr_readarr-config:/config \
  -v $(pwd):/backup alpine \
  tar czf /backup/readarr-backup-$(date +%Y%m%d).tar.gz /config

# Restart
docker compose start readarr
```

**PostgreSQL:**
```bash
docker compose exec postgres pg_dump -U readarr readarr-main > backup.sql
```

#### Restore

**SQLite:**
```bash
docker compose down
docker run --rm -v readarr_readarr-config:/config \
  -v $(pwd):/backup alpine \
  tar xzf /backup/readarr-backup-20260109.tar.gz -C /
docker compose up -d
```

**PostgreSQL:**
```bash
cat backup.sql | docker compose exec -T postgres psql -U readarr readarr-main
```

### Upgrades

#### Upgrade Readarr

```bash
# Pull latest image
docker compose pull readarr

# Recreate container
docker compose up -d readarr
```

#### Version Pinning

Edit `docker-compose.yml`:
```yaml
services:
  readarr:
    image: readarr:0.4.20  # Pin to specific version
```

### Monitoring

#### Logs

**View logs:**
```bash
docker compose logs -f readarr
```

**Log location:**
- Container: `/config/logs/readarr.txt`
- Host: Check volume mount

#### Health Endpoint

Monitor via:
```bash
curl http://localhost:8787/api/v1/health
```

Returns health check results for:
- Download clients
- Indexers
- Import lists
- System health

#### Metrics

Export logs to monitoring tools:
```yaml
# docker-compose.yml
logging:
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "3"
    labels: "service=readarr"
```

Send to Loki, Elasticsearch, or Sentry.

### Troubleshooting

#### Container won't start

```bash
# Check logs
docker compose logs readarr

# Check health
docker inspect readarr | jq '.[0].State.Health'
```

#### Permission errors

Fix volume permissions:
```bash
docker compose down
sudo chown -R 1000:1000 ./config ./books ./downloads
docker compose up -d
```

Or adjust PUID/PGID in `.env`:
```env
PUID=1001
PGID=1001
```

#### Database connection errors

**SQLite:**
- Check disk space
- Verify `/config` volume is writable

**PostgreSQL:**
- Ensure `postgres` container is healthy
- Check connection env vars
- Verify network connectivity:
  ```bash
  docker compose exec readarr ping postgres
  ```

#### Out of memory

Increase container limits:
```yaml
# docker-compose.yml
services:
  readarr:
    mem_limit: 2g
    memswap_limit: 2g
```

## Non-Docker Deployment

### Linux (systemd)

1. **Build from source:**
   ```bash
   ./build.sh --all -r linux-x64
   ```

2. **Copy to `/opt`:**
   ```bash
   sudo cp -r _output/linux-x64/net8.0/Readarr /opt/readarr
   ```

3. **Create systemd service:**
   ```bash
   sudo nano /etc/systemd/system/readarr.service
   ```

   ```ini
   [Unit]
   Description=Readarr Daemon
   After=network.target

   [Service]
   User=readarr
   Group=readarr
   Type=simple
   ExecStart=/opt/readarr/Readarr -nobrowser -data=/var/lib/readarr
   Restart=on-failure
   TimeoutStopSec=20
   KillMode=process

   [Install]
   WantedBy=multi-user.target
   ```

4. **Enable and start:**
   ```bash
   sudo systemctl enable --now readarr
   ```

### Windows

Use the Inno Setup installer:
```bash
./build.sh --installer -r win-x64
```

Installs as Windows Service by default.

### macOS

Use the `.app` bundle:
```bash
./build.sh --packages -r osx-arm64  # or osx-x64
```

Copy `Readarr.app` to `/Applications`.

## Migration from Existing Installation

1. **Backup old installation** (database + config)

2. **Stop old service**

3. **Copy config to new location:**
   ```bash
   docker run --rm -v readarr_readarr-config:/config \
     -v $(pwd)/old-config:/source alpine \
     sh -c "cp -r /source/* /config/"
   ```

4. **Start new Docker container:**
   ```bash
   docker compose up -d
   ```

5. **Verify migration** - check logs and UI

## Next Steps

- [Operations Guide](OPERATIONS.md) - Housekeeping, backups, monitoring
- [Development Guide](DEV.md) - Contributing
- [Architecture Overview](../README.md) - Technical details
