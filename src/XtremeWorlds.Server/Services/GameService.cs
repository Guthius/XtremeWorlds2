using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XtremeWorlds.Server.Game;
using Player = XtremeWorlds.Server.Game.Objects.Player;

namespace XtremeWorlds.Server.Services;

public sealed class GameService(
    ILogger<GameService> logger,
    IConfiguration configuration, 
    IPlayerService playerService, 
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        General.Logger = logger;
        
        Game.Database.ConnectionString = configuration.GetValue<string>("Database:ConnectionString") ?? throw new InvalidOperationException("Database connection string not found in configuration");
        
        try
        {
            await General.ServerStartAsync(configuration);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Critical server error, initiating emergency shutdown");
            
            await General.SendServerAnnouncementAsync("Server shutting down due to critical error.");
            await General.DestroyServerAsync();
        }
        
        foreach (var player in playerService.Players)
        {
            await Player.LeftGame(player.Id);
        }
        
        logger.LogInformation("Game service has stopped");
        
        lifetime.StopApplication();
    }
}