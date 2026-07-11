#!/usr/bin/env pwsh
# start-docker-tunnel.ps1
# Run this script whenever you need to expose your local Docker daemon to Azure.
# It starts the Cloudflare tunnel and prints the public URL.

$dockerCheckUrl = "http://localhost:2375/version"
$cloudflaredExe = "C:\Program Files (x86)\cloudflared\cloudflared.exe"

Write-Host "Checking Docker daemon is running on port 2375..." -ForegroundColor Cyan
try {
    $dockerVersion = Invoke-RestMethod -Uri $dockerCheckUrl -TimeoutSec 3
    Write-Host "Docker is running: $($dockerVersion.Version)" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker is not accessible on tcp://localhost:2375" -ForegroundColor Red
    Write-Host "Please enable: Docker Desktop -> Settings -> General -> 'Expose daemon on tcp://localhost:2375 without TLS'" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Starting Cloudflare tunnel..." -ForegroundColor Cyan
Write-Host "Look for a line like: https://xxxxx.trycloudflare.com" -ForegroundColor Yellow
Write-Host "Then update your Azure App Service setting 'Docker__HostUri' with that URL." -ForegroundColor Yellow
Write-Host ""

& $cloudflaredExe tunnel --url http://localhost:2377 --protocol http2
