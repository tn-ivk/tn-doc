param(
    [Parameter(Mandatory = $true)]
    [string]$CfgElevatorPath,

    [Parameter(Mandatory = $true)]
    [string]$OldCfgDir,

    [Parameter(Mandatory = $true)]
    [string]$NewCfgDir,

    [Parameter(Mandatory = $true)]
    [string]$LogDir
)

try {
    # Ensure log directory exists
    if (-not (Test-Path $LogDir)) {
        New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
    }

    $logFile = Join-Path $LogDir "cfg-elevator.log"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

    Add-Content -Path $logFile -Value "=== Config migration started at $timestamp ==="

    # Run migration only if all prerequisites are met
    if (-not (Test-Path $CfgElevatorPath)) {
        Add-Content -Path $logFile -Value "cfg-elevator not found at $CfgElevatorPath, skipping migration"
    }
    elseif (-not (Test-Path $OldCfgDir)) {
        Add-Content -Path $logFile -Value "Old config directory not found at $OldCfgDir, skipping migration"
    }
    elseif (-not (Test-Path $NewCfgDir)) {
        Add-Content -Path $logFile -Value "New config directory not found at $NewCfgDir, skipping migration"
    }
    else {
        # Run migrate
        Add-Content -Path $logFile -Value "--- Running: cfg-elevator migrate ---"
        $migrateOutput = & $CfgElevatorPath migrate -s $OldCfgDir -t $NewCfgDir --simple-output 2>&1
        Add-Content -Path $logFile -Value $migrateOutput

        # Run fix
        Add-Content -Path $logFile -Value "--- Running: cfg-elevator fix ---"
        $fixOutput = & $CfgElevatorPath fix -t $NewCfgDir --simple-output 2>&1
        Add-Content -Path $logFile -Value $fixOutput

        Add-Content -Path $logFile -Value "=== Config migration completed ==="
    }

    # Cleanup: always remove old config directory and cfg-elevator binary
    # (runs regardless of whether migration was performed)
    if (Test-Path $OldCfgDir) {
        Remove-Item -Path $OldCfgDir -Recurse -Force -ErrorAction SilentlyContinue
        Add-Content -Path $logFile -Value "Old config directory removed: $OldCfgDir"
    }

    if (Test-Path $CfgElevatorPath) {
        Remove-Item -Path $CfgElevatorPath -Force -ErrorAction SilentlyContinue
        Add-Content -Path $logFile -Value "cfg-elevator binary removed: $CfgElevatorPath"
    }
}
catch {
    # Log error but do not fail the installation
    $errorMsg = "Migration error: $_"
    if (Test-Path $LogDir) {
        $logFile = Join-Path $LogDir "cfg-elevator.log"
        Add-Content -Path $logFile -Value $errorMsg
    }
    Write-Warning $errorMsg
    exit 0
}
