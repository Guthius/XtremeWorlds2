using Core;
using Core.Globals;
using Serilog;
using XtremeWorlds.Server.Database;
using XtremeWorlds.Server.Game.Events;
using XtremeWorlds.Server.Game.Objects;
using static Core.Globals.Command;

namespace XtremeWorlds.Server.Game;

public class Loop
{
    public static async Task ServerAsync()
    {
        int tick;
        var tmr25 = 0;
        var tmr500 = 0;
        var tmrWalk = 0;
        var tmr1000 = 0;
        var tmr60000 = 0;
        var lastUpdateSavePlayers = 0;
        var lastUpdateMapSpawnItems = 0;

        do
        {
            // Update our current tick value.
            tick = General.GetTimeMs();
            
            await General.CheckShutDownCountDownAsync();

            if (tick > tmr25)
            {
                // Update all our available events.
                EventLogic.UpdateEventLogic();

                // Move the timer up 25ms.
                tmr25 = General.GetTimeMs() + 25;
            }

            if (tick > tmrWalk)
            {
                foreach (var player in PlayerService.Instance.Players)
                {
                    if (Data.Player[player.Id].Moving > 0)
                    {
                        Objects.Player.PlayerMove(player.Id, Data.Player[player.Id].Dir, Data.Player[player.Id].Moving, false);
                    }
                }

                // Move the timer up 250ms.
                tmrWalk = General.GetTimeMs() + 10;
            }

            if (tick > tmr60000)
            {
                try
                {
                    Script.ServerMinute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                tmr60000 = General.GetTimeMs() + 60000;
            }

            if (tick > tmr1000)
            {
                try
                {
                    Script.ServerSecond();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Clock.Instance.Tick();

                // Move the timer up 1000ms.
                tmr1000 = General.GetTimeMs() + 1000;
            }

            if (tick > tmr500)
            {
                UpdateMapAi();

                // Move the timer up 500ms.
                tmr500 = General.GetTimeMs() + 500;
            }

            // Checks to spawn map items every 1 minute
            if (tick > lastUpdateMapSpawnItems)
            {
                UpdateMapSpawnItems();
                lastUpdateMapSpawnItems = General.GetTimeMs() + 60000;
            }

            // Checks to save players every 5 minutes
            if (tick > lastUpdateSavePlayers)
            {
                UpdateSavePlayers();
                lastUpdateSavePlayers = General.GetTimeMs() + 300000;
            }

            try
            {
                Script.Loop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            await Task.Delay(1);
        } while (true);
    }

    public static void UpdateSavePlayers()
    {
        var playerIds = PlayerService.Instance.Players.ToList();
        if (playerIds.Count == 0)
        {
            return;
        }
        
        Log.Information("Saving all online players...");

        foreach (var player in PlayerService.Instance.Players)
        {
            Database.SaveCharacter(player.Id, Data.TempPlayer[player.Id].Slot);
            Database.SaveBank(player.Id);
        }
    }

    private static void UpdateMapSpawnItems()
    {
        for (var mapNum = 0; mapNum < Core.Globals.Constant.MaxMaps; mapNum++)
        {
            for (var mapItemNum = 0; mapItemNum < Core.Globals.Constant.MaxMapItems; mapItemNum++)
            {
                Database.ClearMapItem(mapItemNum, mapNum);
            }

            Item.SpawnMapItems(mapNum);
            Item.SendMapItemsToAll(mapNum);
        }
    }

    private static void UpdateMapAi()
    {
        // Clear the entity list before repopulating to avoid accumulating instances
        Entity.Instances.Clear();

        var entities = Entity.Instances;
        var mapCount = Core.Globals.Constant.MaxMaps;

        // Use entities from Entity class
        for (int mapNum = 0; mapNum < Core.Globals.Constant.MaxMaps; mapNum++)
        {
            // Add Npcs
            for (int i = 0; i < Core.Globals.Constant.MaxMapNpcs; i++)
            {
                var npc = Entity.FromNpc(i, Data.MapNpc[mapNum].Npc[i]);
                if (npc.Num >= 0)
                {
                    npc.Map = mapNum;
                    entities.Add(npc);
                }
            }

            // Add Players
            foreach (var i in PlayerService.Instance.Players)
            {
                if (Data.Player[i.Id].Map == mapNum)
                {
                    var player = Entity.FromPlayer(i.Id, Data.Player[i.Id]);
                    if (IsPlaying(i.Id))
                    {
                        player.Map = mapNum;
                        entities.Add(player);
                    }
                }
            }
        }

        Script.UpdateMapAi();

        // Use entities from Entity class
        for (int mapNum = 0; mapNum < mapCount; mapNum++)
        {
            // Add Npcs
            for (int i = 0; i < Core.Globals.Constant.MaxMapNpcs; i++)
            {
                var npc = Entity.FromNpc(i, Data.MapNpc[mapNum].Npc[i]);
                if (npc.Num >= 0)
                {
                    npc.Map = mapNum;
                    entities.Add(npc);
                }
            }
        }
    }
}