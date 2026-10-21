# 观测时间

在预设中使用 `[OBS_TIME]` 插入 METAR 观测时间。

| 内部变量 | 说明 |
| --- | --- |
| `{time}` | 完整时间，如 0153 |
| `{hour}` | 小时，如 01 |
| `{minute}` | 分钟，如 53 |
| `{day}` | 月中的日期，如 16 |
| `{special}` | 特别报告或不在例行分钟的报告输出 SPECIAL；文字和语音均可用 |
| `{type}` | SPECI 或 METAR，仅文字可用 |

## 例行观测分钟

**Routine Observation Time** 指定每小时正常发布的分钟，例如 `53`。一小时多次可写 `15,45`。

SPECI 报告，或报文分钟不在此列表时，被视为特别更新，模板中的 `{special}` 会输出 SPECIAL。时间为 METAR 的 UTC 时间，不是本地时区。
