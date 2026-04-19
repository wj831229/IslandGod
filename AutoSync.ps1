$projectPath = "C:\mgod\IslandGod"
$checkIntervalSeconds = 10

Set-Location $projectPath

Write-Host "AutoSync started. Checking every $checkIntervalSeconds seconds..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop." -ForegroundColor Cyan

while ($true) {
    Start-Sleep -Seconds $checkIntervalSeconds

    $status = git status --porcelain
    if ($status) {
        Write-Host "[$( Get-Date -Format 'HH:mm:ss')] Changes detected. Uploading..." -ForegroundColor Yellow
        git add .
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        git commit -m "Auto save: $timestamp"
        git push origin master
        Write-Host "[$( Get-Date -Format 'HH:mm:ss')] Done!" -ForegroundColor Green
    }
}
