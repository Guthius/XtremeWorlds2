using System.IO.Compression;
using System.Runtime.InteropServices;
using Core.Net;

namespace XtremeWorlds.Server.Net;

public abstract class PacketParser<TPacketId, TSession> where TPacketId : Enum
{
    private const uint CompressionFlag = 1u << 31;

    private readonly Dictionary<int, Func<TSession, ReadOnlyMemory<byte>, ValueTask>> _handlers = [];

    protected void Bind(TPacketId packetId, Func<TSession, ReadOnlyMemory<byte>, ValueTask> handler)
    {
        _handlers[Convert.ToInt32(packetId)] = handler;
    }

    protected void Bind(TPacketId packetId, Action<TSession, ReadOnlyMemory<byte>> handler)
    {
        Bind(packetId, (session, bytes) =>
        {
            handler(session, bytes);

            return ValueTask.CompletedTask;
        });
    }

    protected void Bind<TPacket>(TPacketId packetId, Action<TSession, TPacket> handler) where TPacket : IPacket<TPacket>
    {
        Bind(packetId, (session, bytes) =>
        {
            var packetReader = new PacketReader(bytes);
            var packet = TPacket.Deserialize(packetReader);

            handler(session, packet);

            return ValueTask.CompletedTask;
        });
    }

    protected void Bind<TPacket>(TPacketId packetId, Func<TSession, TPacket, ValueTask> handler) where TPacket : IPacket<TPacket>
    {
        Bind(packetId, (session, bytes) =>
        {
            var packetReader = new PacketReader(bytes);
            var packet = TPacket.Deserialize(packetReader);

            return handler(session, packet);
        });
    }

    public async Task<int> ParseAsync(TSession session, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        var totalNumberOfBytes = bytes.Length;

        while (bytes.Length >= 4)
        {
            var packetSize = BitConverter.ToInt32(bytes.Span);
            if (packetSize > bytes.Length - 4)
            {
                break;
            }

            bytes = bytes[4..];
            if (packetSize == 0)
            {
                continue;
            }

            await Handle(session, bytes[..packetSize]);

            bytes = bytes[packetSize..];
        }

        var bytesLeft = bytes.Length;
        var bytesProcessed = totalNumberOfBytes - bytesLeft;

        return bytesProcessed;
    }

    private ValueTask Handle(TSession session, ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return ValueTask.CompletedTask;
        }

        var packetId = BitConverter.ToInt32(bytes.Span);
        var packetData = bytes[4..];

        var compressed = IsCompressed(packetId);
        if (compressed)
        {
            packetId = (int) (packetId & ~CompressionFlag);
        }

        if (!Enum.IsDefined(typeof(TPacketId), packetId) || !_handlers.TryGetValue(packetId, out var handler))
        {
            return ValueTask.CompletedTask;
        }

        if (compressed)
        {
            return HandleCompressed(session, packetData, handler);
        }

        return handler(session, packetData);
    }

    private static ValueTask HandleCompressed(TSession session, ReadOnlyMemory<byte> bytes, Func<TSession, ReadOnlyMemory<byte>, ValueTask> handler)
    {
        if (bytes.Length < 4)
        {
            return ValueTask.CompletedTask;
        }

        var decompressedSize = BitConverter.ToInt32(bytes.Span);
        if (decompressedSize == 0)
        {
            return ValueTask.CompletedTask;
        }

        var buffer = new byte[decompressedSize];
        if (!Decompress(bytes[4..], buffer))
        {
            return ValueTask.CompletedTask;
        }

        return handler(session, buffer.AsMemory());
    }

    private static bool IsCompressed(int packetId)
    {
        return (packetId & CompressionFlag) == CompressionFlag;
    }

    public static bool Decompress(ReadOnlyMemory<byte> src, byte[] dest)
    {
        if (!MemoryMarshal.TryGetArray(src, out var segment) || segment.Array is null)
        {
            return false;
        }

        using var memoryStream = new MemoryStream(segment.Array, segment.Offset, segment.Count);
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);

        int bytesRead, totalBytesRead = 0;
        while ((bytesRead = gzipStream.Read(dest, totalBytesRead, dest.Length - totalBytesRead)) > 0)
        {
            totalBytesRead += bytesRead;
        }

        return totalBytesRead == dest.Length;
    }
}