# ATIS for CAN 中文文档

用浏览器打开 [index.html](index.html) 即可阅读完整文档，不需要服务器或外部脚本。

- `content/`：中文 Markdown 源文件，与原站正文页面一一对应。
- `assets/site.css`：网页样式。
- `assets/images/`：上游原版操作参考截图，本地可离线查看；截图内的英文及原版品牌保留。
- `build.py`：将中文源文件生成带导航、页内目录、上一页和下一页的静态 HTML。
- `installation.html`：安装说明入口。

重新生成：

```powershell
python -m pip install -r doc/requirements.txt
python doc/build.py
python doc/build.py --check
```

## 来源及适配

原文：[vATIS 文档](https://vatis.app/docs/)，源码：[vatis-project/website](https://github.com/vatis-project/website)。
参考提交：`39a2e70a0ac2ab22c0711013f4b2ce22e817fd96`。

本目录是面向 Cerulean Aviation Network 的中文改编版，非 vATIS 官方中文站。
保留协议字段、模板变量、METAR、英文通播示例及界面英文名称，便于与程序对照。
CAN 的服务配置和功能状态以本项目实际实现为准。原版隐私政策不作为 CAN 的隐私承诺。
原站另有拼写错误的 `main-widnow.html` 重定向，本地同样提供兼容入口。

首次补取参考截图可运行 `python doc/build.py --fetch-images`，按上述提交下载，之后正常构建无需联网。
各页均说明截图来自原版，并提供对应原文链接以便核对。
