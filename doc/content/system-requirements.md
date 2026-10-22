# 系统要求

## 普通用户

- **Windows**：推荐 Windows 10 / 11，64 位（x64）。使用与系统架构匹配的 CAN 发布包。
- **网络**：连接 CAN FSD、Mumble 和数据服务。Mumble 的控制连接需要 TCP，UDP 语音还取决于客户端实现和网络条件。
- **音频设备**：试听需要播放设备；人工录音需要麦克风和相应的系统权限。
- **语音组件**：Windows 版本的 TTS 使用系统内置 Windows SAPI，不需要联网的 TTS 服务或额外语音模型；Mumble 音频发送仍需 Python worker、对应依赖和 Opus 动态库。

## 从源码构建

需要 .NET 10 SDK、Visual Studio 的“使用 C++ 的桌面开发”工作负载、CMake 和 Ninja。构建步骤见[本地开发](client/local-development.md)。

独立部署的 .NET 发布包使用 `--self-contained true` 时，无需用户另装 .NET；源码运行则需要 SDK。Python 语音依赖是另一套运行环境。

## macOS 与 Linux

原版文档列出 macOS 14+（Apple Silicon/x64）和现代 Linux x64。CAN 仓库虽然保留跨平台代码，但当前未验证相应发布包、原生音频库和语音 worker 的完整安装流程。以 CAN 实际发布的系统支持列表为准。
