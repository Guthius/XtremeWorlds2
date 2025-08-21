using Core.Globals;
using XtremeWorlds.Client.Features.Objects;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.Systems;
using XtremeWorlds.Client.Features.UI.Controls;
using static Core.Globals.Command;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinShop
{
    private static Window _window = null!;

    public static void Initialize()
    {
        _window = WindowLoader.FromLayout("winShop");

        // TODO: callbackMousemove: WinShop.OnMouseMove,
        // TODO: callbackMousedown: WinShop.OnMouseDown,
        _window.Draw += OnDrawBackground;
        _window.GetChild("btnClose").Click += OnClose;
        _window.GetChild("picParchment").Draw += OnDraw;
        _window.GetChild("btnBuy").Click += OnBuy;
        _window.GetChild("btnSell").Click += OnSell;
        _window.GetChild("CheckboxBuying").Click += OnBuyingChecked;
        _window.GetChild("CheckboxSelling").Click += OnSellingChecked;
    }

    private static void OnDraw()
    {
        if (GameState.InShop < 0 || GameState.InShop > Constant.MaxShops)
        {
            return;
        }

        Shop.StreamShop(GameState.InShop);

        if (GameState.ShopIsSelling)
        {
            DrawSelling(_window);
        }
        else
        {
            DrawBuying(_window);
        }
    }

    private static void OnDrawBackground()
    {
        var xo = _window.X;
        var yo = _window.Y;
        var width = _window.Width;
        var height = _window.Height;

        // render green
        var argpath = Path.Combine(DataPath.Gui, "34");

        GameClient.RenderTexture(ref argpath, xo + 4, yo + 23, 0, 0, width - 8, height - 27, 4, 4);

        width = 76;
        height = 76;

        var y = yo + 23;
        for (var i = 0; i < 3; i++)
        {
            if (i == 3)
            {
                height = 42;
            }

            var argpath1 = Path.Combine(DataPath.Gui, "35");

            GameClient.RenderTexture(ref argpath1, xo + 4, y, 0, 0, width, height, width, height);
            GameClient.RenderTexture(ref argpath1, xo + 80, y, 0, 0, width, height, width, height);
            GameClient.RenderTexture(ref argpath1, xo + 156, y, 0, 0, width, height, width, height);
            GameClient.RenderTexture(ref argpath1, xo + 232, y, 0, 0, 42, height, 42, height);

            y += 76;
        }

        var argpath5 = Path.Combine(DataPath.Gui, "1");

        GameClient.RenderTexture(ref argpath5, xo + 4, y - 34, 0, 0, 270, 72, 270, 72);
    }

    private static void OnClose()
    {
        Shop.CloseShop();
    }

    private static void OnBuyingChecked()
    {
        var winShop = Gui.GetWindowByName("winShop");
        if (winShop is null)
        {
            return;
        }

        var checkBoxBuying = winShop.GetChild<CheckBox>("CheckboxBuying");
        var checkBoxSelling = winShop.GetChild<CheckBox>("CheckboxSelling");

        if (!checkBoxBuying.IsChecked)
        {
            checkBoxSelling.IsChecked = false;
        }
        else
        {
            checkBoxSelling.IsChecked = false;
            checkBoxBuying.IsChecked = false;
            return;
        }

        var buttonBuy = winShop.GetChild("btnBuy");
        var buttonSell = winShop.GetChild("btnSell");

        buttonSell.Visible = false;
        buttonBuy.Visible = true;

        GameState.ShopIsSelling = false;
        GameState.ShopSelectedSlot = 0;

        UpdateShop();
    }

    private static void OnSellingChecked()
    {
        var checkBoxBuying = _window.GetChild<CheckBox>("CheckboxBuying");
        var checkBoxSelling = _window.GetChild<CheckBox>("CheckboxSelling");

        // TODO: Fix this, should use RadioButton instead of CheckBox...
        
        if (!checkBoxSelling.IsChecked)
        {
            checkBoxBuying.IsChecked = false;
        }
        else
        {
            checkBoxBuying.IsChecked = false;
            checkBoxSelling.IsChecked = false;
            return;
        }

        var buttonBuy = _window.GetChild("btnBuy");
        var buttonSell = _window.GetChild("btnSell");

        buttonBuy.Visible = false;
        buttonSell.Visible = true;

        GameState.ShopIsSelling = true;
        GameState.ShopSelectedSlot = 0;

        UpdateShop();
    }

    private static void OnBuy()
    {
        Shop.BuyItem(GameState.ShopSelectedSlot);
    }

    private static void OnSell()
    {
        Shop.SellItem(GameState.ShopSelectedSlot);
    }

    private static void OnMouseDown()
    {
        var slot = General.IsShop(_window.X, _window.Y);
        if (slot >= 0)
        {
            if (GameState.ShopIsSelling)
            {
                if (GetPlayerInv(GameState.MyIndex, slot) >= 0)
                {
                    GameState.ShopSelectedSlot = slot;

                    UpdateShop();
                }
            }
            else
            {
                if (Data.Shop[GameState.InShop].TradeItem[slot].Item >= 0)
                {
                    GameState.ShopSelectedSlot = slot;

                    UpdateShop();
                }
            }
        }

        OnMouseMove();
    }

    private static void OnMouseMove()
    {
        var winDescription = Gui.GetWindowByName("winDescription");
        if (winDescription is null)
        {
            return;
        }

        if (GameState.InShop < 0 || GameState.InShop > Constant.MaxShops)
        {
            return;
        }

        var slot = General.IsShop(_window.X, _window.Y);
        if (slot < 0)
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

        var itemNum = !GameState.ShopIsSelling
            ? Data.Shop[GameState.InShop].TradeItem[slot].Item
            : GetPlayerInv(GameState.MyIndex, slot);

        if (itemNum == -1)
        {
            return;
        }

        GameLogic.ShowShopDesc(x, y, itemNum);
    }

    public static void UpdateShop()
    {
        if (GameState.InShop < 0)
        {
            return;
        }

        var labelName = _window.GetChild("lblName");
        var labelCost = _window.GetChild("lblCost");

        var picItem = _window.GetChild<PictureBox>("picItem");

        if (!GameState.ShopIsSelling)
        {
            GameState.ShopSelectedItem = Data.Shop[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].Item;
            if (GameState.ShopSelectedItem >= 0)
            {
                labelName.Text = Data.Item[GameState.ShopSelectedItem].Name;
                if (Data.Shop[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostItem == 0)
                {
                    labelCost.Text = Data.Shop[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostValue + "g";
                }
                else if (Data.Shop[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostValue == 1)
                {
                    labelCost.Text = Data.Item[Data.Shop[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostItem].Name;
                }
                else
                {
                    labelCost.Text = Data.Shop[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostValue + " " + Data.Item[Data.Shop[GameState.InShop].TradeItem[GameState.ShopSelectedSlot].CostItem].Name;
                }

                picItem.ImagePath = Path.Combine(DataPath.Items, Data.Item[GameState.ShopSelectedItem].Icon + ".png");
            }
            else
            {
                labelName.Text = "Empty Slot";
                labelCost.Text = "";
                
                picItem.ImagePath = null;
            }
        }
        else
        {
            GameState.ShopSelectedItem = GetPlayerInv(GameState.MyIndex, GameState.ShopSelectedSlot);

            if (GameState.ShopSelectedItem >= 0)
            {
                var cost = (long) Math.Round(Data.Item[GameState.ShopSelectedItem].Price / 100d * Data.Shop[GameState.InShop].BuyRate);

                labelName.Text = Data.Item[GameState.ShopSelectedItem].Name;
                labelCost.Text = cost + "g";
                
                picItem.ImagePath = Path.Combine(DataPath.Items, Data.Item[GameState.ShopSelectedItem].Icon + ".png");
            }
            else
            {
                labelName.Text = "Empty Slot";
                labelCost.Text = "";

                picItem.ImagePath = null;
            }
        }
    }

    private static void DrawBuying(Window winShop)
    {
        for (var i = 0; i < Constant.MaxTrades; i++)
        {
            var x = winShop.Y + GameState.ShopLeft + (GameState.ShopOffsetX + 32) * (i % GameState.ShopColumns);
            var y = winShop.X + GameState.ShopTop + (GameState.ShopOffsetY + 32) * (i / GameState.ShopColumns);

            if (GameState.ShopSelectedSlot == i)
            {
                var selectedSlotTexturePath = Path.Combine(DataPath.Gui, "61");

                GameClient.RenderTexture(ref selectedSlotTexturePath, x, y, 0, 0, 32, 32, 32, 32);
            }

            var itemNum = Data.Shop[GameState.InShop].TradeItem[i].Item;
            if (itemNum is < 0 or >= Constant.MaxItems)
            {
                continue;
            }

            Item.StreamItem(itemNum);

            var itemIcon = Data.Item[itemNum].Icon;
            if (itemIcon <= 0 || itemIcon > GameState.NumItems)
            {
                continue;
            }

            var path = Path.Combine(DataPath.Items, itemIcon.ToString());

            GameClient.RenderTexture(ref path, x, y, 0, 0, 32, 32, 32, 32);
        }
    }

    private static void DrawSelling(Window winShop)
    {
        for (var i = 0; i < Constant.MaxTrades; i++)
        {
            var top = winShop.Y + GameState.ShopTop + (GameState.ShopOffsetY + 32) * (i / GameState.ShopColumns);
            var left = winShop.X + GameState.ShopLeft + (GameState.ShopOffsetX + 32) * (i % GameState.ShopColumns);

            if (GameState.ShopSelectedSlot == i)
            {
                var selectedSlotTexturePath = Path.Combine(DataPath.Gui, "61");

                GameClient.RenderTexture(ref selectedSlotTexturePath, left, top, 0, 0, 32, 32, 32, 32);
            }

            var itemNum = GetPlayerInv(GameState.MyIndex, i);
            if (itemNum is < 0 or >= Constant.MaxItems)
            {
                continue;
            }

            Item.StreamItem(itemNum);

            var itemIcon = Data.Item[itemNum].Icon;
            if (itemIcon <= 0 || itemIcon > GameState.NumItems)
            {
                continue;
            }

            var path = Path.Combine(DataPath.Items, itemIcon.ToString());

            GameClient.RenderTexture(ref path, left, top, 0, 0, 32, 32, 32, 32);

            if (GetPlayerInvValue(GameState.MyIndex, i) <= 1)
            {
                continue;
            }

            var y = top + 20;
            var x = left + 1;

            var amount = GetPlayerInvValue(GameState.MyIndex, i);
            var amountColor = TextRenderer.GetColorForAmount(amount);

            TextRenderer.RenderText(GameLogic.ConvertCurrency(amount), x, y, amountColor, amountColor);
        }
    }
}