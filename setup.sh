#!/usr/bin/env bash
# One-command bootstrap for local Operum setup (macOS/Linux/Git Bash).
#
# Creates .env from .env.example (if missing) and fills in every
# __GENERATE__ placeholder with a freshly generated secret: JWT signing key,
# Postgres password, admin login password, Grafana admin password, and a
# VAPID keypair for web push (via `npx web-push`, requires Node).
#
# Re-running is safe: values that are already set are left untouched.
#
# Usage:
#   ./setup.sh [--dev] [--up] [--force]
#
#   --dev    also write backend/src/Operum.API/appsettings.Development.json
#            for running the backend natively with `dotnet run`, pre-filled
#            with the same secrets so both paths stay in sync.
#   --up     run `docker-compose up -d` once setup finishes.
#   --force  regenerate secrets even if .env already has real values.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEV=false
UP=false
FORCE=false

for arg in "$@"; do
    case "$arg" in
        --dev) DEV=true ;;
        --up) UP=true ;;
        --force) FORCE=true ;;
        *) echo "Unknown option: $arg" >&2; exit 1 ;;
    esac
done

random_hex() {
    # $1 = number of bytes
    openssl rand -hex "$1"
}

VAPID_JSON=""
fetch_vapid_json() {
    if [ -z "$VAPID_JSON" ]; then
        echo "  Generating VAPID keypair (npx web-push)..." >&2
        VAPID_JSON="$(npx --yes web-push generate-vapid-keys --json 2>/dev/null || true)"
        if [ -z "$VAPID_JSON" ]; then
            echo "Warning: could not generate VAPID keys automatically (is Node/npx installed?)." >&2
            echo "         Leaving Vapid keys blank - fill them in by hand with 'npx web-push generate-vapid-keys' when you enable Features__Notifications." >&2
        fi
    fi
}

vapid_field() {
    # $1 = publicKey|privateKey - parses the already-fetched $VAPID_JSON.
    # Must be called after fetch_vapid_json, and NOT via command
    # substitution for the fetch itself, or each $( ) subshell would
    # re-run npx and hand back mismatched public/private keys.
    echo "$VAPID_JSON" | grep -o "\"$1\":\"[^\"]*\"" | sed -E "s/\"$1\":\"([^\"]*)\"/\1/"
}

# ------------------------------------------------------------------
# Root .env
# ------------------------------------------------------------------

ENV_PATH="$ROOT/.env"
EXAMPLE_PATH="$ROOT/.env.example"

if [ ! -f "$ENV_PATH" ]; then
    echo "Creating .env from .env.example"
    cp "$EXAMPLE_PATH" "$ENV_PATH"
fi

GENERATED_KEYS=""

set_key() {
    # $1 = key, $2 = new value
    local key="$1" val="$2"
    val="${val//&/\\&}"
    val="${val//\//\\/}"
    # portable in-place edit for both GNU and BSD sed
    sed -i.bak -E "s|^${key}=.*|${key}=${val}|" "$ENV_PATH"
    rm -f "$ENV_PATH.bak"
    GENERATED_KEYS="$GENERATED_KEYS $key"
}

current_val() {
    grep -E "^$1=" "$ENV_PATH" | head -n1 | cut -d= -f2-
}

should_generate() {
    local key="$1"
    if [ "$FORCE" = true ]; then return 0; fi
    [ "$(current_val "$key")" = "__GENERATE__" ]
}

should_generate JwtSettings__Key && set_key JwtSettings__Key "$(random_hex 64)"
should_generate POSTGRES_PASSWORD && set_key POSTGRES_PASSWORD "$(random_hex 16)"
should_generate AdminUserPassword && set_key AdminUserPassword "$(random_hex 10)!1"
should_generate GRAFANA_ADMIN_PASSWORD && set_key GRAFANA_ADMIN_PASSWORD "$(random_hex 12)"

if should_generate Vapid__PublicKey || should_generate Vapid__PrivateKey; then
    fetch_vapid_json
    if [ -n "$VAPID_JSON" ]; then
        pub="$(vapid_field publicKey)"
        priv="$(vapid_field privateKey)"
        if [ -n "$pub" ] && [ -n "$priv" ]; then
            should_generate Vapid__PublicKey && set_key Vapid__PublicKey "$pub"
            should_generate Vapid__PrivateKey && set_key Vapid__PrivateKey "$priv"
        fi
    fi
fi

if [ -n "$GENERATED_KEYS" ]; then
    echo "Generated secrets for:${GENERATED_KEYS}"
else
    echo ".env already configured (use --force to regenerate)"
fi

# ------------------------------------------------------------------
# Native backend dev config (optional)
# ------------------------------------------------------------------

if [ "$DEV" = true ]; then
    DEV_PATH="$ROOT/backend/src/Operum.API/appsettings.Development.json"
    DEV_EXAMPLE_PATH="$ROOT/backend/src/Operum.API/appsettings.Development.Example.txt"

    if [ -f "$DEV_PATH" ] && [ "$FORCE" = false ]; then
        echo "appsettings.Development.json already exists, leaving it alone (use --force to overwrite)"
    else
        echo "Writing backend/src/Operum.API/appsettings.Development.json"

        pg_user="$(current_val POSTGRES_USER)"
        pg_pass="$(current_val POSTGRES_PASSWORD)"
        pg_db="$(current_val POSTGRES_DB)"
        admin_pw="$(current_val AdminUserPassword)"
        jwt_key="$(current_val JwtSettings__Key)"
        vapid_pub="$(current_val Vapid__PublicKey)"
        vapid_priv="$(current_val Vapid__PrivateKey)"

        conn="User ID=${pg_user};Password=${pg_pass};Host=localhost;Port=5433;Database=${pg_db}"

        sed \
            -e "s|\"Operum\": \".*\"|\"Operum\": \"${conn}\"|" \
            -e "s|\"AdminUserPassword\": \".*\"|\"AdminUserPassword\": \"${admin_pw}\"|" \
            -e "s|\"Key\": \".*\"|\"Key\": \"${jwt_key}\"|" \
            -e "s|\"PublicKey\": \".*\"|\"PublicKey\": \"${vapid_pub}\"|" \
            -e "s|\"PrivateKey\": \".*\"|\"PrivateKey\": \"${vapid_priv}\"|" \
            "$DEV_EXAMPLE_PATH" > "$DEV_PATH"

        echo "  -> matches the same DB/JWT/VAPID secrets as .env, pointed at localhost:5433."
        echo "  -> start Postgres for native dev with: docker-compose up -d postgres"
    fi
fi

# ------------------------------------------------------------------
# Summary
# ------------------------------------------------------------------

echo ""
echo "Setup complete."
echo "  Admin login:  admin@example.com / $(current_val AdminUserPassword)"
echo "  Test login:   test@example.com  / Password0!"
echo ""

if [ "$UP" = true ]; then
    echo "Running docker-compose up -d ..."
    docker-compose up -d
else
    echo "Next: docker-compose up -d  (or re-run with --up)"
fi
