<#
.SYNOPSIS
    Installs .NET 10 Runtime on the remote server

.DESCRIPTION
    Quick script to install .NET runtime as a prerequisite for PokManager deployment

.PARAMETER User
    SSH username

.PARAMETER Server
    Server IP or hostname

.PARAMETER SudoPassword
    Sudo password (will prompt if not provided)
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

Write-Host "Installing .NET 10 Runtime on $Server..." -ForegroundColor Cyan
Write-Host ""

# Prompt for sudo password if not provided
if (-not $SudoPassword) {
    $securePassword = Read-Host "Enter sudo password for $User@$Server" -AsSecureString
    $SudoPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    )
}

# Check if .NET is already installed
Write-Host "Checking for existing .NET installation..." -ForegroundColor Yellow
$dotnetCheck = & ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR "$User@$Server" "dotnet --version" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ .NET is already installed (version: $dotnetCheck)" -ForegroundColor Green
    $SudoPassword = $null
    exit 0
}

Write-Host ".NET not found. Installing..." -ForegroundColor Yellow
Write-Host ""

# Get Ubuntu version first
Write-Host "Detecting Ubuntu version..." -ForegroundColor Yellow
$ubuntuVersion = & ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR "$User@$Server" "lsb_release -rs" 2>&1
$ubuntuVersion = $ubuntuVersion.Trim()
Write-Host "Ubuntu version: $ubuntuVersion" -ForegroundColor Cyan

# Install .NET - escape the password and use proper command structure
$escapedPassword = $SudoPassword -replace "'", "'\\''"

$installCmd = "echo '$escapedPassword' | sudo -S bash -c 'wget -q https://packages.microsoft.com/config/ubuntu/$ubuntuVersion/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb && dpkg -i /tmp/packages-microsoft-prod.deb && rm /tmp/packages-microsoft-prod.deb && apt-get update -qq && DEBIAN_FRONTEND=noninteractive apt-get install -y -qq dotnet-runtime-10.0 aspnetcore-runtime-10.0 && echo SUCCESS'"

$result = & ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR "$User@$Server" $installCmd 2>&1

# Clear password from memory
$SudoPassword = $null

if ($result -match "SUCCESS") {
    Write-Host ""
    Write-Host "✓ .NET 10 Runtime installed successfully!" -ForegroundColor Green
    Write-Host ""

    # Verify installation
    $version = & ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR "$User@$Server" "dotnet --version" 2>&1
    Write-Host "Installed version: $version" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "You can now deploy PokManager:" -ForegroundColor Cyan
    Write-Host "  .\scripts\Deploy-PokManager.ps1 -User $User -Server $Server -DeployUser pokuser" -ForegroundColor Gray
    exit 0
} else {
    Write-Host ""
    Write-Host "✗ .NET installation failed" -ForegroundColor Red
    Write-Host "Output:" -ForegroundColor Yellow
    Write-Host $result
    exit 1
}
