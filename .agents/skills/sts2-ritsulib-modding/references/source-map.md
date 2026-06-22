# Source Map

## 路径总表

### 当前仓库

- `D:\My Projects\GodotProjects\the-knowledge-demon`
  - 当前 RitsuLib 前置 mod 项目。
  - 用于确认真实入口、依赖声明、目录约定、本地化文件位置、现有实现风格。

### RitsuLib 教程文档

- `D:\My Projects\GodotProjects\the-knowledge-demon\.agents\RitsuLib-doc`
  - RitsuLib 文档仓库根目录。
  - 含 `RitsuLib`、`Basics`、`Migrations`、`Visuals` 等章节。
- `D:\My Projects\GodotProjects\the-knowledge-demon\.agents\RitsuLib-doc\RitsuLib`
  - RitsuLib 专属教程主入口。
  - 优先从 `01 - 添加基础内容`、`02 - 玩法基底`、`03 - 模组工具` 这三组新版章节开始。

### RitsuLib 源码

- `D:\My Projects\GodotProjects\STS2-RitsuLib-0.4.33`
  - 当前 RitsuLib 源码/API 权威。
  - 用于确认注册器、AutoRegistration 特性、模板基类、内容包构建器、关键词注册、生命周期事件、patch helper、文档中提到的方法签名。

### 游戏运行时程序集目录

- `D:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64`
  - 当前本机实际加载的游戏本体程序集目录。
  - 关键文件：
    - `sts2.dll`
      - 运行时游戏程序集。
      - 用于确认 `csproj` 引用目标和游戏内真实 DLL 是否一致。
    - `sts2.xml`
      - 游戏本体 C# 程序集 XML 文档索引。
      - 适合确认 Hook 目标、参数名、summary 与当前分支 public API。
      - 当 Harmony patch 因 target 签名或形参名失败时，优先对照这里。

### RitsuLib 实战案例

- `D:\My Projects\GodotProjects\the-queen`
  - 结构参考项目。
  - 用于确认一个完整 RitsuLib mod 如何组织入口、卡牌目录、角色池、关键词、Power、Events、Relics、Commands、Content 等模块。

### 游戏反编译代码

- `D:\My Study\Slay The Spire 2 Original`
  - 原生类型、枚举、场景和行为权威。
  - 优先看 `src\Core` 下与目标系统对应的源码树。
  - 当 RitsuLib 文档和 RitsuLib 源码都没有覆盖、或需要确认底层原生行为时再看。

## 章节路由

- `RitsuLib\01 - 添加基础内容\01 - 添加卡牌`: 新卡牌与 starter/token 相关请求
- `RitsuLib\01 - 添加基础内容\02 - 自定义配置`: 配置系统与设置项
- `RitsuLib\01 - 添加基础内容\03 - 添加新遗物`: 自定义遗物
- `RitsuLib\01 - 添加基础内容\04 - 添加卡牌属性`: 动态变量或卡牌属性
- `RitsuLib\01 - 添加基础内容\05 - 添加新能力`: Power
- `RitsuLib\01 - 添加基础内容\06 - 添加新药水`: Potion
- `RitsuLib\01 - 添加基础内容\07 - 添加先古之民`: 先古之民
- `RitsuLib\01 - 添加基础内容\08 - 添加充能球`: 充能球/球体
- `RitsuLib\01 - 添加基础内容\09 - 添加时间线`: Timeline
- `RitsuLib\01 - 添加基础内容\11 - 添加新怪物`: Monster
- `RitsuLib\01 - 添加基础内容\12 - 添加新事件`: Event
- `RitsuLib\01 - 添加基础内容\13 - 添加新附魔`: Enchantment
- `RitsuLib\01 - 添加基础内容\14 - 添加新人物`: Character
- `RitsuLib\01 - 添加基础内容\15 - 添加单例`: Singleton
- `RitsuLib\02 - 玩法基底\01 - 血条覆盖`: 血条覆盖/相关 UI
- `Basics\05 - 变量与描述`: 动态变量、描述、本地化显示
- `Migrations\02 - BaseLib 至 RitsuLib`: BaseLib 迁移到 RitsuLib

## 使用顺序

1. 先从 `.agents\RitsuLib-doc\RitsuLib` 找新版章节。
2. 需要确认 RitsuLib public API、模板、注册属性或 helper 时，查 `D:\My Projects\GodotProjects\STS2-RitsuLib-0.4.33`。
3. 需要确认游戏本体 Hook / 原生方法签名时，再查运行时 `sts2.xml`。
4. 再看当前仓库是否已有接近实现。
5. 需要完整结构样例时看 `D:\My Projects\GodotProjects\the-queen`。
6. 仍然不够时再看 `D:\My Study\Slay The Spire 2 Original\src\Core`。
