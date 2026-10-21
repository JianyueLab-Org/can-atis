# 本地开发

本页面向维护 ATIS for CAN 的开发者。当前 CAN 解决方案没有原版 `DevServer` 项目，不能直接套用“Local Dev 自动启动 localhost:5500”的说明。

## 环境准备

- .NET 10 SDK。
- Visual Studio 的 C++ 桌面开发工具、CMake、Ninja。
- Python 环境及 Mumble 依赖，Opus 动态库。

用 Visual Studio / Rider 打开 `vATIS.slnx`，或在 VS Code 中安装 C# 扩展后打开仓库。网络连接参数来自 CAN 配置提供器。

## 构建原生音频

从仓库根目录，在 Visual Studio Developer PowerShell 中执行：

```powershell
cmake -S NativeAudio -B NativeAudio/build-msvc-release -G Ninja `
  -DCMAKE_BUILD_TYPE=Release `
  -DNATIVE_DLL_OUTPUT_DIR="$PWD/vATIS.Desktop/Voice/Audio/Native/win"
cmake --build NativeAudio/build-msvc-release
```

发布前确认 DLL 位于 `vATIS.Desktop/Voice/Audio/Native/win/NativeAudio.dll`，这与项目文件的复制及发布检查路径一致。

## 构建、运行与测试

```powershell
dotnet build vATIS.slnx
dotnet run --project vATIS.Desktop/vATIS.Desktop.csproj
dotnet test vATIS.Desktop.Tests/vATIS.Desktop.Tests.csproj
```

可在不连接席位的情况下使用[沙盒](atis-configuration/sandbox.md)输入 METAR 并验证模板。沙盒的在线资源请求仍会访问对应服务。

## Mumble worker

当前声明的 Python 依赖文件：

```powershell
python -m pip install -r vATIS.Desktop/requirements-mumble.txt
```

该文件仍使用 `pymumble==1.6.1`，支持 TCP tunnel，尚未升级为 can-audio 的加密 UDP 分支。Python 3.12 及以上的 TLS 兼容由 `mumblecompat.py` 补丁处理；Opus 需要能被运行时找到。

目前 worker 查找打包目录 `python/python.exe`，否则回退系统 Python，并有源码环境候选路径。不要假定每个开发目录中的虚拟环境都能自动识别。

## 发布 Windows x64

```powershell
dotnet publish vATIS.Desktop/vATIS.Desktop.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=false `
  -o release/can-atis-win-x64
```

检查完整输出中的 NativeAudio、worker、兼容脚本、Python 运行时及 Opus。当前 Opus 的项目复制条件仍依赖外部相对目录，发布维护者必须核实实际文件，不应只以 dotnet publish 成功作为播音验证。

## 服务对接

FSD / Mumble / 数据端点见[首页](../index.md)。TTS 可通过 `CAN_TTS_URL`、`CAN_TTS_VOICES_URL` 和运行环境的 `CAN_TTS_JWT` 进行接口测试，但公共 CAN 服务尚需部署。

配置与代码中只使用占位符，不内置维护者账号或令牌。原版 vATIS 的 Mock FSD / DevServer 流程需要显式移植和配置，当前仓库中残留的 Mock 类型不表示已提供完整本地开发服务。
