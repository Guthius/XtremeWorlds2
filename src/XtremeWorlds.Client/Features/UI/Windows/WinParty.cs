using Core.Globals;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.UI.Controls;
using static Core.Globals.Command;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinParty
{
    private static Window _window = null!;

    public static void Initialize()
    {
        _window = WindowLoader.FromLayout("winParty");
    }

    public static void Update()
    {
        if (Data.MyParty.Leader == 0)
        {
            Gui.HideWindow("winParty");
            return;
        }

        Gui.ShowWindow("winParty");

        for (var i = 0; i < 4; i++)
        {
            _window.GetChild("lblName" + i).Text = "";
            _window.GetChild("picEmptyBar_HP" + i).Visible = false;
            _window.GetChild("picEmptyBar_SP" + i).Visible = false;
            _window.GetChild("picBar_HP" + i).Visible = false;
            _window.GetChild("picBar_SP" + i).Visible = false;
            _window.GetChild("picShadow" + i).Visible = false;
            _window.GetChild("picChar" + i).Visible = false;
            // TODO: _window.GetChild("picChar" + i).Value = 0;
        }

        var frame = 0;
        for (var i = 0; i < Data.MyParty.MemberCount; i++)
        {
            var playerIndex = Data.MyParty.Member[i];
            if (playerIndex <= 0)
            {
                continue;
            }

            if (playerIndex == GameState.MyIndex || !IsPlaying(playerIndex))
            {
                continue;
            }

            var pictureBox = _window.GetChild<PictureBox>("picChar" + frame);

            pictureBox.Visible = true;
            // TODO: pictureBox.Value = playerIndex;
            pictureBox.ImagePath =  Path.Combine(DataPath.Characters, GetPlayerSprite(playerIndex) + ".png");
            
            _window.GetChild("lblName" + frame).Visible = true;
            _window.GetChild("lblName" + frame).Text = GetPlayerName(playerIndex);
            _window.GetChild("picShadow" + frame).Visible = true;
            _window.GetChild("picEmptyBar_HP" + frame).Visible = true;
            _window.GetChild("picEmptyBar_SP" + frame).Visible = true;
            _window.GetChild("picBar_HP" + frame).Visible = true;
            _window.GetChild("picBar_SP" + frame).Visible = true;

            frame++;
        }

        GameLogic.UpdatePartyBars();

        _window.Height = Data.MyParty.MemberCount switch
        {
            2 => 78,
            3 => 118,
            4 => 158,
            _ => 0
        };
    }
}