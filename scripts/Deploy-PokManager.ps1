<#
.SYNOPSIS
    Deploys PokManager to a remote Linux server

.DESCRIPTION
    Builds, tests, and deploys the PokManager application to a remote server via SSH.
    Supports SSH as one user, then su to another user for deployment.
    Requires OpenSSH client (built into Windows 10+) or PuTTY's plink/pscp.

    PREREQUISITE: Run Initialize-PokManagerServer.ps1 first to configure passwordless sudo.
    After initial setup, this script runs completely automated without password prompts.

.PARAMETER User
    SSH username for the remote server

.PARAMETER Server
    IP address or hostname of the remote server

.PARAMETER Password
    SSH password (optional - will prompt if not provided)

.PARAMETER DeployUser
    User to switch to after SSH (using su). If not specified, uses SSH user.
    Example: SSH as "admin", then su to "pokuser"

.PARAMETER DeployPath
    Path on remote server where PokManager is located (default: /opt/PokManager)

.PARAMETER WebPort
    Web frontend port for verification (default: 5207)

.PARAMETER ApiPort
    API service port for verification (default: 5374)

.PARAMETER SkipTests
    Skip running tests before deployment

.PARAMETER SkipVerification
    Skip post-deployment verification

.EXAMPLE
    .\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216 -DeployUser pokuser

.EXAMPLE
    .\Deploy-PokManager.ps1 -User admin -Server 10.0.0.216 -DeployUser pokuser -Password "MyPassword123"

.EXAMPLE
    Deploy-PokManager admin MyPassword123 10.0.0.216 pokuser
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$User,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$Password,

    [Parameter(Mandatory=$true, Position=2)]
    [string]$Server,

    [Parameter(Mandatory=$false, Position=3)]
    [string]$DeployUser,

    [Parameter(Mandatory=$false)]
    [string]$DeployPath = "/opt/PokManager",

    [Parameter(Mandatory=$false)]
    [int]$WebPort = 5207,

    [Parameter(Mandatory=$false)]
    [int]$ApiPort = 5374,

    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,

    [Parameter(Mandatory=$false)]
    [switch]$SkipVerification
)

# Colors for output
function Write-Step {
    param([string]$Message)
    Write-Host "▶ " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Failure {
    param([string]$Message)
    Write-Host "✗ " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Blue
    Write-Host "  $Title" -ForegroundColor Blue
    Write-Host "================================================" -ForegroundColor Blue
    Write-Host ""
}

# Check if SSH is available
function Test-SSHAvailable {
    if (Get-Command ssh -ErrorAction SilentlyContinue) {
        return $true
    }

    Write-Failure "SSH client not found. Please install OpenSSH Client:"
    Write-Host "  - Windows 10+: Settings > Apps > Optional Features > OpenSSH Client"
    Write-Host "  - Or install PuTTY: https://www.putty.org/"
    return $false
}

# Wrap command with su if DeployUser is specified
function Get-WrappedCommand {
    param([string]$Command)

    if ($DeployUser) {
        # Escape single quotes in the command for su
        $escapedCmd = $Command -replace "'", "'\\''"
        # Use sudo su since pokuser has no password (configured in sudoers)
        return "sudo su - $DeployUser -c '$escapedCmd'"
    }

    return $Command
}

# Execute SSH command
function Invoke-SSHCommand {
    param(
        [string]$Command,
        [string]$Description,
        [switch]$AsSudo,
        [switch]$AsDeployUser
    )

    Write-Step $Description

    # Handle command wrapping:
    # - If AsSudo is specified, run as SSH user with sudo (don't wrap with deploy user)
    # - If AsDeployUser is specified, wrap with deploy user (adds "sudo su - pokuser -c")
    # - If DeployUser is set but no flags specified, wrap with deploy user
    # - Otherwise, run as SSH user without sudo
    if ($AsSudo) {
        # Run as SSH user with sudo, don't wrap with deploy user
        $Command = "sudo $Command"
    } elseif ($AsDeployUser -or $DeployUser) {
        # Run as deploy user using sudo su
        $Command = Get-WrappedCommand $Command
    }

    $sshCommand = "ssh"
    $sshArgs = @(
        "-o", "StrictHostKeyChecking=no",
        "-o", "UserKnownHostsFile=/dev/null",
        "-o", "LogLevel=ERROR",
        "$User@$Server",
        $Command
    )

    $result = & $sshCommand @sshArgs 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Command failed: $Description"
        Write-Host "  Error: $result" -ForegroundColor Red
        return $false
    }

    Write-Success "Completed: $Description"
    return $true
}

# Copy files to remote server
function Copy-ToRemote {
    param(
        [string]$LocalPath,
        [string]$RemotePath,
        [string]$Description
    )

    Write-Step $Description

    if ($DeployUser) {
        # Use tar to package files, SCP the archive, then extract as pokuser
        $tmpArchive = "/tmp/pokmanager-deploy-$(Get-Random).tar.gz"
        $localArchive = Join-Path $env:TEMP "pokmanager-deploy-$(Get-Random).tar.gz"
        
        # Create tar archive locally
        Write-Step "Creating deployment archive"
        $currentDir = Get-Location
        try {
            Set-Location $LocalPath
            
            # Use Windows tar.exe if available, otherwise use Compress-Archive
            $tarPath = "$env:SystemRoot\System32\tar.exe"
            if (Test-Path $tarPath) {
                & $tarPath -czf $localArchive *
                if ($LASTEXITCODE -ne 0) {
                    Write-Failure "Failed to create tar archive"
                    return $false
                }
            } else {
                # Fallback to Compress-Archive (creates zip)
                $localArchive = $localArchive -replace '\.tar\.gz$', '.zip'
                $tmpArchive = $tmpArchive -replace '\.tar\.gz$', '.zip'
                Compress-Archive -Path * -DestinationPath $localArchive -Force
            }
        } finally {
            Set-Location $currentDir
        }
        
        # SCP archive to /tmp as jd user
        $scpArgs = @(
            "-o", "StrictHostKeyChecking=no",
            "-o", "UserKnownHostsFile=/dev/null",
            "-o", "LogLevel=ERROR",
            $localArchive,
            "$User@${Server}:$tmpArchive"
        )

        Write-Step "Uploading archive to server"
        $result = & scp @scpArgs 2>&1

        # Cleanup local archive
        Remove-Item $localArchive -Force -ErrorAction SilentlyContinue

        if ($LASTEXITCODE -ne 0) {
            Write-Failure "Failed to copy archive to server"
            Write-Host "  Error: $result" -ForegroundColor Red
            return $false
        }

        # As pokuser, create target directory and clean it if it already exists
        # Only clean if it's a subdirectory (not the root deploy path)
        $normalizedRemotePath = $RemotePath.TrimEnd('/')
        $normalizedDeployPath = $DeployPath.TrimEnd('/')

        if ($normalizedRemotePath -ne $normalizedDeployPath) {
            # This is a subdirectory (like /opt/PokManager/web), safe to clean
            $mkdirCmd = "mkdir -p $RemotePath && rm -rf $RemotePath/*"
        } else {
            # This is the root deploy path, just create it without cleaning
            $mkdirCmd = "mkdir -p $RemotePath"
        }

        if (-not (Invoke-SSHCommand $mkdirCmd "Prepare target directory" -AsDeployUser)) {
            return $false
        }

        if ($tmpArchive -match '\.zip$') {
            $extractCmd = "cd $RemotePath && unzip -q $tmpArchive"
        } else {
            $extractCmd = "cd $RemotePath && tar -xzf $tmpArchive"
        }

        if (-not (Invoke-SSHCommand $extractCmd "Extract archive to $RemotePath" -AsDeployUser)) {
            return $false
        }

        # As jd, clean up the tar file
        $cleanupCmd = "rm -f $tmpArchive"
        Invoke-SSHCommand $cleanupCmd "Cleanup temp archive" | Out-Null

        Write-Success "Files deployed successfully"
    } else {
        # Copy directly as SSH user
        $scpArgs = @(
            "-o", "StrictHostKeyChecking=no",
            "-o", "UserKnownHostsFile=/dev/null",
            "-o", "LogLevel=ERROR",
            "-r",
            $LocalPath,
            "$User@${Server}:$RemotePath"
        )

        $result = & scp @scpArgs 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Failure "Failed to copy files"
            Write-Host "  Error: $result" -ForegroundColor Red
            return $false
        }

        # Fix ownership if needed
        $chownCmd = "chown -R $User`:$User $RemotePath"
        Invoke-SSHCommand $chownCmd "Set ownership" -AsSudo | Out-Null
    }

    Write-Success "Files copied successfully"
    return $true
}

# Main deployment function
function Start-Deployment {
    Write-Header "PokManager Deployment"

    Write-Host "SSH User: $User@$Server"
    if ($DeployUser) {
        Write-Host "Deploy User: $DeployUser (will use 'su')"
    }
    Write-Host "Deploy Path: $DeployPath"
    Write-Host "Web Port: $WebPort"
    Write-Host "API Port: $ApiPort"
    Write-Host ""

    # Check SSH availability
    if (-not (Test-SSHAvailable)) {
        return $false
    }

    # Step 1: Build application
    Write-Step "Building Web Frontend (Release configuration)..."
    $buildResult = dotnet build "src/Presentation/PokManager.Web/PokManager.Web.csproj" --configuration Release --verbosity minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Web build failed"
        return $false
    }

    Write-Step "Building API Service (Release configuration)..."
    $buildResult = dotnet build "src/Presentation/PokManager.ApiService/PokManager.ApiService.csproj" --configuration Release --verbosity minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Failure "API build failed"
        return $false
    }
    Write-Success "Build completed"

    # Step 2: Run tests (unless skipped)
    if (-not $SkipTests) {
        Write-Step "Running tests..."
        $testResult = dotnet test --configuration Release --no-build --verbosity minimal

        if ($LASTEXITCODE -ne 0) {
            Write-Failure "Tests failed"
            return $false
        }
        Write-Success "Tests passed"
    } else {
        Write-Host "⚠ Skipping tests" -ForegroundColor Yellow
    }

    # Step 3: Publish applications
    Write-Step "Publishing Web Frontend..."
    $webPublish = Join-Path $env:TEMP "pokmanager-web-publish"
    Remove-Item -Path $webPublish -Recurse -Force -ErrorAction SilentlyContinue

    dotnet publish "src/Presentation/PokManager.Web/PokManager.Web.csproj" `
        -c Release `
        -o $webPublish `
        --no-build `
        --verbosity minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Web publish failed"
        return $false
    }

    Write-Step "Publishing API Service..."
    $apiPublish = Join-Path $env:TEMP "pokmanager-api-publish"
    Remove-Item -Path $apiPublish -Recurse -Force -ErrorAction SilentlyContinue

    dotnet publish "src/Presentation/PokManager.ApiService/PokManager.ApiService.csproj" `
        -c Release `
        -o $apiPublish `
        --no-build `
        --verbosity minimal

    if ($LASTEXITCODE -ne 0) {
        Write-Failure "API publish failed"
        return $false
    }

    Write-Success "Applications published to temp directory"

    # Step 4: Test SSH connection
    Write-Step "Testing SSH connection to $Server..."
    if (-not (Invoke-SSHCommand "echo 'Connection successful'" "Test SSH connection")) {
        return $false
    }

    # Step 5: Create deployment directory on remote
    # Note: /opt/PokManager should already exist from Initialize-PokManagerServer.ps1
    Write-Step "Creating deployment directories..."
    $targetUser = if ($DeployUser) { $DeployUser } else { $User }

    if ($DeployUser) {
        # Create subdirectories as deploy user (assumes /opt/PokManager exists from setup)
        # pokuser has permissions on /opt/PokManager
        $createDirCmd = "mkdir -p $DeployPath/web $DeployPath/api $DeployPath/scripts"
        Invoke-SSHCommand $createDirCmd "Create subdirectories" -AsDeployUser | Out-Null
    } else {
        # Create as SSH user with sudo
        $createDirCmd = "mkdir -p $DeployPath/web $DeployPath/api $DeployPath/scripts"
        Invoke-SSHCommand $createDirCmd "Create deployment directories" -AsSudo | Out-Null
        
        # Set ownership
        $chownCmd = "chown -R $targetUser`:$targetUser $DeployPath"
        Invoke-SSHCommand $chownCmd "Set directory ownership" -AsSudo | Out-Null
    }

    # Step 6: Stop services
    Write-Step "Stopping services on remote server..."
    if ($DeployUser) {
        # Stop as deploy user with sudo (pokuser has passwordless sudo for systemctl)
        $stopWebCmd = "sudo systemctl stop pokmanager-web 2>/dev/null || true"
        $stopApiCmd = "sudo systemctl stop pokmanager-api 2>/dev/null || true"
        Invoke-SSHCommand $stopWebCmd "Stop Web service" -AsDeployUser | Out-Null
        Invoke-SSHCommand $stopApiCmd "Stop API service" -AsDeployUser | Out-Null
    } else {
        # Stop as SSH user with sudo
        $stopCmd = "systemctl stop pokmanager-web pokmanager-api 2>/dev/null || true"
        Invoke-SSHCommand $stopCmd "Stop services" -AsSudo | Out-Null
    }
    Write-Success "Services stopped"

    # Step 7: Copy files to remote
    Write-Step "Copying Web Frontend to remote server..."
    if (-not (Copy-ToRemote $webPublish "$DeployPath/web" "Copy Web Frontend")) {
        return $false
    }

    Write-Step "Copying API Service to remote server..."
    if (-not (Copy-ToRemote $apiPublish "$DeployPath/api" "Copy API Service")) {
        return $false
    }

    # Step 8: Copy scripts
    Write-Step "Copying deployment scripts..."
    if (Test-Path "scripts") {
        Copy-ToRemote "scripts" "$DeployPath/" "Copy scripts" | Out-Null
    }

    # Step 9: Set permissions
    Write-Step "Setting permissions..."
    if ($DeployUser) {
        # pokuser can chmod files in /opt/PokManager
        $permCmd = "sudo chmod -R 755 $DeployPath"
        Invoke-SSHCommand $permCmd "Set permissions" -AsDeployUser | Out-Null
        
        $scriptPermCmd = "sudo chmod +x $DeployPath/scripts/*.sh 2>/dev/null || true"
        Invoke-SSHCommand $scriptPermCmd "Set script permissions" -AsDeployUser | Out-Null
    } else {
        $permCmd = "chmod -R 755 $DeployPath && chmod +x $DeployPath/scripts/*.sh"
        if (-not (Invoke-SSHCommand $permCmd "Set permissions" -AsSudo)) {
            Write-Host "⚠ Could not set all permissions" -ForegroundColor Yellow
        }
    }

    # Step 10: Start services
    Write-Step "Starting services..."
    if ($DeployUser) {
        # pokuser has passwordless sudo for systemctl
        $startWebCmd = "sudo systemctl start pokmanager-web"
        $startApiCmd = "sudo systemctl start pokmanager-api"
        Invoke-SSHCommand $startWebCmd "Start Web service" -AsDeployUser | Out-Null
        Invoke-SSHCommand $startApiCmd "Start API service" -AsDeployUser | Out-Null
    } else {
        $startCmd = "systemctl start pokmanager-web && systemctl start pokmanager-api"
        if (-not (Invoke-SSHCommand $startCmd "Start services" -AsSudo)) {
            Write-Host "⚠ Services may not be configured with systemd yet" -ForegroundColor Yellow
            Write-Host "  See DEPLOYMENT.md for systemd service setup"
        }
    }

    # Step 11: Wait for services to start
    Write-Step "Waiting for services to start..."
    Start-Sleep -Seconds 10

    # Step 12: Verify deployment
    if (-not $SkipVerification) {
        Write-Step "Verifying deployment..."

        # Run remote verification script
        $verifyCmd = "cd $DeployPath && bash scripts/quick-check.sh localhost $WebPort $ApiPort"
        Invoke-SSHCommand $verifyCmd "Remote health check" -AsDeployUser | Out-Null

        # Local verification
        Write-Host ""
        Write-Host "Testing from local machine..." -ForegroundColor Cyan

        try {
            $webResponse = Invoke-WebRequest -Uri "http://${Server}:${WebPort}" -TimeoutSec 10 -UseBasicParsing
            if ($webResponse.StatusCode -eq 200) {
                Write-Success "Web Frontend is accessible"
            }
        } catch {
            Write-Failure "Web Frontend is not accessible: $_"
        }

        try {
            $apiResponse = Invoke-WebRequest -Uri "http://${Server}:${ApiPort}/health" -TimeoutSec 10 -UseBasicParsing
            if ($apiResponse.StatusCode -eq 200) {
                Write-Success "API Service health check passed"
            }
        } catch {
            Write-Host "⚠ API Service health check endpoint not accessible (may not be configured)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠ Skipping verification" -ForegroundColor Yellow
    }

    # Cleanup
    Write-Step "Cleaning up temporary files..."
    Remove-Item -Path $webPublish -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $apiPublish -Recurse -Force -ErrorAction SilentlyContinue
    Write-Success "Cleanup completed"

    # Success!
    Write-Header "Deployment Complete"
    Write-Host "✨ PokManager has been deployed to $Server" -ForegroundColor Green
    if ($DeployUser) {
        Write-Host "   Deployed as user: $DeployUser" -ForegroundColor Green
    }
    Write-Host ""
    Write-Host "Access the application at:"
    Write-Host "  Web Frontend: http://${Server}:${WebPort}" -ForegroundColor Cyan
    Write-Host "  API Service:  http://${Server}:${ApiPort}" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Check service status:"
    Write-Host "  ssh $User@$Server 'sudo systemctl status pokmanager-web'" -ForegroundColor Gray
    Write-Host "  ssh $User@$Server 'sudo systemctl status pokmanager-api'" -ForegroundColor Gray
    Write-Host ""

    return $true
}

# Run deployment
try {
    $success = Start-Deployment

    if ($success) {
        exit 0
    } else {
        Write-Host ""
        Write-Failure "Deployment failed. Please check the errors above."
        exit 1
    }
} catch {
    Write-Host ""
    Write-Failure "Deployment failed with exception: $_"
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
