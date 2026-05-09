#Requires -Version 7
# Starts Postgres + OpenFGA, loads the FGA model, sets user-secrets, then runs the web app.
#
# Usage (from repo root):
#   .\Start-Dev.ps1

$ErrorActionPreference = "Stop"

$API_URL    = "http://localhost:8080"
$STORE_NAME = "cardtrader"
$MODEL_FILE = Join-Path $PSScriptRoot "authz\model.fga"
$FGA        = Join-Path $PSScriptRoot "tools\fga.exe"

if (-not (Test-Path $FGA)) {
    Write-Error "fga CLI not found at tools\fga.exe.`nDownload from https://github.com/openfga/cli/releases"
    exit 1
}

# 1. Start containers
Write-Host "Starting containers..."
docker compose -f docker/docker-compose.yml up -d

# 2. Wait for OpenFGA health endpoint
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

# 3. Find or create the store
Write-Host "Resolving FGA store '$STORE_NAME'..."
$storeJson = & $FGA store list --api-url $API_URL | ConvertFrom-Json
$store = $storeJson.stores | Where-Object { $_.name -eq $STORE_NAME } | Select-Object -First 1

if ($store) {
    $STORE_ID = $store.id
    Write-Host "Found existing store: $STORE_ID"
} else {
    Write-Host "Creating store..."
    $createJson = & $FGA store create --api-url $API_URL --name $STORE_NAME | ConvertFrom-Json
    $STORE_ID = $createJson.id
    Write-Host "Created store: $STORE_ID"
}

# 4. Write the authorization model
Write-Host "Writing authorization model..."
$modelJson = & $FGA model write --api-url $API_URL --store-id $STORE_ID --file $MODEL_FILE | ConvertFrom-Json
$MODEL_ID = $modelJson.authorization_model_id
Write-Host "Model ID: $MODEL_ID"

# 5. Set user secrets
Write-Host "Setting user secrets..."
dotnet user-secrets --project src/CardTrader.Web set "OpenFga:StoreId" $STORE_ID
dotnet user-secrets --project src/CardTrader.Web set "OpenFga:AuthorizationModelId" $MODEL_ID
$connStr = $env:CARDTRADER_DB_CONNECTION
if (-not $connStr) {
    $pwd = Read-Host "Postgres password for 'cardtrader'" -AsSecureString
    $plain = [System.Net.NetworkCredential]::new("", $pwd).Password
    $connStr = "Host=localhost;Port=5432;Database=cardtrader;Username=cardtrader;Password=$plain"
}
dotnet user-secrets --project src/CardTrader.Web set "ConnectionStrings:DefaultConnection" $connStr

# 6. Start the web app
Write-Host ""
Write-Host "Launching CardTrader..."
dotnet run --project src/CardTrader.Web
