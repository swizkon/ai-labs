# Starts the docker-compose stack, rebuilding all images and recreating all containers.
# Usage: ./start.ps1 [-Detached] [-NoCache]
param(
    [switch]$Detached,
    [switch]$NoCache
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

docker compose down --remove-orphans
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$buildArgs = @('compose', 'build', '--pull')
if ($NoCache) { $buildArgs += '--no-cache' }
docker @buildArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$upArgs = @('compose', 'up', '--force-recreate', '--remove-orphans')
if ($Detached) { $upArgs += '-d' }
docker @upArgs
exit $LASTEXITCODE
