using Core.Configurations;
using Core.Globals;
using XtremeWorlds.Client.Features.UI.Controls;
using XtremeWorlds.Client.Net;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinLogin
{
    private static Window _window = null!;

    public static void Initialize()
    {
        _window = WindowLoader.FromLayout("winLogin");

        var username = SettingsManager.Instance.SaveUsername ? SettingsManager.Instance.Username : string.Empty;
        
        _window.GetChild("txtUsername").Text = username;
        _window.GetChild<CheckBox>("chkSaveUsername").IsChecked = SettingsManager.Instance.SaveUsername;
        _window.GetChild("btnAccept").Click += OnAccept;
        _window.GetChild("btnExit").Click += OnExit;
        _window.GetChild("btnRegister").Click += OnRegister;
        _window.GetChild("btnClose").Click += OnClose;

        Gui.SetActiveControl(_window, username.Length == 0 ? "txtUsername" : "txtPassword");
    }

    private static void OnAccept()
    {
        var username = _window.GetChild("txtUsername").Text;
        var password = _window.GetChild("txtPassword").Text;

        if (Network.IsConnected)
        {
            Sender.SendLogin(username, password);
        }
        else
        {
            GameLogic.Dialogue("Invalid Connection", "Cannot connect to game server.", "Please try again.", DialogueType.Alert);
        }
    }

    private static void OnExit()
    {
        try
        {
            General.Client.Exit();
        }
        catch
        {
            General.DestroyGame();
        }
    }

    private static void OnRegister()
    {
        if (!Network.IsConnected)
        {
            GameLogic.Dialogue(
                "Invalid Connection",
                "Cannot connect to game server.",
                "Please try again.",
                DialogueType.Alert);

            return;
        }

        Gui.HideWindows();

        WinRegister.ClearPasswords();

        Gui.ShowWindow("winRegister");
    }

    private static void OnClose()
    {
        try
        {
            General.Client.Exit();
        }
        catch
        {
            General.DestroyGame();
        }
    }

    private static void OnSaveUserClicked()
    {
        var checkBoxSaveUsername = _window.GetChild<CheckBox>("chkSaveUsername");
        if (!checkBoxSaveUsername.IsChecked)
        {
            SettingsManager.Instance.SaveUsername = false;
            SettingsManager.Instance.Username = "";
        }
        else
        {
            SettingsManager.Instance.SaveUsername = true;
        }

        SettingsManager.Save();
    }
}