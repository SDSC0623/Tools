// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.ViewModel.AudioShare;

// 音频同步发送端 ViewModel
public partial class AudioShareSenderViewModel : ObservableObject {
    // 日志
    private readonly ILogger _logger;

    // 音频捕获服务
    private readonly IAudioCaptureService _captureService;

    // UDP 传输服务
    private readonly IAudioTransportService _transportService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 弹窗服务
    private readonly SnackbarServiceHelper _snackbar;

    // 统计定时器
    private readonly DispatcherTimer _statsTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    // 统计计数器
    private long _bytesInInterval;

    private int _packetsInInterval;

    [ObservableProperty] private ObservableCollection<AudioProcessItem> _processes = [];

    [ObservableProperty] private AudioProcessItem? _selectedProcess;

    [ObservableProperty] private bool _isCapturing;

    [ObservableProperty] private bool _isTransportRunning;

    [ObservableProperty] private int _listenPort = 19999;

    [ObservableProperty] private ObservableCollection<ReceiverItem> _receivers = [];

    [ObservableProperty] private string _captureModeText = "未开始捕获";

    [ObservableProperty] private bool _isSystemFallback;

    [ObservableProperty] private string _sendRateText = "0 KB/s";

    [ObservableProperty] private string _transportStatusText = "发送端未启动";

    public AudioShareSenderViewModel(IAudioCaptureService captureService, IAudioTransportService transportService,
        IPreferencesService preferencesService,
        SnackbarServiceHelper snackbar, ILogger logger) {
        _captureService = captureService;
        _transportService = transportService;
        _preferencesService = preferencesService;
        _snackbar = snackbar;
        _logger = logger;

        ListenPort = _preferencesService.Get("AudioShareSenderPort", 19999);

        _transportService.ReceiverConnected += OnReceiverConnected;
        _transportService.ReceiverHeartbeat += OnReceiverHeartbeat;
        _transportService.ReceiverDisconnected += OnReceiverDisconnected;

        _statsTimer.Tick += OnStatsTick;
        _statsTimer.Start();

        RefreshProcesses();
    }

    // ============ 进程选择 ============

    [RelayCommand]
    private void RefreshProcesses() {
        var processes = Process.GetProcesses()
            .Where(p => {
                try {
                    return p.MainWindowHandle != IntPtr.Zero;
                } catch {
                    return false;
                }
            })
            .Select(p => new AudioProcessItem {
                Pid = p.Id,
                ProcessName = p.ProcessName,
                MainWindowTitle = GetMainWindowTitle(p)
            })
            .Where(p => p.Pid != Environment.ProcessId)
            .OrderBy(p => p.ProcessName)
            .ToList();
        Processes = new ObservableCollection<AudioProcessItem>(processes);

        // 尽量保持原选中项
        if (SelectedProcess != null) {
            SelectedProcess = Processes.FirstOrDefault(p => p.Pid == SelectedProcess.Pid);
        }
    }

    private static string GetMainWindowTitle(Process process) {
        try {
            return process.MainWindowTitle;
        } catch {
            return string.Empty;
        }
    }

    // ============ 音频捕获 ============

    [RelayCommand]
    private void StartCapture() {
        if (SelectedProcess == null) {
            _snackbar.ShowWarning("请选择进程", "请先选择要捕获音频的进程（例如 QQ音乐）");
            return;
        }

        if (IsCapturing) {
            return;
        }

        try {
            if (!IsTransportRunning) {
                _transportService.StartSender(ListenPort);
                IsTransportRunning = true;
                TransportStatusText = $"发送端已启动，监听端口: {ListenPort}";
            }

            _captureService.PcmDataAvailable += OnPcmDataAvailable;
            _captureService.CaptureModeChanged += OnCaptureModeChanged;
            _captureService.StartCapture(SelectedProcess.Pid);
            IsCapturing = true;
            CaptureModeText = _captureService.Mode == AudioCaptureMode.ProcessLoopback
                ? $"进程级捕获: {SelectedProcess.ProcessName} (PID: {SelectedProcess.Pid})"
                : "全系统回环捕获";
            _snackbar.ShowSuccess("开始捕获", $"正在捕获 {SelectedProcess.ProcessName} 的音频");
            _ = _preferencesService.Set("AudioShareSenderPort", ListenPort);
        } catch (Exception e) {
            _logger.Error(e, "开始音频捕获失败");
            _captureService.PcmDataAvailable -= OnPcmDataAvailable;
            _captureService.CaptureModeChanged -= OnCaptureModeChanged;
            if (!_captureService.IsCapturing && IsTransportRunning) {
                _transportService.StopSender();
                IsTransportRunning = false;
                TransportStatusText = "发送端已停止";
            }

            _snackbar.ShowError("开始捕获失败", e.Message);
        }
    }

    [RelayCommand]
    private void StopCapture() {
        if (!IsCapturing) {
            return;
        }

        _captureService.PcmDataAvailable -= OnPcmDataAvailable;
        _captureService.CaptureModeChanged -= OnCaptureModeChanged;
        _captureService.StopCapture();
        IsCapturing = false;
        IsSystemFallback = false;
        CaptureModeText = "未开始捕获";
        SendRateText = "0 KB/s";
        _bytesInInterval = 0;
        _packetsInInterval = 0;

        if (IsTransportRunning) {
            _transportService.StopSender();
            IsTransportRunning = false;
            TransportStatusText = "发送端已停止";
            Receivers.Clear();
        }

        _snackbar.ShowInfo("已停止捕获", "音频捕获已停止");
    }

    private void OnPcmDataAvailable(ReadOnlyMemory<byte> data) {
        _bytesInInterval += data.Length;
        _packetsInInterval++;
        _transportService.SendAudio(data);
    }

    private void OnCaptureModeChanged(AudioCaptureMode mode) {
        if (mode == AudioCaptureMode.SystemLoopback) {
            IsSystemFallback = true;
            CaptureModeText = "全系统回环捕获 (当前系统不支持进程级捕获)";
            Application.Current.Dispatcher.Invoke(() => {
                _snackbar.ShowWarning("已回退为全系统回环捕获",
                    "当前系统不支持进程级捕获，将捕获全部系统声音，请注意不要打开其它有声音的程序");
            });
        }
    }

    // ============ 接收端管理 ============

    private void OnReceiverConnected(IPEndPoint endpoint) {
        Application.Current.Dispatcher.Invoke(() => {
            if (Receivers.Any(r => r.Ip == endpoint.Address.ToString() && r.Port == endpoint.Port)) {
                return;
            }

            Receivers.Add(new ReceiverItem {
                Ip = endpoint.Address.ToString(),
                Port = endpoint.Port,
                IsActive = true,
                StatusText = "在线",
                LastActiveTime = DateTime.Now
            });
            _snackbar.ShowInfo("新接收端接入", $"接收端 {endpoint.Address}:{endpoint.Port} 已连接");
        });
    }

    private void OnReceiverHeartbeat(IPEndPoint endpoint) {
        Application.Current.Dispatcher.Invoke(() => {
            var receiver = Receivers.FirstOrDefault(r => r.Ip == endpoint.Address.ToString() && r.Port == endpoint.Port);
            receiver?.UpdateActive();
        });
    }

    private void OnReceiverDisconnected(IPEndPoint endpoint) {
        Application.Current.Dispatcher.Invoke(() => {
            var receiver = Receivers.FirstOrDefault(r => r.Ip == endpoint.Address.ToString() && r.Port == endpoint.Port);
            if (receiver != null) {
                Receivers.Remove(receiver);
            }
        });
    }

    [RelayCommand]
    private void RemoveReceiver(ReceiverItem receiver) {
        if (receiver == null) {
            return;
        }

        if (IPEndPoint.TryParse(receiver.DisplayText, out var endpoint)) {
            // 通知对端会话结束，同时让传输服务停止向其发送音频
            _transportService.EndReceiverSession(endpoint);
        }

        Receivers.Remove(receiver);
    }

    [RelayCommand]
    private void ClearReceivers() {
        // 逐个通知对端结束会话，再清空列表
        _transportService.EndAllReceiverSessions();
        Receivers.Clear();
    }

    // ============ 统计 ============

    private void OnStatsTick(object? sender, EventArgs e) {
        var bytes = _bytesInInterval;
        var packets = _packetsInInterval;
        _bytesInInterval = 0;
        _packetsInInterval = 0;
        SendRateText = IsCapturing ? $"{bytes / 1024.0:F1} KB/s ({packets} 包/s)" : "0 KB/s";

        // 心跳超时标记离线（传输服务会在 5 秒后真正移除该接收端）
        foreach (var receiver in Receivers
                     .Where(r => r.IsActive && DateTime.Now - r.LastActiveTime > TimeSpan.FromSeconds(3))
                     .ToList()) {
            receiver.UpdateInactive();
        }
    }

    public void Dispose() {
        _statsTimer.Stop();
        _statsTimer.Tick -= OnStatsTick;
        _transportService.ReceiverConnected -= OnReceiverConnected;
        _transportService.ReceiverHeartbeat -= OnReceiverHeartbeat;
        _transportService.ReceiverDisconnected -= OnReceiverDisconnected;
        StopCapture();
    }
}
