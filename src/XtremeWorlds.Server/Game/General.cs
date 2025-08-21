using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Core;
using Core.Common;
using Core.Configurations;
using Core.Globals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using XtremeWorlds.Server.Game.Events;
using XtremeWorlds.Server.Game.Network;
using static Core.Globals.Type;
using static Core.Globals.Command;
using Animation = XtremeWorlds.Server.Game.Objects.Animation;
using Event = XtremeWorlds.Server.Game.Events.Event;
using Item = XtremeWorlds.Server.Game.Objects.Item;
using Moral = XtremeWorlds.Server.Game.Objects.Moral;
using Npc = XtremeWorlds.Server.Game.Objects.Npc;
using Projectile = XtremeWorlds.Server.Game.Objects.Projectile;
using Resource = XtremeWorlds.Server.Game.Objects.Resource;
using Task = System.Threading.Tasks.Task;
using Type = Core.Globals.Type;

namespace XtremeWorlds.Server.Game;

public class General
{
    private static readonly Stopwatch MyStopwatch = new();
    public static ILogger Logger;
    private static readonly Lock SyncLock = new();
    private static readonly CancellationTokenSource Cts = new();
    private static Timer? _saveTimer;
    private static int _shutDownLastTimer;

    /// <summary>
    /// Retrieves the shutdown timer for server destruction.
    /// </summary>
    public static Stopwatch GetShutDownTimer { get; } = new();

    /// <summary>
    /// Gets the current server destruction status.
    /// </summary>
    public static bool IsServerDestroyed { get; private set; }

    /// <summary>
    /// Retrieves the random number generator utility.
    /// </summary>
    public static RandomUtility GetRandom { get; } = new();

    /// <summary>
    /// Gets the elapsed time in milliseconds since the server started.
    /// </summary>
    public static int GetTimeMs() => (int) MyStopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Validates a username based on length and allowed characters.
    /// </summary>
    public static int IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return -1;

        if (username.Length < Core.Globals.Constant.MinNameLength || username.Length > Core.Globals.Constant.NameLength)
            return 0;

        return Regex.IsMatch(username, @"^[a-zA-Z0-9_ ]+$") ? 1 : -1;
    }

    public static async Task ServerStartAsync(IConfiguration configuration)
    {
        MyStopwatch.Start();

        await InitializeCoreComponentsAsync(configuration);
        await LoadGameDataAsync();
        await StartGameLoopAsync();
    }

    private static async Task InitializeCoreComponentsAsync(IConfiguration configuration)
    {
        await Task.Run(() =>
        {
            ValidateConfiguration();

            Clock.Instance.GameSpeed = SettingsManager.Instance.TimeSpeed;
        });

        await InitializeDatabaseWithRetryAsync(configuration);
    }

    public static void InitalizeCoreData()
    {
        Data.Job = new Job[Core.Globals.Constant.MaxJobs];
        Data.Moral = new Type.Moral[Core.Globals.Constant.MaxMorals];
        Data.Map = new Map[Core.Globals.Constant.MaxMaps];
        Data.Item = new Type.Item[Core.Globals.Constant.MaxItems];
        Data.Npc = new Type.Npc[Core.Globals.Constant.MaxNpcs];
        Data.Resource = new Type.Resource[Core.Globals.Constant.MaxResources];
        Data.Projectile = new Type.Projectile[Core.Globals.Constant.MaxProjectiles];
        Data.Animation = new Type.Animation[Core.Globals.Constant.MaxAnimations];
        Data.Shop = new Shop[Core.Globals.Constant.MaxShops];
        Data.Player = new Type.Player[Core.Globals.Constant.MaxPlayers];
        Data.Party = new Type.Party[Core.Globals.Constant.MaxParty];
        Data.MapItem = new MapItem[Core.Globals.Constant.MaxMaps, Core.Globals.Constant.MaxMapItems];
        Data.Npc = new Type.Npc[Core.Globals.Constant.MaxNpcs];
        Data.MapNpc = new MapData[Core.Globals.Constant.MaxMaps];

        for (var i = 0; i < Core.Globals.Constant.MaxMaps; i++)
        {
            Data.MapNpc[i].Npc = new MapNpc[Core.Globals.Constant.MaxMapNpcs];
            for (var x = 0; x < Core.Globals.Constant.MaxMapNpcs; x++)
            {
                Data.MapNpc[i].Npc[x].Vital = new int[Enum.GetValues(typeof(Vital)).Length];
                Data.MapNpc[i].Npc[x].SkillCd = new int[Core.Globals.Constant.MaxNpcSkills];
                Data.MapNpc[i].Npc[x].Num = -1;
                Data.MapNpc[i].Npc[x].SkillBuffer = -1;
            }

            var statCount = Enum.GetNames(typeof(Stat)).Length;
            for (var x = 0; x < Core.Globals.Constant.MaxItems; x++)
            {
                Data.Item[x].AddStat = new byte[statCount];
                Data.Item[x].StatReq = new byte[statCount];
            }

            for (var x = 0; x < Core.Globals.Constant.MaxMapItems; x++)
            {
                Data.MapItem[i, x].Num = -1;
            }
        }

        Data.Shop = new Shop[Core.Globals.Constant.MaxShops];
        Data.Skill = new Skill[Core.Globals.Constant.MaxSkills];
        Data.MapResource = new MapResource[Core.Globals.Constant.MaxMaps];
        Data.TempPlayer = new TempPlayer[Core.Globals.Constant.MaxPlayers];
        Data.Account = new Account[Core.Globals.Constant.MaxPlayers];

        for (var i = 0; i < Core.Globals.Constant.MaxPlayers; i++)
        {
            Database.ClearPlayer(i);
        }

        for (var i = 0; i < Core.Globals.Constant.MaxPartyMembers; i++)
        {
            Party.ClearParty(i);
        }

        Event.TempEventMap = new GlobalEvents[Core.Globals.Constant.MaxMaps];
        Data.MapProjectile = new MapProjectile[Core.Globals.Constant.MaxMaps, Core.Globals.Constant.MaxProjectiles];
    }

    private static async Task LoadGameDataAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        InitalizeCoreData();

        await LoadGameContentAsync(Cts.Token);
        await SpawnGameObjectsAsync();

        stopwatch.Stop();

        Logger.LogInformation("Game content loaded in {ElapsedSeconds}s", stopwatch.ElapsedMilliseconds / 1000f);
    }

    private static async Task StartGameLoopAsync()
    {
        InitializeSaveTimer();

        await Loop.ServerAsync();
    }

    /// <summary>
    /// Shuts down the server gracefully, cleaning up all resources.
    /// </summary>
    public static async Task DestroyServerAsync()
    {
        if (IsServerDestroyed) return;
        IsServerDestroyed = true;
        Cts.Cancel();
        _saveTimer?.Dispose();

        Logger.LogInformation("Server shutdown initiated...");

        await Database.SaveAllPlayersOnlineAsync();

        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Constant.MaxPlayers), Cts.Token, async (i, _) =>
            {
                NetworkSend.SendLeftGame(i);

                await Objects.Player.LeftGame(i);
            });
        }
        catch (TaskCanceledException)
        {
            Logger.LogWarning("Server shutdown tasks were canceled.");
        }

        Logger.LogInformation("Server shutdown completed.");
        Environment.Exit(0);
    }

    private static void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(SettingsManager.Instance.GameName))
            throw new InvalidOperationException("GameName is not set in configuration");

        if (SettingsManager.Instance.Port <= 0 || SettingsManager.Instance.Port > 65535)
            throw new InvalidOperationException("Invalid Port number in configuration");
    }

    private static async Task InitializeDatabaseWithRetryAsync(IConfiguration configuration)
    {
        var maxRetries = configuration.GetValue("Database:MaxRetries", 3);
        var retryDelayMs = configuration.GetValue("Database:RetryDelayMs", 1000);
        Logger.LogInformation("Initializing database...");
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var name = GetDatabaseName(configuration);
                await Database.CreateDatabaseAsync(name);
                await Database.CreateTablesAsync();
                await LoadCharacterListAsync();
                
                Logger.LogInformation("Database initialized successfully");
                
                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    Logger.LogCritical(ex, "Failed to initialize database after multiple attempts");
                    throw;
                }

                Logger.LogWarning(ex, $"Database initialization failed, attempt {attempt} of {maxRetries}");
                await Task.Delay(retryDelayMs * attempt, Cts.Token);
            }
        }
    }

    private static async Task LoadCharacterListAsync()
    {
        var ids = await Database.GetDataAsync("account");
        Data.Char = [];
        const int maxConcurrency = 4;

        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = ids.Select(async id =>
        {
            await semaphore.WaitAsync(Cts.Token);
            try
            {
                for (var i = 0; i < Core.Globals.Constant.MaxChars; i++)
                {
                    var data = await Database.SelectRowByColumnAsync("id", id, "account", $"character{i + 1}");
                    if (data?["Name"] is not null)
                    {
                        var name = data["Name"].ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            Data.Char.Add(name);
                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        Logger.LogInformation($"Loaded {Data.Char.Count} character(s).");
    }

    private static async Task LoadGameContentAsync(CancellationToken cancellationToken)
    {
        const int maxConcurrency = 4;

        using var semaphore = new SemaphoreSlim(maxConcurrency);

        await Task.WhenAll(
            LoadContentAsync(semaphore, "Jobs", Database.LoadJobsAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Morals", Moral.LoadMoralsAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Maps", Database.LoadMapsAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Items", Item.LoadItemsAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Npcs", Database.LoadNpcsAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Resources", Resource.LoadResourcesAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Shops", Database.LoadShopsAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Skills", Database.LoadSkillsAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Animations", Animation.LoadAnimationsAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Switches", Event.LoadSwitchesAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Variables", Event.LoadVariablesAsync(), cancellationToken),
            LoadContentAsync(semaphore, "Projectiles", Projectile.LoadProjectilesAsync(), cancellationToken));
    }

    private static async Task LoadContentAsync(SemaphoreSlim semaphore, string contentType, Task loadTask, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await loadTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load {ContentType}", contentType);
        }
        finally
        {
            stopwatch.Stop();

            Logger.LogDebug("Loaded {ContentType} in {ElapsedMilliseconds}ms", contentType, stopwatch.ElapsedMilliseconds / 1000f);

            semaphore.Release();
        }
    }

    private static async Task SpawnGameObjectsAsync()
    {
        await Task.WhenAll(
            Task.Run(Item.SpawnAllMapsItems),
            Npc.SpawnAllMapNpcs(),
            EventLogic.SpawnAllMapGlobalEvents()
        );
        Logger.LogInformation("Game objects spawned.");
    }

    /// <summary>
    /// Counts the number of players currently online.
    /// </summary>
    public static int CountPlayersOnline()
    {
        lock (SyncLock)
        {
            return PlayerService.Instance.PlayerIds.Count(NetworkConfig.IsPlaying);
        }
    }

    private static void InitializeSaveTimer()
    {
        var intervalMinutes = SettingsManager.Instance.SaveInterval;
        _saveTimer = new Timer(async _ => await SavePlayersPeriodicallyAsync(), null,
            TimeSpan.FromMinutes(intervalMinutes), TimeSpan.FromMinutes(intervalMinutes));
    }

    private static async Task SavePlayersPeriodicallyAsync()
    {
        try
        {
            await Database.SaveAllPlayersOnlineAsync();

            Logger.LogInformation("Periodic player save completed.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Periodic player save failed");
        }
    }

    public static async Task SendServerAnnouncementAsync(string message)
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Constant.MaxPlayers), Cts.Token, (i, _) =>
        {
            if (NetworkConfig.IsPlaying(i))
            {
                NetworkSend.PlayerMsg(i, message, (int) ColorName.Yellow);
            }

            return ValueTask.CompletedTask;
        });

        Logger.LogInformation("Server announcement sent.");
    }

    /// <summary>
    /// Handles player commands with expanded functionality.
    /// </summary>
    public static async Task HandlePlayerCommandAsync(string[] command)
    {
        // Defensive: command[1] may not exist for some commands
        var playerIndex = -1;
        if (command.Length > 1)
        {
            playerIndex = GameLogic.FindPlayer(command[1]);
        }

        if (command is ["/help"])
        {
            await SendHelpMessageAsync();
        }

        if (playerIndex == -1 || !NetworkConfig.IsPlaying(playerIndex))
        {
            return;
        }

        switch (command[0].ToLower())
        {
            case "/teleport":
                if (int.TryParse(command[2], out var x) && int.TryParse(command[3], out var y))
                    await TeleportPlayerAsync(playerIndex, x, y);
                break;

            case "/kick":
                await KickPlayerAsync(playerIndex);
                break;

            case "/broadcast":
                await BroadcastMessageAsync(playerIndex, string.Join(" ", command[2..]));
                break;
            
            case "/whisper":
                await SendWhisperAsync(playerIndex, "Server", string.Join(" ", command[2..]));
                break;
            
            case "/save":
                await SavePlayerDataAsync(playerIndex);
                break;

            case "/shutdown":
            {
                if (GetShutDownTimer != null && GetShutDownTimer.IsRunning)
                {
                    GetShutDownTimer.Stop();
                    Console.WriteLine("Server shutdown has been cancelled!");
                    NetworkSend.GlobalMsg("Server shutdown has been cancelled!");
                }
                else
                {
                    if (GetShutDownTimer != null && GetShutDownTimer.ElapsedTicks > 0L)
                    {
                        GetShutDownTimer.Restart();
                    }
                    else
                    {
                        GetShutDownTimer?.Start();
                    }

                    Console.WriteLine("Server shutdown in " + SettingsManager.Instance.ServerShutdown + " seconds!");
                    NetworkSend.GlobalMsg("Server shutdown in " + SettingsManager.Instance.ServerShutdown + " seconds!");
                }

                break;
            }

            case "/exit":
            {
                await DestroyServerAsync();
                break;
            }

            case "/access":
            {
                if (!byte.TryParse(command[2], out var access))
                {
                    Console.WriteLine("Invalid access level.");
                    break;
                }

                // SetPlayerAccess implementation stub
                void SetPlayerAccess(int idx, byte lvl)
                {
                    Data.Player[idx].Access = lvl;
                }

                switch (access)
                {
                    case (byte) AccessLevel.Player:
                        SetPlayerAccess(playerIndex, access);
                        NetworkSend.SendPlayerData(playerIndex);
                        NetworkSend.PlayerMsg(playerIndex, "Your access has been set to Player!", (int) ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(playerIndex));
                        break;
                    case (byte) AccessLevel.Moderator:
                        SetPlayerAccess(playerIndex, access);
                        NetworkSend.SendPlayerData(playerIndex);
                        NetworkSend.PlayerMsg(playerIndex, "Your access has been set to Moderator!", (int) ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(playerIndex));
                        break;
                    case (byte) AccessLevel.Mapper:
                        SetPlayerAccess(playerIndex, access);
                        NetworkSend.SendPlayerData(playerIndex);
                        NetworkSend.PlayerMsg(playerIndex, "Your access has been set to Mapper!", (int) ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(playerIndex));
                        break;
                    case (byte) AccessLevel.Developer:
                        SetPlayerAccess(playerIndex, access);
                        NetworkSend.SendPlayerData(playerIndex);
                        NetworkSend.PlayerMsg(playerIndex, "Your access has been set to Developer!", (int) ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(playerIndex));
                        break;
                    case (byte) AccessLevel.Owner:
                        SetPlayerAccess(playerIndex, access);
                        NetworkSend.SendPlayerData(playerIndex);
                        NetworkSend.PlayerMsg(playerIndex, "Your access has been set to Owner!", (int) ColorName.Yellow);
                        Console.WriteLine("Successfully set the access level to " + access + " for player " + GetPlayerName(playerIndex));
                        break;
                    default:
                        Console.WriteLine("Failed to set the access level to " + access + " for player " + GetPlayerName(playerIndex));
                        break;
                }

                break;
            }

            case "/ban":
            {
                Data.Account[playerIndex].Banned = true;
                var task = Objects.Player.LeftGame(playerIndex);
                task.Wait();
                Console.WriteLine($"Player {GetPlayerName(playerIndex)} has been banned by the server.");

                break;
            }

            case "/timespeed":
            {
                if (!double.TryParse(command[1], out var speed))
                {
                    Console.WriteLine("Invalid speed value.");
                    break;
                }

                Clock.Instance.GameSpeed = speed;
                SettingsManager.Instance.TimeSpeed = speed;
                SettingsManager.Save();
                Console.WriteLine("Set GameSpeed to " + Clock.Instance.GameSpeed + " secs per seconds");
                break;
            }

            default:
                Console.WriteLine("Unknown command. Use /help for assistance.", (int) ColorName.BrightRed);
                break;
        }
    }

    private static async Task TeleportPlayerAsync(int playerIndex, int x, int y)
    {
        try
        {
            ref var player = ref Data.Player[playerIndex];

            if (x < 0 || x >= Data.Map[player.Map].MaxX || y < 0 || y >= Data.Map[player.Map].MaxY)
            {
                NetworkSend.PlayerMsg(playerIndex, "Invalid coordinates for teleportation.", (int) ColorName.BrightRed);
                return;
            }

            player.X = x;
            player.Y = y;
            NetworkSend.SendPlayerXyToMap(playerIndex);
            Logger.LogInformation($"Player {playerIndex} teleported to ({x}, {y})");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to teleport player {playerIndex}");
            NetworkSend.PlayerMsg(playerIndex, "Teleport failed.", (int) ColorName.BrightRed);
        }
    }

    private static async Task KickPlayerAsync(int playerIndex)
    {
        try
        {
            if (NetworkConfig.IsPlaying(playerIndex))
            {
                NetworkSend.SendLeftGame(playerIndex);
                await Objects.Player.LeftGame(playerIndex);
                Logger.LogInformation($"Player {playerIndex} kicked by server!");
                NetworkSend.PlayerMsg(playerIndex, $"Player {playerIndex} has been kicked.", (int) ColorName.BrightGreen);
            }
            else
            {
                NetworkSend.PlayerMsg(playerIndex, "Target player is not online.", (int) ColorName.BrightRed);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to kick player {playerIndex}");
            NetworkSend.PlayerMsg(playerIndex, "Kick operation failed.", (int) ColorName.BrightRed);
        }
    }

    private static async Task BroadcastMessageAsync(int playerIndex, string message)
    {
        try
        {
            if (!await IsAdminAsync(playerIndex))
            {
                NetworkSend.PlayerMsg(playerIndex, "You are not authorized to broadcast.", (int) ColorName.BrightRed);
                return;
            }

            await SendChatMessageAsync(playerIndex, "global", $"[Broadcast] {message}", ColorName.BrightGreen);
            Logger.LogInformation($"Broadcast by {playerIndex}: {message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Broadcast failed");
            NetworkSend.PlayerMsg(playerIndex, "Broadcast failed.", (int) ColorName.BrightRed);
        }
    }

    private static async Task SendHelpMessageAsync()
    {
        var help = "Available Commands:\n" +
                   "/teleport <x> <y> - Teleport to coordinates\n" +
                   "/kick <player> - Kick a player (admin only)\n" +
                   "/broadcast <message> - Send a message to all players (admin only)\n" +
                   "/status - View server status\n" +
                   "/whisper <player> <message> - Send a private message\n" +
                   "/exit - Shutdown the server\n" +
                   "/ban <player> - Ban a player\n" +
                   "/shutdown - Initiate server shutdown\n" +
                   "/stats - View player statistics\n" +
                   "/access <player> <level> - Set player access level (1-5)\n" +
                   "/save - Manually save player data\n" +
                   "/help - Show this message";
        Console.WriteLine(help);
    }

    private static async Task SendWhisperAsync(int senderIndex, string targetName, string message)
    {
        try
        {
            var targetIndex = GameLogic.FindPlayer(targetName);
            if (targetIndex == -1)
            {
                NetworkSend.PlayerMsg(senderIndex, $"Player '{targetName}' not found.", (int) ColorName.BrightRed);
                return;
            }

            await SendChatMessageAsync(senderIndex, $"private:{targetIndex}", $"[Whisper] {message}", ColorName.BrightCyan);
            Logger.LogInformation($"Whisper from {senderIndex} to {targetIndex}: {message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to send whisper from {senderIndex} to {targetName}");
            NetworkSend.PlayerMsg(senderIndex, "Failed to send whisper.", (int) ColorName.BrightRed);
        }
    }

    private static async Task SavePlayerDataAsync(int playerIndex)
    {
        try
        {
            await Database.SaveAccountAsync(playerIndex); // Assuming this method exists
            NetworkSend.PlayerMsg(playerIndex, "Your data has been saved.", (int) ColorName.BrightGreen);
            Logger.LogInformation($"Player {playerIndex} data saved manually.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to save data for player {playerIndex}");
            NetworkSend.PlayerMsg(playerIndex, "Failed to save data.", (int) ColorName.BrightRed);
        }
    }
    
    private static async Task SendChatMessageAsync(int senderIndex, string channel, string message, ColorName color)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 200) // Basic filtering
            {
                NetworkSend.PlayerMsg(senderIndex, "Invalid message.", (int) ColorName.BrightRed);
                return;
            }

            if (channel.StartsWith("private:"))
            {
                var targetIndex = int.Parse(channel.Split(':')[1]);
                NetworkSend.PlayerMsg(targetIndex, $"[From {Data.Player[senderIndex].Name}] {message}", (int) color);
                NetworkSend.PlayerMsg(senderIndex, $"[To {Data.Player[targetIndex].Name}] {message}", (int) color);
            }
            else if (channel == "party" && Data.TempPlayer[senderIndex].InParty != 0)
            {
                await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Constant.MaxPlayers), Cts.Token, async (i, _) =>
                {
                    if (NetworkConfig.IsPlaying(i) && Data.TempPlayer[i].InParty == Data.TempPlayer[senderIndex].InParty)
                        NetworkSend.PlayerMsg(i, $"[Party] {Data.Player[senderIndex].Name}: {message}", (int) color);
                });
            }
            else if (channel == "global")
            {
                await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Constant.MaxPlayers), Cts.Token, async (i, _) =>
                {
                    if (NetworkConfig.IsPlaying(i))
                        NetworkSend.PlayerMsg(i, message, (int) color);
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to send chat message from {senderIndex} to {channel}");
            NetworkSend.PlayerMsg(senderIndex, "Failed to send message.", (int) ColorName.BrightRed);
        }
    }

    private static Task<bool> IsAdminAsync(int playerIndex) =>
        Task.FromResult(playerIndex == 0); // Example admin check

    public static async Task CheckShutDownCountDownAsync()
    {
        if (GetShutDownTimer.ElapsedTicks <= 0) return;

        var time = GetShutDownTimer.Elapsed.Seconds;
        if (_shutDownLastTimer != time)
        {
            if (SettingsManager.Instance.ServerShutdown - time <= 10)
            {
                NetworkSend.GlobalMsg($"Server shutdown in {SettingsManager.Instance.ServerShutdown - time} seconds!");
                Console.WriteLine($"Server shutdown in {SettingsManager.Instance.ServerShutdown - time} seconds!");
                if (SettingsManager.Instance.ServerShutdown - time <= 1)
                {
                    await DestroyServerAsync();
                }
            }

            _shutDownLastTimer = time;
        }
    }

    /// <summary>
    /// Gets the database name from configuration (Database:ConnectionString).
    /// </summary>
    public static string GetDatabaseName(IConfiguration configuration)
    {
        var connStr = configuration["Database:ConnectionString"];
        if (string.IsNullOrEmpty(connStr))
            return string.Empty;
        var builder = new DbConnectionStringBuilder {ConnectionString = connStr};
        return builder.TryGetValue("Database", out var dbNameObj) ? dbNameObj?.ToString() : string.Empty;
    }
}