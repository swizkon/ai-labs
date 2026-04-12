param(
    [string]$MixNumber = "0123456"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "POST /broadcast with mixNumber=$MixNumber"
$body = @{ mixNumber = $MixNumber } | ConvertTo-Json
$resp = Invoke-RestMethod -Uri "http://localhost:8080/broadcast" -Method Post -Body $body -ContentType "application/json"
Write-Host "Response:" ($resp | ConvertTo-Json -Compress)

Write-Host "Waiting for Loki to ingest logs..."
Start-Sleep -Seconds 10

$endNs = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() * 1000000
$startNs = [DateTimeOffset]::UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds() * 1000000
$query = '{container=~".+"} | json | mix_number="' + $MixNumber + '"'
$q = [uri]::EscapeDataString($query)
$url = "http://localhost:3100/loki/api/v1/query_range?query=$q&limit=100&start=$startNs&end=$endNs"

try {
    $r = Invoke-RestMethod -Uri $url -Method Get
    $count = 0
    foreach ($s in $r.data.result) {
        foreach ($v in $s.values) {
            $count++
        }
    }
    Write-Host "Loki query returned $count log line(s) for mix_number=$MixNumber"
    if ($count -eq 0) { exit 1 }
}
catch {
    Write-Warning "Loki query failed (is Loki up on :3100?): $_"
    exit 1
}

Write-Host "Verify OK."
exit 0
