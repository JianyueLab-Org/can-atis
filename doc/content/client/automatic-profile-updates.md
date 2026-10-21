# 配置文件自动更新

CAN 管制单位可以将配置 JSON 发布到公共地址，让客户端在启动或导入配置后检查更新。

## 配置字段

```json
{
  "name": "CAN Guangzhou",
  "id": "3068994d-f5a0-46db-a5d1-9b0fb8e67cc5",
  "updateUrl": "https://example.com/can/profiles/guangzhou.json",
  "updateSerial": 2026102201,
  "stations": []
}
```

示例域名仅说明格式，不是已部署的 CAN 配置源。

## Update URL

`updateUrl` 是公开可读取的配置 JSON 地址。检查时会加 `ts` 时间戳避免旧缓存，例如 `?ts=1792670400`；已有查询参数时追加。

## Update Serial

远端 `updateSerial` 大于本地值（或本地无版本号）时安装。建议格式 **YYYYMMDD##**，末两位为同日修订序号 00–99。

## 覆盖规则

更新自动执行，不弹出确认。大部分本地修改会被远端覆盖，但保留用户自定义的配置名称与各席位的 WAV 语音包选择。

失败时保留当前配置，并写日志，常见原因包括地址无效、网络失败或 JSON 格式错误。这与程序本身的自动更新机制无关。
