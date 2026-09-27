# 附录 B：GTFO 的 BepInExPack 只能作为跨游戏对照

> 原为《R.E.P.O. Mod 开发通用规范》§11.3，2.0 版起移入附录：它是一次跨游戏调研记录，不属于通用规范。

已核对 Thunderstore 上 GTFO 的包页和 `BepInEx-BepInExPack_GTFO-3.2.2` 下载包。它不是通用 BepInExPack 的同名版本，也不能替换 R.E.P.O. 的依赖：

- 最新包版本为 `3.2.2`，manifest 中没有额外的 Thunderstore 依赖；README 标明基础为 **BepInEx 6.0.0-be.665**。
- GTFO 走 IL2CPP 工作流。包中包含：
  - `BepInEx.Unity.IL2CPP.dll`
  - Il2CppInterop / Cpp2IL
  - 随包附带的 CoreCLR/.NET 运行时
  - `GTFO-API.dll` 0.5.0
- 包内容位于 `BepInExPack_GTFO/` 子目录。README 的手动安装步骤明确要求先解压，再把该目录的内容复制到 GTFO 游戏根目录。
- Thunderstore 发布元数据仍然位于 ZIP 根目录：`manifest.json`、`README.md`、`icon.png`。因此"发布元数据必须在 ZIP 根目录"和"payload 可以按安装器约定放在子目录"可以同时成立。
- 该包使用 bleeding-edge 的 BepInEx 6，并明确提示不保证稳定。不能把它的程序集、GTFO-API、CoreCLR payload、IL2CPP hook 方法或编译目标照搬到 R.E.P.O.。

R.E.P.O. 使用 Mono/BepInEx 5，manifest 应继续依赖本社区对应的 `BepInEx-BepInExPack-5.4.2305`。开发和构建引用必须来自 R.E.P.O. 的 profile 与 R.E.P.O. 的 `Assembly-CSharp.dll`。不同社区中团队名相同，不代表二进制兼容。

可以借鉴的是做法，而不是文件：

- 锁定框架版本；
- 记录上游 build；
- 提供游戏专用的默认配置；
- 说明手动安装时的目录边界；
- 在 changelog 中标出不兼容的运行时切换；
- 让游戏 API 适配层与通用插件代码保持分离。

官方/一手来源：

- GTFO package page: <https://thunderstore.io/c/gtfo/p/BepInEx/BepInExPack_GTFO/>
- Thunderstore package API metadata: <https://thunderstore.io/api/experimental/package/BepInEx/BepInExPack_GTFO/>
- 包内 manifest/README：来自该页面指向的官方 `3.2.2` 下载包，只在本地临时目录中检查，没有提交任何二进制文件。
