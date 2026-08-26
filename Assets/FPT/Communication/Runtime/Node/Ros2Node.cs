using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Sensor;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.Protocols;
using UnityEngine;

namespace FPT.Communication
{
    /// <summary>
    /// ROS2 节点 — Unity 侧的 ROS2 节点抽象
    /// 通过 ROS# (ros-sharp) 的 RosSocket 连接 rosbridge_server
    /// 统一管理 Topic 订阅/发布、Service Client 调用
    /// </summary>
    public class Ros2Node : MonoBehaviour
    {
        // ═══════════════════════════════════
        // 单例
        // ═══════════════════════════════════

        public static Ros2Node Instance { get; private set; }

        // ═══════════════════════════════════
        // 配置
        // ═══════════════════════════════════

        [Header("节点配置")]
        [SerializeField] private Ros2NodeConfig _config;

        [Header("内联配置（当 Config 未指定时使用）")]
        [SerializeField] private string _nodeName = "unity_node";
        [SerializeField] private string _rosbridgeUrl = "ws://127.0.0.1:9090";
        [SerializeField] private bool _autoConnect = true;
        [SerializeField] private float _reconnectInterval = 5f;
        [SerializeField] private int _maxReconnectAttempts = -1;

        // ═══════════════════════════════════
        // 属性
        // ═══════════════════════════════════

        public string NodeName => _config != null ? _config.NodeName : _nodeName;
        public string RosbridgeUrl => _config != null ? _config.RosbridgeUrl : _rosbridgeUrl;
        public bool IsConnected { get; private set; }

        /// <summary>ROS# 的 RosSocket 实例</summary>
        public RosSocket RosSocket { get; private set; }

        // ═══════════════════════════════════
        // 事件
        // ═══════════════════════════════════

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnConnectionError;

        // ═══════════════════════════════════
        // 内部状态
        // ═══════════════════════════════════

        private readonly Dictionary<string, string> _subscriberIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _publisherIds = new Dictionary<string, string>();
        // 缓存未连接时的订阅请求，连接成功后自动重新订阅
        private readonly List<Action> _pendingSubscriptions = new List<Action>();
        // 主线程回调队列（ROS# 回调在后台线程，需分发到主线程）
        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        private int _reconnectAttempts;
        private Coroutine _reconnectCoroutine;
        private ManualResetEvent _connectionEvent;

        // ═══════════════════════════════════
        // Unity 生命周期
        // ═══════════════════════════════════

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ApplyConfig();
            Debug.Log($"[Ros2Node] 节点 '{NodeName}' 已创建 (url={RosbridgeUrl})");

            if (_autoConnect) Connect();
        }

        private void OnDestroy()
        {
            Disconnect();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // 分发后台线程的回调到主线程
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ros2Node] 主线程回调异常: {ex.Message}");
                }
            }
        }

        // ═══════════════════════════════════
        // 连接生命周期
        // ═══════════════════════════════════

        public void Connect(string url = null)
        {
            if (url != null) _rosbridgeUrl = url;

            if (IsConnected)
            {
                Debug.LogWarning("[Ros2Node] 已经连接，忽略重复 Connect");
                return;
            }

            _reconnectAttempts = 0;
            _connectionEvent = new ManualResetEvent(false);

            // 在后台线程连接（避免阻塞主线程）
            new Thread(ConnectThread).Start();

            // 启动协程等待连接结果
            StartCoroutine(WaitForConnection());
        }

        public void Disconnect()
        {
            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }

            if (RosSocket != null)
            {
                try { RosSocket.Close(500); }
                catch (Exception ex) { Debug.LogWarning($"[Ros2Node] 关闭连接异常: {ex.Message}"); }
                RosSocket = null;
            }

            if (IsConnected)
            {
                IsConnected = false;
                OnDisconnected?.Invoke();
                Debug.Log("[Ros2Node] 已断开连接");
            }
        }

        // ═══════════════════════════════════
        // Topic 管理
        // ═══════════════════════════════════

        /// <summary>
        /// 订阅 topic
        /// </summary>
        public void Subscribe<T>(string topic, Action<T> callback) where T : Message
        {
            if (!IsConnected || RosSocket == null)
            {
                // 缓存订阅请求，连接成功后自动重新订阅
                Debug.Log($"[Ros2Node] 未连接，缓存订阅请求: {topic}");
                _pendingSubscriptions.Add(() => Subscribe<T>(topic, callback));
                return;
            }

            string fullTopic = EnsureSlashPrefix(topic);
            string subId = RosSocket.Subscribe<T>(fullTopic, (T msg) =>
            {
                // ROS# 回调在 WebSocket 后台线程，分发到主线程执行
                _mainThreadQueue.Enqueue(() =>
                {
                    try
                    {
                        callback?.Invoke(msg);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Ros2Node] 订阅回调异常: {topic} — {ex.Message}\n{ex.StackTrace}");
                    }
                });
            });

            _subscriberIds[topic] = subId;
            Debug.Log($"[Ros2Node] 订阅: {topic} ({typeof(T).Name})");
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe(string topic)
        {
            if (_subscriberIds.TryGetValue(topic, out string subId))
            {
                RosSocket?.Unsubscribe(subId);
                _subscriberIds.Remove(topic);
                Debug.Log($"[Ros2Node] 取消订阅: {topic}");
            }
        }

        /// <summary>
        /// 注册发布者并发布消息
        /// </summary>
        public void Publish<T>(string topic, T message) where T : Message
        {
            if (!IsConnected || RosSocket == null)
            {
                Debug.LogWarning($"[Ros2Node] 未连接，无法发布 {topic}");
                return;
            }

            string fullTopic = EnsureSlashPrefix(topic);

            // 如果还没 Advertise 过，先 Advertise
            if (!_publisherIds.ContainsKey(fullTopic))
            {
                string pubId = RosSocket.Advertise<T>(fullTopic);
                _publisherIds[fullTopic] = pubId;
            }

            RosSocket.Publish(_publisherIds[fullTopic], message);
        }

        // ═══════════════════════════════════
        // Service Client 管理
        // ═══════════════════════════════════

        /// <summary>
        /// 注册 service（rosbridge 不需要预注册，保留方法签名兼容性）
        /// </summary>
        public void RegisterService(string serviceName, string rosServiceType,
            string requestMessageType, string responseMessageType)
        {
            // rosbridge 协议不需要预注册 service，直接调用即可
            Debug.Log($"[Ros2Node] Service 已记录: {serviceName} (type={rosServiceType})");
        }

        /// <summary>
        /// 调用 service（async/await）
        /// </summary>
        public async Task<TResp> CallServiceAsync<TResp>(string serviceName, Message request)
            where TResp : Message, new()
        {
            if (!IsConnected || RosSocket == null)
            {
                throw new InvalidOperationException($"[Ros2Node] 未连接，无法调用 service {serviceName}");
            }

            var tcs = new TaskCompletionSource<TResp>();

            // 使用 Message 基类作为输入类型，TResp 作为输出类型
            string fullServiceName = EnsureSlashPrefix(serviceName);
            RosSocket.CallService<Message, TResp>(fullServiceName, (TResp response) =>
            {
                tcs.TrySetResult(response);
            }, request);

            return await tcs.Task;
        }

        /// <summary>
        /// 调用 service（callback）
        /// </summary>
        public void CallService<TResp>(string serviceName, Message request, Action<TResp> callback)
            where TResp : Message, new()
        {
            if (!IsConnected || RosSocket == null)
            {
                Debug.LogError($"[Ros2Node] 未连接，无法调用 service {serviceName}");
                return;
            }

            string fullServiceName = EnsureSlashPrefix(serviceName);
            RosSocket.CallService<Message, TResp>(fullServiceName, (TResp response) =>
            {
                try
                {
                    callback?.Invoke(response);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ros2Node] Service 回调异常: {serviceName} — {ex.Message}");
                }
            }, request);
        }

        // ═══════════════════════════════════
        // 内部方法
        // ═══════════════════════════════════

        /// <summary>确保 topic/service 名称有前缀 /</summary>
        private static string EnsureSlashPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return "/";
            return name.StartsWith("/") ? name : "/" + name;
        }

        /// <summary>
        /// 执行缓存的订阅请求（连接成功后自动调用）
        /// </summary>
        private void FlushPendingSubscriptions()
        {
            if (_pendingSubscriptions.Count == 0) return;
            Debug.Log($"[Ros2Node] 执行 {_pendingSubscriptions.Count} 个缓存的订阅请求...");
            var snapshot = new List<Action>(_pendingSubscriptions);
            _pendingSubscriptions.Clear();
            foreach (var action in snapshot)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ros2Node] 执行缓存订阅时出错: {ex.Message}");
                }
            }
        }

        private void ApplyConfig()
        {
            if (_config == null) return;
            _nodeName = _config.NodeName;
            _rosbridgeUrl = _config.RosbridgeUrl;
            _autoConnect = _config.AutoConnect;
            _reconnectInterval = _config.ReconnectInterval;
            _maxReconnectAttempts = _config.MaxReconnectAttempts;
        }

        private void ConnectThread()
        {
            try
            {
                IProtocol protocol = ProtocolInitializer.GetProtocol(Protocol.WebSocketSharp, RosbridgeUrl);
                protocol.OnConnected += (sender, e) =>
                {
                    _connectionEvent.Set();
                    Debug.Log("[Ros2Node] WebSocket 连接成功");
                };
                protocol.OnClosed += (sender, e) =>
                {
                    IsConnected = false;
                    Debug.LogWarning("[Ros2Node] WebSocket 连接断开");
                    OnDisconnected?.Invoke();
                };

                RosSocket = new RosSocket(protocol, RosSocket.SerializerEnum.Newtonsoft_JSON);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ros2Node] 连接失败: {ex.Message}");
                OnConnectionError?.Invoke(ex.Message);
            }
        }

        private IEnumerator WaitForConnection()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (!_connectionEvent.WaitOne(0) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_connectionEvent.WaitOne(0))
            {
                IsConnected = true;
                _reconnectAttempts = 0;
                OnConnected?.Invoke();
                Debug.Log("[Ros2Node] 连接就绪");
                FlushPendingSubscriptions();
            }
            else
            {
                Debug.LogError("[Ros2Node] 连接超时");
                OnConnectionError?.Invoke("连接超时");

                if (ShouldReconnect())
                {
                    _reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
                }
            }
        }

        private IEnumerator ReconnectCoroutine()
        {
            while (ShouldReconnect())
            {
                _reconnectAttempts++;
                float delay = _reconnectInterval * Mathf.Pow(1.5f, Mathf.Min(_reconnectAttempts - 1, 5));
                Debug.Log($"[Ros2Node] {delay:F1}s 后重连 (第 {_reconnectAttempts} 次)");
                yield return new WaitForSeconds(delay);

                _connectionEvent = new ManualResetEvent(false);
                new Thread(ConnectThread).Start();
                yield return StartCoroutine(WaitForConnection());

                if (IsConnected)
                {
                    _reconnectCoroutine = null;
                    yield break;
                }
            }

            _reconnectCoroutine = null;
        }

        private bool ShouldReconnect()
        {
            return _maxReconnectAttempts < 0 || _reconnectAttempts < _maxReconnectAttempts;
        }
    }
}
