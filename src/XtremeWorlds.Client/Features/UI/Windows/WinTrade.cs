using Core.Globals;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.Systems;
using XtremeWorlds.Client.Features.UI.Controls;
using static Core.Globals.Command;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinTrade
{
    private static Window _window = null!;
    
    public static void Initialize()
    {
        _window = WindowLoader.FromLayout("winTrade");
        
        _window.Draw += OnDraw;
        _window.GetChild("btnClose").Click += OnClose;
        _window.GetChild("btnAccept").Click += OnAccept;
        _window.GetChild("btnDecline").Click += OnClose;

        // TODO: callbackMousedown: WinTrade.OnYourTradeMouseMove,
        // TODO: callbackMousemove: WinTrade.OnYourTradeMouseMove,
        // TODO: callbackDblclick: WinTrade.OnYourTradeClick,
        _window.GetChild("picYour").Draw += Gui.DrawYourTrade;
        
        // TODO: callbackMousedown: WinTrade.OnTheirTradeMouseMove,
        // TODO: callbackMousemove: WinTrade.OnTheirTradeMouseMove,
        // TODO: callbackDblclick: WinTrade.OnTheirTradeMouseMove,
        _window.GetChild("picTheir").Draw += Gui.DrawTheirTrade;
    }
    
    private static void OnDraw()
    {
        var xo = _window.X;
        var yo = _window.Y;
        var width = _window.Width;
        var height = _window.Height;
        
        var argpath = Path.Combine(DataPath.Gui, 34.ToString());
        
        // render green
        GameClient.RenderTexture(ref argpath, xo + 4, yo + 23, 0, 0, width - 8, height - 27, 4, 4); // ?
        GameClient.RenderTexture(ref argpath, xo + 4, yo + 23, 100, 100, width - 8, 18, width - 8, 18); // Top
        GameClient.RenderTexture(ref argpath, xo + 4, yo + 40, 350, 0, 5, height - 45, 5, height - 45); // Left
        GameClient.RenderTexture(ref argpath, xo + width - 9, yo + 40, 350, 0, 5, height - 45, 5, height - 45); // Right
        GameClient.RenderTexture(ref argpath, xo + 203, yo + 40, 350, 0, 6, height - 45, 6, height - 45); // Center
        GameClient.RenderTexture(ref argpath, xo + 4, yo + 307, 100, 100, width - 8, 75, width - 8, 75); // Bottom
        
        var y = yo + 40;
        for (var i = 0; i < 5; i++)
        {
            if (i == 4)
            {
                height = 38;
            }
            
            var argpath6 = Path.Combine(DataPath.Gui, 35.ToString());
            
            GameClient.RenderTexture(ref argpath6, xo + 4 + 5, y, 0, 0, 76, 76, 76, 76);
            GameClient.RenderTexture(ref argpath6, xo + 80 + 5, y, 0, 0, 76, 76, 76, 76);
            GameClient.RenderTexture(ref argpath6, xo + 156 + 5, y, 0, 0, 42, 76, 42, 76);
            
            y += 76;
        }
        
        y = yo + 40;
        for (var i = 0; i < 5; i++)
        {
            if (i == 4)
            {
                height = 38;
            }
            
            var argpath9 = Path.Combine(DataPath.Gui, 35.ToString());
            
            GameClient.RenderTexture(ref argpath9, xo + 4 + 205, y, 0, 0, 76, 76, 76, 76);
            GameClient.RenderTexture(ref argpath9, xo + 80 + 205, y, 0, 0, 76, 76, 76, 76);
            GameClient.RenderTexture(ref argpath9, xo + 156 + 205, y, 0, 0, 42, 76, 42, 76);

            y += 76;
        }
    }
    
    private static void OnClose()
    {
        Gui.HideWindow(_window);

        Trade.SendDeclineTrade();
    }

    private static void OnAccept()
    {
        Trade.SendAcceptTrade();
    }

    private static void OnYourTradeClick()
    {
        var picYour = _window.GetChild("picYour");
        var x = _window.X + picYour.X;
        var y = _window.Y + picYour.Y;

        var slot = General.IsTrade(x, y);
        if (slot >= 0)
        {
            if (Data.TradeYourOffer[slot].Num == -1)
            {
                return;
            }

            if (GetPlayerInv(GameState.MyIndex, Data.TradeYourOffer[slot].Num) == -1)
            {
                return;
            }

            Trade.UntradeItem(slot);
        }

        OnYourTradeMouseMove();
    }

    private static void OnYourTradeMouseMove()
    {
        var winDescription = Gui.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        var picYour = _window.GetChild("picYour");
        var slotX = _window.X + picYour.X;
        var slotY = _window.Y + picYour.Y;

        var slot = General.IsTrade(slotX, slotY);
        if (YourOfferIsEmpty(slot))
        {
            winDescription.Visible = false;
            return;
        }

        var x = _window.X - winDescription.Width;
        if (x < 0)
        {
            x = _window.X + _window.Width;
        }

        var y = _window.Y - 6;

        GameLogic.ShowItemDesc(x, y, GetPlayerInv(GameState.MyIndex, Data.TradeYourOffer[slot].Num));
    }

    private static void OnTheirTradeMouseMove()
    {
        var winDescription = Gui.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        var picTheir = _window.GetChild("picTheir");
        var slotX = _window.X + picTheir.X;
        var slotY = _window.Y + picTheir.Y;

        var slot = General.IsTrade(slotX, slotY);
        if (TheirOfferIsEmpty(slot))
        {
            winDescription.Visible = false;
            return;
        }

        var x = _window.X - winDescription.Width;
        if (x < 0)
        {
            x = _window.X + _window.Width;
        }

        var y = _window.Y - 6;

        GameLogic.ShowItemDesc(x, y, Data.TradeTheirOffer[slot].Num);
    }

    private static bool YourOfferIsEmpty(int slot)
    {
        return slot < 0 || Data.TradeYourOffer[slot].Num == -1 || GetPlayerInv(GameState.MyIndex, Data.TradeYourOffer[slot].Num) == -1;
    }

    private static bool TheirOfferIsEmpty(int slot)
    {
        return slot < 0 || Data.TradeTheirOffer[slot].Num == -1;
    }
}