# WebSocket API

ATIS for CAN 保留本地 WebSocket 接口，默认 `ws://127.0.0.1:49082/`，用于接收 ATIS 更新、查询配置和控制席位。字段与消息类型保持英文，以便兼容外部工具。

参考项目：[Stream Deck actions for vATIS](https://github.com/neilenns/streamdeck-vatis)，其中提供 TypeScript 消息定义。集成时应验证 CAN 版本实际返回；原版共享 Hub 的 Observer 数据在 CAN 当前未接通。

## 消息封装与席位定位

请求通常使用 `{"type":"消息名","value":{...}}`。席位通过 **id** 或 **station**（机场代码）定位，两者不可同时传；使用 station 时可传 **atisType**。具体默认值见下表。

## 入站消息

### getAtis

请求席位情报，返回一个或多个 `atis`。

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| id | string | 席位唯一 ID |
| station | string | 机场代码 |
| atisType | AtisType | 配合 station 使用，默认 Combined |

不传 id / station 返回当前配置所有席位；两者同时传返回 error。

```json
{"type":"getAtis"}
```

```json
{"type":"getAtis","value":{"id":"9d79f025-cb2e-485c-93a3-7d1b566d4afb"}}
```

```json
{"type":"getAtis","value":{"station":"ZGGG","atisType":"Departure"}}
```

### acknowledgeAtisUpdate

确认新情报，清除 isNewAtis 并停止字母闪烁。定位字段与 getAtis 相同；不传席位时确认全部，同时传 id / station 返回 error。

```json
{"type":"acknowledgeAtisUpdate"}
```

```json
{"type":"acknowledgeAtisUpdate","value":{"station":"ZGGG","atisType":"Combined"}}
```

### getProfiles

请求所有已安装配置，返回 profiles。

```json
{"type":"getProfiles"}
```

### getActiveProfile

查询当前配置，返回 activeProfile。

```json
{"type":"getActiveProfile"}
```

### getStations

请求当前配置中的席位及预设，返回 stations。

```json
{"type":"getStations"}
```

### getContractions

获取某席位或所有席位的缩略语，返回 contractions。传 id 或 station + atisType；都不传返回所有席位，同时传 id 和 station 返回 error，席位不存在则忽略。

```json
{"type":"getContractions"}
```

```json
{"type":"getContractions","value":{"station":"ZGGG","atisType":"Departure"}}
```

### loadProfile

按唯一 ID 加载配置。不存在时忽略且不返回响应。可先用 getProfiles 查 ID。

```json
{"type":"loadProfile","value":{"id":"1410d902-7b75-4157-b6a7-a124e532fd95"}}
```

### configureAtis

配置当前配置文件中的席位。席位不存在忽略，预设不存在或 id 与 station 同传时返回 error。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | string | 席位 ID，与 station 二选一 |
| station | string | 机场代码 |
| atisType | AtisType | 使用 station 时需要 |
| preset | string | 必填，预设名称 |
| airportConditionsFreeText | string | 可选，覆盖当前自由文本，不永久保存 |
| notamsFreeText | string | 可选，覆盖当前自由文本，不永久保存 |
| syncAtisLetter | boolean | 可选，默认 false，是否持续从数字情报同步字母 |

原版规则会在配置时尝试取一次真实字母；syncAtisLetter 为 true 时在连接和新 METAR 到达时再次同步，省略则恢复 false。CAN 中能否获取取决于相应接口和当前调用路径，不表示跨用户 Hub 同步可用。

```json
{
  "type": "configureAtis",
  "value": {
    "station": "ZGGG",
    "atisType": "Departure",
    "preset": "NORTH",
    "airportConditionsFreeText": "DEPARTURE RUNWAY 01.",
    "notamsFreeText": "TWY A CLSD.",
    "syncAtisLetter": false
  }
}
```

### connectAtis

连接席位。席位不存在、已连接或未选择预设时忽略；定位冲突返回 error。成功会收到 atis，失败可收到 connectionError。

```json
{"type":"connectAtis","value":{"station":"ZGGG","atisType":"Departure"}}
```

```json
{"type":"connectAtis","value":{"id":"478ad86d-edd0-47b1-972a-f68345fd3f3f"}}
```

### disconnectAtis

断开已连接席位。不存在或未连接时忽略；正在连接不会被此请求取消。id / station 同传返回 error，成功返回 atis 状态更新。

```json
{"type":"disconnectAtis","value":{"station":"ZGGG","atisType":"Departure"}}
```

### setAtisLetter

指定字母，或在范围内向前 / 向后步进，到末尾循环。席位不存在忽略；字母超范围、请求修改 Observer 席位、定位冲突或参数无效时返回 error。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id / station | string | 二选一 |
| atisType | AtisType | 配合 station，默认 Combined |
| letter | string | 一个字母，小写自动转大写 |
| step | AtisLetterStep | Next / Previous，与 letter 二选一 |

```json
{"type":"setAtisLetter","value":{"station":"ZGGG","atisType":"Departure","letter":"C"}}
```

```json
{"type":"setAtisLetter","value":{"id":"478ad86d-edd0-47b1-972a-f68345fd3f3f","step":"Next"}}
```

### quit

断开所有活动席位并正常退出程序，无响应。

```json
{"type":"quit"}
```

## 出站消息

### atis

对 getAtis 的单独响应，或席位属性变化时广播。除 id 外，无值字段可能省略。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| id | string | 席位 ID |
| station | string | 机场代码 |
| atisType | AtisType | 情报类型 |
| networkConnectionStatus | NetworkConnectionStatus | 网络状态 |
| atisLetter | string | 当前字母 |
| textAtis | string | 完整文字情报 |
| airportConditions | string | 机场运行情况 |
| notams | string | 通告 |
| isNewAtis | boolean | 是否待确认的新情报 |
| metar | string | 气象原报 |
| wind | string | 地面风 |
| altimeter | string | 格式化气压 |
| pressure | Value | 气压数值，如 2990 |
| ceiling | Value | 最低 BKN / OVC 云底高，无值省略 |
| prevailingVisibility | Value | 主导能见度 |

断开时状态为 Disconnected，isNewAtis 为 false，metar / wind / altimeter 省略，但旧字母、textAtis、pressure、ceiling、prevailingVisibility 可能仍存在。**判断在线必须看 networkConnectionStatus。** Observer 表示原版共享席位，当前 CAN 不提供该 Hub 数据源。

```json
{
  "type": "atis",
  "value": {
    "id": "668dd798-38ff-4111-87bf-88cdbe09aece",
    "station": "ZGGG",
    "atisType": "Combined",
    "networkConnectionStatus": "Connected",
    "atisLetter": "Q",
    "textAtis": "ZGGG ATIS INFO Q ...",
    "airportConditions": "ILS APPROACH IN USE.",
    "notams": "TWY A CLSD.",
    "isNewAtis": false,
    "metar": "ZGGG 221200Z 01004MPS 9999 SCT020 25/18 Q1013 NOSIG",
    "wind": "01004MPS",
    "altimeter": "Q1013",
    "pressure": {"actualValue": 1013, "actualUnit": "HectoPascal"},
    "prevailingVisibility": {"actualValue": 9999, "actualUnit": "Meter"}
  }
}
```

### error

只发给引起错误的客户端，例如格式错误或字母越界。value.message 为错误描述；程序返回的错误文字可能仍为英文。

```json
{"type":"error","value":{"message":"Letter Z is outside the code range A-M."}}
```

### connectionError

连接失败、网络拒绝或结束连接时广播。value 包含 id、station、atisType、reason（ConnectionErrorReason）和 message（界面错误描述）。

```json
{
  "type": "connectionError",
  "value": {
    "id": "478ad86d-edd0-47b1-972a-f68345fd3f3f",
    "station": "ZGGG",
    "atisType": "Departure",
    "reason": "ConnectionFailed",
    "message": "Could not connect to the network."
  }
}
```

### profiles

响应 getProfiles，顶层 profiles 数组每项包含 id 和 name，**不是 value 数组**。

```json
{"type":"profiles","profiles":[{"id":"1410d902-7b75-4157-b6a7-a124e532fd95","name":"CAN Guangzhou"}]}
```

### stations

响应 getStations，顶层 stations 数组中每项包含 id、name（席位名称）、atisType、presets（预设名称数组）。

```json
{
  "type": "stations",
  "stations": [{
    "id": "9d79f025-cb2e-485c-93a3-7d1b566d4afb",
    "name": "ZGGG",
    "atisType": "Combined",
    "presets": ["NORTH", "SOUTH"]
  }]
}
```

### activeProfile

响应 getActiveProfile，顶层 id、name 表示当前配置。

```json
{"type":"activeProfile","id":"bf94118a-37c1-4573-99b0-66059f096663","name":"CAN Guangzhou"}
```

### contractions

响应 getContractions。顶层 stations 数组包含 id、name、atisType 和 contractions 字典。字典键为变量名，值含 text 与 voice。

```json
{
  "type": "contractions",
  "stations": [{
    "id": "cefc39ea-6f17-418e-b2cb-f8bf03e0de7b",
    "name": "ZGGG",
    "atisType": "Combined",
    "contractions": {
      "RNAV": {"text": "RNAV", "voice": "R NAV"}
    }
  }]
}
```

## 数据类型

### NetworkConnectionStatus

Connected（已连接）、Connecting（连接中）、Disconnected（未连接）、Observer（观察其他发布者；CAN 当前未接通该共享来源）。

### AtisType

Arrival（进场）、Combined（综合）、Departure（离场）。

### AtisLetterStep

Next（下一字母）、Previous（上一字母）。

### ConnectionErrorReason

| 值 | 含义 |
| --- | --- |
| TooManyConnections | 达到连接数限制；具体限制以 CAN 实现为准 |
| ConfigurationRequired | CAN ID、网络密码或姓名未配置 |
| ConnectionFailed | 无法建立网络连接 |
| NetworkError | 网络返回错误，如呼号被占用或账号无效 |
| Killed | 网络主动断开席位 |

### Value

对象包含数值 `actualValue`（Number）和单位 `actualUnit`（Unit）。保留原单位语义，不要仅依据字段名猜测换算。

### Unit

支持 None、Degree、DegreeCelsius、Feet、HectoPascal、KilometerPerHour、Knot、MercuryInch、Meter、MeterPerSecond、StatuteMile、UnknownUnit。
