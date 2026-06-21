# Unity 游戏项目开发工作清单

> **项目概况**
> - 项目名称：AIProject
> - 平台：PC (Windows/Mac/Linux)
> - 开发者：个人独立开发，长期项目
> - 游戏类型：待定（先搭建通用框架）
> - AI 角色：仅辅助编码开发，不集成到游戏运行时

---

## 📋 阶段〇：项目基础设置（优先完成）

### 〇.1 Unity 编辑器与环境
- [ ] **确认 Unity 版本**（建议 Unity 6 LTS 或 2022 LTS，避免频繁升级）
- [ ] **配置编辑器布局**：保存个人偏好的窗口布局（Window → Layouts → Save Layout）
- [ ] **安装必要模块**：Windows Build Support、Mac Build Support、Linux Build Support
- [ ] **配置外部代码编辑器**：VS Code / Rider / Visual Studio（Preferences → External Tools）
- [ ] **设置 .gitignore**：确保 `Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/` 不入库

### 〇.2 版本控制
- [ ] **初始化 Git 仓库**：`git init`
- [ ] **安装 Git LFS**（大文件管理，用于美术资源、模型等）
- [ ] **配置 .gitattributes**：为大型二进制文件指定 LFS 规则
- [ ] **创建 .gitignore**：[Unity 官方 .gitignore 模板](https://github.com/github/gitignore/blob/main/Unity.gitignore)
- [ ] **首次提交**：提交纯净的项目初始状态

### 〇.3 项目结构规划
- [ ] **设计 Assets 目录结构**（见下方推荐结构）
- [ ] **创建核心文件夹体系**

```
Assets/
├── _Project/                  # 主项目文件夹（下划线前缀置顶）
│   ├── Art/                   # 美术资源
│   │   ├── Materials/
│   │   ├── Models/
│   │   ├── Textures/
│   │   ├── Shaders/
│   │   ├── Animations/
│   │   └── UI/
│   ├── Audio/                 # 音频资源
│   │   ├── Music/
│   │   ├── SFX/
│   │   └── Ambient/
│   ├── Prefabs/               # 预制体
│   │   ├── Characters/
│   │   ├── Environment/
│   │   ├── UI/
│   │   └── Effects/
│   ├── Scenes/                # 场景文件
│   ├── Scripts/               # C# 脚本
│   │   ├── Core/              # 核心系统
│   │   ├── Gameplay/          # 游戏玩法
│   │   ├── UI/                # UI 逻辑
│   │   ├── Utils/             # 工具类和扩展方法
│   │   └── Editor/            # Editor 脚本
│   ├── ScriptableObjects/     # ScriptableObject 资产
│   ├── Settings/              # 项目设置资产
│   └── ThirdParty/            # 第三方插件和资源
├── Scenes/                    # Unity 默认场景文件夹（保留给快速测试）
└── Resources/                 # 动态加载资源（谨慎使用，会影响启动时间）
```

---

## 📋 阶段一：核心系统架构（游戏框架基石）

### 1.1 游戏管理器系统
- [ ] **实现 GameManager（游戏全局管理器）**
  - 游戏状态机（MainMenu → Loading → Playing → Paused → GameOver）
  - 场景加载管理
  - 全局事件系统
- [ ] **实现 Singleton 基类**（MonoBehaviour 单例 + 普通 C# 单例）
- [ ] **实现 ServiceLocator 或简易依赖注入**（避免单例滥用）

### 1.2 事件系统
- [x] **实现 MessageModule（消息系统）**
  - 支持参数传递
  - 防止内存泄漏（自动清理）

### 1.3 输入系统
- [ ] **配置 Unity Input System Package**
- [ ] **创建 Input Action Asset**（键盘 + 鼠标 + 手柄）
- [ ] **实现输入管理器**：统一处理所有输入事件
- [ ] **支持运行时按键重绑定（Rebinding）**（可选）

### 1.4 场景管理
- [ ] **实现 SceneLoader**：
  - 同步加载
  - 异步加载 + 加载画面（Loading Screen）
  - 叠加式场景加载（Additive）
- [ ] **创建启动场景 Bootstrap**（初始化核心系统后跳转到主菜单）

### 1.5 对象池系统
- [ ] **实现通用 ObjectPool\<T\>**
  - 适用于频繁创建/销毁的对象（子弹、特效、敌人等）
  - 支持预热（Pre-warm）
  - 支持自动扩容

---

## 📋 阶段二：基础游戏功能

### 2.1 角色控制器
- [ ] **实现通用 FPS/TPS 角色控制器**（根据最终类型调整）
  - 移动（WASD + 手柄摇杆）
  - 跳跃 + 重力
  - 视角控制（鼠标/手柄）
  - 碰撞检测
- [ ] **实现 CharacterController 或 Rigidbody 方案选型**

### 2.2 摄像机系统
- [ ] **实现 CameraManager**
  - 主摄像机管理
  - 摄像机切换（第一人称 / 第三人称 / 过场）
  - 摄像机震动效果（Camera Shake）
  - 后处理效果切换

### 2.3 UI 框架
- [ ] **实现 UIManager**
  - UI 层级管理（HUD → Overlay → Modal → Tutorial）
  - UI 栈（显示/返回/关闭逻辑）
  - 通用 UI 组件库：
    - Button（普通 / 长按 / Toggle）
    - Slider
    - Dropdown
    - Text / TextMeshPro
    - Tooltip
    - Modal Dialog / 确认框
- [ ] **配置 TextMeshPro 字体**
- [ ] **实现多语言/本地化框架**（可选但建议提前预留接口）

### 2.4 音频系统
- [ ] **实现 AudioManager**
  - BGM 管理（播放 / 暂停 / 淡入淡出 / 切歌）
  - SFX 播放（2D / 3D 音效）
  - 音量控制（主音量 / BGM / SFX 独立调节）
  - Audio Mixer 配置

### 2.5 存档系统
- [ ] **实现 SaveManager**
  - 序列化方案选型（JSON / Binary / PlayerPrefs）
  - 多存档槽位支持
  - 自动存档 + 手动存档
  - 存档加密（可选）
  - 云存档接口预留

---

## 📋 阶段三：游戏数据与配置

### 3.1 ScriptableObject 数据架构
- [ ] **设计数据驱动架构**：使用 ScriptableObject 替代硬编码配置
- [ ] **创建常用数据资产类型**：
  - 角色属性（血量、速度、伤害等）
  - 武器/道具配置
  - 关卡配置
  - 敌人配置
  - UI 样式配置
- [ ] **实现配置表编辑器工具**（Editor 脚本辅助编辑）

### 3.2 配置管理
- [ ] **实现 ConfigManager**：统一加载和管理所有配置数据
- [ ] **支持运行时热重载**（开发阶段）
- [ ] **配置数据验证工具**

---

## 📋 阶段四：开发工具链（Editor 工具）

### 4.1 开发辅助
- [ ] **创建开发者控制台（Debug Console）**
  - 运行时命令输入
  - Cheat 指令系统
  - 日志过滤和搜索
- [ ] **实现 Debug 绘制工具**（Gizmos 扩展、运行时信息显示）

### 4.2 自动化工作流
- [ ] **一键打包脚本**（Build Script）
- [ ] **资源导入管线自动化**（AssetPostprocessor）
- [ ] **自动化命名规范检查工具**
- [ ] **批量资源处理工具**（批量修改 import settings）

### 4.3 测试
- [ ] **集成 Unity Test Framework**
- [ ] **编写核心系统单元测试**
- [ ] **编写游戏逻辑集成测试**
- [ ] **自动化 Build + Test CI/CD 脚本**（可选）

---

## 📋 阶段五：性能与优化

### 5.1 性能框架
- [ ] **集成 Profiler 工具使用习惯**
- [ ] **实现性能监控组件**
  - FPS 计数器
  - 内存使用监控
  - Draw Call 监控
- [ ] **对象池使用规范**（配合阶段一的对象池系统）
- [ ] **资源预加载策略**

### 5.2 渲染优化
- [ ] **LOD 系统配置**
- [ ] **遮挡剔除（Occlusion Culling）**
- [ ] **批处理策略（Static/Dynamic Batching、GPU Instancing）**
- [ ] **Shader 变体管理**

### 5.3 内存管理
- [ ] **AssetBundle / Addressables 资源管理方案**
- [ ] **资源引用追踪（防止冗余加载）**
- [ ] **内存泄漏检测流程**

---

## 📋 阶段六：发布与维护

### 6.1 构建流水线
- [ ] **配置 Player Settings**（应用名、版本号、图标、启动画面）
- [ ] **多平台构建脚本**（Windows / Mac / Linux 一键打包）
- [ ] **构建后处理脚本**（压缩、上传等）

### 6.2 发行准备
- [ ] **实现启动画面（Splash Screen）**
- [ ] **实现设置菜单**（画面设置、音量、语言、按键绑定）
- [ ] **崩溃报告集成**（Unity Analytics / Sentry / 自定义）
- [ ] **更新/热更新机制设计**（如果需要）

### 6.3 文档
- [ ] **编写项目 README**
- [ ] **维护 CHANGELOG**
- [ ] **编写游戏设计文档（GDD）**（当游戏类型确定后）
- [ ] **编写技术文档**（系统架构说明、API 文档）

---

## 🎯 推荐的启动顺序（前两周）

| 优先级 | 任务 | 预计耗时 | 说明 |
|--------|------|----------|------|
| **P0** | Git 初始化 + .gitignore | 30 分钟 | 立刻做，防止丢失代码 |
| **P0** | Assets 目录结构创建 | 30 分钟 | 建立清晰的组织基础 |
| **P1** | GameManager + 基础事件系统 | 2-3 天 | 所有系统的基础 |
| **P1** | 场景管理（Bootstrap + 主菜单 + 示例场景） | 1-2 天 | 可运行的框架 |
| **P1** | Input System 配置 | 1 天 | 后续所有交互的基础 |
| **P2** | UI 框架 + 主菜单 | 2-3 天 | 用户能看到的东西 |
| **P2** | 音频系统基础 | 1 天 | 简单的 BGM + SFX 播放 |
| **P3** | 存档系统 | 1-2 天 | 让玩家进度可保留 |
| **P3** | 对象池 | 半天 | 高频创建销毁的优化基础 |

---

## 📝 待你后续确定的事项

1. **游戏具体类型和核心玩法** → 决定角色控制器、物理系统、核心循环的具体实现
2. **视觉风格** → 决定渲染管线（URP/HDRP/Built-in）、资产风格方向
3. **是否需要多人/联网** → 如果是，需要引入 Netcode 框架（越早决定越好）
4. **是否上架 Steam** → 需要 Steamworks SDK 集成
5. **是否需要 Mod 支持** → 影响资源加载架构设计

---

## 💡 开发建议

1. **每完成一个系统就写一个 Demo 场景** —— 既能验证功能，又能积累测试场景库
2. **ScriptableObject 是你的好朋友** —— 数据驱动 > 硬编码，后续迭代成本低得多
3. **保持场景干净** —— 一个场景尽量只做一件事（Bootstrap.cs 只负责初始化）
4. **善用 Assembly Definitions（.asmdef）** —— 加快编译速度，尤其是中后期脚本多了以后
5. **随手写 Editor 工具** —— 重复操作超过 3 次就值得自动化
6. **Claude + Unity 配合** —— 需要写系统时把上下文喂给我，能快速出代码框架

---

> 📅 创建日期：2026-06-21
> 🔄 最后更新：2026-06-21
> 👤 开发者：个人独立开发
