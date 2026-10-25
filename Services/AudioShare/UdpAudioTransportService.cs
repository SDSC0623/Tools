// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Serilog;
using Tools.Services.IServices;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.Services.AudioShare;

// UDP 音频传输服务
// 通过 EasyTier 虚拟局域网使用 UDP 单播传输音频 PCM 数据
// 每个 UDP 数据包携带: 魔数(2) + 协议版本(1) + 类型(1) + 会话ID(4) + 序号(4) + 时间戳(8) + 数据
// 发送端与接收端可共用同一个监听端口，服务内部按端口复用同一个 UDP 套接字
// 应用层会话：接收端 Hello（心跳） -> 发送端 Ack（携带会话 ID） -> 接收端据此判定连接成功
public class UdpAudioTransportService : IAudioTransportService {
    // 数据包头长度（不含类型负载）
    private const int PacketHeaderLength = 20;

    // 音频分片时长（毫秒）
    private const int AudioPacketMs = 20;

    // 每包采样帧数（48000Hz * 20ms = 960 帧）
    private const int FramesPerAudioPacket = 960;

    // 每包音频数据长度（960帧 * 2声道 * 2字节 = 3840 字节）
    private const int AudioPacketPayloadLength = FramesPerAudioPacket * 2 * 2;

    // 数据包魔数 "SA"
    private const byte Magic1 = 0x53;
    private const byte Magic2 = 0x41;

    // 协议版本（v2：新增会话 ID 与会话结束包）
    private const byte ProtocolVersion = 0x02;

    // 接收端心跳超时时间（超过该时间未收到 Hello 即视为掉线）
    private static readonly TimeSpan ReceiverTimeout = TimeSpan.FromSeconds(5);

    // 接收端超时扫描间隔
    private static readonly TimeSpan ReceiverSweepInterval = TimeSpan.FromSeconds(1);

    // 日志
    private readonly ILogger _logger;

    // 音频发送序号
    private long _audioSeq;

    // 套接字端点集合（按端口复用）
    private readonly List<SocketEndpoint> _endpoints = [];

    // 已连接的接收端列表（key: ip:port）
    private readonly Dictionary<string, ReceiverSession> _receivers = [];

    // 被发送端移除的接收端链路 ID（key: ip:port）
    // 用于区分“心跳重试”和“用户重新发起连接”：链路 ID 相同则继续拒绝，不同则允许重新接入
    private readonly Dictionary<string, uint> _removedLinks = [];

    // 当前发送端会话 ID（0 表示发送端未启动）
    private uint _senderSessionId;

    // 接收端当前认定的发送端会话 ID（0 表示尚未握手）
    private uint _activeSenderSession;

    // 接收端心跳超时扫描定时器
    private Timer? _receiverSweepTimer;

    // 操作锁
    private readonly object _lock = new();

    public UdpAudioTransportService(ILogger logger) {
        _logger = logger;
    }

    public event Action<IPEndPoint>? ReceiverConnected;

    public event Action<IPEndPoint>? ReceiverHeartbeat;

    public event Action<IPEndPoint>? ReceiverDisconnected;

    public event Action<uint>? SenderAcknowledged;

    public event Action<IPEndPoint, uint, SessionEndReason>? SenderSessionEnded;

    public event Action<uint, long, byte[]>? AudioDataReceived;

    public IReadOnlyList<IPEndPoint> Receivers {
        get {
            lock (_lock) {
                return _receivers.Values.Select(r => r.EndPoint).ToList();
            }
        }
    }

    public bool IsSenderRunning {
        get {
            lock (_lock) {
                return _endpoints.Any(e => e.IsSenderRole);
            }
        }
    }

    public bool IsReceiverRunning {
        get {
            lock (_lock) {
                return _endpoints.Any(e => e.IsReceiverRole);
            }
        }
    }

    public uint SenderSessionId {
        get {
            lock (_lock) {
                return _senderSessionId;
            }
        }
    }

    // ============ 发送端 ============

    public void StartSender(int listenPort) {
        var endpoint = GetOrCreateEndpoint(listenPort);
        lock (_lock) {
            if (endpoint.IsSenderRole) {
                return;
            }

            endpoint.IsSenderRole = true;
            _senderSessionId = CreateSessionId();
            _receivers.Clear();
        }

        // 定期清理心跳超时的接收端，避免继续向已掉线地址发送音频
        _receiverSweepTimer ??= new Timer(_ => SweepReceivers(), null, ReceiverSweepInterval,
            ReceiverSweepInterval);

        _logger.Information("发送端已启动，监听端口: {Port}，会话: {Session}", listenPort, _senderSessionId);
    }

    public void StopSender() {
        SocketEndpoint? endpoint;
        List<IPEndPoint> oldReceivers;
        uint session;
        lock (_lock) {
            endpoint = _endpoints.FirstOrDefault(e => e.IsSenderRole);
            if (endpoint == null) {
                return;
            }

            endpoint.IsSenderRole = false;
            oldReceivers = _receivers.Values.Select(r => r.EndPoint).ToList();
            _receivers.Clear();
            session = _senderSessionId;
            _senderSessionId = 0;
        }

        // 先通知对端结束会话，再释放套接字；停止共享时接收端会自动等待重连
        foreach (var receiver in oldReceivers) {
            SendSessionEndPacket(endpoint, receiver, session, SessionEndReason.Stopped);
        }

        lock (_lock) {
            if (!endpoint.IsReceiverRole) {
                DisposeEndpoint(endpoint);
            }
        }

        _receiverSweepTimer?.Dispose();
        _receiverSweepTimer = null;

        foreach (var receiver in oldReceivers) {
            ReceiverDisconnected?.Invoke(receiver);
        }

        _logger.Information("发送端已停止");
    }

    public void EndReceiverSession(IPEndPoint receiver) {
        var key = BuildKey(receiver);
        SocketEndpoint? endpoint;
        uint session;
        uint linkId;
        bool removed;
        lock (_lock) {
            removed = _receivers.TryGetValue(key, out var existing);
            if (removed) {
                linkId = existing!.LinkId;
                _receivers.Remove(key);
                // 记住被移除的链路，防止对方继续用心跳重试悄悄恢复连接
                RememberRemovedLink(key, linkId);
            } else {
                linkId = 0;
            }

            endpoint = _endpoints.FirstOrDefault(e => e.IsSenderRole);
            session = _senderSessionId;
        }

        if (!removed || endpoint == null) {
            return;
        }

        SendSessionEndPacket(endpoint, receiver, session, SessionEndReason.Removed);
        _logger.Information("已结束接收端 {Key} 的会话", key);
        ReceiverDisconnected?.Invoke(receiver);
    }

    public void EndAllReceiverSessions() {
        SocketEndpoint? endpoint;
        List<(IPEndPoint EndPoint, uint LinkId)> receivers;
        uint session;
        lock (_lock) {
            receivers = _receivers.Values.Select(r => (r.EndPoint, r.LinkId)).ToList();
            foreach (var receiver in receivers) {
                RememberRemovedLink(BuildKey(receiver.EndPoint), receiver.LinkId);
            }

            _receivers.Clear();
            endpoint = _endpoints.FirstOrDefault(e => e.IsSenderRole);
            session = _senderSessionId;
        }

        if (endpoint == null) {
            return;
        }

        foreach (var receiver in receivers) {
            SendSessionEndPacket(endpoint, receiver.EndPoint, session, SessionEndReason.Removed);
            ReceiverDisconnected?.Invoke(receiver.EndPoint);
        }
    }

    public void SendAudio(ReadOnlyMemory<byte> pcm16) {
        SocketEndpoint? sender;
        IPEndPoint[] targets;
        uint session;
        lock (_lock) {
            sender = _endpoints.FirstOrDefault(e => e.IsSenderRole);
            targets = _receivers.Values.Select(r => r.EndPoint).ToArray();
            session = _senderSessionId;
        }

        if (sender == null || targets.Length == 0 || pcm16.IsEmpty || session == 0) {
            return;
        }

        lock (_lock) {
            var offset = 0;
            while (offset < pcm16.Length) {
                var length = Math.Min(AudioPacketPayloadLength, pcm16.Length - offset);
                var seq = (uint)Interlocked.Increment(ref _audioSeq);
                var timestamp = Stopwatch.GetTimestamp();
                var packet = BuildPacket(AudioPacketType.Audio, session, seq, timestamp,
                    pcm16.Span.Slice(offset, length));
                SendToAll(sender, packet, targets);
                offset += length;
            }
        }
    }

    // ============ 接收端 ============

    public void StartReceiver(int listenPort) {
        var endpoint = GetOrCreateEndpoint(listenPort);
        lock (_lock) {
            endpoint.IsReceiverRole = true;
        }

        _logger.Information("接收端已启动，监听端口: {Port}", listenPort);
    }

    public void StopReceiver() {
        lock (_lock) {
            var endpoint = _endpoints.FirstOrDefault(e => e.IsReceiverRole);
            if (endpoint == null) {
                return;
            }

            endpoint.IsReceiverRole = false;
            _activeSenderSession = 0;
            if (!endpoint.IsSenderRole) {
                DisposeEndpoint(endpoint);
            }
        }

        _logger.Information("接收端已停止");
    }

    public void SendHello(IPEndPoint senderEndPoint, uint linkId) {
        // Hello 的 session 字段携带接收端链路 ID，发送端据此区分心跳重试与用户重新连接
        SendControl(AudioPacketType.Hello, senderEndPoint, linkId);
    }

    public void SendBye(IPEndPoint senderEndPoint) {
        SendControl(AudioPacketType.Bye, senderEndPoint, 0);
    }

    // ============ 内部实现 ============

    private void SendControl(AudioPacketType type, IPEndPoint target, uint session) {
        SocketEndpoint? receiver;
        lock (_lock) {
            receiver = _endpoints.FirstOrDefault(e => e.IsReceiverRole);
        }

        if (receiver == null) {
            return;
        }

        lock (_lock) {
            try {
                var packet = BuildPacket(type, session, 0, 0, ReadOnlySpan<byte>.Empty);
                _ = receiver.Udp.SendAsync(packet, packet.Length, target);
            } catch (Exception e) {
                _logger.Warning(e, "发送控制包 {Type} 到 {Target} 失败", type, target);
            }
        }
    }

    private SocketEndpoint GetOrCreateEndpoint(int port) {
        lock (_lock) {
            var existing = _endpoints.FirstOrDefault(e => e.Port == port);
            if (existing != null) {
                return existing;
            }

            var endpoint = new SocketEndpoint(port, new UdpClient(new IPEndPoint(IPAddress.Any, port)));
            _endpoints.Add(endpoint);
            _ = Task.Run(() => ReceiveLoopAsync(endpoint));
            return endpoint;
        }
    }

    private void DisposeEndpoint(SocketEndpoint endpoint) {
        endpoint.CancellationTokenSource.Cancel();
        _endpoints.Remove(endpoint);
        try {
            endpoint.Udp.Dispose();
        } catch (Exception e) {
            _logger.Warning(e, "关闭 UDP 套接字失败");
        }

        endpoint.CancellationTokenSource.Dispose();
    }

    private void SendToAll(SocketEndpoint sender, byte[] packet, IPEndPoint[] targets) {
        foreach (var target in targets) {
            try {
                _ = sender.Udp.SendAsync(packet, packet.Length, target);
            } catch (Exception e) {
                _logger.Warning(e, "发送数据到 {Target} 失败", target);
            }
        }
    }

    // 接收循环
    private async Task ReceiveLoopAsync(SocketEndpoint endpoint) {
        var token = endpoint.CancellationTokenSource.Token;
        while (!token.IsCancellationRequested) {
            try {
                var result = await endpoint.Udp.ReceiveAsync(token);
                HandlePacket(endpoint, result.RemoteEndPoint, result.Buffer);
            } catch (OperationCanceledException) {
                break;
            } catch (ObjectDisposedException) {
                break;
            } catch (SocketException) {
                break;
            } catch (Exception e) {
                _logger.Warning(e, "UDP 接收循环发生错误");
                try {
                    await Task.Delay(50, token);
                } catch (OperationCanceledException) {
                    break;
                }
            }
        }
    }

    private void HandlePacket(SocketEndpoint endpoint, IPEndPoint remote, byte[] buffer) {
        if (buffer.Length < PacketHeaderLength ||
            buffer[0] != Magic1 ||
            buffer[1] != Magic2 ||
            buffer[2] != ProtocolVersion) {
            return;
        }

        var session = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(4, 4));

        switch ((AudioPacketType)buffer[3]) {
            case AudioPacketType.Hello:
                if (endpoint.IsSenderRole) {
                    HandleHello(remote, session);
                }

                break;

            case AudioPacketType.Bye:
                if (endpoint.IsSenderRole) {
                    HandleBye(remote);
                }

                break;

            case AudioPacketType.Ack:
                if (endpoint.IsReceiverRole) {
                    HandleAck(session);
                }

                break;

            case AudioPacketType.SessionEnd:
                if (endpoint.IsReceiverRole) {
                    var reason = buffer.Length > PacketHeaderLength
                        ? (SessionEndReason)buffer[PacketHeaderLength]
                        : SessionEndReason.Stopped;
                    HandleSessionEnd(remote, session, reason);
                }

                break;

            case AudioPacketType.Audio:
                if (!endpoint.IsReceiverRole || !IsSessionAccepted(session)) {
                    break;
                }

                var seq = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(8, 4));
                var timestamp = BinaryPrimitives.ReadInt64BigEndian(buffer.AsSpan(12, 8));
                var payload = new byte[buffer.Length - PacketHeaderLength];
                Buffer.BlockCopy(buffer, PacketHeaderLength, payload, 0, payload.Length);
                AudioDataReceived?.Invoke(seq, timestamp, payload);
                break;
        }
    }

    // 接收端只接受当前会话的音频（发送端重启后会话 ID 会变化）
    private bool IsSessionAccepted(uint session) {
        lock (_lock) {
            if (_activeSenderSession == 0) {
                _activeSenderSession = session;
                return true;
            }

            return _activeSenderSession == session;
        }
    }

    private void HandleHello(IPEndPoint remote, uint linkId) {
        var key = BuildKey(remote);
        bool blocked;
        bool isNew = false;
        SocketEndpoint? sender = null;
        uint session = 0;
        lock (_lock) {
            // 被移除的接收端继续用相同链路 ID 重试时拒绝；用户重新连接会生成新的链路 ID
            blocked = _removedLinks.TryGetValue(key, out var removedLink) && removedLink == linkId;
            if (blocked) {
                sender = _endpoints.FirstOrDefault(e => e.IsSenderRole);
                session = _senderSessionId;
            } else {
                _removedLinks.Remove(key);
                if (_receivers.TryGetValue(key, out var existing)) {
                    existing.LastSeenTimestamp = Stopwatch.GetTimestamp();
                    existing.LinkId = linkId;
                } else {
                    _receivers[key] = new ReceiverSession(remote, linkId);
                    isNew = true;
                }
            }
        }

        if (blocked) {
            _logger.Information("接收端 {Key} 已被移除，拒绝链路 {LinkId} 的重试", key, linkId);
            if (sender != null) {
                SendSessionEndPacket(sender, remote, session, SessionEndReason.Removed);
            }

            return;
        }

        if (isNew) {
            _logger.Information("接收端 {Key} 请求连接", key);
            ReceiverConnected?.Invoke(remote);
        }

        ReceiverHeartbeat?.Invoke(remote);
        ReplyAck(remote);
    }

    private void HandleBye(IPEndPoint remote) {
        var key = BuildKey(remote);
        bool removed;
        lock (_lock) {
            removed = _receivers.Remove(key);
        }

        if (removed) {
            _logger.Information("接收端 {Key} 断开连接", key);
            ReceiverDisconnected?.Invoke(remote);
        }
    }

    private void HandleAck(uint session) {
        bool changed;
        lock (_lock) {
            changed = _activeSenderSession != session;
            _activeSenderSession = session;
        }

        if (changed) {
            _logger.Information("发送端会话已确认: {Session}", session);
        }

        SenderAcknowledged?.Invoke(session);
    }

    private void HandleSessionEnd(IPEndPoint remote, uint session, SessionEndReason reason) {
        bool ignored;
        lock (_lock) {
            // 旧会话的结束通知直接忽略（发送端已重启并建立新会话）
            ignored = session != 0 && _activeSenderSession != 0 && session != _activeSenderSession;
            if (!ignored) {
                _activeSenderSession = 0;
            }
        }

        if (ignored) {
            return;
        }

        _logger.Information("发送端 {Remote} 结束会话 {Session}，原因 {Reason}", remote, session, reason);
        SenderSessionEnded?.Invoke(remote, session, reason);
    }

    // 回复连接确认（携带发送端会话 ID，接收端据此判定连接成功）
    private void ReplyAck(IPEndPoint remote) {
        SocketEndpoint? sender;
        uint session;
        lock (_lock) {
            sender = _endpoints.FirstOrDefault(e => e.IsSenderRole);
            session = _senderSessionId;
        }

        if (sender == null || session == 0) {
            return;
        }

        try {
            var packet = BuildPacket(AudioPacketType.Ack, session, 0, 0, ReadOnlySpan<byte>.Empty);
            _ = sender.Udp.SendAsync(packet, packet.Length, remote);
        } catch (Exception e) {
            _logger.Warning(e, "回复连接确认到 {Remote} 失败", remote);
        }
    }

    // 通知接收端会话已结束；连发 3 份降低 UDP 丢包导致对端收不到的概率
    private void SendSessionEndPacket(SocketEndpoint sender, IPEndPoint target, uint session,
        SessionEndReason reason) {
        if (session == 0) {
            return;
        }

        try {
            Span<byte> reasonPayload = stackalloc byte[1];
            reasonPayload[0] = (byte)reason;
            var packet = BuildPacket(AudioPacketType.SessionEnd, session, 0, 0, reasonPayload);
            for (var i = 0; i < 3; i++) {
                _ = sender.Udp.SendAsync(packet, packet.Length, target);
            }
        } catch (Exception e) {
            _logger.Warning(e, "发送会话结束通知到 {Target} 失败", target);
        }
    }

    // 记录被移除的接收端链路（容量有限，避免长期运行时无限增长）
    private void RememberRemovedLink(string key, uint linkId) {
        if (_removedLinks.Count >= 128) {
            _removedLinks.Clear();
        }

        _removedLinks[key] = linkId;
    }

    // 清理心跳超时的接收端
    private void SweepReceivers() {
        List<IPEndPoint> expired = [];
        lock (_lock) {
            var now = Stopwatch.GetTimestamp();
            foreach (var key in _receivers.Keys.ToList()) {
                var receiver = _receivers[key];
                if (Stopwatch.GetElapsedTime(receiver.LastSeenTimestamp, now) <= ReceiverTimeout) {
                    continue;
                }

                _receivers.Remove(key);
                expired.Add(receiver.EndPoint);
            }
        }

        foreach (var receiver in expired) {
            _logger.Information("接收端 {Receiver} 心跳超时，已移除", receiver);
            ReceiverDisconnected?.Invoke(receiver);
        }
    }

    private static string BuildKey(IPEndPoint endpoint) {
        return $"{endpoint.Address}:{endpoint.Port}";
    }

    private static uint CreateSessionId() {
        uint sessionId;
        do {
            sessionId = (uint)Random.Shared.NextInt64(1, uint.MaxValue);
        } while (sessionId == 0);

        return sessionId;
    }

    // 构建普通数据包（音频 / 控制包）
    private static byte[] BuildPacket(AudioPacketType type, uint session, uint seq, long timestamp,
        ReadOnlySpan<byte> payload) {
        var packet = new byte[PacketHeaderLength + payload.Length];
        packet[0] = Magic1;
        packet[1] = Magic2;
        packet[2] = ProtocolVersion;
        packet[3] = (byte)type;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(4, 4), session);
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(8, 4), seq);
        BinaryPrimitives.WriteInt64BigEndian(packet.AsSpan(12, 8), timestamp);
        payload.CopyTo(packet.AsSpan(PacketHeaderLength));
        return packet;
    }

    public void Dispose() {
        lock (_lock) {
            _receiverSweepTimer?.Dispose();
            _receiverSweepTimer = null;
            foreach (var endpoint in _endpoints.ToList()) {
                DisposeEndpoint(endpoint);
            }
        }
    }

    // UDP 套接字端点，按监听端口复用，可同时承担发送端与接收端角色
    private sealed class SocketEndpoint(int port, UdpClient udp) {
        public int Port { get; } = port;

        public UdpClient Udp { get; } = udp;

        public bool IsSenderRole { get; set; }

        public bool IsReceiverRole { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; } = new();
    }

    // 接收端会话（含最近一次心跳时间）
    private sealed class ReceiverSession(IPEndPoint endPoint, uint linkId) {
        public IPEndPoint EndPoint { get; } = endPoint;

        public uint LinkId { get; set; } = linkId;

        public long LastSeenTimestamp { get; set; } = Stopwatch.GetTimestamp();
    }
}
