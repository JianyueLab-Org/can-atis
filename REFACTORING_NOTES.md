# 重构说明

## 项目结构优化 (2026-10-22)

### 主要变更

1. **项目重命名**
   - `vATIS.Desktop` → `CanAtis.Desktop`
   - `Vatsim.Network` → `Can.Network`
   - `vATIS.Desktop.Tests` → `CanAtis.Desktop.Tests`
   - `vATIS.slnx` → `CanAtis.sln`

2. **命名空间更新**
   - `Vatsim.Vatis.*` → `CanAtis.*`
   - `Vatsim.Network.*` → `Can.Network.*`

3. **编译输出优化**
   - 新的统一输出目录: `build/win-x64/`
   - 主程序名称: `atis-for-can.exe`

### 目录结构

```
ATIS-for-CAN-main/
├── src/
│   ├── CanAtis.Desktop/       # 主应用程序 (原 vATIS.Desktop)
│   ├── Can.Network/           # 网络通信库 (原 Vatsim.Network)  
│   └── CanAtis.Desktop.Tests/ # 单元测试 (原 vATIS.Desktop.Tests)
├── NativeAudio/               # 本地音频库
├── build/                     # 编译输出目录
│   └── win-x64/              # Windows x64 发布版本
├── CanAtis.sln               # 解决方案文件 (原 vATIS.slnx)
├── README.md
├── LICENSE
└── REFACTORING_NOTES.md      # 本文件
```

### 功能验证

- ✅ 所有项目成功编译
- ✅ 单元测试项目编译通过
- ✅ 发布构建成功
- ✅ 所有功能保持不变
- ✅ 删除了所有 VATSIM 品牌字样

### 技术细节

- 框架版本: .NET 10.0
- 运行时: win-x64
- 发布模式: 自包含 (self-contained)
- 主程序集: CanAtis.Desktop.dll
- 可执行文件: atis-for-can.exe
