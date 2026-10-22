param(
    [string]$Python = "python",
    [string]$OpusDll = "$PSScriptRoot\src\CanAtis.Desktop\Voice\Audio\Native\win\opus.dll"
)

$ErrorActionPreference = "Stop"
$build = Join-Path $PSScriptRoot "build\voice-worker"
$publish = Join-Path $PSScriptRoot "release\can-atis-single-file"
New-Item -ItemType Directory -Force -Path $build | Out-Null
if (-not (Test-Path -LiteralPath $OpusDll)) { throw "Opus DLL not found: $OpusDll" }
$env:PATH = "$(Split-Path -Parent $OpusDll);$env:PATH"

& $Python -m PyInstaller --noconfirm --clean --onefile --name can-voice-worker `
    --distpath "$build\dist" --workpath "$build\work" --specpath $build `
    --collect-all pymumble_py3 --collect-all opuslib --collect-all google.protobuf `
    --hidden-import mumblecompat --add-binary "$OpusDll;." `
    "$PSScriptRoot\src\CanAtis.Desktop\can_mumble_worker.py"
if ($LASTEXITCODE -ne 0) { throw "Voice worker build failed" }
$worker = Join-Path $build "dist\can-voice-worker.exe"
& $worker --self-test
if ($LASTEXITCODE -ne 0) { throw "Voice worker self-test failed" }

dotnet publish "$PSScriptRoot\src\CanAtis.Desktop\CanAtis.Desktop.csproj" `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true `
    -p:DebugType=embedded "-p:VoiceWorkerExe=$worker" -o $publish --nologo -v quiet -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed" }
Write-Output "Single-file build: $publish\atis-for-can.exe"
