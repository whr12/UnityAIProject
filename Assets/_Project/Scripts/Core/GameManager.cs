using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIProject.Core
{
    /// <summary>
    /// 游戏全局管理器（轻量引导器）。
    /// 职责：注册模块 → 两阶段初始化 → 驱动 Update → 逆序销毁。
    ///
    /// 使用方式：
    ///   纯 C# 模块 —— GameManager 在 Start 中直接 new 并 Register
    ///   MonoBehaviour 模块 —— 在 Awake 中调用 GameManager.Instance.RegisterModule(this)
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Debug")]
        [SerializeField] private bool _verboseLogging = true;

        private readonly List<IGameModule> _modules = new();
        private readonly List<IUpdatable> _updatables = new();
        private readonly List<IFixedUpdatable> _fixedUpdatables = new();

        // ===== Unity 生命周期 =====

        private void Start()
        {
            // 纯 C# 模块在此处直接 new 注册
            // （MonoBehaviour 模块在各自 Awake 中调用 RegisterModule(this)，此时已全部注册完毕）
            RegisterModule(new MessageModule());
            RegisterModule(new CoroutineModule());
            RegisterModule(new ResourceModule());
            RegisterModule(new SceneModule());
            RegisterModule(new UIModule());

            BootstrapModules();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _updatables.Count; i++)
            {
                _updatables[i].OnUpdate(dt);
            }
        }

        private void FixedUpdate()
        {
            float fdt = Time.fixedDeltaTime;
            for (int i = 0; i < _fixedUpdatables.Count; i++)
            {
                _fixedUpdatables[i].OnFixedUpdate(fdt);
            }
        }

        protected override void OnDestroy()
        {
            ReleaseModules();
            base.OnDestroy();
        }

        // ===== 注册 =====

        /// <summary>
        /// 注册模块。MonoBehaviour 模块在 Awake 中调用，纯 C# 模块在 Start 前调用。
        /// 注册顺序即初始化顺序。
        /// </summary>
        public void RegisterModule(IGameModule module)
        {
            if (module == null)
            {
                Debug.LogError("[GameManager] 尝试注册 null 模块。");
                return;
            }

            _modules.Add(module);
            if (_verboseLogging)
                Debug.Log($"[GameManager] 注册模块: {module.ModuleName}");
        }

        // ===== Bootstrap =====

        private void BootstrapModules()
        {
            if (_modules.Count == 0)
            {
                Debug.LogWarning("[GameManager] 没有注册任何模块。");
                return;
            }

            // Phase 1: 模块自身初始化（禁止跨模块访问）
            foreach (var m in _modules)
            {
                try
                {
                    m.Initialize();
                    if (_verboseLogging)
                        Debug.Log($"[GameManager] {m.ModuleName} Init 完成");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameManager] {m.ModuleName} Init 失败: {ex.Message}\n{ex.StackTrace}");
                    return;
                }
            }

            // Phase 2: 跨模块连线
            foreach (var m in _modules)
            {
                try
                {
                    m.PostInitialize();
                    if (_verboseLogging)
                        Debug.Log($"[GameManager] {m.ModuleName} PostInit 完成");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameManager] {m.ModuleName} PostInit 失败: {ex.Message}\n{ex.StackTrace}");
                    return;
                }
            }

            // 收集 IUpdatable / IFixedUpdatable
            foreach (var m in _modules)
            {
                if (m is IUpdatable updatable) _updatables.Add(updatable);
                if (m is IFixedUpdatable fu) _fixedUpdatables.Add(fu);
            }

            Debug.Log($"[GameManager] Bootstrap 完成。{_modules.Count} 个模块就绪。");

            // 切入首个业务场景
            SceneModule.Instance.LoadScene("BootstrapScene");
        }

        // ===== Release =====

        private void ReleaseModules()
        {
            // 逆序销毁
            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _modules[i].Release();
                    if (_verboseLogging)
                        Debug.Log($"[GameManager] {_modules[i].ModuleName} 已销毁");
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[GameManager] {_modules[i].ModuleName} Release 异常: {ex.Message}");
                }
            }

            _updatables.Clear();
            _fixedUpdatables.Clear();
            _modules.Clear();
        }
    }
}
