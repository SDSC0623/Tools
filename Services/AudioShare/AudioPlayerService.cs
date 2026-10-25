// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using NAudio.Wave;
using Tools.Services.IServices;

namespace Tools.Services.AudioShare;

// 音频播放服务
// 使用 WaveOutEvent + BufferedWaveProvider 播放 48kHz / 16bit / 双声道 PCM
// 发送端按 20ms 分片传输，接收端用按序号排序的抖动缓冲消除乱序与抖动
// 音量直接写入 WaveOutEvent.Volume，保证滑动条实时生效
// 暂停恢复时重建播放管线，丢弃暂停期间残留的缓冲与设备队列，避免恢复延迟
public sealed class AudioPlayerService : IAudioPlayerService {
    // 采样参数
    private const int SampleRate = 48000;
    private const int Channels = 2;
    private const int BitsPerSample = 16;

    // 每毫秒字节数（48000 * 2声道 * 2字节 / 1000 = 192）
    private const double BytesPerMs = SampleRate * (double)Channels * BitsPerSample / 8 / 1000.0;

    // 发送端音频分片时长（毫秒），与 UdpAudioTransportService 保持一致
    private const int PacketMs = 20;

    // 正常起播时的抖动缓冲
    private const int StartBufferMs = 120;

    // 暂停恢复时的最小抖动缓冲（越小恢复越快）
    private const int ResumeBufferMs = 40;

    // 抖动缓冲上限，超过则丢弃最旧的数据
    private const int MaxJitterMs = 300;

    // BufferedWaveProvider 容量，需大于抖动缓冲上限并留出设备缓冲余量
    private const int BufferDurationMs = 400;

    // 播放格式
    private static readonly WaveFormat PlaybackFormat = new(SampleRate, BitsPerSample, Channels);

    private readonly object _lock = new();

    // 待播放的音频包（按序号排序的抖动缓冲）
    private readonly SortedDictionary<uint, byte[]> _pending = [];

    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _buffered;

    private bool _started;
    private bool _paused;
    private bool _disposed;

    // 抖动缓冲是否已达到起播条件
    private bool _primed;

    // 起播所需的包数
    private int _prebufferPackets = StartBufferMs / PacketMs;

    private float _volume = 1.0f;

    // 已经写入播放缓冲的最大序号（-1 表示尚未播放过任何数据）
    private long _lastPlayedSeq = -1;

    // 统计
    private long _playedPackets;
    private long _lostPackets;

    public bool IsStarted {
        get {
            lock (_lock) {
                return _started;
            }
        }
    }

    public bool IsPlaying {
        get {
            lock (_lock) {
                return _started && !_paused;
            }
        }
    }

    // 音量 0.0 ~ 1.0，实时写入声卡输出，避免出现爆音
    public float Volume {
        get {
            lock (_lock) {
                return _volume;
            }
        }
        set {
            var volume = Math.Clamp(value, 0f, 1f);
            lock (_lock) {
                _volume = volume;
                if (_waveOut == null) {
                    return;
                }

                try {
                    _waveOut.Volume = volume;
                } catch (Exception) {
                    // 设备已失效时忽略，下一次重建管线会重新应用音量
                }
            }
        }
    }

    public double BufferLevelMs {
        get {
            lock (_lock) {
                return (_buffered?.BufferedBytes ?? 0) / BytesPerMs;
            }
        }
    }

    public double DelayMs {
        get {
            // 播放缓冲深度 + 声卡输出延迟（DesiredLatency = 100ms）
            return BufferLevelMs + 100;
        }
    }

    public double LossRate {
        get {
            lock (_lock) {
                var total = _playedPackets + _lostPackets;
                return total == 0 ? 0 : (double)_lostPackets / total;
            }
        }
    }

    public void Start() {
        lock (_lock) {
            if (_disposed || _started) {
                return;
            }

            _pending.Clear();
            _primed = false;
            _lastPlayedSeq = -1;
            _playedPackets = 0;
            _lostPackets = 0;
            CreatePipeline(StartBufferMs);
            _started = true;
            _paused = false;
        }
    }

    public void Stop() {
        lock (_lock) {
            _started = false;
            _paused = false;
            _primed = false;
            _pending.Clear();
            _lastPlayedSeq = -1;
            DisposePipeline();
        }
    }

    public void Play() {
        lock (_lock) {
            if (_disposed || !_started) {
                return;
            }

            if (!_paused) {
                _waveOut?.Play();
                return;
            }

            // 恢复播放：丢弃暂停期间残留的数据与声卡队列，用最小的抖动缓冲重新起播
            _pending.Clear();
            _primed = false;
            // 暂停期间发送端序号仍在前进，重置播放序号避免把暂停间隔误计入丢包
            _lastPlayedSeq = -1;
            CreatePipeline(ResumeBufferMs);
            _paused = false;
        }
    }

    public void Pause() {
        lock (_lock) {
            if (!_started || _paused) {
                return;
            }

            _paused = true;
            _pending.Clear();
            _primed = false;
            try {
                _waveOut?.Pause();
            } catch (Exception) {
                // 设备异常时保持状态，下次 Play 会重建管线
            }
        }
    }

    public void ResetBuffer() {
        lock (_lock) {
            ResetBufferCore();
        }
    }

    public void Feed(uint seq, long timestamp, byte[] pcm16) {
        if (pcm16 == null || pcm16.Length == 0) {
            return;
        }

        lock (_lock) {
            // 暂停期间到达的数据直接丢弃，避免恢复时播出陈旧音频
            if (!_started || _paused || _buffered == null) {
                return;
            }

            // 发送端重启导致序号大幅回退：清空旧会话数据重新开始
            if (_primed && (long)seq < _lastPlayedSeq && _lastPlayedSeq - seq > 500) {
                ResetBufferCore();
            }

            // 重复包或已经播放过的迟到包
            if (_primed && (long)seq <= _lastPlayedSeq) {
                return;
            }

            if (_pending.ContainsKey(seq)) {
                return;
            }

            _pending[seq] = pcm16;

            // 抖动缓冲超过上限时丢弃最旧的数据，保证延迟不持续增长
            while (_pending.Count > MaxJitterMs / PacketMs) {
                TrimOldest();
            }

            DrainContiguous();

            // timestamp 目前仅用于协议对齐，延迟由本地缓冲深度估算
            _ = timestamp;
        }
    }

    // 把抖动缓冲中已连续的数据写入播放缓冲
    private void DrainContiguous() {
        if (_buffered == null) {
            return;
        }

        if (!_primed) {
            if (_pending.Count < _prebufferPackets) {
                return;
            }

            _primed = true;
            try {
                _waveOut?.Play();
            } catch (Exception) {
                // 设备异常时仍然继续缓冲，下次 Play 会重建管线
            }
        }

        while (_pending.Count > 0) {
            var seq = _pending.Keys.First();
            var data = _pending[seq];
            _pending.Remove(seq);

            // 迟到包（序号小于已播放位置）直接丢弃
            if (_lastPlayedSeq >= 0 && (long)seq <= _lastPlayedSeq) {
                continue;
            }

            // 序号不连续说明中间丢包，计入丢包统计
            if (_lastPlayedSeq >= 0 && (long)seq > _lastPlayedSeq + 1) {
                _lostPackets += (long)seq - _lastPlayedSeq - 1;
            }

            _buffered.AddSamples(data, 0, data.Length);
            _lastPlayedSeq = seq;
            _playedPackets++;
        }
    }

    // 丢弃抖动缓冲中最旧的数据
    private void TrimOldest() {
        if (_pending.Count == 0) {
            return;
        }

        var seq = _pending.Keys.First();
        _pending.Remove(seq);
        if (_lastPlayedSeq >= 0 && (long)seq > _lastPlayedSeq) {
            _lostPackets++;
        }
    }

    // 清空抖动缓冲、播放缓冲与声卡队列，保持播放管线可用
    private void ResetBufferCore() {
        _pending.Clear();
        _primed = false;
        _lastPlayedSeq = -1;
        try {
            _buffered?.ClearBuffer();
            _waveOut?.Stop();
        } catch (Exception) {
            // 设备异常时忽略，后续 Play 会重建管线
        }
    }

    // 创建播放管线（prebufferMs 决定起播所需的抖动缓冲）
    private void CreatePipeline(int prebufferMs) {
        DisposePipeline();

        var buffered = new BufferedWaveProvider(PlaybackFormat) {
            BufferDuration = TimeSpan.FromMilliseconds(BufferDurationMs),
            DiscardOnBufferOverflow = true
        };

        var waveOut = new WaveOutEvent {
            DesiredLatency = 100,
            NumberOfBuffers = 4
        };

        waveOut.Init(buffered);
        waveOut.Volume = _volume;

        _buffered = buffered;
        _waveOut = waveOut;
        _prebufferPackets = Math.Max(1, prebufferMs / PacketMs);
    }

    // 释放播放管线
    private void DisposePipeline() {
        try {
            _waveOut?.Stop();
        } catch (Exception) {
            // 忽略设备停止异常
        }

        try {
            _waveOut?.Dispose();
        } catch (Exception) {
            // 忽略设备释放异常
        }

        _waveOut = null;

        try {
            _buffered?.ClearBuffer();
        } catch (Exception) {
            // 忽略缓冲清理异常
        }

        _buffered = null;
    }

    public void Dispose() {
        lock (_lock) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _started = false;
            _paused = false;
            _primed = false;
            _pending.Clear();
            _lastPlayedSeq = -1;
            DisposePipeline();
        }
    }
}
