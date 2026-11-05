# build-ifbuilder64.ps1
# Script di automazione per la creazione dei pacchetti zip
# di IFBuilder64 (compilatore CLI) e dell'Editor per win-x64, linux-x64, osx-arm64.
#
# I pacchetti sono framework-dependent: richiedono il runtime .NET 10 installato.
# Negli zip linux/osx gli eseguibili hanno i permessi Unix 0755, quindi dopo
# l'estrazione si avviano senza bisogno di chmod +x.

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# ── Parametri ──────────────────────────────────────────────────────
$configuration = "Release"
$runtimes      = @("win-x64", "linux-x64", "osx-arm64")
$projects      = @(
    @{ Name = "IFBuilder64"; Csproj = "IFBuilder64\IFBuilder64.csproj"; Eseguibile = "IFBuilder64" }
    @{ Name = "Editor";      Csproj = "Editor\Editor.csproj";           Eseguibile = "Casasoft.IFBuilder64.Editor" }
)

$outputDir   = Join-Path $rootDir "artifacts"
$publishRoot = Join-Path $outputDir "temp_publish"
$licenseFile = Join-Path $rootDir "LICENSE.txt"

# Attributi esterni zip (16 bit alti = modo Unix): file regolare 0644 e eseguibile 0755
$modoFile       = [Convert]::ToInt32('100644', 8) -shl 16
$modoEseguibile = [Convert]::ToInt32('100755', 8) -shl 16

# ── Funzioni ──────────────────────────────────────────────────────

# Rimuove da runtimes\ le sottocartelle delle RID diverse da quella pubblicata
function Remove-RuntimesNonNecessari([string]$cartella, [string]$rid) {
    $runtimesDir = Join-Path $cartella "runtimes"
    if (-not (Test-Path $runtimesDir)) { return }

    Get-ChildItem -Path $runtimesDir -Directory |
        Where-Object { $_.Name -ne $rid } |
        ForEach-Object {
            Write-Host "  Rimozione runtime non necessario: $($_.Name)" -ForegroundColor DarkGray
            Remove-Item $_.FullName -Recurse -Force
        }

    if (-not (Get-ChildItem -Path $runtimesDir -Force)) {
        Remove-Item $runtimesDir -Force
    }
}

# Imposta "version made by" = Unix (3) in ogni voce della central directory.
# ZipArchive su Windows scrive MS-DOS (0) e in quel caso unzip/macOS ignorano i permessi Unix.
function Set-PiattaformaUnix([string]$zipPath) {
    $bytes = [System.IO.File]::ReadAllBytes($zipPath)

    # End of central directory record (firma 0x06054b50), cercato dal fondo
    $eocd = -1
    for ($i = $bytes.Length - 22; $i -ge 0; $i--) {
        if ([BitConverter]::ToUInt32($bytes, $i) -eq 0x06054b50) { $eocd = $i; break }
    }
    if ($eocd -lt 0) { throw "Record di fine central directory non trovato in $zipPath" }

    $voci   = [BitConverter]::ToUInt16($bytes, $eocd + 10)
    $offset = [int][BitConverter]::ToUInt32($bytes, $eocd + 16)

    for ($n = 0; $n -lt $voci; $n++) {
        if ([BitConverter]::ToUInt32($bytes, $offset) -ne 0x02014b50) {
            throw "Central directory non valida in $zipPath (voce $n)"
        }
        $bytes[$offset + 5] = 3   # byte alto di "version made by": 3 = Unix

        $lungNome       = [BitConverter]::ToUInt16($bytes, $offset + 28)
        $lungExtra      = [BitConverter]::ToUInt16($bytes, $offset + 30)
        $lungCommento   = [BitConverter]::ToUInt16($bytes, $offset + 32)
        $offset += 46 + $lungNome + $lungExtra + $lungCommento
    }

    [System.IO.File]::WriteAllBytes($zipPath, $bytes)
}

# Crea lo zip della cartella; per linux/osx imposta i permessi Unix delle voci
function New-PacchettoZip([string]$cartella, [string]$zipPath, [string]$rid, [string[]]$eseguibili) {
    $unix = $rid -ne "win-x64"
    $zip  = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem -Path $cartella -File -Recurse | ForEach-Object {
            $nomeVoce = [System.IO.Path]::GetRelativePath($cartella, $_.FullName).Replace('\', '/')
            $voce = [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $_.FullName, $nomeVoce, [System.IO.Compression.CompressionLevel]::Optimal)
            if ($unix) {
                $voce.ExternalAttributes = if ($eseguibili -contains $nomeVoce) { $modoEseguibile } else { $modoFile }
            }
        }
    }
    finally {
        $zip.Dispose()
    }

    if ($unix) {
        Set-PiattaformaUnix $zipPath
    }
}

# ── Pulizia precedente ────────────────────────────────────────────
if (Test-Path $publishRoot) {
    Write-Host "Pulizia cartella temporanea: $publishRoot" -ForegroundColor Yellow
    Remove-Item $publishRoot -Recurse -Force
}
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# ── Publish per ogni runtime ──────────────────────────────────────
foreach ($rid in $runtimes) {
    Write-Host "`n===============================================================" -ForegroundColor Cyan
    Write-Host " Publish per runtime: $rid" -ForegroundColor Cyan
    Write-Host "===============================================================" -ForegroundColor Cyan

    $ridPublishDir = Join-Path $publishRoot $rid

    foreach ($proj in $projects) {
        $projName   = $proj.Name
        $csprojPath = Join-Path $rootDir $proj.Csproj

        Write-Host "`n--- $projName ($rid) ---" -ForegroundColor White
        dotnet publish $csprojPath `
            -c $configuration `
            -r $rid `
            --self-contained false `
            -o $ridPublishDir

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Publish fallito per $projName ($rid)"
        }
    }

    # Rimuovi i runtimes nativi delle altre piattaforme
    Remove-RuntimesNonNecessari $ridPublishDir $rid

    # Rimuovi i file .pdb e .xml (non servono nella distribuzione)
    Get-ChildItem -Path $ridPublishDir -Filter "*.pdb" -Recurse | Remove-Item -Force
    Get-ChildItem -Path $ridPublishDir -Filter "*.xml" -Recurse | Remove-Item -Force

    # ── Copia la licenza nella cartella del runtime ────────────────
    if (Test-Path $licenseFile) {
        Copy-Item $licenseFile -Destination $ridPublishDir
    }
}

# ── Creazione zip ─────────────────────────────────────────────────
Write-Host "`n===============================================================" -ForegroundColor Green
Write-Host " Creazione pacchetti zip" -ForegroundColor Green
Write-Host "===============================================================" -ForegroundColor Green

$eseguibili = $projects | ForEach-Object { $_.Eseguibile }

foreach ($rid in $runtimes) {
    $ridPublishDir = Join-Path $publishRoot $rid
    $zipName = "IFBuilder64-$rid.zip"
    $zipPath = Join-Path $outputDir $zipName

    # Rimuovi eventuale zip precedente
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Write-Host "  Creazione $zipName ..." -ForegroundColor White
    New-PacchettoZip $ridPublishDir $zipPath $rid $eseguibili

    $fileInfo = Get-Item $zipPath
    Write-Host "  -> $zipPath ($([math]::Round($fileInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green
}

# ── Pulizia ───────────────────────────────────────────────────────
Write-Host "`nPulizia cartella temporanea..." -ForegroundColor Yellow
Remove-Item $publishRoot -Recurse -Force

# ── Riepilogo ─────────────────────────────────────────────────────
Write-Host "`n================================================" -ForegroundColor Green
Write-Host " Pacchetti IFBuilder64 generati con successo!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host " Cartella di output: $outputDir" -ForegroundColor Green
foreach ($rid in $runtimes) {
    $zipPath = Join-Path $outputDir "IFBuilder64-$rid.zip"
    if (Test-Path $zipPath) {
        $fileInfo = Get-Item $zipPath
        Write-Host "  - IFBuilder64-$rid.zip ($([math]::Round($fileInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green
    }
}
Write-Host " Requisito: runtime .NET 10 installato sul sistema di destinazione." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
