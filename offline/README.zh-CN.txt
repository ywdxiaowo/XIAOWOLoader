XIAOWOLoader Windows x64 完全离线版

使用方法

1. 确认游戏是 64 位 Windows Unity 游戏。
2. 将压缩包内的全部文件解压到游戏主程序 .exe 所在目录。
3. 启动游戏。首次启动 IL2CPP 游戏需要生成程序集，耗时会比后续启动长。
4. 本包的程序集生成过程不会访问 RemoteAPI、GitHub 或 CDN。

覆盖安装

- 可以覆盖 version.dll 和 MelonLoader 目录中的加载器文件。
- 请保留自己的 Mods、Plugins 和 UserData 目录。
- 不要再使用官方 MelonLoader Installer 执行修复或更新，否则可能被替换回官方版本。

范围与限制

- 包含本版本发布时 MelonLoader.UnityDependencies 已发布的全部 Unity 依赖。
- 发布后才出现的新 Unity 版本，需要下载更新后的离线包。
- 游戏本身、MOD 自己实现的联网功能以及第三方 MOD 的更新检查不受本离线模式控制。
- 仅支持 x64；32 位游戏请使用标准 x86 包并保持网络可用。
- 随包分发的 Unity 运行库及第三方组件仍分别受其原始许可条款约束。
