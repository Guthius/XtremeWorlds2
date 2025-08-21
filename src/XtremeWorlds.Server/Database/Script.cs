using Core.Configurations;
using Core.Globals;
using XtremeWorlds.Server.Game;
using XtremeWorlds.Server.Game.Events;
using XtremeWorlds.Server.Game.Network;
using static Core.Globals.Command;
using static XtremeWorlds.Server.Game.Network.NetworkSend;
using static XtremeWorlds.Server.Game.Objects.Player;
using Animation = XtremeWorlds.Server.Game.Objects.Animation;
using Constant = Core.Globals.Constant;
using Event = XtremeWorlds.Server.Game.Events.Event;
using EventCommand = Core.Globals.EventCommand;
using Item = XtremeWorlds.Server.Game.Objects.Item;
using Npc = XtremeWorlds.Server.Game.Objects.Npc;
using Projectile = XtremeWorlds.Server.Game.Objects.Projectile;
using Resource = XtremeWorlds.Server.Game.Objects.Resource;
using Type = Core.Globals.Type;

namespace XtremeWorlds.Server.Database;

public static class Script
{
    public static void Loop()
    {
    }

    public static void ServerSecond()
    {
    }


    public static void ServerMinute()
    {
    }

    public static void JoinGame(int index)
    {
        // Warp the player to his saved location
        PlayerWarp(index, GetPlayerMap(index), GetPlayerX(index), GetPlayerY(index), (byte)Direction.Down);

        // Notify everyone that a player has joined the game.
        GlobalMsg(string.Format("{0} has joined {1}!", GetPlayerName(index), SettingsManager.Instance.GameName));

        // Send all the required game data to the user.
        CheckEquippedItems(index);
        SendInventory(index);
        SendWornEquipment(index);
        SendExp(index);
        SendHotbar(index);
        SendPlayerSkills(index);
        SendStats(index);
        SendJoinMap(index);

        // Send the flag so they know they can start doing stuff
        SendInGame(index);

        // Send welcome messages
        SendWelcome(index);
    }

    public static void MapDropItem(int index, int mapSlot, int invSlot, int amount, int mapNum, Type.Item item, int itemNum)
    {
        // Determine if the item is currency or stackable
        if (item.Type == (byte)ItemCategory.Currency || item.Stackable == 1)
        {
            // Check if dropping more than the player has, drop all if so
            var playerInvValue = GetPlayerInvValue(index, invSlot);
            if (amount >= playerInvValue)
            {
                amount = playerInvValue;
                SetPlayerInv(index, invSlot, -1);
                SetPlayerInvValue(index, invSlot, 0);
            }
            else
            {
                SetPlayerInvValue(index, invSlot, playerInvValue - amount);
            }
            MapMsg(mapNum, string.Format("{0} has dropped {1} ({2}x).", GetPlayerName(index), GameLogic.CheckGrammar(item.Name), amount));
        }
        else
        {
            // Not a currency or stackable item
            SetPlayerInv(index, invSlot, -1);
            SetPlayerInvValue(index, invSlot, 0);

            MapMsg(mapNum, string.Format("{0} has dropped {1}.", GetPlayerName(index), GameLogic.CheckGrammar(item.Name)));
        }

        // Send inventory update
        SendInventoryUpdate(index, invSlot);

        // Spawn the item on the map
        Item.SpawnItemSlot(mapSlot, itemNum, amount, mapNum, GetPlayerX(index), GetPlayerY(index));
    }

    public static void MapGetItem(int index, int mapNum, int mapSlot, int invSlot)
    {
        // Set item in players inventor
        SetPlayerInv(index, invSlot, Data.MapItem[mapNum, mapSlot].Num);

        string msg;

        if (Data.Item[GetPlayerInv(index, invSlot)].Type == (byte)ItemCategory.Currency | Data.Item[GetPlayerInv(index, invSlot)].Stackable == 1)
        {
            SetPlayerInvValue(index, invSlot, GetPlayerInvValue(index, invSlot) + Data.MapItem[mapNum, mapSlot].Value);
            msg = Data.MapItem[mapNum, mapSlot].Value + " " + Data.Item[GetPlayerInv(index, invSlot)].Name;
        }
        else
        {
            SetPlayerInvValue(index, invSlot, 1);
            msg = Data.Item[GetPlayerInv(index, invSlot)].Name;
        }

        // Erase item from the map
        Item.SpawnItemSlot(mapSlot, -1, 0, GetPlayerMap(index), Data.MapItem[mapNum, mapSlot].X, Data.MapItem[mapNum, mapSlot].Y);
        SendInventoryUpdate(index, invSlot);
        SendActionMsg(GetPlayerMap(index), msg, (int)ColorName.White, (byte)ActionMessageType.Static, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
    }

    public static void UnEquipItem(int index, int eqSlot)
    {
        var m = FindOpenInvSlot(index, Data.Player[index].Equipment[eqSlot]);
        SetPlayerInv(index, m, Data.Player[index].Equipment[eqSlot]);
        SetPlayerInvValue(index, m, 0);

        PlayerMsg(index, "You unequip " + GameLogic.CheckGrammar(Data.Item[GetPlayerEquipment(index, (Equipment)eqSlot)].Name), (int)ColorName.Yellow);

        // remove equipment
        SetPlayerEquipment(index, -1, (Equipment)eqSlot);
        SendWornEquipment(index);
        SendMapEquipment(index);
        SendStats(index);
        SendInventory(index);

        // send vitals
        SendVitals(index);
    }

    public static void UseItem(int index, int itemNum, int invNum)
    {
        var tempItem = 0;

        // Find out what kind of item it is
        switch (Data.Item[itemNum].Type)
        {
            case (byte)ItemCategory.Equipment:
            {
                int m;
                switch (Data.Item[itemNum].SubType)
                {
                    case (byte)Equipment.Weapon:
                    {

                        if (GetPlayerEquipment(index, Equipment.Weapon) >= 0)
                        {
                            tempItem = GetPlayerEquipment(index, Equipment.Weapon);
                        }

                        SetPlayerEquipment(index, itemNum, Equipment.Weapon);

                        PlayerMsg(index, "You equip " + GameLogic.CheckGrammar(Data.Item[itemNum].Name), (int)ColorName.BrightGreen);
                        TakeInv(index, itemNum, 1);

                        if (tempItem >= 0) // give back the stored item
                        {
                            m = FindOpenInvSlot(index, tempItem);
                            SetPlayerInv(index, m, tempItem);
                            SetPlayerInvValue(index, m, 0);
                        }

                        SendWornEquipment(index);
                        SendMapEquipment(index);
                        SendInventory(index);
                        SendInventoryUpdate(index, invNum);
                        SendStats(index);

                        // send vitals
                        SendVitals(index);
                        break;
                    }

                    case (byte)Equipment.Armor:
                    {
                        if (GetPlayerEquipment(index, Equipment.Armor) >= 0)
                        {
                            tempItem = GetPlayerEquipment(index, Equipment.Armor);
                        }

                        SetPlayerEquipment(index, itemNum, Equipment.Armor);

                        PlayerMsg(index, "You equip " + GameLogic.CheckGrammar(Data.Item[itemNum].Name), (int)ColorName.BrightGreen);
                        TakeInv(index, itemNum, 1);

                        if (tempItem >= 0) // Return their old equipment to their inventory.
                        {
                            m = FindOpenInvSlot(index, tempItem);
                            SetPlayerInv(index, m, tempItem);
                            SetPlayerInvValue(index, m, 0);
                        }

                        SendWornEquipment(index);
                        SendMapEquipment(index);

                        SendInventory(index);
                        SendStats(index);

                        // send vitals
                        SendVitals(index);
                        break;
                    }

                    case (byte)Equipment.Helmet:
                    {
                        if (GetPlayerEquipment(index, Equipment.Helmet) >= 0)
                        {
                            tempItem = GetPlayerEquipment(index, Equipment.Helmet);
                        }

                        SetPlayerEquipment(index, itemNum, Equipment.Helmet);

                        PlayerMsg(index, "You equip " + GameLogic.CheckGrammar(Data.Item[itemNum].Name), (int)ColorName.BrightGreen);
                        TakeInv(index, itemNum, 1);

                        if (tempItem >= 0) // give back the stored item
                        {
                            m = FindOpenInvSlot(index, tempItem);
                            SetPlayerInv(index, m,  tempItem);
                            SetPlayerInvValue(index, m, 0);
                        }

                        SendWornEquipment(index);
                        SendMapEquipment(index);
                        SendInventory(index);
                        SendStats(index);

                        // send vitals
                        SendVitals(index);
                        break;
                    }

                    case (byte)Equipment.Shield:
                    {
                        if (GetPlayerEquipment(index, Equipment.Shield) >= 0)
                        {
                            tempItem = GetPlayerEquipment(index, Equipment.Shield);
                        }

                        SetPlayerEquipment(index, itemNum, Equipment.Shield);

                        PlayerMsg(index, "You equip " + GameLogic.CheckGrammar(Data.Item[itemNum].Name), (int)ColorName.BrightGreen);
                        TakeInv(index, itemNum, 1);

                        if (tempItem >= 0) // give back the stored item
                        {
                            m = FindOpenInvSlot(index, tempItem);
                            SetPlayerInv(index, m, tempItem);
                            SetPlayerInvValue(index, m, 0);
                        }

                        SendWornEquipment(index);
                        SendMapEquipment(index);
                        SendInventory(index);
                        SendStats(index);

                        // send vitals
                        SendVitals(index);
                        break;
                    }

                }

                break;
            }

            case (byte)ItemCategory.Consumable:
            {
                switch (Data.Item[itemNum].SubType)
                {
                    case (byte)ConsumableEffect.RestoresHealth:
                    {
                        SendActionMsg(GetPlayerMap(index), "+" + Data.Item[itemNum].Data1, (int)ColorName.BrightGreen, (byte)ActionMessageType.Scroll, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
                        Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                        SetPlayerVital(index, Vital.Health, GetPlayerVital(index, Vital.Health) + Data.Item[itemNum].Data1);
                        if (Data.Item[itemNum].Stackable == 1)
                        {
                            TakeInv(index, itemNum, 1);
                        }
                        else
                        {
                            TakeInv(index, itemNum, 0);
                        }
                        SendVital(index, Vital.Health);
                        break;
                    }

                    case (byte)ConsumableEffect.RestoresMana:
                    {
                        SendActionMsg(GetPlayerMap(index), "+" + Data.Item[itemNum].Data1, (int)ColorName.BrightBlue, (byte)ActionMessageType.Scroll, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
                        Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                        SetPlayerVital(index, Vital.Stamina, GetPlayerVital(index, Vital.Stamina) + Data.Item[itemNum].Data1);
                        if (Data.Item[itemNum].Stackable == 1)
                        {
                            TakeInv(index, itemNum, 1);
                        }
                        else
                        {
                            TakeInv(index, itemNum, 0);
                        }
                        SendVital(index, Vital.Stamina);
                        break;
                    }

                    case (byte)ConsumableEffect.RestoresStamina:
                    {
                        Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                        SetPlayerVital(index, Vital.Stamina, GetPlayerVital(index, Vital.Stamina) + Data.Item[itemNum].Data1);
                        if (Data.Item[itemNum].Stackable == 1)
                        {
                            TakeInv(index, itemNum, 1);
                        }
                        else
                        {
                            TakeInv(index, itemNum, 0);
                        }
                        SendVital(index, Vital.Stamina);
                        break;
                    }

                    case (byte)ConsumableEffect.GrantsExperience:
                    {
                        Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                        SetPlayerExp(index, GetPlayerExp(index) + Data.Item[itemNum].Data1);
                        if (Data.Item[itemNum].Stackable == 1)
                        {
                            TakeInv(index, itemNum, 1);
                        }
                        else
                        {
                            TakeInv(index, itemNum, 0);
                        }
                        SendExp(index);
                        break;
                    }

                }

                break;
            }

            case (byte)ItemCategory.Projectile:
            {
                if (Data.Item[itemNum].Ammo > 0)
                {
                    if (HasItem(index, Data.Item[itemNum].Ammo) > 0)
                    {
                        TakeInv(index, Data.Item[itemNum].Ammo, 1);
                        Projectile.PlayerFireProjectile(index);
                    }
                    else
                    {
                        PlayerMsg(index, "No More " + Data.Item[Data.Item[GetPlayerEquipment(index, Equipment.Weapon)].Ammo].Name + " !", (int)ColorName.BrightRed);
                        return;
                    }
                }
                else
                {
                    Projectile.PlayerFireProjectile(index);
                    return;
                }

                break;
            }

            case (byte)ItemCategory.Event:
            {
                var n = Data.Item[itemNum].Data1;

                switch (Data.Item[itemNum].SubType)
                {
                    case (byte)EventCommand.ModifyVariable:
                    {
                        Data.Player[index].Variables[n] = Data.Item[itemNum].Data2;
                        break;
                    }
                    case (byte)EventCommand.ModifySwitch:
                    {
                        Data.Player[index].Switches[n] = (byte)Data.Item[itemNum].Data2;
                        break;
                    }
                    case (byte)EventCommand.Key:
                    {
                        EventLogic.TriggerEvent(index, 1, 0, GetPlayerX(index), GetPlayerY(index));
                        break;
                    }
                }

                break;
            }

            case (byte)ItemCategory.Skill:
            {
                PlayerLearnSkill(index, itemNum);
                break;
            }
        }
    }

    public static void PlayerLearnSkill(int index, int itemNum, int skillNum = -1)
    {
        int n;

        // Get the skill num
        if (skillNum >= 0)
        {
            n = skillNum;
        }
        else
        {
            n = Data.Item[itemNum].Data1;
        }

        if (n < 0 | n > Constant.MaxSkills)
            return;

        // Make sure they are the right class
        if (Data.Skill[n].JobReq == GetPlayerJob(index) | Data.Skill[n].JobReq == -1)
        {
            // Make sure they are the right level
            var i = Data.Skill[n].LevelReq;

            if (i <= GetPlayerLevel(index))
            {
                i = FindOpenSkill(index);

                // Make sure they have an open skill slot
                if (i >= 0)
                {
                    // Make sure they dont already have the skill
                    if (!HasSkill(index, n))
                    {
                        SetPlayerSkill(index, i, n);
                        if (itemNum >= 0)
                        {
                            Animation.SendAnimation(GetPlayerMap(index), Data.Item[itemNum].Animation, 0, 0, (byte)TargetType.Player, index);
                            TakeInv(index, itemNum, 0);
                        }
                        PlayerMsg(index, "You study the skill carefully.", (int)ColorName.Yellow);
                        PlayerMsg(index, "You have learned a new skill!", (int)ColorName.BrightGreen);
                        SendPlayerSkills(index);
                    }
                    else
                    {
                        PlayerMsg(index, "You have already learned this skill!", (int)ColorName.BrightRed);
                    }
                }
                else
                {
                    PlayerMsg(index, "You have learned all that you can learn!", (int)ColorName.BrightRed);
                }
            }
            else
            {
                PlayerMsg(index, "You must be level " + i + " to learn this skill.", (int)ColorName.Yellow);
            }
        }
        else
        {
            PlayerMsg(index, string.Format("Only {0} can use this skill.", GameLogic.CheckGrammar(Data.Job[Data.Skill[n].JobReq].Name, 1)), (int)ColorName.BrightRed);
        }
    }

    public static void JoinMap(int index)
    {
        byte[] data;
        var mapNum = GetPlayerMap(index);

        // Send all players on current map to index
        foreach (var player in PlayerService.Instance.Players)
        {
            if (IsPlaying(player.Id))
            {
                if (player.Id != index)
                {
                    if (GetPlayerMap(player.Id) == mapNum)
                    {
                        data = GetPlayerDataPacket(player.Id);
                        PlayerService.Instance.SendDataTo(index, data);
                        SendPlayerXyTo(index, player.Id);
                        SendMapEquipmentTo(index, player.Id);
                    }
                }
            }
        }

        EventLogic.SpawnMapEventsFor(index, GetPlayerMap(index));

        // Send index's player data to everyone on the map including himself
        data = GetPlayerDataPacket(index);
        NetworkConfig.SendDataToMap(mapNum, data);
        SendPlayerXyToMap(index);
        SendMapEquipment(index);
        SendVitals(index);
    }

    public static void LeaveMap(int index, int mapNum)
    {
    }

    public static void LeftGame(int index)
    {
    }

    public static void OnDeath(int index)
    {
        // Set HP to nothing
        SetPlayerVital(index, Vital.Health, 0);

        // Restore vitals
        var count = Enum.GetValues(typeof(Vital)).Length;
        for (int i = 0, loopTo = count; i < loopTo; i++)
            SetPlayerVital(index, (Vital)i, GetPlayerMaxVital(index, (Vital)i));

        // If the player the attacker killed was a pk then take it away
        if (GetPlayerPk(index))
        {
            SetPlayerPk(index, false);
        }

        ref var withBlock = ref Data.Map[GetPlayerMap(index)];

        // Warp player away
        SetPlayerDir(index, (byte)Direction.Down);

        // to the bootmap if it is set
        if (withBlock.BootMap > 0)
        {
            PlayerWarp(index, withBlock.BootMap, withBlock.BootX, withBlock.BootY, (int)Direction.Down);
        }
        else
        {
            PlayerWarp(index, Data.Job[GetPlayerJob(index)].StartMap, Data.Job[GetPlayerJob(index)].StartX, Data.Job[GetPlayerJob(index)].StartY, (int)Direction.Down);
        }
    }

    public static  void BufferSkill(int mapNum, int index, int skillNum)
    {
  
    }

    public static int KillPlayer(int index)
    {
        int exp = GetPlayerExp(index) / 3;
        
        if (exp == 0)
        {
            PlayerMsg(index, "You've lost no experience.", (int)ColorName.BrightGreen);
        }
        else
        {                   
            SendExp(index);
            PlayerMsg(index, string.Format("You've lost {0} experience.", exp), (int)ColorName.BrightRed);
        }

        return exp;
    }

    public static void TrainStat(int index, int tmpStat)
    {
        // make sure their stats are not maxed
        if (GetPlayerRawStat(index, (Stat)tmpStat) >= Constant.MaxStats)
        {
            PlayerMsg(index, "You cannot spend any more points on that stat.", (int)ColorName.BrightRed);
            return;
        }

        // increment stat
        SetPlayerStat(index, (Stat)tmpStat, GetPlayerRawStat(index, (Stat)tmpStat) + 1);

        // decrement points
        SetPlayerPoints(index, GetPlayerPoints(index) - 1);

        // send player new data
        SendPlayerData(index);
    }

    public static void PlayerMove(int index)
    {

    }

    public static void UpdateMapAi()
    {

        long tickCount = General.GetTimeMs();
        var entities = Entity.Instances;

        for (int x = 0; x < entities.Count; x++)
        {
            var entity = entities[x];
            var mapNum = entity.Map;
            if (entity == null) continue;

            // Only process entities that are Npcs
            if (entity.Num < 0) continue;

            // check if they've completed casting, and if so set the actual skill going
            if (entity.SkillBuffer >= 0)
            {
                if (General.GetTimeMs() > entity.SkillBufferTimer + Data.Skill[entity.SkillBuffer].CastTime * 1000)
                {
                    if (Data.Moral[Data.Map[mapNum].Moral].CanCast)
                    {
                        //BufferSkill(mapNum, [Core.Globals.Entity.Index(entity), entity.SkillBuffer);
                        entity.SkillBuffer = -1;
                        entity.SkillBufferTimer = 0;
                    }
                }
            }
            else
            {
                // ATTACKING ON SIGHT
                if (entity.Behaviour == (byte)NpcBehavior.AttackOnSight || entity.Behaviour == (byte)NpcBehavior.Guard)
                {
                    // make sure it's not stunned
                    if (!(entity.StunDuration > 0))
                    {
                        foreach (var player in PlayerService.Instance.Players)
                        {
                            if (NetworkConfig.IsPlaying(player.Id))
                            {
                                if (GetPlayerMap(player.Id) == mapNum && entity.TargetType == 0 && GetPlayerAccess(player.Id) <= (byte)AccessLevel.Moderator)
                                {
                                    int n = entity.Range;
                                    int distanceX = entity.X - GetPlayerX(player.Id);
                                    int distanceY = entity.Y - GetPlayerY(player.Id);

                                    if (distanceX < 0) distanceX *= -1;
                                    if (distanceY < 0) distanceY *= -1;

                                    if (distanceX <= n && distanceY <= n)
                                    {
                                        if (entity.Behaviour == (byte)NpcBehavior.AttackOnSight || GetPlayerPk(player.Id))
                                        {
                                            if (!string.IsNullOrEmpty(entity.AttackSay))
                                            {
                                                PlayerMsg(player.Id, GameLogic.CheckGrammar(entity.Name, 1) + " says, '" + entity.AttackSay + "' to you.", (int)ColorName.Yellow);
                                            }
                                            entity.TargetType = (byte)TargetType.Player;
                                            entity.Target = player.Id;
                                        }
                                    }
                                }
                            }
                        }

                        // Check if target was found for Npc targeting
                        if (entity.TargetType == 0 && entity.Faction > 0)
                        {
                            for (int i = 0; i < entities.Count; i++)
                            {
                                var otherEntity = entities[i];
                                if (otherEntity != null && otherEntity.Num >= 0)
                                {
                                    if (otherEntity.Map != mapNum) continue;
                                    if (ReferenceEquals(otherEntity, entity)) continue;
                                    if (otherEntity.Faction > 0 && otherEntity.Faction != entity.Faction)
                                    {
                                        int n = entity.Range;
                                        int distanceX = entity.X - otherEntity.X;
                                        int distanceY = entity.Y - otherEntity.Y;

                                        if (distanceX < 0) distanceX *= -1;
                                        if (distanceY < 0) distanceY *= -1;

                                        if (distanceX <= n && distanceY <= n && entity.Behaviour == (byte)NpcBehavior.AttackOnSight)
                                        {
                                            entity.TargetType = (byte)TargetType.Npc;
                                            entity.Target = i;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                bool targetVerify = false;

                // Npc walking/targeting
                if (entity.StunDuration > 0)
                {
                    if (General.GetTimeMs() > entity.StunTimer + entity.StunDuration * 1000)
                    {
                        entity.StunDuration = 0;
                        entity.StunTimer = 0;
                    }
                }
                else
                {
                    int target = entity.Target;
                    byte targetType = entity.TargetType;
                    int targetX = 0, targetY = 0;

                    if (entity.Type == Entity.EntityType.Npc)
                    {
                        if (entity.Behaviour != (byte)NpcBehavior.ShopKeeper && entity.Behaviour != (byte)NpcBehavior.QuestGiver)
                        {
                            if (target > 0)
                            {
                                if (entities[mapNum].Map == mapNum)
                                {
                                    targetVerify = true;
                                    targetX = entities[target].X;
                                    targetY = entities[target].Y;
                                }
                                else
                                {
                                    entity.TargetType = 0;
                                    entity.Target = 0;
                                }
                            }

                            if (targetVerify)
                            {
                                if (!Event.IsOneBlockAway(targetX, targetY, entity.X, entity.Y))
                                {
                                    int i = EventLogic.FindNpcPath(mapNum, Entity.Index(entity), targetX, targetY);
                                    if (i < 4)
                                    {
                                        if (Npc.CanNpcMove(mapNum, Entity.Index(entity), (byte)i))
                                        {
                                            Npc.NpcMove(mapNum, Entity.Index(entity), (byte)i, (int)MovementState.Walking);
                                        }
                                    }
                                    else
                                    {
                                        i = (int)Math.Round(new Random().NextDouble() * 3) + 1;
                                        if (i == 1)
                                        {
                                            i = (int)Math.Round(new Random().NextDouble() * 3) + 1;
                                            if (Npc.CanNpcMove(mapNum, Entity.Index(entity), (byte)i))
                                            {
                                                Npc.NpcMove(mapNum, Entity.Index(entity), (byte)i, (int)MovementState.Walking);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Npc.NpcDir(mapNum, Entity.Index(entity), Event.GetNpcDir(targetX, targetY, entity.X, entity.Y));
                                }
                            }
                            else
                            {
                                int i = (int)Math.Round(new Random().NextDouble() * 4);
                                if (i == 1)
                                {
                                    i = (int)Math.Round(new Random().NextDouble() * 4);
                                    if (Npc.CanNpcMove(mapNum, Entity.Index(entity), (byte)i))
                                    {
                                        Npc.NpcMove(mapNum, Entity.Index(entity), (byte)i, (int)MovementState.Walking);
                                    }
                                }
                            }
                        }
                    }

                    // Npcs attack targets
                    int attackTarget = entity.Target;
                    byte attackTargetType = entity.TargetType;

                    if (attackTarget > 0)
                    {                    
                        if (GetPlayerMap(attackTarget) == mapNum)
                        {
                            // Placeholder for attack logic
                        }
                        else
                        {
                            entity.Target = 0;
                            entity.TargetType = 0;
                        }                        
                    }

                    // Placeholder for Regen logic

                    // Check if the npc is dead or not
                    if (entity.Vital[(byte)Vital.Health] < 0 && entity.SpawnWait > 0)
                    {
                        entity.Num = 0;
                        entity.SpawnWait = General.GetTimeMs();
                        entity.Vital[(byte)Vital.Health] = 0;
                    }

                    // Spawning an Npc
                    if (entity.Type == Entity.EntityType.Npc)
                    {
                        if (entity.Num == -1)
                        {
                            if (entity.SpawnSecs > 0)
                            {
                                if (tickCount > entity.SpawnWait + entity.SpawnSecs * 1000)
                                {
                                    Npc.SpawnNpc(x, mapNum);
                                }
                            }
                        }
                    }
                }
            }
        }

        var now = General.GetTimeMs();
        var itemCount = Constant.MaxMapItems;
        var mapCount = Constant.MaxMaps;

        for (int mapNum = 0; mapNum < mapCount; mapNum++)
        {
            // Handle map items (public/despawn)
            for (int i = 0; i < itemCount; i++)
            {
                var item = Data.MapItem[mapNum, i];
                if (item.Num >= 0 && !string.IsNullOrEmpty(item.PlayerName))
                {
                    if (item.PlayerTimer < now)
                    {
                        item.PlayerName = "";
                        item.PlayerTimer = 0;
                        Item.SendMapItemsToAll(mapNum);
                    }
                    if (item.CanDespawn && item.DespawnTimer < now)
                    {
                        Game.Database.ClearMapItem(i, mapNum);
                        Item.SendMapItemsToAll(mapNum);
                    }
                }
            }

            // Respawn resources
            var mapResource = Data.MapResource[mapNum];
            if (mapResource.ResourceCount > 0)
            {
                for (int i = 0; i < mapResource.ResourceCount; i++)
                {
                    var resData = mapResource.ResourceData[i];
                    int resourceindex = Data.Map[mapNum].Tile[resData.X, resData.Y].Data1;
                    if (resourceindex > 0)
                    {
                        if (resData.State == 1 || resData.Health < 1)
                        {
                            if (resData.Timer + Data.Resource[resourceindex].RespawnTime * 1000 < now)
                            {
                                resData.Timer = now;
                                resData.State = 0;
                                resData.Health = (byte)Data.Resource[resourceindex].Health;
                                Resource.SendMapResourceToMap(mapNum);
                            }
                        }
                    }
                }
            }
        }
    }

    public static void CheckPlayerLevelUp(int index)
    {
        var level_count = 0;

        while (GetPlayerExp(index) >= GetPlayerNextLevel(index))
        {
            var expRollover = GetPlayerExp(index) - GetPlayerNextLevel(index);
            SetPlayerLevel(index, GetPlayerLevel(index) + 1);
            SetPlayerPoints(index, GetPlayerPoints(index) + XtremeWorlds.Server.Game.Constant.StatPerLevel);
            SetPlayerExp(index, expRollover);
            level_count += 1;
        }

        if (level_count > 0)
        {
            if (level_count == 1)
            {
                // singular
                GlobalMsg(GetPlayerName(index) + " has gained " + level_count + " level!");
            }
            else
            {
                // plural
                GlobalMsg(GetPlayerName(index) + " has gained " + level_count + " levels!");
            }
            SendActionMsg(GetPlayerMap(index), "Level Up", (int) ColorName.Yellow, 1, GetPlayerX(index) * 32, GetPlayerY(index) * 32);
            SendExp(index);
            SendPlayerData(index);
        }
    }
}