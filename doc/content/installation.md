# 安装与更新

## 获取客户端

从 [CAN 网站](https://ceruleanavi.net/)或 CAN 管理员公布的发布渠道获取 **ATIS for CAN**。请确认程序名为 `atis-for-can.exe`。原版 vATIS 的下载按钮安装的是原版客户端，不是 CAN 版。

## Windows 安装

### 压缩包版本

1. 下载适用于 Windows x64 的完整发布包。
2. 将压缩包**全部解压**到有写入权限的目录。
3. 保留 DLL、Python worker、数据文件和其他随包组件的目录结构。
4. 双击 `atis-for-can.exe`。需要时可自行创建桌面快捷方式。

当前仓库提供的是构建与发布流程，没有可据此确认的固定安装器路径。因此安装目录可以由用户选择，不沿用原版“必须安装在 org.vatsim.vatis”这一说明。

### 语音运行环境

完整发布包应由维护者配置好 `NativeAudio.dll`、Python、Mumble 依赖、`mumblecompat.py`、`can_mumble_worker.py` 和 Opus 动态库。缺少组件时，窗口可能仍能打开，但不能据此判断录音、试听或网络播音可用。

源码运行和依赖安装见[本地开发](client/local-development.md)。Windows 客户端的 TTS 使用系统已安装的 SAPI 语音，不依赖在线 TTS 服务；如果需要其他音色，请先在 Windows 的语音设置中安装对应语音包。

## 首次运行

1. 打开[用户设置](client/user-settings.md)，填写自己的 CAN 资料。
2. 导入管制单位提供的配置，或[创建配置文件](client/profiles.md)。
3. 选择机场席位，核对频率、ATIS 类型和字母范围。
4. 选择录音、WAV 语音包或已经配置好的 TTS 服务。
5. 在[沙盒](client/atis-configuration/sandbox.md)核对文字和音频，然后上线。

## 数据目录

Windows 用户数据位于：

```text
%LOCALAPPDATA%\ceruleanavi.atis-for-can
```

其中包含 `AppConfig.json`、`Profiles`、`VoicePacks` 和 `Logs` 等内容。程序文件目录与用户数据目录不是同一概念。不要把自己的 `AppConfig.json` 放入公共发布包。

## 更新客户端

源码保留了自动更新机制，但它只在兼容的安装包和更新源有效时工作。当前 CAN 更新源的服务端格式尚需与 Velopack 配套验证，因此不能保证每种包都会自动更新。

压缩包用户可先退出程序、备份数据目录，再按照发布说明替换完整程序目录。**客户端更新**与[配置文件自动更新](client/automatic-profile-updates.md)是不同机制。

## 其他系统

只有 CAN 正式发布对应安装包后，才按该包说明安装 macOS 或 Linux 版本。不要将原版的 `.app`、AppImage 下载地址当作 CAN 发行版。
