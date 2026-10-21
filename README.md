# ATIS for CAN

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

Requirements:

- Visual Studio with the Desktop C++ workload
- .NET 10 SDK
- CMake and Ninja

Build the native audio library from a **Visual Studio Developer PowerShell**:

```powershell
cd NativeAudio
cmake -S . -B build-msvc-release -G Ninja `
  -DCMAKE_BUILD_TYPE=Release `
  -DNATIVE_DLL_OUTPUT_DIR="$PWD/../vATIS.Desktop/Voice/Audio/Native"
cmake --build build-msvc-release
```

Build and publish the desktop client:

```powershell
cd ../vATIS.Desktop
dotnet publish vATIS.Desktop.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=false `
  -o ../release/can-atis-win-x64
```

Copy `NativeAudio.dll` to the publish directory if it is not copied by the
project file. The executable is `atis-for-can.exe`.

## Repository layout

```text
vATIS.Desktop/       CAN ATIS desktop application
vATIS.Desktop.Tests/ Desktop and ATIS tests
Vatsim.Network/      FSD protocol library used by the client
NativeAudio/         Native audio backend and CMake build
```

## License

This project is distributed under the GPLv3 license. See [LICENSE](LICENSE).
