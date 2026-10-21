# 气压与高度表设定

预设使用 `[PRESSURE]` 或 `[QNH]`，元素内部支持：

| 变量 | 含义 |
| --- | --- |
| `{altimeter}` | 数值，如 2998 或 1013 |
| `{altimeter\|inhg}` | 转为英寸汞柱 |
| `{altimeter\|hpa}` | 转为百帕 |
| `{altimeter\|text}` | 数值展开为词语，例如 TWO NINER NINER TWO |
| `{qfe\|ELEVATION_FT}` | 按机场或入口标高（英尺）计算 QFE，仅非 FAA 模式 |

模板的另一机场气压和跑道入口 QFE 用法见[预设](../presets.md)。

## Variable Format

altimeter、altimeter|text、inhg、hpa 和 qfe 都有独立行，各行分别设置 Text / Voice 的 Style 与 Suffix。

- Default：内置文字数字、语音逐位。
- Digits：数字，如 1013。
- Serial：逐位词语，如 ONE ZERO ONE THREE。
- Group：成组词语，如 TEN THIRTEEN。

后缀精确追加，按需加空格。`{altimeter|text}` 既能放在文字模板，也能放在语音模板。
