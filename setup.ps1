<#
.SYNOPSIS
    One-command bootstrap for local Operum setup (Windows/PowerShell).

.DESCRIPTION
    Creates .env from .env.example (if missing) and fills in every
    __GENERATE__ placeholder with a freshly generated secret: JWT signing
    key, Postgres password, admin login password, Grafana admin password,
    and a VAPID keypair for web push (via `npx web-push`, requires Node).

    Re-running is safe: values that are already set are left untouched.
    Use -Force to regenerate everything from scratch.

.PARAMETER Dev
    Also create backend/src/Operum.API/appsettings.Development.json (for
    running the backend natively with `dotnet run` instead of Docker),
    pre-filled with the same secrets so both paths stay in sync.

.PARAMETER Up
    Run `docker-compose up -d` once setup finishes.

.PARAMETER Force
    Regenerate secrets even if .env already has real values.

.EXAMPLE
    ./setup.ps1 -Dev -Up
#>
param(
    [switch]$Dev,
    [switch]$Up,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function New-RandomHex([int]$Bytes) {
    $buffer = [byte[]]::new($Bytes)
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($buffer) } finally { $rng.Dispose() }
    -join ($buffer | ForEach-Object { $_.ToString("x2") })
}

function Get-VapidKeys {
    Write-Host "  Generating VAPID keypair (npx web-push)..." -ForegroundColor DarkGray
    try {
        $json = npx --yes web-push generate-vapid-keys --json 2>$null
        $keys = $json | ConvertFrom-Json
        if (-not $keys.publicKey -or -not $keys.privateKey) { throw "empty output" }
        return $keys
    } catch {
        Write-Warning "Could not generate VAPID keys automatically (is Node/npx installed?). Leaving Vapid keys blank - notifications will stay disabled until you set Features__Notifications=true and fill them in by hand, e.g. via 'npx web-push generate-vapid-keys'."
        return $null
    }
}

# ------------------------------------------------------------------
# Root .env
# ------------------------------------------------------------------

$envPath = Join-Path $root ".env"
$examplePath = Join-Path $root ".env.example"

if (-not (Test-Path $envPath)) {
    Write-Host "Creating .env from .env.example" -ForegroundColor Cyan
    Copy-Item $examplePath $envPath
}

$knownKeys = @(
    'JwtSettings__Key', 'POSTGRES_PASSWORD', 'AdminUserPassword',
    'GRAFANA_ADMIN_PASSWORD', 'Vapid__PublicKey', 'Vapid__PrivateKey'
)

$lines = Get-Content $envPath
$vapid = $null
$generated = @{}

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -notmatch '^(?<key>[A-Za-z_][A-Za-z0-9_]*)=(?<val>.*)$') { continue }
    $key = $Matches.key
    $val = $Matches.val

    if ($key -notin $knownKeys) { continue }
    if (-not $Force -and $val -ne '__GENERATE__') { continue }

    switch -Regex ($key) {
        '^JwtSettings__Key$'        { $new = New-RandomHex 64 }
        '^POSTGRES_PASSWORD$'       { $new = New-RandomHex 16 }
        '^AdminUserPassword$'       { $new = (New-RandomHex 10) + "!1" }
        '^GRAFANA_ADMIN_PASSWORD$'  { $new = New-RandomHex 12 }
        '^Vapid__PublicKey$' {
            if (-not $vapid) { $vapid = Get-VapidKeys }
            $new = if ($vapid) { $vapid.publicKey } else { $val }
        }
        '^Vapid__PrivateKey$' {
            if (-not $vapid) { $vapid = Get-VapidKeys }
            $new = if ($vapid) { $vapid.privateKey } else { $val }
        }
    }

    $lines[$i] = "$key=$new"
    $generated[$key] = $new
}

Set-Content -Path $envPath -Value $lines -Encoding utf8

if ($generated.Count -gt 0) {
    Write-Host "Generated secrets for: $($generated.Keys -join ', ')" -ForegroundColor Green
} else {
    Write-Host ".env already configured (use -Force to regenerate)" -ForegroundColor DarkGray
}

# ------------------------------------------------------------------
# Native backend dev config (optional)
# ------------------------------------------------------------------

if ($Dev) {
    $devPath = Join-Path $root "backend/src/Operum.API/appsettings.Development.json"
    $devExamplePath = Join-Path $root "backend/src/Operum.API/appsettings.Development.Example.txt"

    if ((Test-Path $devPath) -and -not $Force) {
        Write-Host "appsettings.Development.json already exists, leaving it alone (use -Force to overwrite)" -ForegroundColor DarkGray
    } else {
        Write-Host "Writing backend/src/Operum.API/appsettings.Development.json" -ForegroundColor Cyan

        $envMap = @{}
        foreach ($line in (Get-Content $envPath)) {
            if ($line -match '^(?<key>[A-Za-z_][A-Za-z0-9_]*)=(?<val>.*)$') {
                $envMap[$Matches.key] = $Matches.val
            }
        }

        $json = Get-Content $devExamplePath -Raw | ConvertFrom-Json
        $json.ConnectionStrings.Operum = "User ID=$($envMap['POSTGRES_USER']);Password=$($envMap['POSTGRES_PASSWORD']);Host=localhost;Port=5433;Database=$($envMap['POSTGRES_DB'])"
        $json.AdminUserPassword = $envMap['AdminUserPassword']
        $json.JwtSettings.Key = $envMap['JwtSettings__Key']
        $json.Vapid.PublicKey = $envMap['Vapid__PublicKey']
        $json.Vapid.PrivateKey = $envMap['Vapid__PrivateKey']

        $json | ConvertTo-Json -Depth 10 | Set-Content -Path $devPath -Encoding utf8
        Write-Host "  -> matches the same DB/JWT/VAPID secrets as .env, pointed at localhost:5433." -ForegroundColor DarkGray
        Write-Host "  -> start Postgres for native dev with: docker-compose up -d postgres" -ForegroundColor DarkGray
    }
}

# ------------------------------------------------------------------
# Summary
# ------------------------------------------------------------------

$envMap = @{}
foreach ($line in (Get-Content $envPath)) {
    if ($line -match '^(?<key>[A-Za-z_][A-Za-z0-9_]*)=(?<val>.*)$') {
        $envMap[$Matches.key] = $Matches.val
    }
}

Write-Host ""
Write-Host "Setup complete." -ForegroundColor Green
Write-Host "  Admin login:  admin@example.com / $($envMap['AdminUserPassword'])"
Write-Host "  Test login:   test@example.com  / Password0!"
Write-Host ""

if ($Up) {
    Write-Host "Running docker-compose up -d ..." -ForegroundColor Cyan
    docker-compose up -d
} else {
    Write-Host "Next: docker-compose up -d  (or re-run with -Up)"
}
