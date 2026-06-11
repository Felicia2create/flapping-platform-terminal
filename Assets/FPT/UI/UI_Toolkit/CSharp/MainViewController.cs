using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using FPT.Business;
using FPT.Visualization;

namespace FPT.UI
{
    /// <summary>
    /// 主控制器 — 挂载到 UIDocument GameObject
    /// 负责加载 UXML、注入依赖、初始化子控制器、同步 3D 视口
    /// </summary>
    public class MainViewController : MonoBehaviour
    {
        [SerializeField] private StyleSheet[] _styleSheets;

        private UIDocument _doc;
        private VisualElement _root;
        private VisualElement _centerView;
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

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            // 等几帧，确保 AppContext.Awake 先执行 + UXML 加载完成
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

            // 从序列化字段加载 USS（保证播放模式生效）
            if (_styleSheets != null)
                foreach (var s in _styleSheets)
                    if (s != null) _root.styleSheets.Add(s);

            var topBarEl = _root.Q("TopBar");
            _leftPanel = _root.Q("LeftPanel");
            _rightPanel = _root.Q("RightPanel");
            var statusBarEl = _root.Q("StatusBar");
            _centerView = _root.Q("CenterView");

            if (topBarEl == null) { Debug.LogError("[MainView] TopBar 元素未找到"); yield break; }

            // 初始化子控制器
            _topBar = new TopBarController(topBarEl, _ctx.DeviceManager);
            _dashboard = new DashboardController(_leftPanel, _ctx.ArmDriver);
            _controlPanel = new ControlPanelController(_rightPanel, _ctx.ArmDriver);
            _statusBar = new StatusBarController(statusBarEl);

            // 绑定 3D 相机
            SetupCamera();

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
            if (_centerView == null || _orbitCamera == null) return;

            _orbitCamera.ActiveArea = ToScreenRect(_centerView);
            _orbitCamera.ExcludeAreas.Clear();
            if (_leftPanel != null) _orbitCamera.ExcludeAreas.Add(ToScreenRect(_leftPanel));
            if (_rightPanel != null) _orbitCamera.ExcludeAreas.Add(ToScreenRect(_rightPanel));
        }

        private void OnDisable()
        {
            _topBar?.Dispose();
            _dashboard?.Dispose();
            _controlPanel?.Dispose();
            _statusBar?.Dispose();
        }
    }
}
