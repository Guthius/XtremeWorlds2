using Core.Globals;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.UI.Controls;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinNewChar
{
    private static Window _window = null!;

    public static void Initialize()
    {
        _window = WindowLoader.FromLayout("winNewChar");
        _window.GetChild("btnClose").Click += OnCancel;
        _window.GetChild("btnAccept").Click += OnAccept;
        _window.GetChild("btnCancel").Click += OnCancel;
        _window.GetChild("picScene").Draw += OnDrawSprite;
        _window.GetChild("btnLeft").Click += OnLeftClick;
        _window.GetChild("btnRight").Click += OnRightClick;

        Gui.SetActiveControl(_window, "txtName");
    }

    private static void OnDrawSprite()
    {
        var spriteIndex = GameState.NewCnarGender == Sex.Male ? Data.Job[GameState.NewCharJob].MaleSprite : Data.Job[GameState.NewCharJob].FemaleSprite;
        if (spriteIndex == 0)
        {
            spriteIndex = 1;
        }

        var spritePath = Path.Combine(DataPath.Characters, spriteIndex.ToString());
        var sprite = GameClient.GetGfxInfo(Path.Combine(DataPath.Characters, spriteIndex.ToString()));
        if (sprite is null)
        {
            return;
        }

        var w = sprite.Width / 4;
        var h = sprite.Height / 4;

        GameClient.RenderTexture(ref spritePath,
            _window.X + 190,
            _window.Y + 100, 0, 0,
            w, h, w, h);
    }

    private static void OnLeftClick()
    {
        var spriteIndex = GameState.NewCnarGender == Sex.Male ? Data.Job[GameState.NewCharJob].MaleSprite : Data.Job[GameState.NewCharJob].FemaleSprite;
        if (GameState.NewCharSprite < 0)
        {
            GameState.NewCharSprite = spriteIndex;
        }
        else
        {
            GameState.NewCharSprite -= 1;
        }
    }

    private static void OnRightClick()
    {
        var spriteIndex = GameState.NewCnarGender == Sex.Male
            ? Data.Job[GameState.NewCharJob].MaleSprite
            : Data.Job[GameState.NewCharJob].FemaleSprite;

        if (GameState.NewCharSprite >= spriteIndex)
        {
            GameState.NewCharSprite = 1;
        }
        else
        {
            GameState.NewCharSprite += 1;
        }
    }

    private static void OnMaleChecked()
    {
        GameState.NewCharSprite = 1;
        GameState.NewCnarGender = Sex.Male;

        if (_window.GetChild<CheckBox>("chkMale").IsChecked)
        {
            return;
        }

        _window.GetChild<CheckBox>("chkFemale").IsChecked = false;
        _window.GetChild<CheckBox>("chkMale").IsChecked = true;
    }

    private static void OnFemaleChecked()
    {
        GameState.NewCharSprite = 1;
        GameState.NewCnarGender = Sex.Female;

        if (_window.GetChild<CheckBox>("chkFemale").IsChecked)
        {
            return;
        }

        _window.GetChild<CheckBox>("chkFemale").IsChecked = true;
        _window.GetChild<CheckBox>("chkMale").IsChecked = false;
    }

    private static void OnCancel()
    {
        _window.GetChild("txtName").Text = "";
        _window.GetChild<CheckBox>("chkMale").IsChecked = false;
        _window.GetChild<CheckBox>("chkFemale").IsChecked = false;

        GameState.NewCharSprite = 1;
        GameState.NewCnarGender = Sex.Male;

        Gui.HideWindows();

        Gui.ShowWindow("winChars");
    }

    private static void OnAccept()
    {
        var name = _window.GetChild("txtName").Text;

        Gui.HideWindows();

        GameLogic.AddChar(name, (int) GameState.NewCnarGender, GameState.NewCharJob, GameState.NewCharSprite);
    }
}