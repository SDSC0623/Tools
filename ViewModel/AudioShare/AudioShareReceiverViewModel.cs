// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Tools.Services;
using Tools.Services.IServices;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.ViewModel.AudioShare;

// 音频同步接收端 ViewModel
// UDP 本身无连接，因此“连接/断线”由应用层会话维护：
// 发送 Hello（同时作为心跳） -> 收到 Ack（携带发送端会话 ID）才算连接成功
// 心跳超时判定断线 -> 指数退避重连 -> 收到 Ack 后恢复
public partial class AudioShareReceiverViewModel : ObservableObject {
    // 应用层链路状态
    private enum LinkState {
        // 空闲（未连接）
        Idle,

        // 握手中（已发出 Hello，等待 Ack）
        Connecting,

        // 已连接（持续收到 Ack / 心跳）
        Connected,

        // 重连中（心跳超时，正在退避重试）
        Reconnecting
    }

    // 握手超时时间（从点击连接到必须收到 Ack）
    private const int HandshakeTimeoutMs = 2000;

    // 握手阶段 Hello 重发间隔
    private const int HelloRetryMs = 500;

    // 连接建立后的心跳间隔
    private const int HeartbeatMs = 1000;

    // 心跳存活超时（超过该时间未收到 Ack 即判定断线）
    private const int LivenessTimeoutMs = 3000;

    // 重连初始退避时间
    private const int ReconnectInitialDelayMs = 500;

    // 重连最大退避时间
    private const int ReconnectMaxDelayMs = 4000;

    // 链路状态巡检间隔
    private const int LinkCheckIntervalMs = 200;

    // 日志
    private readonly ILogger _logger;

    // UDP 传输服务
    private readonly IAudioTransportService _transportService;

    // 音频播放服务
    private readonly IAudioPlayerService _playerService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 弹窗服务
    private readonly SnackbarServiceHelper _snackbar;

    // 发送端地址
    private IPEndPoint? _senderEndPoint;

    // 统计定时器
    private readonly DispatcherTimer _statsTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    // 链路巡检定时器（握手超时、心跳、断线判定、退避重连）
    private readonly DispatcherTimer _linkTimer = new() {
        Interval = TimeSpan.FromMilliseconds(LinkCheckIntervalMs)
    };

    // 初始化标记，避免加载配置时触发保存
    private bool _isInitialized;

    // 当前链路状态
    private LinkState _state = LinkState.Idle;

    // 握手截止时间
    private DateTime _handshakeDeadline = DateTime.MinValue;

    // 下一次发送 Hello 的时间
    private DateTime _nextHelloAt = DateTime.MinValue;

    // 最近一次收到 Ack 的时间
    private DateTime _lastAckUtc = DateTime.MinValue;

    // 当前的重连退避时间
    private int _reconnectDelayMs = ReconnectInitialDelayMs;

    // 已确认的发送端会话 ID（用于识别对端重启）
    private uint _acknowledgedSession;

    // 本次连接的链路 ID（同一次连接的心跳重试保持不变，用户重新连接时更换）
    private uint _linkId;

    [ObservableProperty] private string _senderIp = string.Empty;

    [ObservableProperty] private int _senderPort = 19999;

    [ObservableProperty] private int _listenPort = 19999;

    // 是否真正连接成功（只有收到发送端 Ack 后才为 true）
    [ObservableProperty] private bool _isConnected;

    // 是否处于会话中（连接中 / 已连接 / 重连中），用于控制连接按钮的显隐
    [ObservableProperty] private bool _isSessionActive;

    [ObservableProperty] private bool _isPlaying;

    [ObservableProperty] private double _volume = 1.0;

    [ObservableProperty] private string _connectStateText = "未连接";

    [ObservableProperty] private string _statusText = "延迟: -- ms   丢包率: -- %   缓冲: -- ms";

    public AudioShareReceiverViewModel(IAudioTransportService transportService, IAudioPlayerService playerService,
        IPreferencesService preferencesService, SnackbarServiceHelper snackbar, ILogger logger) {
        _transportService = transportService;
        _playerService = playerService;
        _preferencesService = preferencesService;
        _snackbar = snackbar;
        _logger = logger;

        SenderIp = _preferencesService.Get("AudioShareSenderIp", string.Empty) ?? string.Empty;
        SenderPort = _preferencesService.Get("AudioShareSenderPort", 19999);
        ListenPort = _preferencesService.Get("AudioShareReceiverListenPort", 19999);
        _isInitialized = true;
        Volume = _preferencesService.Get("AudioShareVolume", 1.0);

        // 只在这里订阅一次，避免重复 Feed 导致丢包率虚高
        _transportService.AudioDataReceived += OnAudioDataReceived;
        _transportService.SenderAcknowledged += OnSenderAcknowledged;
        _transportService.SenderSessionEnded += OnSenderSessionEnded;

        _statsTimer.Tick += OnStatsTick;
        _statsTimer.Start();

        _linkTimer.Tick += OnLinkTick;
        _linkTimer.Start();
    }

    partial void OnVolumeChanged(double value) {
        _playerService.Volume = (float)value;
        if (_isInitialized) {
            _ = _preferencesService.Set("AudioShareVolume", value);
        }
    }

    // ============ 连接管理 ============

    [RelayCommand]
    private void Connect() {
        if (_state != LinkState.Idle) {
            return;
        }

        if (!IPAddress.TryParse(SenderIp, out var ip)) {
            _snackbar.ShowWarning("IP 地址无效", "请输入正确的发送端 IP 地址（例如 EasyTier 虚拟 IP 10.0.0.1）");
            return;
        }

        if (SenderPort is < 1 or > 65535 || ListenPort is < 1 or > 65535) {
            _snackbar.ShowWarning("端口无效", "端口范围应为 1 ~ 65535");
            return;
        }

        try {
            _playerService.Start();
            if (!_transportService.IsReceiverRunning) {
                _transportService.StartReceiver(ListenPort);
            }

            _senderEndPoint = new IPEndPoint(ip, SenderPort);
            _acknowledgedSession = 0;
            _linkId = CreateLinkId();
            _reconnectDelayMs = ReconnectInitialDelayMs;
            _lastAckUtc = DateTime.UtcNow;
            _handshakeDeadline = DateTime.UtcNow.AddMilliseconds(HandshakeTimeoutMs);

            // 握手未完成前不显示“已连接”，只显示“连接中”
            SetState(LinkState.Connecting);
            IsConnected = false;
            IsSessionActive = true;
            IsPlaying = true;
            ConnectStateText = "连接中…";
            StatusText = "正在等待发送端确认…";
            SendHello();
            _nextHelloAt = DateTime.UtcNow.AddMilliseconds(HelloRetryMs);

            _ = _preferencesService.Set("AudioShareSenderIp", SenderIp);
            _ = _preferencesService.Set("AudioShareSenderPort", SenderPort);
            _ = _preferencesService.Set("AudioShareReceiverListenPort", ListenPort);
        } catch (Exception e) {
            _logger.Error(e, "连接发送端失败");
            ResetSession();
            _snackbar.ShowError("连接失败", e.Message);
        }
    }

    [RelayCommand]
    private void Disconnect() {
        if (_state == LinkState.Idle) {
            return;
        }

        if (_senderEndPoint != null) {
            _transportService.SendBye(_senderEndPoint);
        }

        StopLink();
        ConnectStateText = "未连接";
        StatusText = "延迟: -- ms   丢包率: -- %   缓冲: -- ms";
        _snackbar.ShowInfo("已断开", "已断开与发送端的连接");
    }

    // 握手超时：对端未启动、IP/端口错误或网络不可达时都会走到这里
    private void FailConnect() {
        _logger.Warning("连接发送端 {EndPoint} 握手超时", _senderEndPoint);
        ResetSession();
        ConnectStateText = "连接失败（未收到发送端确认）";
        StatusText = "请检查发送端 IP/端口，或确认对端已开始共享";
        _snackbar.ShowWarning("连接超时", "未收到发送端确认，请检查 IP/端口或对端是否已开始共享");
    }

    // 心跳超时：进入指数退避重连，UI 显示“重连中”
    private void EnterReconnecting() {
        _logger.Warning("发送端 {EndPoint} 心跳超时，开始重连", _senderEndPoint);
        SetState(LinkState.Reconnecting);
        IsConnected = false;
        _reconnectDelayMs = ReconnectInitialDelayMs;
        _nextHelloAt = DateTime.UtcNow;
        ConnectStateText = "重连中…";
        StatusText = "连接已中断，正在自动重连…";
    }

    // 发送 Hello（同时作为心跳）
    private void SendHello() {
        if (_senderEndPoint == null || _linkId == 0) {
            return;
        }

        try {
            _transportService.SendHello(_senderEndPoint, _linkId);
        } catch (Exception e) {
            _logger.Warning(e, "发送心跳失败");
        }
    }

    // 停止链路并释放资源（不弹提示）
    private void StopLink() {
        if (_transportService.IsReceiverRunning) {
            _transportService.StopReceiver();
        }

        _playerService.Stop();
        _senderEndPoint = null;
        _acknowledgedSession = 0;
        _linkId = 0;
        SetState(LinkState.Idle);
        IsConnected = false;
        IsSessionActive = false;
        IsPlaying = false;
    }

    // 连接失败时回到空闲并释放资源
    private void ResetSession() {
        if (_transportService.IsReceiverRunning) {
            _transportService.StopReceiver();
        }

        _playerService.Stop();
        _senderEndPoint = null;
        _acknowledgedSession = 0;
        _linkId = 0;
        SetState(LinkState.Idle);
        IsConnected = false;
        IsSessionActive = false;
        IsPlaying = false;
    }

    private void SetState(LinkState state) {
        _state = state;
    }

    // 生成链路 ID：每次用户点击“连接”都换新值，发送端据此区分心跳重试和用户重新连接
    private static uint CreateLinkId() {
        var linkId = (uint)Random.Shared.NextInt64(1, uint.MaxValue);
        return linkId == 0 ? 1u : linkId;
    }

    // ============ 链路巡检 ============

    private void OnLinkTick(object? sender, EventArgs e) {
        var now = DateTime.UtcNow;

        switch (_state) {
            case LinkState.Connecting:
                if (now >= _handshakeDeadline) {
                    FailConnect();
                    return;
                }

                if (now >= _nextHelloAt) {
                    SendHello();
                    _nextHelloAt = now.AddMilliseconds(HelloRetryMs);
                }

                break;

            case LinkState.Connected:
                if (now - _lastAckUtc > TimeSpan.FromMilliseconds(LivenessTimeoutMs)) {
                    EnterReconnecting();
                    return;
                }

                if (now >= _nextHelloAt) {
                    SendHello();
                    _nextHelloAt = now.AddMilliseconds(HeartbeatMs);
                }

                break;

            case LinkState.Reconnecting:
                // 指数退避：500 -> 1000 -> 2000 -> 4000（封顶）
                if (now >= _nextHelloAt) {
                    SendHello();
                    _reconnectDelayMs = Math.Min(_reconnectDelayMs * 2, ReconnectMaxDelayMs);
                    _nextHelloAt = now.AddMilliseconds(_reconnectDelayMs);
                }

                break;
        }
    }

    // ============ 播放控制 ============

    [RelayCommand]
    private void TogglePlayPause() {
        if (IsPlaying) {
            _playerService.Pause();
            IsPlaying = false;
        } else {
            _playerService.Play();
            IsPlaying = true;
        }
    }

    // ============ 接收回调 ============

    private void OnAudioDataReceived(uint seq, long timestamp, byte[] pcm) {
        _playerService.Feed(seq, timestamp, pcm);
    }

    // 收到发送端连接确认（可能来自心跳应答），只有这里才允许显示“已连接”
    private void OnSenderAcknowledged(uint session) {
        RunOnUiThread(() => {
            if (_state is LinkState.Idle) {
                return;
            }

            var wasReconnecting = _state == LinkState.Reconnecting;
            var sessionChanged = _acknowledgedSession != 0 && _acknowledgedSession != session;

            // 发送端重启（会话 ID 变化）时清空旧会话残留的音频数据
            if (_acknowledgedSession != session && _playerService.IsStarted) {
                _playerService.ResetBuffer();
            }

            _acknowledgedSession = session;
            _lastAckUtc = DateTime.UtcNow;

            if (_state is LinkState.Connecting or LinkState.Reconnecting) {
                SetState(LinkState.Connected);
                IsConnected = true;
                IsSessionActive = true;
                // 保持用户的暂停状态，不因重连自动恢复播放
                IsPlaying = _playerService.IsPlaying;
                ConnectStateText = "已连接";
                StatusText = "已连接，等待音频数据…";

                if (wasReconnecting || sessionChanged) {
                    _snackbar.ShowInfo("已重新连接", "与发送端的连接已恢复");
                } else {
                    _snackbar.ShowSuccess("连接成功", $"已连接发送端 {_senderEndPoint}");
                }
            }
        });
    }

    // 发送端结束会话（停止共享或移除了本接收端）
    private void OnSenderSessionEnded(IPEndPoint remote, uint session, SessionEndReason reason) {
        RunOnUiThread(() => {
            if (_state is LinkState.Idle) {
                return;
            }

            _logger.Information("发送端 {Remote} 结束会话 {Session}，原因 {Reason}", remote, session, reason);

            if (reason == SessionEndReason.Removed) {
                // 被发送端移除：彻底停止接收，重新连接需要用户再次点击“连接”
                StopLink();
                ConnectStateText = "发送端已移除本设备";
                StatusText = "发送端已移除本设备，已停止接收";
                _snackbar.ShowWarning("已停止接收", "发送端已将本设备移除");
                return;
            }

            // 发送端停止共享：保留链路 ID 与监听端口，进入退避重连，发送端恢复后自动接上
            if (_senderEndPoint == null) {
                return;
            }

            SetState(LinkState.Reconnecting);
            IsConnected = false;
            _reconnectDelayMs = ReconnectInitialDelayMs;
            _nextHelloAt = DateTime.UtcNow.AddMilliseconds(ReconnectInitialDelayMs);
            ConnectStateText = "发送端已停止共享，等待恢复…";
            StatusText = "对端已停止共享，将自动重连…";
        });
    }

    // 传输服务的事件在后台线程触发，统一切回 UI 线程处理
    private static void RunOnUiThread(Action action) {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess()) {
            action();
            return;
        }

        dispatcher.BeginInvoke(action);
    }

    // ============ 统计 ============

    private void OnStatsTick(object? sender, EventArgs e) {
        if (!IsConnected) {
            return;
        }

        StatusText =
            $"延迟: {_playerService.DelayMs:F0} ms   丢包率: {_playerService.LossRate * 100:F1} %   缓冲: {_playerService.BufferLevelMs:F0} ms";
    }

    public void Dispose() {
        _statsTimer.Stop();
        _statsTimer.Tick -= OnStatsTick;
        _linkTimer.Stop();
        _linkTimer.Tick -= OnLinkTick;
        _transportService.AudioDataReceived -= OnAudioDataReceived;
        _transportService.SenderAcknowledged -= OnSenderAcknowledged;
        _transportService.SenderSessionEnded -= OnSenderSessionEnded;
        Disconnect();
    }
}
