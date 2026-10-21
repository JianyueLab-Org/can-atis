"""Build and validate the offline CAN Chinese documentation."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import html
from html.parser import HTMLParser
from pathlib import Path
import posixpath
import re
from urllib.parse import unquote, urlsplit
from urllib.request import urlopen

import markdown

ROOT = Path(__file__).resolve().parent
# path, Chinese title, navigation depth; order matches the upstream documentation.
PAGES = [
    ("index", "欢迎使用", 0), ("system-requirements", "系统要求", 0),
    ("installation", "安装与更新", 0), ("client/index", "客户端使用指南", 0),
    ("client/concepts", "术语与概念", 1), ("client/profiles", "配置文件", 1),
    ("client/main-window", "主窗口", 1), ("client/user-settings", "用户设置", 1),
    ("client/mini-window", "迷你窗口", 1), ("client/atis-station", "ATIS 席位", 1),
    ("client/airport-conditions", "机场运行情况", 1), ("client/notams", "航行通告", 1),
    ("client/voice-record-atis", "人工录制 ATIS", 1), ("client/text-parsing", "文本解析", 1),
    ("client/atis-configuration", "ATIS 配置", 1),
    ("client/atis-configuration/general", "常规设置", 2),
    ("client/atis-configuration/presets", "预设与模板", 2),
    ("client/atis-configuration/formatting", "格式设置", 2),
    *[("client/atis-configuration/formatting/" + slug, title, 3) for slug, title in [
        ("observation-time", "观测时间"), ("wind", "地面风"), ("visibility", "能见度"),
        ("runway-visual-range", "跑道视程"), ("weather", "天气现象"), ("clouds", "云况"),
        ("temperature", "气温"), ("dewpoint", "露点"), ("altimeter", "气压与高度表设定"),
        ("wind-shear", "风切变"), ("runway-condition", "跑道状况"), ("remark-wind", "备注风"),
        ("trend-forecast", "趋势预报"), ("transition-level", "过渡高度层"),
        ("automatic-observation", "自动观测"), ("notams", "通告格式"),
        ("closing-statement", "结束语")]],
    ("client/atis-configuration/contractions", "自定义缩略语", 2),
    ("client/atis-configuration/default-contractions", "默认缩略语表", 3),
    ("client/atis-configuration/sandbox", "沙盒测试", 2),
    ("client/atis-configuration/voice-packs", "WAV 语音包", 2),
    ("client/automatic-profile-updates", "配置文件自动更新", 1),
    ("client/websockets", "WebSocket API", 1), ("client/local-development", "本地开发", 1),
    ("getting-help", "获取帮助", 0), ("faq", "常见问题", 0),
    ("privacy-policy", "隐私与数据说明", 0),
]

IMAGES = {
    "client/profiles": ["ProfileDialog.png"],
    "client/main-window": ["MainScreen_Steps.png"],
    "client/user-settings": ["UserSettings.png"],
    "client/mini-window": ["MiniWindow.png", "MiniWindowControls.png", "MiniWindowFilterStations.webp"],
    "client/atis-station": ["AtisStations.png", "AcknowledgeUpdate.gif", "TypeAtisLetter.gif",
                            "MetarOverride_Menu.png", "MetarOverride_Editing.png", "MetarOverride_Overridden.png", "MetarOverride_Error.png"],
    "client/airport-conditions": ["AirportConditionsDialog.png", "StaticDefinitionEditorDialog.png"],
    "client/notams": ["NotamsDialog.png", "StaticDefinitionEditorDialog_Notam.png"],
    "client/voice-record-atis": ["VoiceRecordAtisDialog.png"],
    "client/atis-configuration/general": ["AtisConfiguration.png"],
    "client/atis-configuration/presets": ["AtisConfiguration_Presets.png", "Formatting_ExternalGenerator.png", "AtisConfiguration_Presets_Datis.png"],
    "client/atis-configuration/formatting": ["Formatting.png"],
    "client/atis-configuration/contractions": ["Contractions.png", "NewContractionDialog.png"],
    "client/atis-configuration/sandbox": ["Sandbox.png"],
    "client/atis-configuration/voice-packs": ["AtisConfiguration_WavVoicePack.png", "VoicePacks.png"],
}
for slug, images in {
    "observation-time": ["ObservationTime"], "wind": ["SurfaceWind", "SurfaceWind_VariableFormat"],
    "visibility": ["Visibility", "Visibility_ICAO", "Visibility_UnitWords", "Visibility_Fractions"],
    "runway-visual-range": ["RunwayVisualRange_Phrases"], "weather": ["Weather", "Weather_Descriptors"],
    "clouds": ["Clouds", "Clouds_Types", "Clouds_ConvectiveTypes", "Clouds_VariableFormat"],
    "temperature": ["Temperature", "Temperature_VariableFormat"], "dewpoint": ["Dewpoint", "Dewpoint_VariableFormat"],
    "altimeter": ["Altimeter", "Altimeter_VariableFormat"], "wind-shear": ["WindShear"],
    "runway-condition": ["RunwayCondition", "RunwayCondition_Wording"], "remark-wind": ["RemarkWind"],
    "trend-forecast": ["TrendForecast"], "transition-level": ["TransitionLevel", "TransitionLevel_Levels"],
    "automatic-observation": ["AutomaticObservation"], "notams": ["Notams"], "closing-statement": ["ClosingStatement"],
}.items():
    IMAGES["client/atis-configuration/formatting/" + slug] = ["Formatting_" + name + ".png" for name in images]


def fetch_images():
    folder = ROOT / "assets/images"
    folder.mkdir(parents=True, exist_ok=True)

    def fetch(name):
        target = folder / name
        if not target.exists():
            url = "https://raw.githubusercontent.com/vatis-project/website/39a2e70a0ac2ab22c0711013f4b2ce22e817fd96/assets/images/" + name
            with urlopen(url, timeout=45) as response:
                data = response.read()
            target.write_bytes(data)
        return name

    names = sorted({name for images in IMAGES.values() for name in images})
    with ThreadPoolExecutor(max_workers=8) as executor:
        downloaded = list(executor.map(fetch, names))
    print(f"Fetched {len(downloaded)} upstream reference images.")


def relative(target, page):
    return posixpath.relpath(target, posixpath.dirname(page) or ".")


def build():
    sources = {p.relative_to(ROOT / "content").with_suffix("").as_posix()
               for p in (ROOT / "content").rglob("*.md")}
    expected = {p[0] for p in PAGES}
    if sources != expected:
        raise SystemExit(f"Missing: {expected - sources}; unlisted: {sources - expected}")
    for index, (path, title, _) in enumerate(PAGES):
        source = (ROOT / "content" / (path + ".md")).read_text(encoding="utf-8")
        # Local Markdown links are authored relative to each source page.
        source = re.sub(r'(\]\([^\s)]+)\.md(?=[#)])', r'\1.html', source)
        renderer = markdown.Markdown(extensions=["tables", "fenced_code", "toc", "sane_lists"],
                                     extension_configs={"toc": {"toc_depth": "2-3"}})
        body = renderer.convert(source)
        pictures = IMAGES.get(path, [])
        if pictures:
            body += '<h2 id="reference-images">操作截图参考</h2><p>以下为上游原版截图，保留英文标签。用于定位操作；品牌、登录字段与网络行为以本页 CAN 说明为准。</p>'
            for picture in pictures:
                body += f'<figure><img loading="lazy" src="{relative("assets/images/" + picture, path)}" alt="{html.escape(title)}：原版界面操作参考 {html.escape(picture)}"><figcaption>原版界面参考 · {html.escape(picture)}</figcaption></figure>'
        nav = "".join(
            f'<a class="depth-{depth}" href="{relative(slug + ".html", path)}"'
            + (' aria-current="page"' if slug == path else '')
            + f'>{html.escape(label)}</a>' for slug, label, depth in PAGES)
        pager = []
        for offset, label in [(-1, "上一页"), (1, "下一页")]:
            n = index + offset
            if 0 <= n < len(PAGES):
                slug, name, _ = PAGES[n]
                pager.append(f'<a href="{relative(slug + ".html", path)}">{label}：{html.escape(name)}</a>')
        original = "https://vatis.app/docs/" + ("" if path == "index" else path.removesuffix("index") + ".html")
        if path == "client/index":
            original = "https://vatis.app/docs/client/"
        page = f'''<!doctype html>
<html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="description" content="ATIS for CAN 中文使用文档：{html.escape(title)}">
<title>{html.escape(title)} · ATIS for CAN</title>
<link rel="stylesheet" href="{relative('assets/site.css', path)}"></head><body id="top">
<aside><a class="brand" href="{relative('index.html', path)}">ATIS for CAN</a><div class="tag">Cerulean Aviation Network · 中文文档</div>
<label for="nav-search">查找章节</label><input id="nav-search" class="search" type="search" placeholder="输入标题关键字…" autocomplete="off">
<nav aria-label="文档导航">{nav}</nav></aside>
<main><div class="eyebrow">CAN 用户指南 / {html.escape(title)}</div><article>
<details class="toc"><summary>本页目录</summary>{renderer.toc}</details>{body}
<div class="pager">{''.join(pager)}</div></article>
<footer>ATIS for CAN 中文改编文档 · <a href="{original}">查看对应原文及原版截图</a> · <a href="#top">返回顶部</a><br>
依据 vATIS 文档改编；CAN 功能状态以当前发布版本为准。此站不是 vATIS 官方文档。</footer></main>
<script>document.getElementById('nav-search').addEventListener('input',function(){{const q=this.value.trim().toLocaleLowerCase();document.querySelectorAll('nav a').forEach(a=>a.hidden=!a.textContent.toLocaleLowerCase().includes(q));}});</script>
</body></html>'''
        destination = ROOT / (path + ".html")
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(page, encoding="utf-8")
    (ROOT / "client/main-widnow.html").write_text(
        '<!doctype html><html lang="zh-CN"><meta charset="utf-8">'
        '<meta http-equiv="refresh" content="0;url=main-window.html">'
        '<title>主窗口 · ATIS for CAN</title><a href="main-window.html">前往主窗口说明</a></html>', encoding="utf-8")
    print(f"Built {len(PAGES)} Chinese pages and 1 compatibility redirect.")


class Links(HTMLParser):
    def __init__(self):
        super().__init__()
        self.links = []
        self.ids = set()

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if "id" in attrs:
            self.ids.add(attrs["id"])
        if tag in {"a", "link", "img", "script"}:
            url = attrs.get("href", attrs.get("src"))
            if url:
                self.links.append(url)


def check():
    parsed = {}
    for file in ROOT.rglob("*.html"):
        parser = Links()
        parser.feed(file.read_text(encoding="utf-8"))
        parsed[file.resolve()] = parser
    errors = []
    for slug, _, _ in PAGES:
        source = ROOT / "content" / (slug + ".md")
        text = source.read_text(encoding="utf-8")
        if not re.search(r"[\u4e00-\u9fff]", text):
            errors.append(f"Page is not Chinese: {slug}")
        for sample in re.findall(r"```json\s*\n(.*?)\n```", text, flags=re.S):
            import json
            try:
                json.loads(sample)
            except json.JSONDecodeError as error:
                errors.append(f"{slug}: invalid JSON example: {error}")
    for file, parser in parsed.items():
        for link in parser.links:
            url = urlsplit(link)
            if url.scheme or url.netloc:
                continue
            target = (file.parent / unquote(url.path)).resolve() if url.path else file
            if not target.is_relative_to(ROOT.resolve()) or not target.is_file():
                errors.append(f"{file.relative_to(ROOT)}: missing {link}")
            elif url.fragment and target in parsed and unquote(url.fragment) not in parsed[target].ids:
                errors.append(f"{file.relative_to(ROOT)}: missing anchor {link}")
    for slug, _, _ in PAGES:
        if not (ROOT / (slug + ".html")).is_file():
            errors.append(f"Missing page {slug}")
    if errors:
        raise SystemExit("\n".join(errors))
    print(f"Checked {len(parsed)} HTML files: local links, anchors, Chinese content and JSON examples passed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--fetch-images", action="store_true", help="Download pinned upstream reference screenshots")
    args = parser.parse_args()
    if args.fetch_images:
        fetch_images()
    check() if args.check else build()
