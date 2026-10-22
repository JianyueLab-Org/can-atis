# ATIS for CAN

The C# desktop release series starts at **0.0.1**. Download the Windows x64
single-file executable from [GitHub Releases](https://github.com/JianyueLab-Org/can-atis/releases).

ATIS for CAN is the ATIS desktop client for the **Cerulean Aviation Network**.
It is based on the ATIS workflow and editor from the original project, but its
branding, service endpoints, login flow, and voice integration are adapted for
CAN.

## Features

中文使用文档：[ATIS for CAN 用户指南](doc/index.html)（可直接用浏览器打开）。
文档源码及构建方式见 [doc/README.md](doc/README.md)。

- ATIS station profiles, presets, templates, NOTAMs, and airport conditions
- METAR processing and ATIS letter management
- CAN FSD connection for controller/ATIS presence and text ATIS replies
- CAN Mumble voice integration
- English and Chinese ATIS content support
- Dark CAN-themed Avalonia UI
- Local bundled airport data for first launch and offline configuration
- Compact CAN client UI adapted from [can-client-ui](https://github.com/JianyueLab-Org/can-client-ui)
  (`AtisClient`, revision `ee728d7e`), with Chinese navigation and shared dark design tokens
- Station navigation on the left or across the top; change **用户设置 → 席位列表放在顶部**
- Native Windows audio backend built with MSVC

## CAN services

The desktop client uses these defaults:

```text
FSD:    fsd.ceruleanavi.net:6809
Mumble: audio.ceruleanavi.net:64738
Data:   https://data.ceruleanavi.net/v1/data.json
```

FSD authentication uses the CAN ID and network password. The client does not
use VATSIM authentication, the vATIS SignalR hub, or a VATSIM voice REST API.

## Text to Speech

The client loads the original vATIS catalogue from
`https://hub.vatis.app/speech/voices`: Default, US Male/Female, UK Male/Female,
and AU Male/Female. Select **Text to Speech** in the station configuration,
choose a voice and speech rate, save, and use **Sandbox** to preview audio.

**Synthesis for general CAN users is not yet configured.** The original service
requires a VATSIM JWT; listing its voices does not make synthesis available.
The client does not read the original vATIS account or request VATSIM tokens.
`CAN_TTS_JWT` can supply a service token for integration testing; manually supplied
tokens are not automatically refreshed. A shared CAN deployment needs its own
speech backend and authentication integration.

For a compatible CAN-hosted service, set `CAN_TTS_URL` and `CAN_TTS_VOICES_URL`
to its synthesis and catalogue endpoints and provide its token in `CAN_TTS_JWT`.
The protocol matches vATIS: JSON requests with Text, Voice, SpeechRate and Jwt,
and raw mono 48 kHz signed 16-bit little-endian PCM responses. The catalogue
uses `[{"name":"US Male","id":"us-male"}, ...]`. Matching catalogue labels alone
does not supply the original voice models; the configured service must actually
provide those voices.

## Build on Windows

### UI integration

The production client keeps Avalonia 11 and ReactiveUI. The Avalonia 12 UI prototype's
design tokens and ATIS layout are adapted in `Ui/Styles/CanTokens.axaml`,
`Ui/Styles/CanTheme.axaml`, and the existing `Ui/` views. Controls remain bound to
the production station view models for METAR overrides, ATIS acknowledgement,
airport conditions/NOTAM editing, presets, recording, preview, and CAN connection.
The station navigation preference is saved in the existing application configuration
and takes effect immediately after saving settings.

Run the desktop app from the repository root:

```powershell
dotnet run --project src/CanAtis.Desktop
```

### Single-file Windows release

```powershell
# In a Python environment with the dependencies from requirements-mumble.txt and PyInstaller:
./build-single-file.ps1 -Python python -OpusDll C:/path/to/opus.dll
```

Output: `release/can-atis-single-file/atis-for-can.exe`. The executable embeds
the .NET runtime, native audio, airport data, and the frozen Python voice worker
(including pymumble-udp and Opus). Only the executable needs to be distributed;
runtime files are extracted automatically. `atis-for-can.exe --self-test` checks
the bundled airport data, native audio initialization, and voice worker startup.

Login uses the member's CAN ID, name and network password. No controller rating
selection is required; the FSD connection advertises the ATIS facility and the
base network rating. Pilot accounts can use the same login form.

The current station's bottom status bar shows FSD TCP RTT and the voice worker's
actual TCP/UDP transport and keepalive RTT. Missing measurements appear as `-- ms`.
Voice transport statistics reuse the Python CAN clients' `udp_active`,
`udp_ping_stats` and `ping_stats` logic. The worker remains connected and loops
the broadcast until stopped or the connection is lost.

Click **日志** in the main toolbar to open
the `Logs` folder beside `atis-for-can.exe`. Logs roll daily as
`can-atis-yyyyMMdd.log`, also roll at 20 MB, and retain the most recent 30 files.
They include FSD connection events, voice transport/latency changes, worker errors,
and application exceptions. Login packets and network passwords are excluded.

Requirements:

- Visual Studio with the Desktop C++ workload
- .NET 10 SDK
- CMake and Ninja

Build the native audio library from a **Visual Studio Developer PowerShell**:

```powershell
cd NativeAudio
cmake -S . -B build-msvc-release -G Ninja `
  -DCMAKE_BUILD_TYPE=Release `
  -DNATIVE_DLL_OUTPUT_DIR="$PWD/../src/CanAtis.Desktop/Voice/Audio/Native"
cmake --build build-msvc-release
```

Build and publish the desktop client:

```powershell
cd ..
dotnet publish src/CanAtis.Desktop/CanAtis.Desktop.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=false `
  -o release/can-atis-win-x64
```

Copy `NativeAudio.dll` to the publish directory if it is not copied by the
project file. The executable is `atis-for-can.exe`.

## Repository layout

```text
src/CanAtis.Desktop/       C# CAN ATIS desktop application and required voice bridge
src/CanAtis.Desktop.Tests/ Desktop and ATIS tests
src/Can.Network/           FSD protocol library used by the client
NativeAudio/              Native audio backend and CMake build
build-single-file.ps1     Windows self-contained single-file release build
```

## License

This project is distributed under the GPLv3 license. See [LICENSE](LICENSE).
