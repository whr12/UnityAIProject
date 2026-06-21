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

## 项目规则（随 git 同步）

### Git 操作
- commit 前需展示文件清单和 message，用户确认后才执行
- push 前需展示推送摘要，用户确认后才执行

### 代码规范
- 错误处理只用 `Debug.LogError`，**禁止** `UnityEditor.EditorApplication.isPaused`（由 Unity ErrorPause 控制暂停）

### 模块开发
- 模块命名以 `Module` 结尾，继承 `GameModule` 基类
- 每个模块独立文件夹，辅助类就近存放
- 按需实现 `IUpdatable` / `IFixedUpdatable`

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
