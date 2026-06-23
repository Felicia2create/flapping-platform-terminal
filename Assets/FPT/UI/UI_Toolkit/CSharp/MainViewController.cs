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

        // 导航按钮
        private Button _navControlBtn;
        private Button _navAnimationBtn;

        // 动画页面控制器
        private AnimationPageController _animationController;

        // 折叠按钮与面板
        private Button _collapseRightBtn;
        private VisualElement _animRightPanel;
        private VisualElement _animLeftPanel;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

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

            // 绑定 3D 相机
            SetupCamera();

            // ── 页面容器 ──
            _controlPage = _root.Q("ControlPage");
            _animationPage = _root.Q("AnimationPage");
            _animationCenterView = _root.Q("AnimationCenterView");

            // ── 导航按钮 ──
            _navControlBtn = _root.Q<Button>("NavControlButton");
            _navAnimationBtn = _root.Q<Button>("NavAnimationButton");
            if (_navControlBtn != null)
                _navControlBtn.clicked += () => SwitchPage("control");
            if (_navAnimationBtn != null)
                _navAnimationBtn.clicked += () => SwitchPage("animation");

            // ── 动画页面 UI 控制器 ──
            _animationController = new AnimationPageController(_animationPage, _ctx.AnimationDemo);

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
            if (_orbitCamera == null) return;

            // 根据当前页面更新相机交互区域
            if (_animationPage != null && _animationPage.style.display == DisplayStyle.Flex)
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
        }

        // ═══════════════════════════════════════════
        // 页面切换
        // ═══════════════════════════════════════════

        private void SwitchPage(string page)
        {
            var isControl = page == "control";
            if (_controlPage != null)
                _controlPage.style.display = isControl ? DisplayStyle.Flex : DisplayStyle.None;
            if (_animationPage != null)
                _animationPage.style.display = isControl ? DisplayStyle.None : DisplayStyle.Flex;
            _navControlBtn?.EnableInClassList("nav-active", isControl);
            _navAnimationBtn?.EnableInClassList("nav-active", !isControl);

            // 联动平台显示切换
            if (isControl)
                _ctx?.AnimationDemo?.Deactivate();
            else
                _ctx?.AnimationDemo?.Activate();
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
            _topBar?.Dispose();
            _dashboard?.Dispose();
            _controlPanel?.Dispose();
            _statusBar?.Dispose();
            _animationController?.Dispose();
        }
    }
}
