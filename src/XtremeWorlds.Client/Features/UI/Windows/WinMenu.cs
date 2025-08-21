using XtremeWorlds.Client.Features.UI.Controls;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinMenu
{
    private static Window _window = null!;

    public static void Initialize()
    {
        _window = WindowLoader.FromLayout("winMenu");
        _window.GetChild("btnChar").Click += OnCharacterClick;
        _window.GetChild("btnInv").Click += OnInventoryClick;
        _window.GetChild("btnSkills").Click += OnSkillsClick;
    }

    private static void ToggleWindow(string windowName)
    {
        var window = Gui.GetWindowByName(windowName);
        if (window is null)
        {
            return;
        }

        if (window.Visible)
        {
            Gui.HideWindow(window);
        }
        else
        {
            Gui.ShowWindow(window, resetPosition: false);
        }
    }

    public static void OnCharacterClick()
    {
        ToggleWindow("winCharacter");
    }

    public static void OnInventoryClick()
    {
        ToggleWindow("winInventory");
    }

    public static void OnSkillsClick()
    {
        ToggleWindow("winSkills");
    }
}