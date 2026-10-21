# 露点

预设变量 `[DEW]`。内部变量 `{dewpoint}` 为露点温度，`{prefix_symbol}` 仅文字可用。

可启用正温 PLUS 前缀与语音前导零，例如 DEWPOINT PLUS TWO FIVE、DEWPOINT ZERO SIX。

## 文字格式

| 模板 | 示例行为 |
| --- | --- |
| `{dewpoint}` / `{dewpoint:##}` | 默认两位 |
| `{dewpoint:#}` | 08 → 8 |
| `{dewpoint:M}` / `{dewpoint:##M}` | M05 → 05 |
| `{dewpoint:##-}` | M05 → -05 |
| `{dewpoint:##'MS'}` | M05 → MS05 |

## Variable Format

配置文字后缀、语音样式和语音后缀。文字数字格式在模板中设置；语音支持 Default、Digits、Serial、Group。后缀按原样附加，不会自动补空格。
