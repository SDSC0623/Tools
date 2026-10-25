// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Net;

namespace Tools.Services.IServices;

// 音频数据包类型
public enum AudioPacketType : byte {
    // 接收端 -> 发送端：连接请求（同时用作心跳）
    Hello = 0x01,

    // 接收端 -> 发送端：断开连接
    Bye = 0x02,

    // 发送端 -> 接收端：音频 PCM 数据
    Audio = 0x03,

    // 发送端 -> 接收端：连接确认（同时用作心跳应答）
    Ack = 0x05,

    // 发送端 -> 接收端：会话结束（发送端停止共享或移除了该接收端）
    SessionEnd = 0x06
}

// 会话结束原因
public enum SessionEndReason : byte {
    // 发送端停止共享（接收端可等待并自动重连）
    Stopped = 0x01,

    // 发送端移除了该接收端（接收端应停止接收，重新连接需要用户重新发起）
    Removed = 0x02
}

public interface IAudioTransportService : IDisposable {
    // ============ 发送端 ============

    /// <summary>
    /// 新接收端连接事件
    /// </summary>
    event Action<IPEndPoint>? ReceiverConnected;

    /// <summary>
    /// 接收端心跳事件（接收端定期发送连接请求以保持在线状态）
    /// </summary>
    event Action<IPEndPoint>? ReceiverHeartbeat;

    /// <summary>
    /// 接收端断开事件（接收端主动离开、心跳超时或发送端移除）
    /// </summary>
    event Action<IPEndPoint>? ReceiverDisconnected;

    /// <summary>
    /// 当前已连接的接收端列表
    /// </summary>
    IReadOnlyList<IPEndPoint> Receivers { get; }

    /// <summary>
    /// 发送端是否已启动
    /// </summary>
    bool IsSenderRunning { get; }

    /// <summary>
    /// 当前发送端会话 ID（每次启动发送端都会变化，用于接收端识别对端重启）
    /// </summary>
    uint SenderSessionId { get; }

    /// <summary>
    /// 启动发送端（监听指定端口，等待接收端连接）
    /// </summary>
    void StartSender(int listenPort);

    /// <summary>
    /// 停止发送端（会向所有接收端发送会话结束通知）
    /// </summary>
    void StopSender();

    /// <summary>
    /// 结束指定接收端的会话（向对端发送会话结束通知，并停止向其发送音频）
    /// </summary>
    void EndReceiverSession(IPEndPoint receiver);

    /// <summary>
    /// 结束当前所有接收端会话（不停止发送端本身）
    /// </summary>
    void EndAllReceiverSessions();

    /// <summary>
    /// 发送音频 PCM 数据（48kHz / 16bit / 双声道），内部自动分包发送
    /// </summary>
    void SendAudio(ReadOnlyMemory<byte> pcm16);

    // ============ 接收端 ============

    /// <summary>
    /// 音频数据接收事件（按发送端序号与时间戳回调）
    /// </summary>
    event Action<uint, long, byte[]>? AudioDataReceived;

    /// <summary>
    /// 收到发送端连接确认事件（参数为发送端会话 ID），用于判定应用层连接成功
    /// </summary>
    event Action<uint>? SenderAcknowledged;

    /// <summary>
    /// 发送端结束会话事件（参数为发送端地址、会话 ID 与结束原因），用于接收端停止播放并清理
    /// </summary>
    event Action<IPEndPoint, uint, SessionEndReason>? SenderSessionEnded;

    /// <summary>
    /// 接收端是否已启动
    /// </summary>
    bool IsReceiverRunning { get; }

    /// <summary>
    /// 启动接收端（监听指定端口）
    /// </summary>
    void StartReceiver(int listenPort);

    /// <summary>
    /// 停止接收端
    /// </summary>
    void StopReceiver();

    /// <summary>
    /// 向发送端发起连接请求（同时用作心跳）
    /// </summary>
    /// <param name="senderEndPoint">发送端地址</param>
    /// <param name="linkId">接收端本次连接的链路 ID（同一次连接的重试保持不变，用户重新连接时更换）</param>
    void SendHello(IPEndPoint senderEndPoint, uint linkId);

    /// <summary>
    /// 向发送端发送断开请求
    /// </summary>
    void SendBye(IPEndPoint senderEndPoint);
}
