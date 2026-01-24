<#
.SYNOPSIS
    Quick health check for PokManager deployment

.DESCRIPTION
    Tests if PokManager services are responding on the remote server

.PARAMETER Server
    IP address or hostname of the remote server

.PARAMETER WebPort
    Web frontend port (default: 5207)

.PARAMETER ApiPort
    API service port (default: 5374)

.EXAMPLE
    .\Test-PokManager.ps1 -Server 10.0.0.216
    
.EXAMPLE
    Test-PokManager 10.0.0.216 5207 5374
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$Server,
    
    [Parameter(Mandatory=$false, Position=1)]
    [int]$WebPort = 5207,
    
    [Parameter(Mandatory=$false, Position=2)]
    [int]$ApiPort = 5374
)

Write-Host ""
Write-Host "🔍 Quick health check for PokManager on $Server..." -ForegroundColor Cyan
Write-Host ""

$passed = 0
$failed = 0

# Test Web Frontend
try {
    $webResponse = Invoke-WebRequest -Uri "http://${Server}:${WebPort}" -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    if ($webResponse.StatusCode -eq 200) {
        Write-Host "✅ Web Frontend ($WebPort) - OK" -ForegroundColor Green
        $passed++
    }
} catch {
    Write-Host "❌ Web Frontend ($WebPort) - FAILED" -ForegroundColor Red
    $failed++
}

# Test API Service Health
try {
    $apiHealthResponse = Invoke-WebRequest -Uri "http://${Server}:${ApiPort}/health" -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    if ($apiHealthResponse.StatusCode -eq 200) {
        Write-Host "✅ API Service ($ApiPort) - OK" -ForegroundColor Green
        $passed++
    }
} catch {
    # Try base API endpoint if health check fails
    try {
        $apiResponse = Invoke-WebRequest -Uri "http://${Server}:${ApiPort}" -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
        Write-Host "⚠️  API Service ($ApiPort) - UP (health endpoint not configured)" -ForegroundColor Yellow
        $passed++
    } catch {
        Write-Host "❌ API Service ($ApiPort) - FAILED" -ForegroundColor Red
        $failed++
    }
}

Write-Host ""

if ($failed -eq 0) {
    Write-Host "✨ All services are healthy!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Access the application at:"
    Write-Host "  http://${Server}:${WebPort}" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "⚠️  Some services failed health checks ($failed failed, $passed passed)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Troubleshooting:"
    Write-Host "  - Check if services are running: ssh user@$Server 'systemctl status pokmanager-*'"
    Write-Host "  - Check firewall rules"
    Write-Host "  - View logs: ssh user@$Server 'journalctl -u pokmanager-web -n 50'"
    exit 1
}
