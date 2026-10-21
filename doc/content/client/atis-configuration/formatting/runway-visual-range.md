# 跑道视程（RVR）

预设使用 `[RVR]`，内容来自 METAR 中的跑道视程组。

## Spoken

**Spoken text** 设置 RVR 的引导词，默认 `R-V-R`，留空省略。**Tendencies (Spoken)** 分别设置 N（无明显变化）、U（上升）和 D（下降）的读法。

## Phrases (Voice)

单一距离使用 `{value}`；变化范围使用 `{min}`、`{max}`。

| 场景 | 默认语音短语 |
| --- | --- |
| 小于 | `less than {value}` |
| 大于 | `more than {value}` |
| 范围变化 | `variable between {min} and {max}` |
| 起点小于 | `variable from less than {min} to {max}` |
| 终点大于 | `{min} variable to greater than {max}` |
| 起点小于且终点大于 | `variable from less than {min} to greater than {max}` |

## Text

文字趋势可独立设置，默认显示 N / U / D 原报码，不影响语音趋势。
