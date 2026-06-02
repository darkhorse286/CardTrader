#Requires -Version 7
# Starts Postgres + OpenFGA, applies DB migrations, loads the FGA model, then runs the web app.
#
# Usage (from repo root):
#   .\Start-Dev.ps1           — normal start
#   .\Start-Dev.ps1 -Reset    — wipe the Postgres volume and start fresh (required after docker-compose auth changes)
#
# Set CARDTRADER_DB_CONNECTION in your shell profile to avoid the password prompt:
#   $env:CARDTRADER_DB_CONNECTION = "Host=localhost;Port=5432;Database=cardtrader;Username=cardtrader;Password=<password>"
param([switch]$Reset)

$ErrorActionPreference = "Stop"

$API_URL    = "http://localhost:8080"
$STORE_NAME = "cardtrader"
$MODEL_FILE = Join-Path $PSScriptRoot "authz\model.fga"
$FGA        = Join-Path $PSScriptRoot "tools\fga.exe"

if (-not (Test-Path $FGA)) {
    Write-Error "fga CLI not found at tools\fga.exe.`nDownload from https://github.com/openfga/cli/releases"
    exit 1
}

# Resolve connection string
$connStr = $env:CARDTRADER_DB_CONNECTION
if (-not $connStr) {
    $securePwd = Read-Host "Postgres password for 'cardtrader'" -AsSecureString
    $plain = [System.Net.NetworkCredential]::new("", $securePwd).Password
    $connStr = "Host=localhost;Port=5432;Database=cardtrader;Username=cardtrader;Password=$plain"
}

# 1. Check for a local Postgres service that would shadow the Docker-mapped port
$localPg = Get-Service -Name '*postgresql*','*postgres*' -ErrorAction SilentlyContinue |
    Where-Object { $_.Status -eq 'Running' }
if ($localPg) {
    Write-Warning "A local PostgreSQL service ($($localPg.Name)) is running on this machine."
    Write-Warning "It may intercept connections on port 5432 before they reach the Docker container."
    Write-Warning "Stop it with: Stop-Service $($localPg.Name)"
    $answer = Read-Host "Continue anyway? [y/N]"
    if ($answer -ne 'y') { exit 1 }
}

# 2. Start containers (optionally wiping the Postgres volume for a clean slate)
if ($Reset) {
    Write-Host "Resetting Postgres volume..."
    docker compose -f docker/docker-compose.yml down -v
}
Write-Host "Starting containers..."
docker compose -f docker/docker-compose.yml up -d


# 3. Wait for OpenFGA health endpoint
Write-Host "Waiting for OpenFGA to be ready..."
$deadline = (Get-Date).AddSeconds(60)
do {
    Start-Sleep -Seconds 2
    $ok = $false
    try {
        $resp = Invoke-WebRequest -Uri "$API_URL/healthz" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        $ok = $resp.StatusCode -eq 200
    } catch { }
    if ((Get-Date) -gt $deadline) {
        Write-Error "OpenFGA did not become healthy within 60 seconds."
        exit 1
    }
} until ($ok)
Write-Host "OpenFGA is ready."

# 3. Apply DB migrations (generate SQL inside container — avoids SCRAM-SHA-256 host auth issue)
Write-Host "Applying migrations..."
$tmpDir = "C:\Temp"
if (-not (Test-Path $tmpDir)) { New-Item -ItemType Directory -Path $tmpDir | Out-Null }

$env:CARDTRADER_DB_CONNECTION = "dummy"
dotnet ef migrations script --project src/CardTrader.Infrastructure --context AppDbContext `
    --idempotent --output "$tmpDir\migration-app.sql" --no-build
dotnet ef migrations script --project src/CardTrader.Identity --context CardTraderIdentityDbContext `
    --idempotent --output "$tmpDir\migration-identity.sql" --no-build
$env:CARDTRADER_DB_CONNECTION = $connStr

docker cp "$tmpDir\migration-app.sql"      docker-postgres-1:/tmp/migration-app.sql
docker cp "$tmpDir\migration-identity.sql" docker-postgres-1:/tmp/migration-identity.sql
docker exec docker-postgres-1 psql -U cardtrader -d cardtrader -f /tmp/migration-app.sql
docker exec docker-postgres-1 psql -U cardtrader -d cardtrader -f /tmp/migration-identity.sql
Write-Host "Migrations applied."

# 4. Find or create the FGA store
Write-Host "Resolving FGA store '$STORE_NAME'..."
$storeJson = & $FGA store list --api-url $API_URL | ConvertFrom-Json
$store = $storeJson.stores | Where-Object { $_.name -eq $STORE_NAME } | Select-Object -First 1

if ($store) {
    $STORE_ID = $store.id
    Write-Host "Found existing store: $STORE_ID"
} else {
    Write-Host "Creating store..."
    $createJson = & $FGA store create --api-url $API_URL --name $STORE_NAME | ConvertFrom-Json
    $STORE_ID = $createJson.store.id
    Write-Host "Created store: $STORE_ID"
}

# 5. Write the authorization model
Write-Host "Writing authorization model..."
$modelJson = & $FGA model write --api-url $API_URL --store-id $STORE_ID --file $MODEL_FILE | ConvertFrom-Json
$MODEL_ID = $modelJson.authorization_model_id
Write-Host "Model ID: $MODEL_ID"

# 6. Set user secrets (for IDE / manual dotnet run use)
dotnet user-secrets --project src/CardTrader.Web set "OpenFga:StoreId" $STORE_ID
dotnet user-secrets --project src/CardTrader.Web set "OpenFga:AuthorizationModelId" $MODEL_ID
dotnet user-secrets --project src/CardTrader.Web set "ConnectionStrings:DefaultConnection" $connStr

# 7. Launch the web app — env vars take precedence over user secrets, guaranteeing the right values
Write-Host ""
Write-Host "Launching CardTrader..."
$env:ASPNETCORE_ENVIRONMENT              = "Development"
$env:ConnectionStrings__DefaultConnection = $connStr
$env:OpenFga__StoreId                    = $STORE_ID
$env:OpenFga__AuthorizationModelId       = $MODEL_ID
dotnet run --project src/CardTrader.Web
