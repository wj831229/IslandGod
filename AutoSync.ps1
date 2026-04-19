$projectPath = "C:\mgod\IslandGod"
$debounceSeconds = 10

$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $projectPath
$watcher.IncludeSubdirectories = $true
$watcher.EnableRaisingEvents = $true
$watcher.Filter = "*.*"

$global:lastTrigger = [DateTime]::MinValue

function Sync-ToGitHub {
    Write-Host "[$( Get-Date -Format 'HH:mm:ss')] Uploading to GitHub..." -ForegroundColor Yellow
    Set-Location $projectPath
    git add .
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    git commit -m "Auto save: $timestamp"
    git push origin master
    Write-Host "[$( Get-Date -Format 'HH:mm:ss')] Done!" -ForegroundColor Green
}

$action = {
    $path = $Event.SourceEventArgs.FullPath
    if ($path -match "\\Library\\" -or $path -match "\\Temp\\" -or $path -match "\\Logs\\") { return }
    $global:lastTrigger = [DateTime]::Now
}

Register-ObjectEvent $watcher "Changed" -Action $action | Out-Null
Register-ObjectEvent $watcher "Created" -Action $action | Out-Null
Register-ObjectEvent $watcher "Deleted" -Action $action | Out-Null

Write-Host "AutoSync started. Save in Unity to trigger upload." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop." -ForegroundColor Cyan

while ($true) {
    Start-Sleep -Seconds 1
    if ($global:lastTrigger -ne [DateTime]::MinValue) {
        $elapsed = ([DateTime]::Now - $global:lastTrigger).TotalSeconds
        if ($elapsed -ge $debounceSeconds) {
            $global:lastTrigger = [DateTime]::MinValue
            try {
                Sync-ToGitHub
            } catch {
                Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}
