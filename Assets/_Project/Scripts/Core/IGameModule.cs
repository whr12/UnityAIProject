namespace AIProject.Core
{
    /// <summary>
    /// 核心模块接口。所有游戏功能模块必须实现此接口。
    /// 模块为纯 C# 类，不继承 MonoBehaviour，以支持单元测试。
    /// </summary>
    public interface IGameModule
    {
        /// <summary>模块名称，用于日志和调试</summary>
        string ModuleName { get; }

        /// <summary>
        /// 第一阶段初始化：模块自身内部初始化。
        /// 此阶段禁止访问任何其他模块。
        /// 做：创建内部对象、设置初始值。
        /// 不做：获取其他模块引用、向其他模块发事件。
        /// </summary>
        void Initialize();

        /// <summary>
        /// 第二阶段初始化：跨模块连线。
        /// 此阶段所有模块的 Initialize 已全部完成，可以安全访问其他模块。
        /// 做：获取其他模块引用、订阅跨模块事件、注册服务。
        /// </summary>
        void PostInitialize();

        /// <summary>
        /// 释放模块。取消订阅、释放资源。
        /// </summary>
        void Release();
    }

    /// <summary>
    /// 可选接口。需要每帧更新的模块实现此接口，
    /// 由 GameManager.Update 统一调用。
    /// </summary>
    public interface IUpdatable
    {
        void OnUpdate(float deltaTime);
    }

    /// <summary>
    /// 可选接口。需要固定帧率更新的模块实现此接口，
    /// 由 GameManager.FixedUpdate 统一调用。
    /// </summary>
    public interface IFixedUpdatable
    {
        void OnFixedUpdate(float fixedDeltaTime);
    }

    /// <summary>
    /// 模块抽象基类（泛型自引用）。
    /// 自动管理单例 Instance（构造时赋值）+ ModuleName（类名），子类无需手写。
    ///
    /// 继承方式：class MyModule : GameModule&lt;MyModule&gt;
    /// 访问方式：MyModule.Instance
    /// </summary>
    public abstract class GameModule<T> : IGameModule where T : GameModule<T>
    {
        public static T Instance { get; private set; }

        public string ModuleName => typeof(T).Name;

        protected GameModule()
        {
            Instance = (T)this;
        }

        public abstract void Initialize();
        public abstract void PostInitialize();
        public abstract void Release();
    }
}
