# 本地开发

本页面向维护 ATIS for CAN 的开发者。当前 CAN 解决方案没有原版 `DevServer` 项目，不能直接套用“Local Dev 自动启动 localhost:5500”的说明。

## 环境准备

- .NET 10 SDK。
- Visual Studio 的 C++ 桌面开发工具、CMake、Ninja（只在需要重新编译原生音频时）。
- 要在本地打包或调试网络播音时：Python 3.12 x64 和 [can-audio](https://github.com/JianyueLab-Org/can-audio) 仓库。

用 Visual Studio / Rider 打开 `CanAtis.sln`，或在 VS Code 中安装 C# 扩展后打开仓库。网络连接参数来自 CAN 配置提供器。

## 构建原生音频

仓库里已提交 `src/CanAtis.Desktop/Voice/Audio/Native/win/NativeAudio.dll`。需要重新编译时，从仓库根目录在 Visual Studio Developer PowerShell 中执行：

```powershell
cmake -S NativeAudio -B NativeAudio/build-msvc-release -G Ninja `
  -DCMAKE_BUILD_TYPE=Release `
  -DNATIVE_DLL_OUTPUT_DIR="$PWD/src/CanAtis.Desktop/Voice/Audio/Native/win"
cmake --build NativeAudio/build-msvc-release
```

## 构建、运行与测试

```powershell
dotnet build CanAtis.sln
dotnet run --project src/CanAtis.Desktop/CanAtis.Desktop.csproj
dotnet test src/CanAtis.Desktop.Tests/CanAtis.Desktop.Tests.csproj
```

可在不连接席位的情况下使用[沙盒](atis-configuration/sandbox.md)输入 METAR 并验证模板。沙盒的在线资源请求仍会访问对应服务。

## 语音发送进程

网络播音由一个 Python 进程完成：它连上 Mumble、进入频率频道并循环播报客户端合成好的音频。这部分**不在本仓库**，由 C# 与 Python 开发共同维护在 can-audio 仓库 `new-client-sys` 分支的 [`atis-worker/`](https://github.com/JianyueLab-Org/can-audio/tree/new-client-sys/atis-worker) 目录，命令行参数、输入输出和退出码见那里的 README。

发行版把它打成 `can-voice-worker.exe` 嵌在 `atis-for-can.exe` 里。从源码运行时没有嵌入的进程，需要用环境变量指过去：

```powershell
git clone -b new-client-sys https://github.com/JianyueLab-Org/can-audio.git ..\can-audio
py -3.12 -m venv ..\can-audio\.venv
$env:PYTHONUTF8 = 1
..\can-audio\.venv\Scripts\python.exe -m pip install -r ..\can-audio\requirements-voice.txt

$env:CAN_VOICE_WORKER = (Resolve-Path ..\can-audio\atis-worker\can_mumble_worker.py)
$env:CAN_PYTHON = (Resolve-Path ..\can-audio\.venv\Scripts\python.exe)
dotnet run --project src/CanAtis.Desktop/CanAtis.Desktop.csproj
```

`CAN_VOICE_WORKER` 也可以指向打包好的 `can-voice-worker.exe`，这时不需要 `CAN_PYTHON`。两个都不设时，开始播报会报“CAN Mumble worker is missing”。

## 发布 Windows x64

```powershell
..\can-audio\.venv\Scripts\python.exe -m pip install -r ..\can-audio\requirements-build.txt
.\build-single-file.ps1 -Python ..\can-audio\.venv\Scripts\python.exe -CanAudio ..\can-audio
```

不给 `-CanAudio` 时脚本会把 can-audio 的 `new-client-sys` 分支克隆到 `build\can-audio`。脚本依次跑 can-audio 的 atis-worker 测试、用 PyInstaller 打包语音进程并自检、`dotnet publish` 单文件，最后运行 `atis-for-can.exe --self-test`。产物在 `release\can-atis-single-file\atis-for-can.exe`，同目录的 `can-audio-commit.txt` 记录了用的是 can-audio 的哪个提交。

正式发布由 GitHub Actions（`.github/workflows/release.yml`）完成：推到 `main` 就从 can-audio 拉最新的语音发送进程打包，补丁号按已有的 `v1.0.*` 标签自动加一，打标签并发 Release。主版本和次版本写在 `CanAtis.Desktop.csproj` 的 `<Version>` 里，改系列时要和工作流的 `SERIES` 一起改。

## 服务对接

FSD / Mumble / 数据端点见[首页](../index.md)。TTS 可通过 `CAN_TTS_URL`、`CAN_TTS_VOICES_URL` 和运行环境的 `CAN_TTS_JWT` 进行接口测试，但公共 CAN 服务尚需部署。

配置与代码中只使用占位符，不内置维护者账号或令牌。原版 vATIS 的 Mock FSD / DevServer 流程需要显式移植和配置，当前仓库中残留的 Mock 类型不表示已提供完整本地开发服务。
