# 备注风

非 FAA 席位使用 `[RMK_WIND]`，处理 METAR 备注中的特定高度风信息。

默认文字：`WIND {height}FT {dir}/{speed}KT`。

默认语音：`Wind {height} feet {dir} degrees {speed} knots`。

| 变量 | 含义 |
| --- | --- |
| `{height}` | 观测高度，英尺；语音逐位 |
| `{dir}` | 风向度数或 VRB；语音逐位或 variable |
| `{speed}` | 节为单位的风速及阵风，如 03G10；语音用 gusting 引出阵风 |

文字与语音模板分别设置，未报告的备注风不会自动补造。
