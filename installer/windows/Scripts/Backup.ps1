param(
    [Parameter(Mandatory = $true)]
    [string]$InstallDir,

    [Parameter(Mandatory = $true)]
    [string]$BackupDir,

    [Parameter(Mandatory = $true)]
    [string]$Version
)

try {
    # Only backup if install directory exists and has content
    if (-not (Test-Path $InstallDir)) {
        Write-Host "Install directory not found, skipping backup"
        exit 0
    }

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupName = "${timestamp}_TN_Doc_before-update-to_v${Version}.zip"
    $backupPath = Join-Path $BackupDir $backupName

    # Create backup directory if it does not exist
    if (-not (Test-Path $BackupDir)) {
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    }

    # Create a temporary copy excluding logs
    $tempDir = Join-Path $env:TEMP "TN_Doc_backup_$timestamp"
    Copy-Item -Path $InstallDir -Destination $tempDir -Recurse -Force

    # Remove log directories and log files from the temporary copy
    Get-ChildItem -Path $tempDir -Directory -Filter "logs" -Recurse |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path $tempDir -File -Filter "*.log" -Recurse |
        Remove-Item -Force -ErrorAction SilentlyContinue

    # Create ZIP archive
    Compress-Archive -Path "$tempDir\*" -DestinationPath $backupPath -Force

    # Cleanup temporary directory
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue

    $sizeMB = (Get-Item $backupPath).Length / 1MB
    Write-Host ("Backup created: {0} ({1:N1} MB)" -f $backupPath, $sizeMB)
}
catch {
    Write-Warning "Backup failed: $_"
    # Do not fail the installation if backup fails
    exit 0
}
