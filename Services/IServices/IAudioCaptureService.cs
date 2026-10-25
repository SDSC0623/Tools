// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Tools.Services.IServices;

// 音频捕获模式
public enum AudioCaptureMode {
    // 进程级捕获（仅捕获指定进程及其子进程的音频）
    ProcessLoopback,

    // 全系统回环捕获（捕获系统所有声音）
    SystemLoopback
}

public interface IAudioCaptureService : IDisposable {
    /// <summary>
    /// PCM 数据可用事件，输出格式固定为 48kHz / 16bit / 双声道 小端交错 PCM
    /// </summary>
    event Action<ReadOnlyMemory<byte>>? PcmDataAvailable;

    /// <summary>
    /// 捕获模式变化事件（例如从进程级回退到全系统回环捕获）
    /// </summary>
    event Action<AudioCaptureMode>? CaptureModeChanged;

    /// <summary>
    /// 是否正在捕获
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// 输出采样率
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// 输出声道数
    /// </summary>
    int Channels { get; }

    /// <summary>
    /// 当前捕获模式
    /// </summary>
    AudioCaptureMode Mode { get; }

    /// <summary>
    /// 当前系统是否支持进程级捕获（需要 Windows 10 Build 20348+ 或 Windows 11）
    /// </summary>
    bool IsProcessLoopbackSupported { get; }

    /// <summary>
    /// 开始捕获指定进程（含进程树）的音频
    /// </summary>
    /// <param name="processId">目标进程 ID</param>
    void StartCapture(int processId);

    /// <summary>
    /// 停止捕获
    /// </summary>
    void StopCapture();
}
