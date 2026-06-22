---
name: sts2-ritsulib-modding
description: Build or modify Slay the Spire 2 mods that use RitsuLib as a required dependency. Use when Codex needs a Chinese-first workflow for RitsuLib project setup, `ModInitializer` entrypoints, `RegisterCard` or `RegisterRelic` auto-registration, `ModCardTemplate` or `ModRelicTemplate` content authoring, BaseLib-to-RitsuLib migration, or debugging why a RitsuLib-based mod is not loading or registering content.
---

# STS2 RitsuLib Modding

## Overview

把这个 skill 当成“RitsuLib 前置 STS2 Mod 开发工作流”。它不替代 BaseLib skill；当仓库已经依赖 `STS2-RitsuLib`，或任务明确要求 `RegisterCard`、`ModTypeDiscoveryHub.RegisterModAssembly`、`ModCardTemplate`、`ModRelicTemplate`、`RitsuLibFramework` 时，优先使用这个 skill。

这个 skill 只负责 RitsuLib 共性工作流，不承载单一仓库的日志路径、命名链约束或 feature-family 经验。若当前仓库还有自己的 agent 文档或 repo overlay skill，先读这个通用层，再继续读仓库层。

## Updated Reference Roots

- `D:\My Projects\GodotProjects\the-knowledge-demon\.agents\RitsuLib-doc\RitsuLib`
  - 新版教程主入口，优先看这里的 `01 - 添加基础内容`、`02 - 玩法基底`、`03 - 模组工具`。
- `D:\My Projects\GodotProjects\the-knowledge-demon\.agents\RitsuLib-doc`
  - RitsuLib 文档仓库根目录，含 `Basics`、`Migrations`、`Visuals` 等非 RitsuLib 专属章节。
- `D:\My Projects\GodotProjects\STS2-RitsuLib-0.4.33`
  - 当前 RitsuLib 源码/API 权威；用于确认注册器、模板基类、生命周期事件、patch helper、关键词和工具实现。
- `D:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64`
  - 当前本机运行时游戏程序集目录。
  - 其中 `sts2.xml` 是游戏本体程序集 XML 文档索引；新版已为不少原生类型和方法补了注释，排查原生 Hook、参数名和 public API 时优先一起看。
- `D:\My Projects\GodotProjects\the-queen`
  - RitsuLib 实战示例项目。只在需要完整项目组织参考时查看。
- `D:\My Study\Slay The Spire 2 Original`
  - 当前反编译源码主入口，优先查该目录下 `src\Core` 的具体系统源码。

默认按照这条优先级取证并落地方案：

1. `RitsuLib-doc`
2. `STS2-RitsuLib-0.4.33`
4. 当前工作仓库
5. 游戏运行时 `sts2.xml`
6. `the-queen`
7. 游戏反编译源码

## Quick Start

1. 先判断任务类型：项目初始化、卡牌、遗物、能力、药水、事件、角色、时间线/附魔、迁移、排错。
2. 打开 [references/workflows.md](references/workflows.md)，选对应工作流。
3. 如果问题是“回合开始该挂哪一种 / 哪个 hook 更窄更安全 / 是模型 override 还是 lifecycle event”，先读 [references/timing-map.md](references/timing-map.md)。
4. 打开 [references/source-map.md](references/source-map.md)，确认本机可用的权威路径。
5. 如果任务涉及 BaseLib 写法迁移，先看 [references/migration-map.md](references/migration-map.md)。
6. 如果任务需要项目组织参考或目录落点，先看 [references/project-patterns.md](references/project-patterns.md)。
7. 如果当前仓库还有自己的 agent 文档或 overlay skill，继续加载仓库层。
8. 需要找真实符号、特性、注册器、示例时，运行 `scripts/find-ritsulib-symbol.ps1`，不要手写长搜索命令。

## Working Rules

- 优先使用 RitsuLib 已公开的注册/模板/内容包能力，不要先上 Harmony。
- 只有在 RitsuLib 没覆盖目标行为，或者任务显式要求改原生流程时，才看 Harmony 或反编译代码。
- 先确认“入口是否注册正确”，再写内容代码。很多“卡牌没出现”“遗物没加载”本质上是初始化或依赖问题。
- 先做最小可运行切片：入口、依赖、1 个内容类型、本地化、图标或场景路径，然后再扩展复杂联动。
- 当概念已明确，但 public 接口名、override 面、参数名、返回值语义还不确定时，先查 `STS2-RitsuLib-0.4.33` 源码。
- 当前游戏 API `0.105+` 分支默认把 manifest 当成新版口径处理：`min_game_version` 必填；依赖优先写对象形态并补 `min_version`，不要继续沿用旧字符串依赖数组。
- 对 Harmony / patcher 直连原生方法时，不要只按旧代码记忆写 patch 形参；参数名也要对照运行时 `sts2.xml` 或反编译确认，例如 `choiceContext` 不能随手写成 `context`。
- 遇到 hook/时机选择问题，不要凭感觉选最宽的入口；先看 `timing-map.md`，再决定是模型 override、RitsuLib lifecycle event，还是更窄的仓库 base/helper。
- `the-queen` 是可选 RitsuLib 实战案例，但不要无脑照抄。先抽取结构模式，再贴合当前仓库实现。
- 如果仓库已经有自己的 working example、agent 文档、技能 overlay 或命名/资源约定，优先在仓库内找同类参照，不要强推通用模板。
- 需要跨存档、联机或显示稳定性的概率/倍率/百分比状态，优先存成稳定整型刻度（如 basis points），不要默认直接持久化 `float` / `decimal`。
- 游戏 API 漂移若只影响单条功能链，优先做 feature-local compat helper；不要一上来把兼容逻辑扩成全局 patch 或全仓抽象。
- 对依赖本地化、UI 节点树或延后初始化对象的 patch，优先考虑 required patcher + deferred patcher 分层；延迟 patch 要和明确初始化条件绑定。
- 多阶段复杂玩法链若有来源、目标、交付方式、bonus/fallback 等多维状态，优先增加一个窄 `...ExecutionContext` / `...ResolveContext` 收口，而不是直接扩成大框架。
- 构建 warning 不是纯样式噪音；若它暴露空引用、不安全重载、可空边界失真，默认按潜在 runtime 薄弱点处理。
- 对选择、生成、抽取类 API，默认接受“空结果/数量不符即安全返回”的保守语义，不要强行继续结算。

## Task Routing

### 项目初始化与依赖

处理这些请求时优先看：

- `RitsuLib-doc\README.md`
- `STS2-RitsuLib-0.4.33\docs`
- 当前项目的 `*.csproj`
- 当前项目的 manifest / mod json
- 当前项目的入口文件

重点检查：

- `STS2-RitsuLib` DLL 或 NuGet 引用是否存在
- 当前 `STS2-RitsuLib-0.4.33` 源码与项目引用版本是否匹配
- 运行时 `sts2.dll` 与 `sts2.xml` 是否存在，且和当前参考的 API 分支一致
- mod manifest 是否声明 `min_game_version`
- mod manifest `dependencies` 是否包含 `STS2-RitsuLib`，以及当前分支是否应改为对象写法并补 `min_version`
- 入口是否包含 `RitsuLibFramework.EnsureGodotScriptsRegistered(...)`
- 入口是否包含 `ModTypeDiscoveryHub.RegisterModAssembly(modId, assembly)`

### 添加卡牌

优先看：

- `RitsuLib-doc\01 - 添加卡牌\README.md`
- `STS2-RitsuLib-0.4.33\Interop\AutoRegistration\RegistrationAttributes.cs`
- `STS2-RitsuLib-0.4.33\Scaffolding\Content\ModCardTemplate.cs`
- `D:\My Projects\GodotProjects\the-queen\Cards\`

默认流程：

1. 先确认卡池类型和基类。
2. 用 `[RegisterCard(typeof(...Pool))]` 而不是 BaseLib 的 `[Pool(...)]`。
3. 如果接口名、virtual 面、构造参数或 helper 返回值不清楚，先查 `STS2-RitsuLib.xml`。
4. 用 `ModCardTemplate` 或项目自定义卡牌基类，而不是 `CustomCardModel`。
5. 补齐图片、本地化、必要的 starter 或 token 约定。

### 添加遗物

优先看：

- `RitsuLib-doc\03 - 添加新遗物\README.md`
- `STS2-RitsuLib-0.4.33` 中 `RegisterRelic` 和 `ModRelicTemplate`
- 当前仓库遗物目录
- `the-queen` 的遗物目录与基类模式

默认检查点：

- 是否使用 `[RegisterRelic(typeof(...Pool))]`
- 是否继承 `ModRelicTemplate` 或项目自定义遗物模板
- 如果 override 面或 helper 入口不清楚，先查 `STS2-RitsuLib-0.4.33` 源码。
- 是否补齐 `relics.json` 和图标路径
- 是否需要 starter relic 或角色绑定

### 添加能力、药水、事件、角色、时间线、附魔

直接按章节走：

- 能力: `05 - 添加新能力`
- 药水: `06 - 添加新药水`
- 时间线: `09 - 添加时间线`
- 事件: `12 - 添加新事件`
- 附魔: `13 - 添加新附魔`
- 角色: `14 - 添加新人物`

先看 RitsuLib 文档章节；如果 public 接口面不清楚，先补查 `STS2-RitsuLib-0.4.33` 源码；再对照 `the-queen` 或当前仓库已有实现。

做附魔时默认额外确认一件事，不要省略：

- 先明确 `CanEnchant(CardModel card)` 的目标牌型与边界。
- 默认优先实现 `CanEnchantCardType(CardType)`，先把合法牌型在类型层收死。
- 默认不要只写 `base.CanEnchant(card)` 就结束。
- 推荐写法是双层判断：
  - `CanEnchantCardType(CardType)` 负责限制 `Attack`、`Skill` 或其它明确牌型集合
  - `CanEnchant(CardModel card)` 再补关键词、已有状态、额外限制
- 只要场景是“从牌组里选择可附魔卡牌”，优先使用 `CardSelectCmd.FromDeckForEnchantment(...)` 这类专用入口，而不是 generic deck picker 再手写一层附魔 filter。
- 至少要确认它是否应限制为：
  - `CardType.Attack`
  - `CardType.Skill`
  - `Attack or Skill`
  - 或其它明确牌型集合
- 不要只靠事件、遗物、休息房等调用点的 filter 去兜底错误牌型；牌型约束应优先落在附魔模型自身。
- 如果用户没指定，默认主动确认或从同类成品中找出稳定口径；否则很容易把本不该吃附魔的牌型也放进候选，后续再补兼容会更贵。

### BaseLib -> RitsuLib 迁移

遇到这些请求时，先打开 [references/migration-map.md](references/migration-map.md)：

- “把这个 BaseLib 卡牌/遗物改成 RitsuLib”
- “`[Pool]` 在 RitsuLib 里对应什么”
- “为什么 ID 格式变了”
- “角色池、starter card、starter relic 怎么换”
- “事件、先古之民、升级替换在 RitsuLib 怎么写”

迁移时默认先替换这几类内容：

1. 注册特性
2. 内容基类
3. ID 约定
4. 关键词与动态变量
5. 角色/卡池/起始内容
6. 事件与先古之民
7. 升阶与替换映射

### 排错与定位

当用户说“没注册成功”“mod 没加载”“卡不出现”“Godot 脚本没绑定”“DLL 路径不对”时：

1. 先检查入口：`[ModInitializer]`、`EnsureGodotScriptsRegistered`、`RegisterModAssembly`
2. 再检查 manifest：`min_game_version`、`dependencies` 写法、依赖最低版本
3. 再检查 `STS2-RitsuLib-0.4.33`、`sts2.dll` / `sts2.xml` 是否都在、是否和本地引用链一致
4. 再检查 `csproj`：RitsuLib、`sts2.dll`、`0Harmony.dll` 引用路径
5. 再检查本地化、图片、场景路径
6. 如果是 patch apply 失败，先对照运行时 XML / 反编译确认 target 签名与 patch 形参名，再怀疑逻辑本身
7. 如果核心问题是“该挂哪一种时机”，回到 `timing-map.md`
8. 最后才看更宽的 Harmony 重写或别的分支行为

如果怀疑符号或注册器名称写错，立刻运行：

```powershell
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "RegisterCard"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModRelicTemplate"
& "<skill>/scripts/find-ritsulib-symbol.ps1" -Pattern "ModTypeDiscoveryHub.RegisterModAssembly"
```

## Resources

### references/

- [references/source-map.md](references/source-map.md): 本机所有关键路径与权威用途
- [references/workflows.md](references/workflows.md): 各任务类型的“先看什么、再看什么”
- [references/timing-map.md](references/timing-map.md): 游戏本体 hook 与 RitsuLib lifecycle 事件的时机选择索引
- [references/migration-map.md](references/migration-map.md): BaseLib 到 RitsuLib 对照表
- [references/project-patterns.md](references/project-patterns.md): the-queen 与常见 RitsuLib 项目组织模式

### scripts/

- `scripts/find-ritsulib-symbol.ps1`: 跨 `RitsuLib-doc`、`STS2-RitsuLib-0.4.33`、the-queen、反编译代码和当前仓库搜索符号与案例
