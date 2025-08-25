﻿using Core.Globals;
using static Core.Globals.Command;

namespace XtremeWorlds.Server.Game;

public static class GameLogic
{
    public static int GetTotalMapPlayers(int mapNum)
    {
        return PlayerService.Instance.PlayerIds.Count(i => GetPlayerMap(i) == mapNum);
    }

    public static int GetNpcMaxVital(double npcNum, Vital vital)
    {
        if (npcNum is < 0 or > Core.Globals.Constant.MaxNpcs)
        {
            return 0;
        }

        return vital switch
        {
            Vital.Health => Data.Npc[(int) npcNum].Hp,
            Vital.Stamina => Data.Npc[(int) npcNum].Stat[(byte) Stat.Intelligence] * 2,
            _ => 0
        };
    }
    
    public static int FindPlayer(string playerName)
    {
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (string.Equals(GetPlayerName(i), playerName, StringComparison.InvariantCultureIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static string CheckGrammar(string word, byte caps = 0)
    {
        const string vowels = "aeiou";

        if (string.IsNullOrEmpty(word))
        {
            return string.Empty;
        }

        var firstLetter = char.ToLowerInvariant(word[0]);
        if (firstLetter == '$')
        {
            return word[1..];
        }

        if (vowels.Contains(firstLetter))
        {
            return (caps != 0 ? "An " : "an ") + word;
        }

        return (caps != 0 ? "A " : "a ") + word;
    }
}