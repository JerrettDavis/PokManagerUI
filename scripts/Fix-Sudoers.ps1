<#
.SYNOPSIS
    Fixes the pokmanager sudoers file

.DESCRIPTION
    Recreates the sudoers file with correct syntax directly on the server
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$User,

    [Parameter(Mandatory=$true)]
    [string]$Server,

    [Parameter(Mandatory=$false)]
    [string]$SudoPassword
)

Write-Host "Fixing sudoers configuration..." -ForegroundColor Cyan

# Prompt for sudo password if not provided
if (-not $SudoPassword) {
    Write-Host ""
    Write-Host "We need to remove the broken sudoers file and create a new one." -ForegroundColor Yellow
    Write-Host "This requires your sudo password." -ForegroundColor Yellow
    $securePassword = Read-Host "Enter sudo password for $User@$Server" -AsSecureString
    $SudoPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    )
}

# Create sudoers file directly on server with correct syntax
$createAndInstallCmd = @"
# Remove old broken file (with password)
echo '$SudoPassword' | sudo -S rm -f /etc/sudoers.d/pokmanager 2>/dev/null

# Create new sudoers file
cat > /tmp/pokmanager-sudoers << 'SUDOEOF'
# PokManager deployment permissions
Defaults:pokuser !requiretty

# systemd service management
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl start pokmanager-web
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl stop pokmanager-web
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl restart pokmanager-web
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl status pokmanager-web
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl start pokmanager-api
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl stop pokmanager-api
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl restart pokmanager-api
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl status pokmanager-api
pokuser ALL=(ALL) NOPASSWD: /bin/systemctl daemon-reload

# Docker management - allow all docker commands
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker
pokuser ALL=(ALL) NOPASSWD: /usr/bin/docker-compose

# File operations - allow all chown/chmod
pokuser ALL=(ALL) NOPASSWD: /bin/chown
pokuser ALL=(ALL) NOPASSWD: /bin/chmod

# Allow SSH user to su to pokuser without password
$User ALL=(ALL) NOPASSWD: /bin/su - pokuser
$User ALL=(ALL) NOPASSWD: /bin/su - pokuser *
SUDOEOF

# Validate and install (with password)
echo '$SudoPassword' | sudo -S visudo -c -f /tmp/pokmanager-sudoers && \
echo '$SudoPassword' | sudo -S cp /tmp/pokmanager-sudoers /etc/sudoers.d/pokmanager && \
echo '$SudoPassword' | sudo -S chmod 0440 /etc/sudoers.d/pokmanager && \
sudo rm /tmp/pokmanager-sudoers && \
echo 'SUCCESS: Sudoers file installed'
"@

Write-Host "Removing old file and installing new sudoers configuration..." -ForegroundColor Yellow

$result = & ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR "${User}@${Server}" $createAndInstallCmd 2>&1

# Clear password from memory
$SudoPassword = $null

if ($result -match "SUCCESS: Sudoers file installed") {
    Write-Host "✓ Sudoers file fixed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run deployment:" -ForegroundColor Cyan
    Write-Host "  .\scripts\Deploy-PokManager.ps1 -User $User -Server $Server -DeployUser pokuser" -ForegroundColor Gray
    exit 0
} else {
    Write-Host "✗ Failed to install sudoers file" -ForegroundColor Red
    Write-Host "Output:" -ForegroundColor Yellow
    Write-Host $result
    exit 1
}
