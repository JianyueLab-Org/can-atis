# 欢迎使用 ATIS for CAN

**ATIS for CAN** 是 Cerulean Aviation Network（CAN）的机场自动终端情报服务客户端。它根据 METAR、机场运行情况和航行通告生成文字与语音 ATIS，并通过 CAN 网络向飞行员提供服务。

本手册覆盖原版 vATIS 文档的完整章节结构，并将操作说明、服务地址和网络概念适配为 CAN。界面仍有英文标签，文档采用“中文说明（English Label）”便于查找。

## 从这里开始

1. 阅读[系统要求](system-requirements.md)并完成[安装](installation.md)。
2. 在[用户设置](client/user-settings.md)中填写自己的 CAN ID 和网络密码。
3. 导入所属管制单位提供的[配置文件](client/profiles.md)。
4. 配置席位频率、预设和语音方式，通过[沙盒](client/atis-configuration/sandbox.md)检查输出。
5. 在[主窗口](client/main-window.md)选择预设，再连接 CAN。

## CAN 服务与当前功能状态

| 服务 | 当前配置 | 用途 |
| --- | --- | --- |
| FSD | `fsd.ceruleanavi.net:6809` | ATIS 席位上线、文字情报回复 |
| Mumble | `audio.ceruleanavi.net:64738` | 语音连接与频率频道 |
| 数据源 | `https://data.ceruleanavi.net/v1/data.json` | CAN 网络与气象数据 |
| D-ATIS | `https://ceruleanavi.net/api/v1/atis` | 获取服务覆盖机场的数字情报 |

> 当前源码已包含录音、WAV 拼接和 Mumble 发送逻辑。Mumble worker 的依赖仍是旧版 pymumble，尚不能把 UDP 音频和持续循环播报视为已完成的发布功能。TTS 列表虽可显示原版音色，面向全体 CAN 用户的合成后端尚未接通。详见[常见问题](faq.md)。

## CAN 与原版的差异

- 网络连接使用 CAN 账号，不需要在文档、配置包或安装包中附带任何人的个人账号。
- CAN 版不连接原版 ATIS Hub，跨用户席位订阅、字母同步和接班保留不能照搬原版行为。
- 原版 TTS 是远端服务，不是客户端自带语音模型。可选音色列表不代表已经能合成。
- 本项目主要提供 Windows 源码构建流程；其他系统不能仅凭原版支持就视为已有 CAN 安装包。

## 关于本手册

本手册依据 [vATIS 文档](https://vatis.app/docs/)及其公开源码整理、汉化和改编。保留技术变量、接口名称和英文通播示例；各页页脚可打开原文及参考截图。完整目录在左侧，窄屏时位于页面上方。
