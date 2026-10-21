# 气温

预设变量 `[TEMP]`。内部变量 `{temp}` 为温度，`{prefix_symbol}` 是正负符号，只可用于文字模板。

- **Prefix positive temperature with plus**：正温读作 PLUS TWO FIVE。
- **Speak leading zero**：06 读作 ZERO SIX。

## 文字格式

| 模板 | 示例行为 |
| --- | --- |
| `{temp}` / `{temp:##}` | 默认两位 |
| `{temp:#}` | 08 显示为 8 |
| `{temp:M}` / `{temp:##M}` | 去掉负温 M，M05 → 05 |
| `{temp:##-}` | M05 → -05 |
| `{temp:##'MS'}` | M05 → MS05 |

## Variable Format

`{temp}` 行提供 Text suffix、Voice style 和 Voice suffix。文字位数由模板本身决定，没有单独的文字 Style。语音可选 Default（内置逐位）、Digits、Serial、Group。后缀按原样追加，必要时包含前导空格。
