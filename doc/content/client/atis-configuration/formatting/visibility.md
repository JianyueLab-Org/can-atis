# 能见度

预设使用 `[VIS]` 或 `[PREVAILING_VISIBILITY]`；元素内部使用 `{visibility}`，例如 `10SM`。

## ICAO 设置

非 FAA 席位提供额外选项：

- **Include visibility unit suffix**：读出数值后的单位，例如 FIVE THOUSAND METERS。
- **Meters Cutoff**：达到阈值后由米切换为千米。
- **9999 (Voice / Text)**：分别指定 9999 时的语音和文字输出。
- **CAVOK (Voice / Text)**：分别指定 CAVOK 的语音和文字，默认 CAVOK。
- **Visibility Direction**：给 `1200SE 6000N` 等方向设置语音读法。

## Direction (Text)

非 FAA 席位可独立配置文字方向，默认保留 METAR 中的方向缩写，不受语音方向表影响。

## Unit Words (Voice)

配置米、千米的语音词汇，默认 `meters`、`kilometers`。只有包含单位后缀时才使用。

## Fractions (Voice)

FAA 席位可设置 `1/2SM`、`1 1/2SM` 等分数能见度的朗读文本。文字仍保留 METAR 原值。
