using Core.Globals;
using XtremeWorlds.Client.Features.Objects;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.Systems;
using XtremeWorlds.Client.Features.UI.Controls;
using static Core.Globals.Command;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinBank
{
    private static Window _window = null!;

    public static void Initialize()
    {
        _window = WindowLoader.FromLayout("winBank");

        // TODO: callbackMousemove: WinBank.OnMouseMove,
        // TODO: callbackMousedown: WinBank.OnMouseDown,
        // TODO: callbackDblclick: WinBank.OnDoubleClick,

        _window.Draw += OnDraw;
        _window.GetChild("btnClose").Click += OnClose;
    }

    private static void OnDraw()
    {
        if (GameState.MyIndex < 0 || GameState.MyIndex > Constant.MaxPlayers)
        {
            return;
        }

        var argpath = Path.Combine(DataPath.Gui, "34");

        GameClient.RenderTexture(ref argpath,
            _window.X + 4,
            _window.Y + 23,
            0, 0,
            _window.Width - 8,
            _window.Height - 27,
            4, 4);

        var height = 76;

        var xo = _window.X;
        var yo = _window.Y;

        var y = _window.Y + 23;
        for (var i = 0; i < 5; i++)
        {
            if (i == 4)
            {
                height = 42;
            }

            var argpath1 = Path.Combine(DataPath.Gui, "35");

            GameClient.RenderTexture(ref argpath1, xo + 4, y, 0, 0, 76, height, 76, height);
            GameClient.RenderTexture(ref argpath1, xo + 80, y, 0, 0, 76, height, 76, height);
            GameClient.RenderTexture(ref argpath1, xo + 156, y, 0, 0, 76, height, 76, height);
            GameClient.RenderTexture(ref argpath1, xo + 232, y, 0, 0, 76, height, 76, height);
            GameClient.RenderTexture(ref argpath1, xo + 308, y, 0, 0, 79, height, 79, height);

            y += 76;
        }

        for (var slot = 0; slot < Constant.MaxBank; slot++)
        {
            var itemNum = GetBank(GameState.MyIndex, slot);
            if (itemNum is < 0 or >= Constant.MaxItems)
            {
                continue;
            }

            Item.StreamItem(itemNum);

            if (Gui.DragBox.Origin == PartOrigin.Bank &&
                Gui.DragBox.Slot == slot)
            {
                continue;
            }

            var itemIcon = Data.Item[itemNum].Icon;
            if (itemIcon <= 0 || itemIcon > GameState.NumItems)
            {
                continue;
            }

            var top = yo + GameState.BankTop + (GameState.BankOffsetY + 32) * (slot / GameState.BankColumns);
            var left = xo + GameState.BankLeft + (GameState.BankOffsetX + 32) * (slot % GameState.BankColumns);

            // draw icon
            var argpath6 = Path.Combine(DataPath.Items, itemIcon.ToString());

            GameClient.RenderTexture(ref argpath6, left, top, 0, 0, 32, 32, 32, 32);

            if (GetBankValue(GameState.MyIndex, slot) <= 1)
            {
                continue;
            }

            var amount = GetBankValue(GameState.MyIndex, slot);
            var amountColor = TextRenderer.GetColorForAmount(amount);

            TextRenderer.RenderText(GameLogic.ConvertCurrency(amount), left + 1, top + 20, amountColor, amountColor);
        }
    }

    private static void OnMouseMove()
    {
        if (Gui.DragBox.Type != DraggablePartType.None)
        {
            return;
        }

        var winDescription = Gui.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        var slot = General.IsBank(_window.X, _window.Y);
        if (slot < 0)
        {
            winDescription.Visible = false;
            return;
        }

        if (Gui.DragBox.Type == DraggablePartType.Item &&
            Gui.DragBox.Value == slot)
        {
            return;
        }

        var x = _window.X - winDescription.Width;
        if (x < 0)
        {
            x = _window.X + _window.Width;
        }

        var y = _window.Y - 6;

        GameLogic.ShowItemDesc(x, y, GetBank(GameState.MyIndex, slot));
    }

    private static void OnMouseDown()
    {
        var slot = General.IsBank(_window.X, _window.Y);
        if (slot >= 0)
        {
            ref var dragBox = ref Gui.DragBox;

            dragBox.Type = DraggablePartType.Item;
            dragBox.Value = GetBank(GameState.MyIndex, slot);
            dragBox.Origin = PartOrigin.Bank;
            dragBox.Slot = slot;

            var windowIndex = Gui.GetWindowIndex("winDragBox");
            var window = Gui.Windows[windowIndex];

            window.X = GameState.CurMouseX;
            window.Y = GameState.CurMouseY;
            window.MovedX = GameState.CurMouseX - window.X;
            window.MovedY = GameState.CurMouseY - window.Y;

            Gui.ShowWindow(windowIndex, resetPosition: false);
        }

        OnMouseMove();
    }

    private static void OnDoubleClick()
    {
        var slot = General.IsBank(_window.X, _window.Y);
        if (slot >= 0)
        {
            Bank.WithdrawItem(slot, GetBankValue(GameState.MyIndex, slot));

            return;
        }

        OnMouseMove();
    }

    private static void OnClose()
    {
        if (_window.Visible)
        {
            Bank.CloseBank();
        }
    }
}