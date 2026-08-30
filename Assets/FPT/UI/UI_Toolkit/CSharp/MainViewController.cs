using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using FPT.Business;
using FPT.Visualization;

namespace FPT.UI
{
    /// <summary>
    /// 主控制器 — 挂载到 UIDocument GameObject
    /// 负责加载 UXML、注入依赖、初始化子控制器、驱动 GhostArm、同步 3D 视口
    /// </summary>
    public class MainViewController : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root;
        private VisualElement _controlCenterView;
        private VisualElement _leftPanel;
        private VisualElement _rightPanel;

        // 子控制器
        private TopBarController _topBar;
        private DashboardController _dashboard;
        private ControlPanelController _controlPanel;
        private StatusBarController _statusBar;

        // 业务层依赖
        private AppContext _ctx;

        // 3D 相机
        private OrbitCameraController _orbitCamera;

        // 预览机械臂
        private GhostArmController _ghostArm;

        // 页面容器
        private VisualElement _controlPage;
        private VisualElement _animationPage;
        private VisualElement _animationCenterView;
        private VisualElement _dataPage;

        // 导航按钮
        private Button _navControlBtn;
        private Button _navAnimationBtn;
        private Button _navDataBtn;

        // 动画页面控制器
        private AnimationPageController _animationController;

        // 数据分析页面控制器
        private DataPageController _dataAnalysisController;

        // 折叠按钮与面板
        private Button _collapseRightBtn;
        private VisualElement _animRightPanel;
        private VisualElement _animLeftPanel;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
            _root.AddToClassList("app-root");

            StartCoroutine(InitWhenReady());
        }

        private IEnumerator InitWhenReady()
        {
            // 等待 AppContext 就绪（最多等 3 秒）
            var waited = 0f;
            while (AppContext.Instance == null && waited < 3f)
            {
                yield return null;
                waited += Time.deltaTime;
            }

            _ctx = AppContext.Instance;
            if (_ctx == null)
            {
                Debug.LogError("[MainView] AppContext.Instance 为空！");
                yield break;
            }

            yield return null;

            var topBarEl = _root.Q("TopBar");
            _leftPanel = _root.Q("LeftPanel");
            _rightPanel = _root.Q("RightPanel");
            var statusBarEl = _root.Q("StatusBar");
            _controlCenterView = _root.Q("ControlCenterView");

            if (topBarEl == null) { Debug.LogError("[MainView] TopBar 元素未找到"); yield break; }

            // 初始化子控制器（注入 InputTerminal）
            _topBar = new TopBarController(topBarEl, _ctx.DeviceManager);
            _dashboard = new DashboardController(_leftPanel, _ctx.ArmDriver, _ctx.InputTerminal);
            _controlPanel = new ControlPanelController(_rightPanel, _ctx.InputTerminal, _ctx.ArmDriver);
            _statusBar = new StatusBarController(statusBarEl);

            // ── 顶栏急停（常驻，全程可达）──
            var estopBtn = _root.Q<Button>("EstopButton");
            if (estopBtn != null)
                estopBtn.clicked += OnEstopClicked;

            // ── 全局状态消息：终端状态 → 状态栏 + 顶栏徽标 ──
            if (_ctx.InputTerminal != null)
                _ctx.InputTerminal.OnStatusChanged += OnTerminalStatus;

            // ── ROS 连接状态 → 状态栏端点显示 ──
            if (_ctx.Ros2Node != null)
            {
                _ctx.Ros2Node.OnConnected += OnRosConnectionChanged;
                _ctx.Ros2Node.OnDisconnected += OnRosConnectionChanged;
                OnRosConnectionChanged();
            }

            // ── 查找场景中预置的 GhostArm ──
            _ghostArm = FindObjectOfType<GhostArmController>();
            if (_ghostArm != null)
            {
                _ghostArm.Bind(_ctx.InputTerminal);
                Debug.Log("[MainView] GhostArm 已绑定（场景预置）");
            }
            else
            {
                Debug.LogWarning("[MainView] 场景中未找到 GhostArmController，预览机械臂不可用");
            }

            // ── 查找场景中预置的 FormationVisualizer 并注入依赖 ──
            var formationVis = FindObjectOfType<FormationVisualizer>();
            if (formationVis != null)
            {
                formationVis.Bind(_ctx.AnimationDemo);
                Debug.Log("[MainView] FormationVisualizer 已绑定 AnimationDemoController");
            }
            else
            {
                Debug.LogWarning("[MainView] 场景中未找到 FormationVisualizer，阵型连线不可用");
            }

            // 绑定 3D 相机
            SetupCamera();

            // ── 页面容器 ──
            _controlPage = _root.Q("ControlPage");
            _animationPage = _root.Q("AnimationPage");
            _animationCenterView = _root.Q("AnimationCenterView");
            _dataPage = _root.Q("DataPage");

            // ── 导航按钮 ──
            _navControlBtn = _root.Q<Button>("NavControlButton");
            _navAnimationBtn = _root.Q<Button>("NavAnimationButton");
            _navDataBtn = _root.Q<Button>("NavDataButton");
            if (_navControlBtn != null)
                _navControlBtn.clicked += () => SwitchPage("control");
            if (_navAnimationBtn != null)
                _navAnimationBtn.clicked += () => SwitchPage("animation");
            if (_navDataBtn != null)
                _navDataBtn.clicked += () => SwitchPage("data");

            // ── 动画页面 UI 控制器 ──
            _animationController = new AnimationPageController(_animationPage, _ctx.AnimationDemo);

            // ── 数据分析页面 UI 控制器 ──
            if (_dataPage != null)
                _dataAnalysisController = new DataPageController(_dataPage, _ctx.SensorDriver);

            // ── 折叠按钮 ──
            _collapseRightBtn = _root.Q<Button>("CollapseRightBtn");
            _animRightPanel = _root.Q("AnimationRightPanel");
            _animLeftPanel = _root.Q("AnimationLeftPanel");
            if (_collapseRightBtn != null)
                _collapseRightBtn.clicked += () => TogglePanel();

            Debug.Log("[MainView] UI Toolkit + 3D 视口初始化完成");
        }

        private void SetupCamera()
        {
            var mainCam = Camera.main;
            if (mainCam == null) return;

            _orbitCamera = mainCam.GetComponent<OrbitCameraController>();
            if (_orbitCamera == null)
                _orbitCamera = mainCam.gameObject.AddComponent<OrbitCameraController>();
        }

        private Rect ToScreenRect(VisualElement el)
        {
            var r = el.worldBound;
            return new Rect(r.x, Screen.height - r.yMax, r.width, r.height);
        }

        private void Update()
        {
            // 顶栏 FPS / 徽标复位 + 状态栏时钟（先于相机判断，保证始终走表）
            _topBar?.Tick();
            _statusBar?.Tick(Time.unscaledDeltaTime);

            if (_orbitCamera == null) return;

            // 根据当前页面更新相机交互区域
            if (_dataPage != null && _dataPage.style.display == DisplayStyle.Flex)
            {
                // 数据分析页面：全屏可用，排除左侧面板
                _orbitCamera.ActiveArea = new Rect(0, 0, Screen.width, Screen.height);
                _orbitCamera.ExcludeAreas.Clear();
                var dataLeftPanel = _root.Q("DataLeftPanel");
                if (dataLeftPanel != null)
                    _orbitCamera.ExcludeAreas.Add(ToScreenRect(dataLeftPanel));
            }
            else if (_animationPage != null && _animationPage.style.display == DisplayStyle.Flex)
            {
                // 动画页面：排除未折叠的右侧面板
                if (_animationCenterView != null)
                    _orbitCamera.ActiveArea = ToScreenRect(_animationCenterView);
                _orbitCamera.ExcludeAreas.Clear();
                if (_animLeftPanel != null)
                    _orbitCamera.ExcludeAreas.Add(ToScreenRect(_animLeftPanel));
                if (_animRightPanel != null && !_animRightPanel.ClassListContains("collapsed"))
                    _orbitCamera.ExcludeAreas.Add(ToScreenRect(_animRightPanel));
            }
            else
            {
                // 控制面板页面（默认）
                if (_controlCenterView != null)
                    _orbitCamera.ActiveArea = ToScreenRect(_controlCenterView);
                _orbitCamera.ExcludeAreas.Clear();
                if (_leftPanel != null) _orbitCamera.ExcludeAreas.Add(ToScreenRect(_leftPanel));
                if (_rightPanel != null) _orbitCamera.ExcludeAreas.Add(ToScreenRect(_rightPanel));
            }

            // 控制面板去抖 + 模式提示
            _controlPanel?.UpdateEeDebounce();
            _controlPanel?.UpdateModeHint();

            // 数据页：刷新通道面板（新通道到达时延迟重建）
            _dataAnalysisController?.Tick();
        }

        // ═══════════════════════════════════════════
        // 急停 / 全局状态
        // ═══════════════════════════════════════════

        private void OnEstopClicked()
        {
            if (_ctx?.ArmDriver != null)
                _ctx.ArmDriver.ExecuteCommand(new FPT.Core.StopCommand(_ctx.ArmDriver.DeviceId, true));
            _topBar?.SetBadge(TopBarController.Badge.Estop);
            _statusBar?.ShowMessage("⚠ 紧急停止已触发");
        }

        private void OnTerminalStatus(string status)
        {
            switch (status)
            {
                case "planning":
                    _topBar?.SetBadge(TopBarController.Badge.Planning);
                    _statusBar?.ShowMessage("正在规划轨迹...");
                    break;
                case "success":
                    _topBar?.SetBadge(TopBarController.Badge.Idle);
                    _statusBar?.ShowMessage("规划成功 ✓");
                    break;
                case "failed":
                    _topBar?.SetBadge(TopBarController.Badge.Idle);
                    _statusBar?.ShowMessage("规划失败 ✗ — 目标不可达或超出限位");
                    break;
                case "executed":
                    _statusBar?.ShowMessage("轨迹已下发执行");
                    break;
                case "cancelled":
                    _statusBar?.ShowMessage("已取消");
                    break;
            }
        }

        private void OnRosConnectionChanged()
        {
            var node = _ctx?.Ros2Node;
            if (node == null) return;
            _statusBar?.SetEndpoint(node.IsConnected
                ? $"ROS2 · {node.RosbridgeUrl}"
                : "ROS2 · 未连接");
            _statusBar?.ShowMessage(node.IsConnected ? "ROS2 桥已连接" : "ROS2 桥已断开");
        }

        // ═══════════════════════════════════════════
        // 页面切换
        // ═══════════════════════════════════════════

        private void SwitchPage(string page)
        {
            var isControl = page == "control";
            var isAnimation = page == "animation";
            var isData = page == "data";

            if (_controlPage != null)
                _controlPage.style.display = isControl ? DisplayStyle.Flex : DisplayStyle.None;
            if (_animationPage != null)
                _animationPage.style.display = isAnimation ? DisplayStyle.Flex : DisplayStyle.None;
            if (_dataPage != null)
                _dataPage.style.display = isData ? DisplayStyle.Flex : DisplayStyle.None;

            _navControlBtn?.EnableInClassList("nav-active", isControl);
            _navAnimationBtn?.EnableInClassList("nav-active", isAnimation);
            _navDataBtn?.EnableInClassList("nav-active", isData);

            // 联动平台显示切换（仅动画页面激活演示）
            if (isAnimation)
                _ctx?.AnimationDemo?.Activate();
            else
                _ctx?.AnimationDemo?.Deactivate();
        }

        private void TogglePanel()
        {
            if (_animRightPanel == null) return;
            var c = _animRightPanel.ClassListContains("collapsed");
            if (c) _animRightPanel.RemoveFromClassList("collapsed");
            else _animRightPanel.AddToClassList("collapsed");
            if (_collapseRightBtn != null) _collapseRightBtn.text = c ? "▶" : "◀";
        }

        private void OnDisable()
        {
            if (_ctx != null)
            {
                if (_ctx.InputTerminal != null)
                    _ctx.InputTerminal.OnStatusChanged -= OnTerminalStatus;
                if (_ctx.Ros2Node != null)
                {
                    _ctx.Ros2Node.OnConnected -= OnRosConnectionChanged;
                    _ctx.Ros2Node.OnDisconnected -= OnRosConnectionChanged;
                }
            }
            _topBar?.Dispose();
            _dashboard?.Dispose();
            _controlPanel?.Dispose();
            _statusBar?.Dispose();
            _animationController?.Dispose();
            _dataAnalysisController?.Dispose();
        }
    }
}
