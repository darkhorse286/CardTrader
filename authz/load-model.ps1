#Requires -Version 7
# Idempotent: creates the "cardtrader" store if it doesn't exist, then writes
# the current model.fga. Prints the user-secrets commands to run.
#
# Usage (from repo root):
#   .\authz\load-model.ps1

$ErrorActionPreference = "Stop"

$API_URL    = $env:OPENFGA_API_URL ?? "http://localhost:8080"
$STORE_NAME = "cardtrader"
$SCRIPT_DIR = $PSScriptRoot
$MODEL_FILE = Join-Path $SCRIPT_DIR "model.fga"

# Resolve fga binary (local tools/ takes priority)
$localFga = Join-Path $SCRIPT_DIR "..\tools\fga.exe"
if (Test-Path $localFga) {
    $FGA = (Resolve-Path $localFga).Path
} elseif (Get-Command fga -ErrorAction SilentlyContinue) {
    $FGA = "fga"
} else {
    Write-Error "fga CLI not found.`n  Download fga.exe from https://github.com/openfga/cli/releases and place at tools\fga.exe"
    exit 1
}

# 1. Find or create the store
Write-Host "Looking for store '$STORE_NAME' at $API_URL ..."
$storeJson = & $FGA store list --api-url $API_URL | ConvertFrom-Json
$store = $storeJson.stores | Where-Object { $_.name -eq $STORE_NAME } | Select-Object -First 1

if ($store) {
    $STORE_ID = $store.id
    Write-Host "Found existing store: $STORE_ID"
} else {
    Write-Host "Store not found — creating ..."
    $createJson = & $FGA store create --api-url $API_URL --name $STORE_NAME | ConvertFrom-Json
    $STORE_ID = $createJson.id
    Write-Host "Created store: $STORE_ID"
}

# 2. Write the authorization model
Write-Host "Writing authorization model from $MODEL_FILE ..."
$modelJson = & $FGA model write --api-url $API_URL --store-id $STORE_ID --file $MODEL_FILE | ConvertFrom-Json
$MODEL_ID = $modelJson.authorization_model_id

# 3. Print the user-secrets commands
Write-Host ""
Write-Host "Run these commands to store the IDs in user-secrets:"
Write-Host ""
Write-Host "  dotnet user-secrets --project src/CardTrader.Web set `"OpenFga:StoreId`" `"$STORE_ID`""
Write-Host "  dotnet user-secrets --project src/CardTrader.Web set `"OpenFga:AuthorizationModelId`" `"$MODEL_ID`""
