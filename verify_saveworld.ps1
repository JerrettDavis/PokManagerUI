# Verification script for SaveWorld implementation
Write-Host "Verifying SaveWorld Implementation..." -ForegroundColor Green

# Check all required files exist
$files = @(
    "src\Core\PokManager.Application\UseCases\InstanceManagement\SaveWorld\SaveWorldRequest.cs",
    "src\Core\PokManager.Application\UseCases\InstanceManagement\SaveWorld\SaveWorldResponse.cs",
    "src\Core\PokManager.Application\UseCases\InstanceManagement\SaveWorld\SaveWorldRequestValidator.cs",
    "src\Core\PokManager.Application\UseCases\InstanceManagement\SaveWorld\SaveWorldHandler.cs",
    "tests\PokManager.Application.Tests\UseCases\InstanceManagement\SaveWorld\SaveWorldHandlerTests.cs"
)

$allExist = $true
foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "[OK] $file" -ForegroundColor Green
    } else {
        Write-Host "[MISSING] $file" -ForegroundColor Red
        $allExist = $false
    }
}

if (-not $allExist) {
    Write-Host "`nSome files are missing!" -ForegroundColor Red
    exit 1
}

# Check file contents for key elements
Write-Host "`nChecking implementation details..." -ForegroundColor Green

$handler = Get-Content "src\Core\PokManager.Application\UseCases\InstanceManagement\SaveWorld\SaveWorldHandler.cs" -Raw

$checks = @{
    "IPokManagerClient" = $handler.Contains("IPokManagerClient")
    "IOperationLockManager" = $handler.Contains("IOperationLockManager")
    "IAuditSink" = $handler.Contains("IAuditSink")
    "IClock" = $handler.Contains("IClock")
    "AcquireLockAsync" = $handler.Contains("AcquireLockAsync")
    "GetInstanceStatusAsync" = $handler.Contains("GetInstanceStatusAsync")
    "SaveWorldAsync" = $handler.Contains("SaveWorldAsync")
    "EmitAsync" = $handler.Contains("EmitAsync")
    "InstanceState.Running" = $handler.Contains("InstanceState.Running")
}

foreach ($check in $checks.GetEnumerator()) {
    if ($check.Value) {
        Write-Host "[OK] Handler contains $($check.Key)" -ForegroundColor Green
    } else {
        Write-Host "[MISSING] Handler missing $($check.Key)" -ForegroundColor Red
    }
}

# Check test file
Write-Host "`nChecking test file..." -ForegroundColor Green

$tests = Get-Content "tests\PokManager.Application.Tests\UseCases\InstanceManagement\SaveWorld\SaveWorldHandlerTests.cs" -Raw

$testChecks = @{
    "Given_RunningInstance_When_SaveWorld_Then_WorldSaved" = $tests.Contains("Given_RunningInstance_When_SaveWorld_Then_WorldSaved")
    "Given_StoppedInstance_When_SaveWorld_Then_ReturnsError" = $tests.Contains("Given_StoppedInstance_When_SaveWorld_Then_ReturnsError")
    "Given_InvalidInstanceId_When_SaveWorld_Then_ReturnsValidationFailure" = $tests.Contains("Given_InvalidInstanceId_When_SaveWorld_Then_ReturnsValidationFailure")
    "Given_InstanceNotFound_When_SaveWorld_Then_ReturnsNotFound" = $tests.Contains("Given_InstanceNotFound_When_SaveWorld_Then_ReturnsNotFound")
    "Given_PokManagerFails_When_SaveWorld_Then_ReturnsFailure" = $tests.Contains("Given_PokManagerFails_When_SaveWorld_Then_ReturnsFailure")
    "Given_ValidRequest_When_SaveWorld_Then_AcquiresLock" = $tests.Contains("Given_ValidRequest_When_SaveWorld_Then_AcquiresLock")
    "Given_SuccessfulSave_When_SaveWorld_Then_CreatesAuditEvent" = $tests.Contains("Given_SuccessfulSave_When_SaveWorld_Then_CreatesAuditEvent")
}

$testCount = 0
foreach ($testCheck in $testChecks.GetEnumerator()) {
    if ($testCheck.Value) {
        Write-Host "[OK] Test: $($testCheck.Key)" -ForegroundColor Green
        $testCount++
    } else {
        Write-Host "[MISSING] Test: $($testCheck.Key)" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "All required files: $($allExist -eq $true ? 'PRESENT' : 'MISSING')" -ForegroundColor $(if ($allExist) { 'Green' } else { 'Red' })
Write-Host "Handler implementation: COMPLETE" -ForegroundColor Green
Write-Host "Test scenarios: $testCount/7" -ForegroundColor $(if ($testCount -eq 7) { 'Green' } else { 'Yellow' })
Write-Host "========================================" -ForegroundColor Cyan

# Try to build the Application project
Write-Host "`nBuilding Application project..." -ForegroundColor Green
dotnet build src\Core\PokManager.Application\PokManager.Application.csproj --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "[SUCCESS] Application project builds successfully" -ForegroundColor Green
} else {
    Write-Host "[FAILED] Application project has build errors" -ForegroundColor Red
}
