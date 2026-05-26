# Readarr Operations Guide

## Health Monitoring

### Health Endpoint

Readarr provides a comprehensive health check API:

```bash
curl http://localhost:8787/api/v1/health \
  -H "X-Api-Key: YOUR_READARR_API_KEY"
```

**Health Check Categories:**

| Check | Purpose |
|-------|---------|
| `IndexerStatusCheck` | Monitors indexer connectivity |
| `DownloadClientCheck` | Verifies download client configuration |
| `ImportListStatusCheck` | Checks import list health |
| `NotificationStatusCheck` | Monitors notification services |
| `RootFolderCheck` | Validates root folder accessibility |
| `MountCheck` | Detects unmounted volumes |
| `UpdateCheck` | Checks for available updates |
| `SystemTimeCheck` | Validates system clock sync |

**Health Status Levels:**
- `Ok` - System operating normally
- `Notice` - Non-critical issue, action recommended
- `Warning` - Issue requiring attention
- `Error` - Critical problem affecting functionality

### Monitoring Best Practices

**1. Automated Health Checks:**
```bash
#!/bin/bash
# health-check.sh
HEALTH=$(curl -s http://localhost:8787/api/v1/health -H "X-Api-Key: $API_KEY")
ERRORS=$(echo $HEALTH | jq '[.[] | select(.type=="error")] | length')

if [ "$ERRORS" -gt 0 ]; then
    echo "CRITICAL: $ERRORS health errors detected"
    echo $HEALTH | jq '.[] | select(.type=="error")'
    exit 2
fi
```

Run via cron every 5 minutes:
```cron
*/5 * * * * /path/to/health-check.sh
```

**2. Prometheus Metrics** (if using exporters):
```yaml
# Sample Prometheus scrape config
scrape_configs:
  - job_name: 'readarr'
    static_configs:
      - targets: ['localhost:8787']
    metrics_path: '/api/v1/health'
    params:
      apikey: [YOUR_READARR_API_KEY]
```

**3. Log Aggregation:**
Ship logs to centralized system:
```bash
# Using Loki/Promtail
docker run -d \
  --name promtail \
  -v /var/lib/docker/volumes/readarr_readarr-config/_data/logs:/logs \
  -v /path/to/promtail-config.yaml:/etc/promtail/config.yml \
  grafana/promtail:latest
```

## Automated Housekeeping

Readarr performs automatic maintenance tasks daily:

### Built-in Housekeeping Tasks

**Database Maintenance:**
- `TrimLogDatabase` - Removes old log entries
- `TrimHttpCache` - Cleans expired HTTP cache
- `CleanupCommandQueue` - Purges old commands

**Orphan Cleanup:**
- `CleanupOrphanedBooks` - Removes books without authors
- `CleanupOrphanedBookFiles` - Deletes file records for missing files
- `CleanupOrphanedEditions` - Removes editions without books
- `CleanupOrphanedHistoryItems` - Purges orphaned history
- `CleanupOrphanedMetadataFiles` - Cleans orphaned metadata

**Data Integrity:**
- `FixFutureRunScheduledTasks` - Corrects future-dated tasks
- `FixMultipleMonitoredEditions` - Resolves duplicate monitors
- `UpdateCleanTitleForAuthor` - Refreshes search titles

**Disk Cleanup:**
- `DeleteBadMediaCovers` - Removes corrupt cover images
- `CleanupTemporaryUpdateFiles` - Deletes old update artifacts

### Manual Housekeeping

Trigger manual housekeeping:
```bash
curl -X POST http://localhost:8787/api/v1/command \
  -H "X-Api-Key: YOUR_READARR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"name": "housekeeping"}'
```

### Housekeeping Schedule

Tasks run automatically every 24 hours at 04:00 local time. To change:

**Via UI:** Settings → General → Advanced → Housekeeping Interval

**Via Config File** (`config.xml`):
```xml
<Config>
  <HousekeepingInterval>24</HousekeepingInterval>
</Config>
```

## Backup & Restore

### Automated Backups

Readarr creates automatic backups:

**Backup Location:** `/config/Backups/scheduled/`

**Default Schedule:**
- Frequency: Every 7 days
- Retention: 28 days (4 weeks)

**Backup Contents:**
- `readarr.db` - Main database (SQLite)
- `config.xml` - Application configuration
- Database backup manifest

### Manual Backup

**Docker:**
```bash
# Stop Readarr
docker compose stop readarr

# Create backup
docker run --rm \
  -v readarr_readarr-config:/config \
  -v $(pwd):/backup \
  alpine \
  tar czf /backup/readarr-backup-$(date +%Y%m%d-%H%M%S).tar.gz \
    /config/readarr.db \
    /config/config.xml \
    /config/Backups

# Restart
docker compose start readarr
```

**Bare Metal:**
```bash
# Stop service
sudo systemctl stop readarr

# Backup
tar czf readarr-backup-$(date +%Y%m%d).tar.gz \
  /var/lib/readarr/readarr.db \
  /var/lib/readarr/config.xml \
  /var/lib/readarr/Backups

# Restart
sudo systemctl start readarr
```

### PostgreSQL Backups

If using PostgreSQL:

**Automated (via cron):**
```bash
#!/bin/bash
# backup-postgres.sh
BACKUP_DIR="/backups/readarr"
DATE=$(date +%Y%m%d-%H%M%S)

mkdir -p $BACKUP_DIR

docker compose exec -T postgres pg_dump -U readarr readarr-main | \
  gzip > $BACKUP_DIR/readarr-db-$DATE.sql.gz

# Retain last 30 days
find $BACKUP_DIR -name "readarr-db-*.sql.gz" -mtime +30 -delete
```

Add to crontab:
```cron
0 2 * * * /path/to/backup-postgres.sh
```

### Restore from Backup

**Docker (SQLite):**
```bash
# Stop Readarr
docker compose down

# Extract backup
tar xzf readarr-backup-20260109.tar.gz -C /tmp

# Restore to volume
docker run --rm \
  -v readarr_readarr-config:/config \
  -v /tmp/config:/backup \
  alpine \
  sh -c "cp /backup/readarr.db /config/ && cp /backup/config.xml /config/"

# Restart
docker compose up -d
```

**PostgreSQL:**
```bash
# Drop and recreate database
docker compose exec postgres psql -U readarr -c "DROP DATABASE IF EXISTS \"readarr-main\""
docker compose exec postgres psql -U readarr -c "CREATE DATABASE \"readarr-main\""

# Restore
gunzip < readarr-db-20260109.sql.gz | \
  docker compose exec -T postgres psql -U readarr readarr-main

# Restart Readarr
docker compose restart readarr
```

### Backup Best Practices

1. **3-2-1 Rule:**
   - 3 copies of data
   - 2 different storage types
   - 1 offsite copy

2. **Test Restores** monthly

3. **Automated backup verification:**
   ```bash
   # Verify SQLite database integrity
   sqlite3 readarr.db "PRAGMA integrity_check;"
   ```

4. **Offsite backup:**
   ```bash
   # Sync to remote storage (e.g., S3)
   aws s3 cp readarr-backup-$(date +%Y%m%d).tar.gz \
     s3://my-backups/readarr/
   ```

## Upgrades

### Docker Upgrades

**Check for updates:**
```bash
docker compose pull readarr
```

**Upgrade process:**
```bash
# 1. Backup (see above)

# 2. Pull latest image
docker compose pull readarr

# 3. Recreate container
docker compose up -d readarr

# 4. Check logs
docker compose logs -f readarr

# 5. Verify health
curl http://localhost:8787/api/v1/health -H "X-Api-Key: $API_KEY"
```

**Rollback if needed:**
```bash
# Use specific version
docker compose down
# Edit docker-compose.yml: image: readarr:0.4.19
docker compose up -d
```

### Version Pinning

**Recommended:** Pin to specific versions in production:

```yaml
# docker-compose.yml
services:
  readarr:
    image: readarr:0.4.20  # Pin version
```

**Update strategy:**
1. Test new version in staging
2. Update pinned version
3. Deploy via `docker compose up -d`

### Database Migrations

Readarr automatically migrates the database on startup.

**Monitor migration:**
```bash
docker compose logs -f readarr | grep -i migration
```

**Migration logs location:** `/config/logs/readarr.txt`

**Rollback migration** (emergency only):
```bash
# Restore from pre-upgrade backup
# DO NOT manually edit database
```

## Log Management

### Log Locations

| Log Type | Path |
|----------|------|
| Application | `/config/logs/readarr.txt` |
| Update | `/config/logs/readarr.update.txt` |
| Database (if enabled) | Database LogRecords table |

### Log Levels

Configure via: Settings → General → Log Level

- `Trace` - Extremely verbose, debug only
- `Debug` - Detailed diagnostic info
- `Info` - General informational messages (default)
- `Warn` - Warning messages, potential issues
- `Error` - Error messages only
- `Fatal` - Critical failures only

### Log Rotation

**Default rotation:**
- Log files automatically rotate at 1MB
- Keeps last 5 files
- Archives compressed

**Manual cleanup:**
```bash
# Docker
docker compose exec readarr sh -c "rm /config/logs/readarr.*.txt.gz"

# Bare metal
rm /var/lib/readarr/logs/readarr.*.txt.gz
```

### Log Analysis

**Find errors:**
```bash
docker compose exec readarr grep -i "error" /config/logs/readarr.txt | tail -20
```

**Monitor live:**
```bash
docker compose logs -f readarr
```

**Export logs:**
```bash
docker compose cp readarr:/config/logs/readarr.txt ./readarr-logs-$(date +%Y%m%d).txt
```

## Performance Tuning

### Database Optimization

**SQLite:**
```bash
# Vacuum database (reclaim space)
docker compose exec readarr sqlite3 /config/readarr.db "VACUUM;"

# Analyze (update statistics)
docker compose exec readarr sqlite3 /config/readarr.db "ANALYZE;"
```

**PostgreSQL:**
```bash
# Vacuum and analyze
docker compose exec postgres psql -U readarr readarr-main -c "VACUUM ANALYZE;"
```

### Resource Limits

**Docker resource constraints:**
```yaml
# docker-compose.yml
services:
  readarr:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          memory: 512M
```

### Indexer Tuning

**Limit concurrent searches:** Settings → Indexers → Options
- RSS Sync Interval: 60 minutes (default)
- Maximum Size: 0 (unlimited)
- Retention: 0 (unlimited)

**Rate limiting:**
- Configure per-indexer rate limits
- Use delay profiles for popular indexers

## Troubleshooting

### Common Issues

**1. High Memory Usage:**
```bash
# Check memory
docker stats readarr --no-stream

# Restart to clear
docker compose restart readarr
```

**2. Slow UI:**
- Check database size: `du -sh /config/readarr.db`
- Vacuum if > 500MB
- Check network latency to metadata sources

**3. Failed Imports:**
```bash
# Check import history
curl http://localhost:8787/api/v1/history \
  -H "X-Api-Key: $API_KEY" | jq '.[] | select(.eventType=="downloadFailed")'
```

**4. Metadata Failures:**
- Check `/api/v1/health` for OpenLibrary connectivity
- Verify network access to openlibrary.org
- Review logs: `grep -i "openlibrary" /config/logs/readarr.txt`

### Debug Mode

Enable debug logging:

**Temporary (via API):**
```bash
curl -X PUT http://localhost:8787/api/v1/config/host \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"logLevel": "debug"}'
```

**Permanent:** Settings → General → Log Level → Debug

**Disable after troubleshooting** (increases disk usage).

### Support Resources

- **GitHub Issues:** https://github.com/maddefientist/Readarr/issues
- **Logs:** Always include logs when reporting issues
- **Config:** Share sanitized config.xml (remove API keys)

## Security

### API Key Management

**Rotate API key:**
1. UI: Settings → General → Security → API Key → Regenerate
2. Update all clients/scripts with new key
3. Old key immediately invalid

**Secure API key storage:**
```bash
# Use environment variables
export READARR_API_KEY="your-key-here"

# Or use secrets management
docker secret create readarr_api_key <(echo "your-key")
```

### Authentication

**Enable authentication:** Settings → General → Security

- **Forms:** Username/password login
- **Basic:** HTTP Basic authentication
- **None:** No authentication (LAN only)

**Required for:** `Enabled` (all access) or `DisabledForLocalAddresses` (recommended)

### Network Security

**Firewall rules:**
```bash
# Allow only from specific network
iptables -A INPUT -p tcp -s 192.168.1.0/24 --dport 8787 -j ACCEPT
iptables -A INPUT -p tcp --dport 8787 -j DROP
```

**Reverse proxy with HTTPS:**
See [DEPLOYMENT.md](DEPLOYMENT.md) for Nginx/Traefik config.

### Security Scanning

**Scan container for vulnerabilities:**
```bash
docker scan readarr:latest
```

**Update dependencies** regularly via automated upgrades.

## Next Steps

- [Deployment Guide](DEPLOYMENT.md) - Initial setup
- [Development Guide](DEV.md) - Contributing
- [Architecture Overview](../README.md) - Technical details
