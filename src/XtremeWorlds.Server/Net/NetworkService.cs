namespace XtremeWorlds.Server.Net;

public abstract class NetworkService<TSession> : INetworkService<TSession>
{
    /// <inheritdoc />
    public virtual Task OnConnectedAsync(TSession session, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task OnDisconnectedAsync(TSession session, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task OnBytesReceivedAsync(TSession session, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}