# 获取帮助

优先通过 [CAN 网站](https://ceruleanavi.net/)公布的支持渠道，或项目仓库的 Issues 联系维护者。机场运行配置问题可联系本管制单位的配置维护人员。

## 反馈问题时提供

1. ATIS for CAN 版本和操作系统版本。
2. 所选机场、ATIS 类型、预设及语音方式。
3. 复现操作、预期结果和实际结果。
4. 可公开的 METAR、模板片段、错误提示和截图。
5. 必要时附相关日志片段，路径见[常见问题](faq.md)。

分享之前检查内容，去掉个人账号、密码、令牌及不相关的私人数据。不要把完整 AppConfig 当作问题附件。

## 按症状定位

- 启动或安装失败：[安装](installation.md)、[系统要求](system-requirements.md)。
- 内容与发音不符：[文本解析](client/text-parsing.md)、[格式设置](client/atis-configuration/formatting.md)。
- WAV 缺词：[语音包](client/atis-configuration/voice-packs.md)。
- 文字可用但没有语音：[FAQ](faq.md)中的语音链路说明。
- 外部工具连接失败：[WebSocket API](client/websockets.md)。

本手册依据原版 vATIS 文档改编，但 CAN 的登录、Mumble 和发布问题应反馈给 CAN 维护者；原版项目的支持渠道不负责 CAN 服务部署。
