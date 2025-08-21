using XtremeWorlds.Client.Features.UI.Controls;
using XtremeWorlds.Client.Net;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinEscMenu
{
    private static Window _window = null!;

    public static void Initialize()
    {
        _window = WindowLoader.FromLayout("winEscMenu");
        _window.GetChild("btnReturn").Click += OnClose;
        _window.GetChild("btnOptions").Click += OnOptionsClick;
        _window.GetChild("btnMainMenu").Click += OnMainMenuClick;
        _window.GetChild("btnExit").Click += OnExitClick;
    }

    private static void OnClose()
    {
        Gui.HideWindow(_window);
    }

    private static void OnOptionsClick()
    {
        Gui.HideWindow(_window);
        Gui.ShowWindow("winOptions", true);
    }

    private static void OnMainMenuClick()
    {
        Gui.HideWindows();
        Gui.ShowWindow("winLogin");

        Sender.SendLogout();
    }

    private static void OnExitClick()
    {
        Gui.HideWindow(_window);

        General.DestroyGame();
    }
}