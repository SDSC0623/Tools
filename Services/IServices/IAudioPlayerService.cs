// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Tools.Services.IServices;

public interface IAudioPlayerService : IDisposable {
    /// <summary>
    /// 是否已启动（播放管线已创建）
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    /// 是否正在播放
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// 音量（0.0 ~ 1.0），实时生效
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// 当前缓冲深度（毫秒）
    /// </summary>
    double BufferLevelMs { get; }

    /// <summary>
    /// 估算的播放延迟（毫秒）
    /// </summary>
    double DelayMs { get; }

    /// <summary>
    /// 丢包率（0.0 ~ 1.0）
    /// </summary>
    double LossRate { get; }

    /// <summary>
    /// 启动播放管线
    /// </summary>
    void Start();

    /// <summary>
    /// 停止并释放播放管线
    /// </summary>
    void Stop();

    /// <summary>
    /// 继续播放（会丢弃暂停期间残留的音频，按最小抖动缓冲重新起播）
    /// </summary>
    void Play();

    /// <summary>
    /// 暂停播放
    /// </summary>
    void Pause();

    /// <summary>
    /// 清空抖动缓冲与播放缓冲（用于对端会话重启、序号回退等场景），保持播放管线可用
    /// </summary>
    void ResetBuffer();

    /// <summary>
    /// 喂入音频数据（经抖动缓冲排序后播放）
    /// </summary>
    /// <param name="seq">发送端序号</param>
    /// <param name="timestamp">发送端时间戳</param>
    /// <param name="pcm16">PCM16 数据（48kHz/16bit/双声道）</param>
    void Feed(uint seq, long timestamp, byte[] pcm16);
}
