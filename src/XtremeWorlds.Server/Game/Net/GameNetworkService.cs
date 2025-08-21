using System.Security.Cryptography;
using XtremeWorlds.Server.Game.Net.Protocol;
using XtremeWorlds.Server.Net;

namespace XtremeWorlds.Server.Game.Net;

public sealed class GameNetworkService : NetworkService<GameSession>
{
    public override Task OnConnectedAsync(GameSession session, CancellationToken cancellationToken)
    {
        session.Aes = Aes.Create();
        session.Channel.Send(new AesPacket(session.Aes.Key, session.Aes.IV));

        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(GameSession session, CancellationToken cancellationToken)
    {
        await Objects.Player.LeftGame(session.Id);
    }

    public override Task OnBytesReceivedAsync(GameSession session, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        return session.ParseAsync(bytes, cancellationToken);
    }
}