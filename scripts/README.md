# PokManager Deployment Scripts

Quick reference for deployment scripts - both Linux/Bash and Windows/PowerShell.

## Quick Start

### 🪟 Windows (PowerShell)

```powershell
# Deploy from Windows to Linux server
.\scripts\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216

# Quick health check
.\scripts\Test-PokManager.ps1 -Server 10.0.0.216
```

**📖 See:** [WINDOWS-QUICK-START.md](WINDOWS-QUICK-START.md) | [Full Windows Guide](../DEPLOYMENT-WINDOWS.md)

### 🐧 Linux/macOS (Bash)

```bash
# Test current deployment
./scripts/quick-check.sh 10.0.0.216 5207 5374

# Comprehensive verification
./scripts/verify-deployment.sh 10.0.0.216 5207 5374

# Full deployment (on server)
ssh user@10.0.0.216
cd /path/to/PokManager
sudo ./scripts/deploy.sh
```

**📖 See:** [Linux Deployment Guide](../DEPLOYMENT.md)

## Available Scripts

### PowerShell (Windows)
| Script | Purpose | Duration |
|--------|---------|----------|
| `Initialize-PokManagerServer.ps1` | First-time server setup wizard (run once) | ~2-3 minutes |
| `Deploy-PokManager.ps1` | Full deployment from Windows to Linux server | ~3-5 minutes |
| `Test-PokManager.ps1` | Quick health check from Windows | ~5 seconds |

### Bash (Linux/macOS)
| Script | Purpose | Duration |
|--------|---------|----------|
| `quick-check.sh` | Fast health check of both services | ~5 seconds |
| `verify-deployment.sh` | Comprehensive deployment verification | ~30 seconds |
| `deploy.sh` | Full deployment automation (run on server) | ~3-5 minutes |

## Usage Examples

### Local Development Testing

```bash
# Test locally running Aspire app
./scripts/quick-check.sh localhost 7134 7536

# Full verification
./scripts/verify-deployment.sh localhost 7134 7536
```

### Remote Server Testing

```bash
# Quick check from your machine
./scripts/quick-check.sh 10.0.0.216 5207 5374

# SSH in for detailed check
ssh user@10.0.0.216
cd /path/to/PokManager
./scripts/verify-deployment.sh localhost 5207 5374
```

### CI/CD Integration

```yaml
# GitHub Actions example
- name: Verify Deployment
  run: |
    sleep 15  # Wait for services to start
    ./scripts/verify-deployment.sh ${{ secrets.SERVER_IP }} 5207 5374
```

## What Gets Checked

### quick-check.sh
- ✅ Web Frontend responds
- ✅ API Service responds  
- ✅ Health endpoints (if available)

### verify-deployment.sh
- ✅ Web Frontend accessibility
- ✅ API Service health checks
- ✅ Web pages load (Dashboard, Instances, Backups, Configuration)
- ✅ API endpoints respond
- ✅ Static assets load correctly
- ✅ Response time performance
- ✅ Process status (if local)

### deploy.sh
- 🔨 Builds application
- 🧪 Runs tests
- 🛑 Stops existing services
- 📦 Publishes to deployment directory
- 🔐 Sets correct permissions
- ▶️  Starts services
- ✅ Verifies deployment

## Health Check Endpoints

Both services expose health check endpoints:

```bash
# Comprehensive health check
curl http://10.0.0.216:5207/health  # Web Frontend
curl http://10.0.0.216:5374/health  # API Service

# Liveness check (basic responsiveness)
curl http://10.0.0.216:5207/alive
curl http://10.0.0.216:5374/alive
```

**Response Codes:**
- `200` - Healthy
- `503` - Unhealthy (one or more health checks failed)
- `404` - Health checks not enabled (check configuration)

## Troubleshooting

### Scripts Don't Execute

```bash
# Make scripts executable
chmod +x scripts/*.sh
```

### Permission Denied on Server

```bash
# deploy.sh requires sudo for systemctl
sudo ./scripts/deploy.sh

# Verification scripts don't need sudo
./scripts/quick-check.sh localhost 5207 5374
```

### Health Endpoints Return 404

Health checks are now enabled in all environments. If you get 404:

1. Verify the application is using `ServiceDefaults` (it should be)
2. Check that `MapDefaultEndpoints()` is called in `Program.cs`
3. Rebuild the application after changes

### Connection Refused

```bash
# Check if services are running
ps aux | grep PokManager

# Check if ports are listening
netstat -tulpn | grep -E '5207|5374'

# Check service status (if using systemd)
sudo systemctl status pokmanager-web
sudo systemctl status pokmanager-api
```

## Default Ports

| Service | HTTP Port | HTTPS Port (Dev) |
|---------|-----------|------------------|
| Web Frontend | 5207 | 7134 |
| API Service | 5374 | 7536 |

## More Information

See [DEPLOYMENT.md](../DEPLOYMENT.md) for:
- Complete deployment workflow
- systemd service configuration
- Security considerations
- Monitoring setup
- Production best practices
