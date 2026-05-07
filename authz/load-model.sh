#!/usr/bin/env bash
# Idempotent: creates the "cardtrader" store if it doesn't exist, then writes
# the current model.fga. Prints the IDs to paste into appsettings.Development.json.
#
# Requires: curl, jq, fga CLI (https://github.com/openfga/cli)
#   brew install openfga/tap/fga
#   or: go install github.com/openfga/cli/cmd/fga@latest
set -euo pipefail

API_URL="${OPENFGA_API_URL:-http://localhost:8080}"
STORE_NAME="cardtrader"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODEL_FILE="$SCRIPT_DIR/model.fga"

# ── 1. Resolve or create the store ───────────────────────────────────────────
echo "Looking for store '$STORE_NAME' at $API_URL ..."
STORE_ID=$(curl -sf "$API_URL/stores" \
  | jq -r --arg name "$STORE_NAME" '.stores[]? | select(.name==$name) | .id' \
  | head -1)

if [[ -z "$STORE_ID" ]]; then
  echo "Store not found — creating ..."
  STORE_ID=$(curl -sf -X POST "$API_URL/stores" \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"$STORE_NAME\"}" \
    | jq -r '.id')
  echo "Created store: $STORE_ID"
else
  echo "Found existing store: $STORE_ID"
fi

# ── 2. Write the authorization model ─────────────────────────────────────────
if ! command -v fga &> /dev/null; then
  echo ""
  echo "ERROR: 'fga' CLI not found. Install it to write the model:"
  echo "  brew install openfga/tap/fga"
  echo "  or: https://github.com/openfga/cli#installation"
  exit 1
fi

echo "Writing authorization model from $MODEL_FILE ..."
MODEL_ID=$(fga model write \
  --api-url "$API_URL" \
  --store-id "$STORE_ID" \
  --file "$MODEL_FILE" \
  | jq -r '.authorization_model_id')

# ── 3. Print the values to add to appsettings.Development.json ───────────────
echo ""
echo "Add to src/CardTrader.Web/appsettings.Development.json:"
echo ""
cat <<JSON
  "OpenFga": {
    "ApiUrl": "$API_URL",
    "StoreId": "$STORE_ID",
    "AuthorizationModelId": "$MODEL_ID"
  }
JSON
