# Builds release\can-atis-single-file\atis-for-can.exe.
#
# The Python voice worker is not in this repository: it lives in
# JianyueLab-Org/can-audio under atis-worker/ and is maintained together with the
# Python side. This script builds it from a can-audio checkout, then embeds the
# resulting can-voice-worker.exe into the single-file desktop client.
#
# Python must already have can-audio's requirements-build.txt installed
# (Python 3.12 x64; set PYTHONUTF8=1 before pip on Windows).
param(
    [string]$Python = "python",
    # A can-audio checkout. If it does not exist, CanAudioRef is cloned there.
    [string]$CanAudio = "$PSScriptRoot\build\can-audio",
    [string]$CanAudioRef = "new-client-sys",
    # Defaults to <Version> in CanAtis.Desktop.csproj; CI passes the release number.
    [string]$Version = ""
)

# Not "Stop": Windows PowerShell 5.1 turns a native command's stderr into terminating
# errors once output is redirected, and unittest / PyInstaller log to stderr. Every
# native call below checks $LASTEXITCODE instead.
$ErrorActionPreference = "Continue"
$env:PYTHONUTF8 = "1"
# A relative interpreter path would break after Push-Location below.
if (Test-Path -LiteralPath $Python) { $Python = (Resolve-Path -LiteralPath $Python).Path }

if (-not (Test-Path -LiteralPath (Join-Path $CanAudio "atis-worker"))) {
    git clone --depth 1 --branch $CanAudioRef https://github.com/JianyueLab-Org/can-audio.git $CanAudio
    if ($LASTEXITCODE -ne 0) { throw "Could not clone can-audio ($CanAudioRef)" }
}
$atisWorker = (Resolve-Path (Join-Path $CanAudio "atis-worker") -ErrorAction Stop).Path
$canAudioCommit = git -C $CanAudio rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw "$CanAudio is not a git checkout of can-audio" }
Write-Output "can-audio $canAudioCommit"

Push-Location $atisWorker -ErrorAction Stop
try {
    & $Python -m unittest discover -v
    if ($LASTEXITCODE -ne 0) { throw "can-audio atis-worker tests failed" }
}
finally { Pop-Location }

$build = Join-Path $PSScriptRoot "build\voice-worker"
$publish = Join-Path $PSScriptRoot "release\can-atis-single-file"
New-Item -ItemType Directory -Force -Path $build -ErrorAction Stop | Out-Null
$env:PATH = "$atisWorker;$env:PATH"

& $Python -m PyInstaller --noconfirm --clean --onefile --name can-voice-worker `
    --distpath "$build\dist" --workpath "$build\work" --specpath $build `
    --paths $atisWorker --collect-all pymumble_py3 --collect-all opuslib --collect-all google.protobuf `
    --hidden-import mumblecompat --add-binary "$atisWorker\opus.dll;." `
    "$atisWorker\can_mumble_worker.py"
if ($LASTEXITCODE -ne 0) { throw "Voice worker build failed" }
$worker = Join-Path $build "dist\can-voice-worker.exe"
& $worker --self-test
if ($LASTEXITCODE -ne 0) { throw "Voice worker self-test failed" }

$versionArgs = @()
if ($Version) { $versionArgs = @("-p:Version=$Version") }
dotnet publish "$PSScriptRoot\src\CanAtis.Desktop\CanAtis.Desktop.csproj" `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true `
    -p:DebugType=embedded "-p:VoiceWorkerExe=$worker" @versionArgs -o $publish --nologo -v quiet -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw "Desktop publish failed" }

$exe = Join-Path $publish "atis-for-can.exe"
# WinExe: & would not wait for it, so start it explicitly and check the exit code.
# The self-test checks the embedded airports, NativeAudio and the embedded worker's own --self-test.
$selfTest = Start-Process -FilePath $exe -ArgumentList "--self-test" -Wait -PassThru -NoNewWindow
if ($selfTest.ExitCode -ne 0) { throw "atis-for-can.exe self-test failed (exit $($selfTest.ExitCode))" }
Set-Content -Path (Join-Path $publish "can-audio-commit.txt") -Value $canAudioCommit -Encoding ascii -ErrorAction Stop
Write-Output "Single-file build: $exe"
