using Core.Globals;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Net;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinChars
{
    public static void Initialize()
    {
        var window = WindowLoader.FromLayout("winChars");

        window.GetChild("btnClose").Click += OnClose;
        window.GetChild("picScene_3").Draw += OnDraw;
        window.GetChild("btnSelectChar_1").Click += OnSelectCharacter1Click;
        window.GetChild("btnCreateChar_1").Click += OnCreateCharacter1Click;
        window.GetChild("btnDelChar_1").Click += OnDeleteCharacter1Click;
        window.GetChild("btnSelectChar_2").Click += OnSelectCharacter2Click;
        window.GetChild("btnCreateChar_2").Click += OnCreateCharacter2Click;
        window.GetChild("btnDelChar_2").Click += OnDeleteCharacter2Click;
        window.GetChild("btnSelectChar_3").Click += OnSelectCharacter3Click;
        window.GetChild("btnCreateChar_3").Click += OnCreateCharacter3Click;
        window.GetChild("btnDelChar_3").Click += OnDeleteCharacter3Click;
    }

    private static void OnSelectCharacter1Click()
    {
        Sender.SendUseChar(1);
    }

    private static void OnSelectCharacter2Click()
    {
        Sender.SendUseChar(2);
    }

    private static void OnSelectCharacter3Click()
    {
        Sender.SendUseChar(3);
    }

    private static void TryDeleteCharacter(int slot)
    {
        GameLogic.Dialogue(
            "Delete Character",
            "Deleting this character is permanent.",
            "Delete this character?",
            DialogueType.DeleteCharacter,
            DialogueStyle.YesNo,
            slot);
    }

    private static void OnDeleteCharacter1Click()
    {
        TryDeleteCharacter(1);
    }

    private static void OnDeleteCharacter2Click()
    {
        TryDeleteCharacter(1);
    }

    private static void OnDeleteCharacter3Click()
    {
        TryDeleteCharacter(1);
    }

    private static void TryCreateCharacter(int slot)
    {
        GameState.CharNum = (byte) slot;

        var winJobs = Gui.GetWindowByName("winJobs");
        if (winJobs is null)
        {
            return;
        }

        Gui.HideWindows();

        GameState.NewCharJob = 0;
        GameState.NewCharSprite = 1;
        GameState.NewCnarGender = (long) Sex.Male;

        winJobs.GetChild("lblJobName").Text = Data.Job[GameState.NewCharJob].Name;

        Gui.ShowWindow(winJobs);
    }

    private static void OnCreateCharacter1Click()
    {
        TryCreateCharacter(1);
    }

    private static void OnCreateCharacter2Click()
    {
        TryCreateCharacter(2);
    }

    private static void OnCreateCharacter3Click()
    {
        TryCreateCharacter(3);
    }

    private static void OnClose()
    {
        Gui.HideWindows();
        Gui.ShowWindow("winLogin");
    }

    private static void OnDraw()
    {
        var winChars = Gui.GetWindowByName("winChars");
        if (winChars is null)
        {
            return;
        }

        var x = winChars.X + 24;
        var y = winChars.Y;

        for (var i = 0; i <= Constant.MaxChars - 1; i++)
        {
            if (!string.IsNullOrEmpty(GameState.CharName[i]))
            {
                if (GameState.CharSprite[i] > 0) // Ensure character sprite is valid
                {
                    var spritePath = Path.Combine(DataPath.Characters, GameState.CharSprite[i].ToString());
                    var sprite = GameClient.GetGfxInfo(spritePath);
                    if (sprite is null)
                    {
                        continue;
                    }

                    var w = sprite.Width / 4;
                    var h = sprite.Height / 4;

                    if (GameState.CharSprite[i] <= GameState.NumCharacters)
                    {
                        GameClient.RenderTexture(ref spritePath, x + 30, y + 100, 0, 0, w, h, w, h);
                    }
                }
            }

            // Move to the next position for the next character
            x += 110;
        }
    }
}