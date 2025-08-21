using Core.Globals;
using XtremeWorlds.Client.Features.States;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinDialogue
{
    public static void Initialize()
    {
        var window = WindowLoader.FromLayout("winDialogue");

        window.GetChild("btnClose").Click += OnClose;
        window.GetChild("btnYes").Click += OnYes;
        window.GetChild("btnNo").Click += OnNo;
        window.GetChild("btnOkay").Click += OnOkay;

        Gui.SetActiveControl(window, "txtInput");
    }

    private static void OnOkay()
    {
        GameLogic.DialogueHandler(1);
    }

    private static void OnYes()
    {
        GameLogic.DialogueHandler(2);
    }

    private static void OnNo()
    {
        GameLogic.DialogueHandler(3);
    }

    private static void OnClose()
    {
        switch (GameState.DiaStyle)
        {
            case DialogueStyle.Okay:
                GameLogic.DialogueHandler(1);
                break;

            case DialogueStyle.YesNo:
                GameLogic.DialogueHandler(3);
                break;
        }
    }
}