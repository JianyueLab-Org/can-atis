# 结束语

结束语用于提醒飞行员确认情报字母。文字和语音分别设置：

- `{letter}`：当前字母，如 A。
- `{letter|word}`：字母的通话词，如 ALPHA。

预设可通过 `[CLOSING]` 在指定位置插入。启用 **Automatically include closing statement at the end of the ATIS** 后，若模板没有相应结束语变量，会自动追加到末尾。

示例语音：`ADVISE ON INITIAL CONTACT YOU HAVE INFORMATION {letter|word}`。
