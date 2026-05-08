#!/usr/bin/env bash
# Idempotent: creates the "cardtrader" store if it doesn't exist, then writes
# the current model.fga. Prints the user-secrets commands to run.
#
# Requires: fga CLI  https://github.com/openfga/cli/releases
#   macOS/Linux: place fga on $PATH  (brew install openfga/tap/fga)
#   Windows:     place tools/fga.exe in the repo root (gitignored)
#
# Usage (from repo root):
#   bash authz/load-model.sh
set -euo pipefail

API_URL="${OPENFGA_API_URL:-http://localhost:8080}"
STORE_NAME="cardtrader"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODEL_FILE="$SCRIPT_DIR/model.fga"

# Resolve fga binary (local tools/ dir takes priority over $PATH)
if [[ -f "$SCRIPT_DIR/../tools/fga.exe" ]]; then
  FGA="$SCRIPT_DIR/../tools/fga.exe"
elif command -v fga &>/dev/null; then
  FGA="fga"
else
  echo "ERROR: fga CLI not found."
  echo "  macOS/Linux: brew install openfga/tap/fga"
  echo "  Windows:     download fga.exe from https://github.com/openfga/cli/releases"
  echo "               and place at tools/fga.exe (gitignored)"
  exit 1
fi

# ── 1. Resolve or create the store ───────────────────────────────────────────
echo "Looking for store '$STORE_NAME' at $API_URL ..."
STORE_JSON=$("$FGA" store list --api-url "$API_URL" 2>/dev/null || echo "{}")
STORE_ID=$(echo "$STORE_JSON" | grep -oP '"id":"\K[^"]+(?=[^}]*"name":"'"$STORE_NAME"'")' | head -1 || true)

if [[ -z "$STORE_ID" ]]; then
  echo "Store not found — creating ..."
  CREATE_JSON=$("$FGA" store create --api-url "$API_URL" --name "$STORE_NAME")
  STORE_ID=$(echo "$CREATE_JSON" | grep -oP '"id":"\K[^"]+' | head -1)
  echo "Created store: $STORE_ID"
else
  echo "Found existing store: $STORE_ID"
fi

# ── 2. Write the authorization model ─────────────────────────────────────────
echo "Writing authorization model from $MODEL_FILE ..."
MODEL_JSON=$("$FGA" model write \
  --api-url "$API_URL" \
  --store-id "$STORE_ID" \
  --file "$MODEL_FILE")
MODEL_ID=$(echo "$MODEL_JSON" | grep -oP '"authorization_model_id":"\K[^"]+')

# ── 3. Print the user-secrets commands to run ────────────────────────────────
echo ""
echo "Run these commands to store the IDs in user-secrets:"
echo ""
echo "  dotnet user-secrets --project src/CardTrader.Web set \"OpenFga:StoreId\" \"$STORE_ID\""
echo "  dotnet user-secrets --project src/CardTrader.Web set \"OpenFga:AuthorizationModelId\" \"$MODEL_ID\""
