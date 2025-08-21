namespace XtremeWorlds.Server.Net;

internal interface INetworkChannelProxy
{
    Task OnConnectedAsync(INetworkChannel channel, CancellationToken cancellationToken);
    Task OnDisconnectedAsync(INetworkChannel channel, CancellationToken cancellationToken);
    Task OnBytesReceivedAsync(INetworkChannel channel, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);
}