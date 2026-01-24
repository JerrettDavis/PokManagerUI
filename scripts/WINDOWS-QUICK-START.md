# Windows PowerShell Quick Start

## First-Time Setup (Run Once)

```powershell
cd C:\git\PokManagerApi

# Configure server for automated deployment (one-time setup)
.\scripts\Initialize-PokManagerServer.ps1 -User admin -Server 10.0.0.216 -DeployUser pokuser
```

After this one-time setup, all future deployments will be fully automated with no password prompts!

**📖 See:** [DEPLOYMENT-SETUP.md](../DEPLOYMENT-SETUP.md) for details.

## TL;DR - Deploy in 30 Seconds

```powershell
# 1. Navigate to project
cd C:\git\PokManagerApi

# 2. Deploy (will prompt for password)
.\scripts\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216

# 2a. Or deploy with su to pokuser
.\scripts\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216 -DeployUser pokuser

# 3. Test
.\scripts\Test-PokManager.ps1 -Server 10.0.0.216

# 4. Open in browser
Start-Process "http://10.0.0.216:5207"
```

## One-Time Setup (5 minutes)

### 1. Enable PowerShell Scripts
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 2. Set Up SSH Keys (Optional but Recommended)
```powershell
# Generate key
ssh-keygen -t ed25519

# Copy to server
type $env:USERPROFILE\.ssh\id_ed25519.pub | ssh admin@10.0.0.216 "cat >> ~/.ssh/authorized_keys"

# Test (should not ask for password)
ssh admin@10.0.0.216
```

### 3. Create Aliases (Optional)
Add to PowerShell profile (`notepad $PROFILE`):

```powershell
function Deploy-PokManager {
    param(
        [string]$User = "admin",
        [string]$Server = "10.0.0.216",
        [string]$DeployUser = "pokuser"
    )
    & "C:\\git\\PokManagerApi\\scripts\\Deploy-PokManager.ps1" -User $User -Server $Server -DeployUser $DeployUser
}

function Test-PokManager {
    & "C:\git\PokManagerApi\scripts\Test-PokManager.ps1" -Server "10.0.0.216"
}

Set-Alias deploy-pok Deploy-PokManager
Set-Alias test-pok Test-PokManager
```

Now you can simply type:
```powershell
deploy-pok
test-pok
```

## Common Commands

```powershell
# Deploy with password
.\scripts\Deploy-PokManager.ps1 -User admin -Password "YourPass" -Server 10.0.0.216

# Deploy with su to pokuser
.\scripts\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216 -DeployUser pokuser

# Deploy without tests (faster for quick updates)
.\scripts\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216 -SkipTests

# Test deployment health
.\scripts\Test-PokManager.ps1 -Server 10.0.0.216

# Check service status via SSH
ssh admin@10.0.0.216 "systemctl status pokmanager-web pokmanager-api"

# View logs
ssh admin@10.0.0.216 "journalctl -u pokmanager-web -n 50"

# Restart services
ssh admin@10.0.0.216 "sudo systemctl restart pokmanager-web pokmanager-api"

# Open in browser
Start-Process "http://10.0.0.216:5207"
```

## Troubleshooting

### "SSH client not found"
```powershell
# Install OpenSSH (as Administrator)
Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0
```

### "Cannot be loaded because running scripts is disabled"
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### "Connection refused" after deployment
```powershell
# Check if services are running
ssh admin@10.0.0.216 "systemctl status pokmanager-web pokmanager-api"

# View logs
ssh admin@10.0.0.216 "journalctl -u pokmanager-web -n 100"
```

### Services not starting automatically
Server needs systemd configuration. See [DEPLOYMENT-WINDOWS.md](../DEPLOYMENT-WINDOWS.md) Section "First-Time Setup on Server".

## What Each Script Does

### Deploy-PokManager.ps1
1. Builds application (Release)
2. Runs tests
3. Publishes Web & API
4. Stops services on server
5. Copies files via SCP
6. Starts services
7. Verifies deployment

### Test-PokManager.ps1
- Quick health check
- Tests Web Frontend (HTTP 200)
- Tests API Service health endpoint
- Shows pass/fail results

## Full Documentation

- [Complete Windows Guide](../DEPLOYMENT-WINDOWS.md) - Detailed Windows deployment guide
- [Linux Deployment Guide](../DEPLOYMENT.md) - Server-side configuration
- [Script Reference](README.md) - All available scripts

## Examples

### Deploy after pulling latest code
```powershell
cd C:\git\PokManagerApi
git pull origin main
.\scripts\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216
Start-Process "http://10.0.0.216:5207"
```

### Quick test without full deployment
```powershell
.\scripts\Test-PokManager.ps1 -Server 10.0.0.216
```

### Deploy to different server
```powershell
.\scripts\Deploy-PokManager.ps1 -User ubuntu -Server 192.168.1.100 -DeployPath "/home/ubuntu/pokmanager"
```

### Skip tests for faster deployment
```powershell
.\scripts\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216 -SkipTests
```
