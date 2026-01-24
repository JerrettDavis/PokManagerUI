<#
.SYNOPSIS
    First-time setup wizard for PokManager deployment server

.DESCRIPTION
    Interactive wizard that configures the remote server for headless PokManager deployment.
    Sets up:
    - User accounts and permissions
    - Passwordless sudo for deployment operations
    - systemd services
    - Directory structure
    - Firewall rules

    SECURITY NOTE: This wizard prompts for your sudo password ONLY during setup.
    The password is used temporarily to configure the server and is NEVER saved to disk.
    After setup completes, the password is cleared from memory.
    All subsequent deployments use passwordless sudo for authorized commands only.

.PARAMETER User
    SSH username for the remote server (typically admin user with sudo access)

.PARAMETER Server
    IP address or hostname of the remote server

.PARAMETER Password
    SSH password (optional - will prompt if not provided)

.PARAMETER DeployUser
    User account to create/use for running PokManager (default: pokuser)

.PARAMETER DeployPath
    Path on remote server where PokManager will be installed (default: /opt/PokManager)

.PARAMETER WebPort
    Web frontend port (default: 5207)

.PARAMETER ApiPort
    API service port (default: 5374)

.EXAMPLE
    .\Initialize-PokManagerServer.ps1 -User admin -Server 10.0.0.216

.EXAMPLE
    .\Initialize-PokManagerServer.ps1 -User admin -Server 10.0.0.216 -DeployUser pokuser
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$User,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$Password,

    [Parameter(Mandatory=$true, Position=2)]
    [string]$Server,

    [Parameter(Mandatory=$false)]
    [string]$DeployUser = "pokuser",

    [Parameter(Mandatory=$false)]
    [string]$DeployPath = "/opt/PokManager",

    [Parameter(Mandatory=$false)]
    [int]$WebPort = 5207,

    [Parameter(Mandatory=$false)]
    [int]$ApiPort = 5374
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

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ " -ForegroundColor Cyan -NoNewline
    Write-Host $Message
}

# Script-level variable for sudo password
$script:SudoPassword = $null

# Execute SSH command
function Invoke-SSHCommand {
    param(
        [string]$Command,
        [string]$Description,
        [switch]$UseSudo,
        [switch]$IgnoreErrors
    )

    if ($Description) {
        Write-Step $Description
    }

    # Wrap command with sudo if needed
    if ($UseSudo -and $script:SudoPassword) {
        # Use -S to read password from stdin, redirect stderr of echo to suppress it
        $escapedPassword = $script:SudoPassword -replace "'", "'\\''"
        $Command = "echo '$escapedPassword' 2>/dev/null | sudo -S bash -c `"$Command`""
    } elseif ($UseSudo) {
        $Command = "sudo $Command"
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

    if ($LASTEXITCODE -ne 0 -and -not $IgnoreErrors) {
        Write-Failure "Command failed: $Description"
        Write-Host "  Error: $result" -ForegroundColor Red
        return $false
    }

    if ($Description) {
        Write-Success "Completed: $Description"
    }
    return $result
}

# Main setup function
function Start-ServerSetup {
    Write-Header "PokManager Server Setup Wizard"

    Write-Info "This wizard will configure your server for PokManager deployment."
    Write-Host ""
    Write-Host "Configuration:" -ForegroundColor Cyan
    Write-Host "  Server:       $Server"
    Write-Host "  SSH User:     $User"
    Write-Host "  Deploy User:  $DeployUser"
    Write-Host "  Deploy Path:  $DeployPath"
    Write-Host "  Web Port:     $WebPort"
    Write-Host "  API Port:     $ApiPort"
    Write-Host ""

    # Confirm setup
    $confirm = Read-Host "Continue with server setup? (y/N)"
    if ($confirm -ne "y" -and $confirm -ne "Y") {
        Write-Host "Setup cancelled." -ForegroundColor Yellow
        return $false
    }

    # Prompt for sudo password (used only during setup, never saved to disk)
    Write-Host ""
    Write-Info "This setup requires sudo access on the remote server."
    Write-Info "Your password will be used ONLY during setup and will NOT be saved."
    $script:SudoPassword = Read-Host "Enter sudo password for $User@$Server" -AsSecureString
    $script:SudoPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($script:SudoPassword)
    )

    # Step 1: Test SSH connection
    Write-Header "Step 1: Testing SSH Connection"
    if (-not (Invoke-SSHCommand "echo 'SSH connection successful'" "Connect to $Server")) {
        return $false
    }

    # Step 2: Install .NET Runtime
    Write-Header "Step 2: Installing .NET Runtime"

    # Check if .NET is already installed
    $dotnetCheck = Invoke-SSHCommand "dotnet --version" "" -IgnoreErrors

    if ($LASTEXITCODE -ne 0) {
        Write-Info ".NET runtime not found. Installing .NET 10 runtime..."

        # Add Microsoft package repository and install .NET
        $installDotnet = @"
# Add Microsoft package signing key and repository
wget https://packages.microsoft.com/config/ubuntu/\$(lsb_release -rs)/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb && \
dpkg -i /tmp/packages-microsoft-prod.deb && \
rm /tmp/packages-microsoft-prod.deb && \
# Update package list
apt-get update && \
# Install .NET Runtime 10.0
apt-get install -y dotnet-runtime-10.0 aspnetcore-runtime-10.0
"@

        if (-not (Invoke-SSHCommand $installDotnet "Install .NET Runtime" -UseSudo)) {
            Write-Failure ".NET installation failed"
            return $false
        }

        Write-Success ".NET 10 runtime installed"
    } else {
        Write-Success ".NET runtime already installed (version: $dotnetCheck)"
    }

    # Step 3: Create deploy user if it doesn't exist
    Write-Header "Step 3: Creating Deploy User"
    $userExists = Invoke-SSHCommand "id -u $DeployUser 2>/dev/null" "" -IgnoreErrors

    if ($LASTEXITCODE -ne 0) {
        Write-Info "User $DeployUser does not exist. Creating..."

        if (-not (Invoke-SSHCommand "useradd -m -s /bin/bash $DeployUser" "Create user $DeployUser" -UseSudo)) {
            return $false
        }

        Write-Success "User $DeployUser created"
    } else {
        Write-Success "User $DeployUser already exists"
    }

    # Step 4: Create deployment directory
    Write-Header "Step 4: Creating Deployment Directory"
    if (-not (Invoke-SSHCommand "mkdir -p $DeployPath/web $DeployPath/api $DeployPath/scripts" "Create directory structure" -UseSudo)) {
        return $false
    }

    if (-not (Invoke-SSHCommand "chown -R $DeployUser`:$DeployUser $DeployPath" "Set directory ownership" -UseSudo)) {
        return $false
    }

    # Step 5: Configure sudoers for passwordless sudo
    Write-Header "Step 5: Configuring Passwordless Sudo"

    Write-Info "Configuring passwordless sudo for deployment operations..."
    Write-Info "This allows $DeployUser to run specific commands without password prompts."

    # Create sudoers configuration
    $sudoersContent = @"
# PokManager deployment permissions
# Allow $DeployUser to run deployment-related commands without password

# systemd service management
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl start pokmanager-web
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl stop pokmanager-web
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl restart pokmanager-web
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl reload pokmanager-web
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl status pokmanager-web
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl start pokmanager-api
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl stop pokmanager-api
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl restart pokmanager-api
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl reload pokmanager-api
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl status pokmanager-api
$DeployUser ALL=(ALL) NOPASSWD: /bin/systemctl daemon-reload

# Docker management for PokManager
$DeployUser ALL=(ALL) NOPASSWD: /usr/bin/docker ps
$DeployUser ALL=(ALL) NOPASSWD: /usr/bin/docker-compose up *
$DeployUser ALL=(ALL) NOPASSWD: /usr/bin/docker-compose down *
$DeployUser ALL=(ALL) NOPASSWD: /usr/bin/docker-compose restart *
$DeployUser ALL=(ALL) NOPASSWD: /usr/bin/docker-compose pull *
$DeployUser ALL=(ALL) NOPASSWD: /usr/bin/docker restart pokmanager*
$DeployUser ALL=(ALL) NOPASSWD: /usr/bin/docker stop pokmanager*
$DeployUser ALL=(ALL) NOPASSWD: /usr/bin/docker start pokmanager*

# File operations
$DeployUser ALL=(ALL) NOPASSWD: /bin/chown -R $DeployUser\:$DeployUser $DeployPath*
$DeployUser ALL=(ALL) NOPASSWD: /bin/chmod * $DeployPath*

# Allow SSH user to su to deploy user without password
$User ALL=(ALL) NOPASSWD: /bin/su - $DeployUser*
"@

    # Write sudoers file safely using visudo
    $tmpFile = "/tmp/pokmanager-sudoers"
    $escapedContent = $sudoersContent -replace '"', '\"' -replace '`', '\`' -replace '\$', '\$'

    $createSudoersCmd = @"
echo '$escapedContent' | tee $tmpFile > /dev/null && \
visudo -c -f $tmpFile && \
cp $tmpFile /etc/sudoers.d/pokmanager && \
chmod 0440 /etc/sudoers.d/pokmanager && \
rm $tmpFile
"@

    if (-not (Invoke-SSHCommand $createSudoersCmd "Create sudoers configuration" -UseSudo)) {
        return $false
    }

    Write-Success "Passwordless sudo configured"

    # Step 6: Create systemd service files
    Write-Header "Step 6: Creating systemd Services"

    # Web service
    $webServiceContent = @"
[Unit]
Description=PokManager Web Frontend
After=network.target

[Service]
Type=simple
User=$DeployUser
WorkingDirectory=$DeployPath/web
ExecStart=/usr/bin/dotnet $DeployPath/web/PokManager.Web.dll
Restart=on-failure
RestartSec=10
SyslogIdentifier=pokmanager-web

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:$WebPort

[Install]
WantedBy=multi-user.target
"@

    # API service
    $apiServiceContent = @"
[Unit]
Description=PokManager API Service
After=network.target

[Service]
Type=simple
User=$DeployUser
WorkingDirectory=$DeployPath/api
ExecStart=/usr/bin/dotnet $DeployPath/api/PokManager.ApiService.dll
Restart=on-failure
RestartSec=10
SyslogIdentifier=pokmanager-api

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:$ApiPort

[Install]
WantedBy=multi-user.target
"@

    # Create web service
    $escapedWebService = $webServiceContent -replace '"', '\"' -replace '`', '\`' -replace '\$', '\$'
    $createWebServiceCmd = "echo '$escapedWebService' | tee /etc/systemd/system/pokmanager-web.service > /dev/null"

    if (-not (Invoke-SSHCommand $createWebServiceCmd "Create Web service file" -UseSudo)) {
        return $false
    }

    # Create API service
    $escapedApiService = $apiServiceContent -replace '"', '\"' -replace '`', '\`' -replace '\$', '\$'
    $createApiServiceCmd = "echo '$escapedApiService' | tee /etc/systemd/system/pokmanager-api.service > /dev/null"

    if (-not (Invoke-SSHCommand $createApiServiceCmd "Create API service file" -UseSudo)) {
        return $false
    }

    # Reload systemd and enable services
    if (-not (Invoke-SSHCommand "systemctl daemon-reload" "Reload systemd" -UseSudo)) {
        return $false
    }

    if (-not (Invoke-SSHCommand "systemctl enable pokmanager-web pokmanager-api" "Enable services" -UseSudo)) {
        return $false
    }

    Write-Success "systemd services created and enabled"

    # Step 7: Configure firewall
    Write-Header "Step 7: Configuring Firewall"

    $firewallInstalled = Invoke-SSHCommand "command -v ufw" "" -IgnoreErrors

    if ($LASTEXITCODE -eq 0) {
        Write-Info "Configuring ufw firewall..."

        Invoke-SSHCommand "ufw allow $WebPort/tcp" "Allow Web port $WebPort" -UseSudo | Out-Null
        Invoke-SSHCommand "ufw allow $ApiPort/tcp" "Allow API port $ApiPort" -UseSudo | Out-Null

        Write-Success "Firewall configured"
    } else {
        Write-Host "⚠ ufw not installed. Skipping firewall configuration." -ForegroundColor Yellow
        Write-Info "Manually ensure ports $WebPort and $ApiPort are accessible."
    }

    # Step 8: Verify configuration
    Write-Header "Step 8: Verifying Configuration"

    Write-Step "Checking user permissions..."
    $suTest = Invoke-SSHCommand "su - $DeployUser -c 'whoami'" "" -UseSudo -IgnoreErrors
    if ($suTest -eq $DeployUser) {
        Write-Success "User switching works correctly"
    } else {
        Write-Failure "User switching may have issues"
    }

    Write-Step "Checking directory permissions..."
    $dirTest = Invoke-SSHCommand "su - $DeployUser -c 'test -w $DeployPath && echo OK'" "" -UseSudo -IgnoreErrors
    if ($dirTest -eq "OK") {
        Write-Success "Directory permissions correct"
    } else {
        Write-Failure "Directory permissions may have issues"
    }

    # Success!
    Write-Header "Setup Complete"
    Write-Host "✨ Server is now configured for PokManager deployment!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Deploy PokManager:" -ForegroundColor Gray
    Write-Host "     .\scripts\Deploy-PokManager.ps1 -User $User -Server $Server -DeployUser $DeployUser" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Test deployment:" -ForegroundColor Gray
    Write-Host "     .\scripts\Test-PokManager.ps1 -Server $Server" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. View in browser:" -ForegroundColor Gray
    Write-Host "     http://${Server}:${WebPort}" -ForegroundColor Gray
    Write-Host ""

    return $true
}

# Run setup
try {
    $success = Start-ServerSetup

    if ($success) {
        # Clear password from memory for security
        $script:SudoPassword = $null
        exit 0
    } else {
        Write-Host ""
        Write-Failure "Server setup failed. Please check the errors above."
        # Clear password from memory for security
        $script:SudoPassword = $null
        exit 1
    }
} catch {
    Write-Host ""
    Write-Failure "Server setup failed with exception: $_"
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    # Clear password from memory for security
    $script:SudoPassword = $null
    exit 1
}

# Note: Password is only used during this setup wizard and is never saved to disk.
# After setup completes, all operations use passwordless sudo for authorized commands.
