// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Runtime.InteropServices;
using ApplicationLoopback.NET;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;
using Tools.Services.IServices;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.Services.AudioShare;

// 音频捕获服务
// 优先使用 ApplicationLoopback.NET 实现进程级捕获（Windows 10 Build 20348+）
// 不满足系统要求时回退为 NAudio 的全系统回环捕获，并在 UI 中提示用户
public class ProcessAudioCaptureService : IAudioCaptureService {
    // 统一输出格式：48kHz / 16bit / 双声道
    private const int CaptureChannels = 2;
    private const int CaptureSampleRate = 48000;

    // 进程级捕获所需的最低系统构建版本
    private const int ProcessLoopbackMinBuild = 20348;

    // 日志
    private readonly ILogger _logger;

    // 进程级捕获实例
    private ApplicationLoopbackCapture? _processCapture;

    // 系统回环捕获实例（回退方案）
    private WasapiLoopbackCapture? _fallbackCapture;

    // 系统回环捕获的重采样管线
    private BufferedWaveProvider? _fallbackProvider;

    // 重采样后的采样提供器（48kHz 立体声 float）
    private ISampleProvider? _fallbackSampleProvider;

    // 捕获源的实际格式
    private WaveFormat? _fallbackInputFormat;

    public ProcessAudioCaptureService(ILogger logger) {
        _logger = logger;
    }

    public event Action<ReadOnlyMemory<byte>>? PcmDataAvailable;

    public event Action<AudioCaptureMode>? CaptureModeChanged;

    public bool IsCapturing { get; private set; }

    public int SampleRate => CaptureSampleRate;

    public int Channels => CaptureChannels;

    public AudioCaptureMode Mode { get; private set; } = AudioCaptureMode.ProcessLoopback;

    public bool IsProcessLoopbackSupported => Environment.OSVersion.Version.Build >= ProcessLoopbackMinBuild;

    public void StartCapture(int processId) {
        if (IsCapturing) {
            return;
        }

        if (IsProcessLoopbackSupported) {
            StartProcessCapture(processId);
        } else {
            StartSystemLoopbackCapture();
        }
    }

    private void StartProcessCapture(int processId) {
        try {
            _processCapture = new ApplicationLoopbackCapture(CaptureChannels, CaptureSampleRate);
            _processCapture.NewDataAvailable += OnProcessDataAvailable;
            _processCapture.CaptureStopped += OnCaptureStopped;
            _processCapture.StartCapture((uint)processId, CaptureMode.IncludeProcessTree);
            Mode = AudioCaptureMode.ProcessLoopback;
            IsCapturing = true;
            _logger.Information("开始进程级音频捕获，进程ID: {ProcessId}，格式: {SampleRate}Hz/{Channels}声道",
                processId, CaptureSampleRate, CaptureChannels);
            CaptureModeChanged?.Invoke(Mode);
        } catch (Exception e) {
            _processCapture?.Dispose();
            _processCapture = null;
            _logger.Error(e, "启动进程级音频捕获失败");
            throw new InvalidOperationException($"启动进程级音频捕获失败: {e.Message}", e);
        }
    }

    private void StartSystemLoopbackCapture() {
        try {
            _fallbackCapture = new WasapiLoopbackCapture();
            _fallbackInputFormat = _fallbackCapture.WaveFormat;

            // 输入缓冲
            _fallbackProvider = new BufferedWaveProvider(_fallbackInputFormat) {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(500)
            };

            // 统一转为 48kHz 立体声 float，便于后续转为 PCM16
            _fallbackSampleProvider = new WdlResamplingSampleProvider(
                new SampleChannel(_fallbackProvider), CaptureSampleRate);

            _fallbackCapture.DataAvailable += OnFallbackDataAvailable;
            _fallbackCapture.RecordingStopped += OnFallbackRecordingStopped;
            _fallbackCapture.StartRecording();
            Mode = AudioCaptureMode.SystemLoopback;
            IsCapturing = true;
            _logger.Warning("当前系统不支持进程级音频捕获（需要 Build {MinBuild}+），已回退为全系统回环捕获，输入格式: {InputFormat}",
                ProcessLoopbackMinBuild, _fallbackInputFormat);
            CaptureModeChanged?.Invoke(Mode);
        } catch (Exception e) {
            _fallbackCapture?.Dispose();
            _fallbackCapture = null;
            _fallbackProvider = null;
            _fallbackSampleProvider = null;
            _fallbackInputFormat = null;
            _logger.Error(e, "启动系统回环捕获失败");
            throw new InvalidOperationException($"启动系统回环捕获失败: {e.Message}", e);
        }
    }

    // 进程级捕获数据回调（float 交错采样）
    private void OnProcessDataAvailable(ReadOnlySpan<float> data) {
        if (data.Length == 0) {
            return;
        }

        var pcm = new byte[data.Length * 2];
        var shorts = MemoryMarshal.Cast<byte, short>(pcm);
        for (int i = 0; i < data.Length; i++) {
            var sample = data[i];
            if (sample > 1f) {
                sample = 1f;
            } else if (sample < -1f) {
                sample = -1f;
            }

            shorts[i] = (short)(sample * 32767f);
        }

        PcmDataAvailable?.Invoke(pcm);
    }

    // 系统回环捕获数据回调（任意格式，需重采样）
    private void OnFallbackDataAvailable(object? sender, WaveInEventArgs e) {
        if (e.BytesRecorded <= 0 || _fallbackProvider == null || _fallbackSampleProvider == null ||
            _fallbackInputFormat == null) {
            return;
        }

        // 将原始数据放入输入缓冲
        _fallbackProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);

        // 计算期望的输出采样数（重采样到 48kHz 后）
        var inputFrames = e.BytesRecorded / _fallbackInputFormat.BlockAlign;
        var outputSamples = (int)((long)inputFrames * CaptureSampleRate * CaptureChannels /
                                  _fallbackInputFormat.SampleRate);
        if (outputSamples <= 0) {
            return;
        }

        var temp = ArrayPool<float>.Shared.Rent(outputSamples);
        try {
            var read = _fallbackSampleProvider.Read(temp, 0, outputSamples);
            if (read <= 0) {
                return;
            }

            var pcm = new byte[read * 2];
            var shorts = MemoryMarshal.Cast<byte, short>(pcm);
            for (int i = 0; i < read; i++) {
                var sample = temp[i];
                if (sample > 1f) {
                    sample = 1f;
                } else if (sample < -1f) {
                    sample = -1f;
                }

                shorts[i] = (short)(sample * 32767f);
            }

            PcmDataAvailable?.Invoke(pcm);
        } finally {
            ArrayPool<float>.Shared.Return(temp);
        }
    }

    private void OnFallbackRecordingStopped(object? sender, StoppedEventArgs e) {
        OnCaptureStopped();
    }

    private void OnCaptureStopped() {
        IsCapturing = false;
        _logger.Information("音频捕获已停止");
    }

    public void StopCapture() {
        if (_processCapture != null) {
            try {
                _ = _processCapture.StopCapture();
            } catch (Exception e) {
                _logger.Warning(e, "停止进程级捕获时发生错误");
            }

            _processCapture.Dispose();
            _processCapture = null;
        }

        if (_fallbackCapture != null) {
            try {
                _fallbackCapture.StopRecording();
            } catch (Exception e) {
                _logger.Warning(e, "停止系统回环捕获时发生错误");
            }

            _fallbackCapture.Dispose();
            _fallbackCapture = null;
        }

        _fallbackProvider = null;
        _fallbackSampleProvider = null;
        _fallbackInputFormat = null;
        IsCapturing = false;
        Mode = AudioCaptureMode.ProcessLoopback;
    }

    public void Dispose() {
        StopCapture();
    }
}
