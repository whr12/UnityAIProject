# AIProject — Unity 游戏项目

## 项目概况

- Unity 2022.3.61f1 LTS
- PC 平台 (Windows/Mac/Linux)
- 个人独立开发，长期项目
- 游戏类型未定，先搭建通用框架

## 目录结构

```
Assets/_Project/
├── Art/          （Materials, Models, Textures, Shaders, Animations, UI）
├── Audio/        （Music, SFX, Ambient）
├── Prefabs/      （Characters, Environment, UI, Effects）
├── Scenes/
├── Scripts/
│   ├── Core/     ★ 核心系统（当前开发中）
│   ├── Gameplay/
│   ├── UI/
│   ├── Utils/
│   └── Editor/
├── ScriptableObjects/
├── Settings/
└── ThirdParty/
```

## 核心架构（Core/）

### 设计理念

- GameManager 是轻量引导器，不是状态机。只负责模块注册 → 两阶段初始化 → Update 驱动 → 销毁
- 所有游戏功能通过实现 `IGameModule` 以模块方式添加
- 模块优先设计为纯 C# 类（可单测），需要时也可作为 MonoBehaviour
- 错误处理只用 `Debug.LogError`，**禁止** `UnityEditor.EditorApplication.isPaused`（由 Unity ErrorPause 控制暂停）

### Core 目录规则

1. **模块命名以 `Module` 结尾** — 如 `MessageModule`、`SceneLoaderModule`
2. **业务模块继承 `GameModule<T>`** — 泛型自引用：`class MyModule : GameModule<MyModule>`，基类自动管理 `ModuleName` 和单例 `Instance`
3. **每个模块独立文件夹** — 文件夹名与模块名一致。例如 `MessageModule/`、`SceneLoaderModule/`
4. **辅助类就近存放** — 模块用到的辅助类放在对应文件夹下
5. **按需实现接口** — 模块根据自身需求自主选择是否实现 `IUpdatable` / `IFixedUpdatable`

```
Core/
├── IGameModule.cs          ← 接口 + GameModule 基类
├── Singleton.cs            ← 单例基类
├── GameManager.cs          ← 引导器
├── Utils/                  ← 工具类
│   ├── ObjectPool.cs       ← 纯 C# 通用对象池
│   └── GameObjectPool.cs   ← Unity GameObject 池
├── MessageModule/          ← 消息模块
│   └── MessageModule.cs
├── CoroutineModule/        ← 协程模块
│   └── CoroutineModule.cs
├── ResourceModule/         ← 资源模块
│   ├── IAssetProvider.cs
│   ├── ResourcesAssetProvider.cs
│   ├── ResourceHandle.cs
│   └── ResourceModule.cs
├── SceneModule/            ← 场景切换
│   ├── ISceneTransition.cs ← 过渡接口
│   ├── InstantTransition.cs
│   ├── SceneModule.cs
│   └── SceneLoad*Event.cs
└── ...
```

### 关键文件

| 文件 | 说明 |
|------|------|
| `Core/IGameModule.cs` | `IGameModule` + `GameModule` 基类 + `IUpdatable` + `IFixedUpdatable` |
| `Core/MessageModule/MessageModule.cs` | 消息模块。AddEvent / RemoveEvent / SendEvent，同步分发，防回调修改列表 |
| `Core/CoroutineModule/CoroutineModule.cs` | 协程模块。内部 GameObject 借 Unity 原生协程，纯 C# 模块零成本使用 |
| `Core/ResourceModule/IAssetProvider.cs` | Provider 接口：Load / LoadAsync / Unload / GetScenePath |
| `Core/ResourceModule/ResourcesAssetProvider.cs` | Resources 实现 |
| `Core/ResourceModule/ResourceHandle.cs` | 资源凭证：AddRef / Release / ForceUnload / CreateGameObject |
| `Core/ResourceModule/ResourceModule.cs` | 资源生命周期：引用计数、缓存 FlushCache、GameObject 绑定、场景 ForceRelease |
| `Core/SceneModule/SceneModule.cs` | 场景切换。配合 ISceneTransition 做过渡，通过 MessageModule 发事件 |
| `Core/Singleton.cs` | MonoBehaviour 单例基类。重复检测 + DontDestroyOnLoad。`Instance` 为空时报错，不做 FindObjectOfType 兜底 |
| `Core/GameManager.cs` | 引导器。`RegisterModule()` 收集模块，Start 中执行两阶段初始化，Update/FixedUpdate 驱动 Tick，OnDestroy 逆序 Release |
| `Core/Utils/ObjectPool.cs` | 纯 C# 泛型对象池。`Get()` / `Return()` / `Prewarm()`，支持 `onGet`/`onReturn` 回调 |
| `Core/Utils/GameObjectPool.cs` | 泛型 Unity 池 `GameObjectPool<T>`。`Get()` 直接返回 T 组件，`Return(T)` |

### 两阶段初始化

```
Phase 1: Initialize()  × N  → 各自内部准备，不跨模块
Phase 2: PostInitialize() × N  → 互相连线，此时可安全访问其他模块
```

- 任一阶段抛异常 → 立即中止，暂停编辑器
- 注册顺序 = 初始化顺序
- MonoBehaviour 模块在 Awake 中 `RegisterModule(this)`，纯 C# 模块在 Start 中 `new` 并注册

### 模块注册方式

```csharp
// 方式一：纯 C# 模块（在 GameManager.Start 中创建）
RegisterModule(new MessageModule());  // 继承 GameModule

// 方式二：MonoBehaviour 模块（在自身 Awake 中注册）
void Awake() => GameManager.Instance.RegisterModule(this);
```

## 目录管理规则（全局）

### 核心原则

1. **事前归类、一步到位** — 创建前判断归属，目录命名一次定下，**永不搬迁**
2. **不设兜底目录** — 没有 `Tools/`、`Utils/`、`Misc/` 之类模糊目录
3. **归属不清先讨论** — 宁可暂停，不强行塞。讨论过程本身就是明确功能边界

```
新文件/资源 → 运行时对应系统是什么？
  ├─ 有明确领域 → 放入对应目录
  └─ 无法归类？ → 先讨论，不创建
```

### 脚本目录（Scripts/）

| 目录 | 归属判断 |
|------|----------|
| `Core/{ModuleName}/` | 框架级模块（GameModule 子类及其辅助类） |
| `Gameplay/` | 游戏玩法逻辑（角色、AI、战斗等） |
| `UI/` | UI 面板逻辑（UIPanel 子类）、UI 组件 |
| `Utils/` | **纯工具类**，无状态、无模块依赖（如扩展方法、数学库） |
| `Editor/{领域}/` | 按决策树分类，见 Editor 目录规则 |

### 资源目录（Art/ Audio/ Prefabs/）

| 目录 | 子目录 | 归属判断 |
|------|------|----------|
| `Art/Materials/` | 按效果/场景分 | 材质球 |
| `Art/Models/` | Characters/ Environment/ Props/ | 3D 模型 |
| `Art/Textures/` | 跟随 Model 或 UI 路径 | 贴图 |
| `Art/Shaders/` | 按用途分 | Shader 文件 |
| `Art/Animations/` | Characters/ UI/ Effects/ | 动画 Clip / Controller |
| `Art/UI/` | 按面板/组件分 | UI 专属美术资源 |
| `Audio/Music/` | — | BGM |
| `Audio/SFX/` | UI/ Gameplay/ Ambient/ | 音效 |
| `Prefabs/Characters/` | — | 角色预制体 |
| `Prefabs/Environment/` | — | 场景物件 |
| `Prefabs/UI/` | 按面板名 | UI 预制体（当前 MainMenuPanel.prefab） |
| `Prefabs/Effects/` | — | 粒子/特效 |

### 顶层文件（ScriptableObjects/ Settings/ Scenes/ ThirdParty/）

| 目录 | 归属判断 |
|------|----------|
| `ScriptableObjects/` | 数据资产，按功能子目录 |
| `Settings/` | 项目级配置资产 |
| `Scenes/` | 场景文件，一个场景一个目录（场景 + 关联的专属资源） |
| `ThirdParty/` | 第三方插件和资源，保持原目录结构 |

---

## 项目规则（随 git 同步）

### Git 操作
- commit 前需展示文件清单和 message，用户确认后才执行
- push 前需展示推送摘要，用户确认后才执行

### 代码规范
- 错误处理只用 `Debug.LogError`，**禁止** `UnityEditor.EditorApplication.isPaused`（由 Unity ErrorPause 控制暂停）
- **事件类型以 `Event` 为前缀**（如 `EventSceneLoadStart`），不使用后缀。方便 IDE 补全时输入 `Event` 列出所有事件类型
- MessageModule 事件类型必须是 struct（值类型），防止订阅者意外修改数据影响后续订阅者
- **不使用 `internal` 访问修饰符**，需要跨程序集（如 Editor 访问运行时代码）时直接使用 `public`。已有代码不做调整

### 模块开发
- 模块命名以 `Module` 结尾，继承 `GameModule` 基类
- 每个模块独立文件夹，辅助类就近存放
- 按需实现 `IUpdatable` / `IFixedUpdatable`

### Editor 目录规则

Editor 脚本按功能分子目录，**事前归类、一步到位，不搬迁**。

**决策树：编辑器工具，运行时对应系统是？**

```
├─ Core 模块相关     → Editor/CoreModuleName/   （如 Editor/UIModule/）
├─ UI 面板/组件      → Editor/UI/               （当前：UIPrefabGenerator）
├─ 场景/关卡         → Editor/Scene/
├─ 构建/发布         → Editor/Build/
├─ 资源导入/管线     → Editor/AssetPipeline/    （AssetPostprocessor 等）
├─ 开发者调试        → Editor/Debug/            （控制台、Debug 工具）
└─ 无法归类？        → ⚠ 先和我讨论，不设兜底目录
```

| 规则 | 说明 |
|------|------|
| **事前归类** | 创建文件前先判断归属，目录一步到位 |
| **不搬迁** | 禁止事后"文件多了一个新目录移过去"，保护 git 历史 |
| **不设兜底** | 没有 `Tools/` 或 `Utils/` 命名空间。归属不清则讨论 |
| 命名无强制后缀 | 如 `UIPrefabGenerator.cs`，不需要 `Editor` 后缀 |
| 目录名 PascalCase | 首字母大写 |
| Editor → 运行时 | ✅ 可引用 `Core/`、`Gameplay/`、`UI/`、`Utils/` |
| 运行时 → Editor | ❌ 运行时禁止引用 Editor 目录下的任何类型 |

> **归属不清晰时的讨论，是为了提醒开发者在创建工具前先明确功能的领域边界。**

### 初始化
- 注册顺序 = 初始化顺序
- Phase 1 `Initialize()` 禁止跨模块访问
- Phase 2 `PostInitialize()` 做跨模块连线
- 任一阶段抛异常 → `Debug.LogError` + 中止启动

## 工作清单

详见 DEVELOPMENT_ROADMAP.md

### 当前阶段

- [x] 〇.2 Git 初始化 + .gitignore + .gitattributes
- [x] 〇.3 目录结构创建
- [x] 1.1 GameManager + Singleton + GameModule（Core 基础框架完成）
- [x] 1.2 MessageModule（订阅/发布事件系统完成）
- [ ] 1.3 Input System（跳过，Gameplay 阶段再补）
- [x] 1.4 ObjectPool（ObjectPool<T> + GameObjectPool）
- [x] 1.5 CoroutineModule
- [x] 1.6 ResourceModule（引用计数 + 缓存 + 绑定 + 场景管理）
- [x] 1.7 SceneModule（场景切换 + ISceneTransition + 事件通知）
