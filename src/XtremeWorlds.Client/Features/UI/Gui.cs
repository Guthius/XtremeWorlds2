using System.Diagnostics;
using Core.Globals;
using Microsoft.VisualBasic.CompilerServices;
using Serilog;
using XtremeWorlds.Client.Features.Objects;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.UI.Controls;
using XtremeWorlds.Client.Features.UI.Windows;
using static Core.Globals.Command;
using Type = Core.Globals.Type;

namespace XtremeWorlds.Client.Features.UI;

public static class Gui
{
    public static List<Window> Windows { get; } = [];
    public static Window? ActiveWindow => Windows.FirstOrDefault(window => window is {Visible: true, CanFocus: true});
    public static Control? ActiveControl => ActiveWindow?.ActiveControl;

    public static Type.ControlPart DragBox;

    // Declare a timer to control when dragging can begin
    private static readonly Stopwatch DragTimer = new();
    private const double DragInterval = 100d; // Set the interval in milliseconds to start dragging
    private static bool _canDrag; // Flag to control when dragging is allowed
    private static bool _isDragging;

    /// <summary>
    /// Moves the specified <paramref name="window"/> to the front.
    /// </summary>
    /// <param name="window">The window to move to the front.</param>
    public static void MoveToFront(Window window)
    {
        var index = Windows.IndexOf(window);
        if (index == -1)
        {
            return;
        }

        for (var i = index; i > 0; i--)
        {
            Windows[i] = Windows[i - 1];
        }

        Windows[0] = window;
    }

    /// <summary>
    /// Moves the window at the specified index to be centered on screen.
    /// </summary>
    /// <param name="windowIndex">The index of the window to move.</param>
    public static void MoveToCenterScreen(int windowIndex)
    {
        var window = Windows[windowIndex];

        window.X = (GameState.ResolutionWidth - window.Width) / 2;
        window.Y = (GameState.ResolutionHeight - window.Height) / 2;
        window.InitialX = window.X;
        window.InitialY = window.Y;
    }

    /// <summary>
    /// Gets the window with the specified name.
    /// </summary>
    /// <param name="windowName">The name of the window.</param>
    /// <returns>The window if it exists; otherwise, null.</returns>
    public static Window? GetWindowByName(string windowName)
    {
        return Windows.ToArray()
            .FirstOrDefault(window =>
                string.Equals(window.Name, windowName,
                    StringComparison.OrdinalIgnoreCase));
    }

    public static void Register(Window window)
    {
        Windows.Add(window);
    }

    public static int GetWindowIndex(string windowName)
    {
        for (var i = 0; i < Windows.Count; i++)
        {
            if (string.Equals(Windows[i].Name, windowName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static int GetControlIndex(string window, string controlName)
    {
        var index = GetWindowIndex(window);

        for (var i = 0; i < Windows[index].Controls.Count; i++)
        {
            if (string.Equals(Windows[index].Controls[i].Name, controlName, StringComparison.CurrentCultureIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public static bool SetActiveControl(Window window, string controlName)
    {
        var controlIndex = GetControlIndex(window.Name, controlName);

        switch (window.Controls[controlIndex])
        {
            case TextBox:
                window.LastControl = window.ActiveControl;
                window.ActiveControl = window.Controls[controlIndex];
                return true;
        }

        return false;
    }

    public static void HideWindows()
    {
        var windows = Windows.ToArray();
        foreach (var window in windows)
        {
            HideWindow(window);
        }
    }

    public static void ShowWindow(int windowIndex, bool forceFocus = false, bool resetPosition = true)
    {
        if (windowIndex == 0)
        {
            return;
        }

        ShowWindow(Windows[windowIndex], forceFocus, resetPosition);
    }

    public static void ShowWindow(Window window, bool forceFocus = false, bool resetPosition = true)
    {
        window.Visible = true;

        if (window.CanFocus || forceFocus)
        {
            MoveToFront(window);
        }

        if (!resetPosition)
        {
            return;
        }

        window.X = window.InitialX;
        window.Y = window.InitialY;
    }

    public static void HideWindow(string windowName)
    {
        var window = GetWindowByName(windowName);
        if (window is not null)
        {
            HideWindow(window);
        }
    }

    public static void ShowWindow(string windowName, bool forceFocus = false, bool resetPosition = true)
    {
        var window = GetWindowByName(windowName);
        if (window is null)
        {
            Log.Warning("Unable to show window '{WindowName}' (window not found)", windowName);
            return;
        }

        ShowWindow(window, forceFocus, resetPosition);
    }

    public static void HideWindow(int windowIndex)
    {
        HideWindow(Windows[windowIndex]);
    }

    public static void HideWindow(Window window)
    {
        window.Visible = false;
    }

    public static void Init()
    {
        FontManager.Reset();
        FontManager.Load("Coolvetica", "Coolvetica Rg.otf");

        Windows.Clear();

        WinRegister.Initialize();
        WinLogin.Initialize();
        WinNewChar.Initialize();
        WinJobs.Initialize();
        WinChars.Initialize();
        WinChat.Initialize();
        WinMenu.Initialize();
        WinDescription.Initialize();
        WinInventory.Initialize();
        WinSkills.Initialize();
        WinCharacter.Initialize();
        WinHotBar.Initialize();
        WinBank.Initialize();
        WinShop.Initialize();
        WinEscMenu.Initialize();
        WinBars.Initialize();
        WinDialogue.Initialize();
        WinDragBox.Initialize();
        WinOptions.Initialize();
        WinTrade.Initialize();
        WinParty.Initialize();
    }

    private static Component? _mouseCaptureTarget;

    public static void CaptureMouse(Component target)
    {
        // If the mouse is currently captured, do not allow another component to take over.
        // This should never happen. If it does it indicates a problem with the implementation of a control.
        if (_mouseCaptureTarget is not null &&
            _mouseCaptureTarget != target)
        {
            throw new UIException($"Unable to set mouse capture target, mouse already captured by {_mouseCaptureTarget}");
        }

        _mouseCaptureTarget = target;
    }

    public static void ReleaseMouse()
    {
        _mouseCaptureTarget = null;
    }

    public static void OnMouseMoved(int x, int y)
    {
        if (_mouseCaptureTarget is not null)
        {
            (x, y) = _mouseCaptureTarget.PointToLocal(x, y);

            _mouseCaptureTarget.HandleMouseMove(x, y);
            return;
        }

        var windows = Windows.ToArray();
        foreach (var window in windows)
        {
            if (!window.Visible || !window.Contains(x, y))
            {
                continue;
            }

            var (localX, localY) = window.PointToLocal(x, y);

            window.HandleMouseMove(localX, localY);
            return;
        }
    }

    public static void OnMousePressed(int x, int y)
    {
        if (_mouseCaptureTarget is not null)
        {
            (x, y) = _mouseCaptureTarget.PointToLocal(x, y);

            _mouseCaptureTarget.HandleMousePressed(x, y);
            return;
        }

        var windows = Windows.ToArray();
        foreach (var window in windows)
        {
            if (!window.Visible || window.Clickthrough || !window.Contains(x, y))
            {
                continue;
            }

            var (localX, localY) = window.PointToLocal(x, y);

            window.HandleMousePressed(localX, localY);
            return;
        }
    }

    public static void OnMouseReleased(int x, int y)
    {
        if (_mouseCaptureTarget is not null)
        {
            (x, y) = _mouseCaptureTarget.PointToLocal(x, y);

            _mouseCaptureTarget.HandleMouseReleased(x, y);
            return;
        }

        var windows = Windows.ToArray();
        foreach (var window in windows)
        {
            if (!window.Visible || window.Clickthrough || !window.Contains(x, y))
            {
                continue;
            }

            var (localX, localY) = window.PointToLocal(x, y);

            window.HandleMouseReleased(localX, localY);
            return;
        }
    }

    public static void HandleInterfaceEvents(ControlState entState)
    {
        // Window? curWindow = null;
        // var curControl = 0;
        //
        // // Check for MouseDown to start the drag timer
        // if (GameClient.IsMouseButtonDown(MouseButton.Left) && GameClient.PreviousMouseState.LeftButton == ButtonState.Released)
        // {
        //     DragTimer.Restart(); // Start the timer on initial mouse down
        //     _canDrag = false; // Reset drag flag to ensure it doesn't drag immediately
        // }
        //
        // // Check for MouseUp to reset dragging
        // if (GameClient.IsMouseButtonUp(MouseButton.Left))
        // {
        //     _isDragging = false;
        //
        //     DragTimer.Reset(); // Stop the timer on mouse up
        // }
        //
        // // Enable dragging if the mouse has been held down for the specified interval
        // _canDrag = DragTimer.ElapsedMilliseconds >= DragInterval;
        //
        // lock (GameClient.InputLock)
        // {
        //     foreach (var window in Windows.Values)
        //     {
        //         if (!window.Visible)
        //         {
        //             continue;
        //         }
        //
        //         if (window.State != ControlState.MouseDown)
        //         {
        //             window.State = ControlState.Normal;
        //         }
        //
        //         if (GameState.CurMouseX >= window.X &&
        //             GameState.CurMouseX <= window.Width + window.X &&
        //             GameState.CurMouseY >= window.Y &&
        //             GameState.CurMouseY <= window.Height + window.Y)
        //         {
        //             // Handle combo menu logic
        //             if (window.Design[0] == Design.ComboMenuNormal)
        //             {
        //                 switch (entState)
        //                 {
        //                     case ControlState.MouseMove or ControlState.Hover:
        //                         ComboMenu_MouseMove(window);
        //                         break;
        //
        //                     case ControlState.MouseDown:
        //                         ComboMenu_MouseDown(window);
        //                         break;
        //                 }
        //             }
        //
        //             // Track the top-most window
        //             if (curWindow is null || window.ZOrder > curWindow.ZOrder)
        //             {
        //                 curWindow = window;
        //
        //                 _isDragging = true;
        //             }
        //
        //             if (ActiveWindow is not null)
        //             {
        //                 if (!ActiveWindow.Visible || !ActiveWindow.CanDrag)
        //                 {
        //                     ActiveWindow = curWindow;
        //                 }
        //             }
        //             else
        //             {
        //                 ActiveWindow = curWindow;
        //             }
        //         }
        //
        //         if (entState != ControlState.MouseMove || !GameClient.IsMouseButtonDown(MouseButton.Left))
        //         {
        //             continue;
        //         }
        //
        //         if (ActiveWindow is not null && _isDragging)
        //         {
        //             if (_canDrag && ActiveWindow is {CanDrag: true, Visible: true})
        //             {
        //                 ActiveWindow.X = GameLogic.Clamp(
        //                     ActiveWindow.X +
        //                     (GameState.CurMouseX - ActiveWindow.X - ActiveWindow.MovedX), 0,
        //                     GameState.ResolutionWidth - ActiveWindow.Width);
        //                 ActiveWindow.Y = GameLogic.Clamp(
        //                     ActiveWindow.Y +
        //                     (GameState.CurMouseY - ActiveWindow.Y - ActiveWindow.MovedY), 0,
        //                     GameState.ResolutionHeight - ActiveWindow.Height);
        //                 break;
        //             }
        //         }
        //     }
        //
        //     if (curWindow is not null)
        //     {
        //         // Handle the active window's callback
        //         var callBack = curWindow.CallBack[(int) entState];
        //
        //         // Execute the callback if it exists
        //         callBack?.Invoke();
        //
        //         // Handle controls in the active window
        //         for (var i = 0; i < curWindow.Controls.Count; i++)
        //         {
        //             var control = curWindow.Controls[i];
        //
        //             if (control is {Enabled: true, Visible: true})
        //             {
        //                 if (GameState.CurMouseX >= control.X + curWindow.X &&
        //                     GameState.CurMouseX <= control.X + control.Width + curWindow.X &&
        //                     GameState.CurMouseY >= control.Y + curWindow.Y &&
        //                     GameState.CurMouseY <= control.Y + control.Height + curWindow.Y)
        //                 {
        //                     if (curControl == 0L || control.ZOrder > curWindow.Controls[curControl].ZOrder)
        //                     {
        //                         curControl = i;
        //                     }
        //                 }
        //             }
        //         }
        //
        //         if (curControl > 0)
        //         {
        //             // Reset all control states
        //             for (var j = 0; j < curWindow.Controls.Count; j++)
        //             {
        //                 if (curControl != j)
        //                 {
        //                     curWindow.Controls[j].State = ControlState.Normal;
        //                 }
        //             }
        //
        //             var withBlock2 = curWindow.Controls[curControl];
        //
        //             withBlock2.State = entState switch
        //             {
        //                 ControlState.MouseMove => ControlState.Hover,
        //                 ControlState.MouseDown => ControlState.MouseDown,
        //                 _ => withBlock2.State
        //             };
        //
        //             // Handle specific control types
        //             switch (withBlock2)
        //             {
        //                 case CheckBox checkBox:
        //                 {
        //                     if (checkBox.Group > 0 && withBlock2.Value == 0)
        //                     {
        //                         foreach (var control in curWindow.Controls.OfType<CheckBox>())
        //                         {
        //                             if (control != checkBox && control.Group == checkBox.Group)
        //                             {
        //                                 control.Value = 0;
        //                             }
        //                         }
        //
        //                         withBlock2.Value = 0;
        //                     }
        //
        //                     break;
        //                 }
        //
        //                 case ComboBox:
        //                     WinComboMenu.Show(curWindow, curControl);
        //                     break;
        //             }
        //
        //             if (GameClient.IsMouseButtonDown(MouseButton.Left))
        //             {
        //                 SetActiveControl(curWindow, curControl);
        //             }
        //
        //             callBack = withBlock2.GetCallbackForState(entState);
        //             callBack?.Invoke();
        //         }
        //     }
        //
        //     if (curWindow is null)
        //     {
        //         ResetInterface();
        //     }
        //
        //     if (entState == ControlState.MouseUp)
        //     {
        //         ResetMouseDown();
        //     }
        // }
    }

    public static void Render()
    {
        var windows = Windows.Where(x => x.Visible).ToArray();
        if (windows.Length == 0)
        {
            return;
        }

        for (var i = windows.Length - 1; i >= 0; i--)
        {
            windows[i].Render();
        }
    }

    private static void ComboMenu_MouseMove(Window window)
    {
        var y = GameState.CurMouseY - window.Y;

        for (var i = 0; i < window.List.Count - 1; i++)
        {
            if (y >= 16 * i && y <= 16 * i)
            {
                window.Group = i;
            }
        }
    }

    private static void ComboMenu_MouseDown(Window window)
    {
        if (window.List.Count == 0)
        {
            return;
        }

        var y = GameState.CurMouseY - window.Y;
        for (var i = 0; i < window.List.Count; i++)
        {
            if (y < 16 * i || y > 16 * i)
            {
                continue;
            }

            if (window.ParentControl is not null)
            {
                // TODO: window.ParentControl.Value = i;
            }

            WinComboMenu.Close();
            break;
        }
    }

    public static void ResizeGui()
    {
        // move Hotbar
        Windows[GetWindowIndex("winHotbar")].X = GameState.ResolutionWidth - 432;

        // move chat
        Windows[GetWindowIndex("winChat")].Y = GameState.ResolutionHeight - 178;
        Windows[GetWindowIndex("winChatSmall")].Y = GameState.ResolutionHeight - 162;

        // move menu
        Windows[GetWindowIndex("winMenu")].X = GameState.ResolutionWidth - 238;
        Windows[GetWindowIndex("winMenu")].Y = GameState.ResolutionHeight - 42;
    }

    public static void DrawMenuBackground()
    {
        var path = Path.Combine(DataPath.Pictures, "1");

        GameClient.RenderTexture(
            path: ref path,
            dX: 0, dY: 0, sX: 0, sY: 0,
            dW: 1920, dH: 1080,
            sW: 1920, sH: 1080);
    }

    public static void DrawYourTrade()
    {
        var color = 0;

        var winTrade = GetWindowByName("winTrade");
        if (winTrade is null)
        {
            return;
        }

        var picYour = winTrade.GetChild("picYour");

        var xo = winTrade.X + picYour.X;
        var yo = winTrade.Y + picYour.Y;

        // your items
        for (var i = 0; i < Constant.MaxInv; i++)
        {
            if (Data.TradeYourOffer[i].Num < 0)
            {
                continue;
            }

            var itemNum = GetPlayerInv(GameState.MyIndex, Data.TradeYourOffer[i].Num);
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

            var top = yo + GameState.TradeTop + (GameState.TradeOffsetY + 32) * (i / GameState.TradeColumns);
            var left = xo + GameState.TradeLeft + (GameState.TradeOffsetX + 32) * (i % GameState.TradeColumns);

            var path = Path.Combine(DataPath.Items, itemIcon.ToString());

            GameClient.RenderTexture(ref path, left, top, 0, 0, 32, 32, 32, 32);

            // If item is a stack - draw the amount you have
            if (Data.TradeYourOffer[i].Value > 1)
            {
                var y = top + 20;
                var x = left + 1;
                var amount = Data.TradeYourOffer[i].Value.ToString();

                // Draw currency but with k, m, b etc. using a convertion function
                if (Conversions.ToLong(amount) < 1000000L)
                {
                    color = (int) ColorName.White;
                }
                else if (Conversions.ToLong(amount) > 1000000L & Conversions.ToLong(amount) < 10000000L)
                {
                    color = (int) ColorName.Yellow;
                }
                else if (Conversions.ToLong(amount) > 10000000L)
                {
                    color = (int) ColorName.BrightGreen;
                }

                TextRenderer.RenderText(GameLogic.ConvertCurrency(Conversions.ToInteger(amount)), x, y, GameClient.QbColorToXnaColor(color), GameClient.QbColorToXnaColor(color));
            }
        }
    }

    public static void DrawTheirTrade()
    {
        var color = 0;

        var xo = Windows[GetWindowIndex("winTrade")].X + Windows[GetWindowIndex("winTrade")].Controls[GetControlIndex("winTrade", "picTheir")].X;
        var yo = Windows[GetWindowIndex("winTrade")].Y + Windows[GetWindowIndex("winTrade")].Controls[GetControlIndex("winTrade", "picTheir")].Y;

        // their items
        for (var i = 0; i < Constant.MaxInv; i++)
        {
            var itemNum = Data.TradeTheirOffer[i].Num;
            if (itemNum >= 0 && itemNum < Constant.MaxItems)
            {
                Item.StreamItem(itemNum);
                var itemPic = Data.Item[itemNum].Icon;

                if (itemPic > 0 && itemPic <= GameState.NumItems)
                {
                    var top = yo + GameState.TradeTop + (GameState.TradeOffsetY + 32L) * (i / GameState.TradeColumns);
                    var left = xo + GameState.TradeLeft + (GameState.TradeOffsetX + 32L) * (i % GameState.TradeColumns);

                    // draw icon
                    var argpath = Path.Combine(DataPath.Items, itemPic.ToString());
                    GameClient.RenderTexture(ref argpath, (int) left, (int) top, 0, 0, 32, 32, 32, 32);

                    // If item is a stack - draw the amount you have
                    if (Data.TradeTheirOffer[i].Value > 1)
                    {
                        var y = top + 20L;
                        var x = left + 1L;
                        var amount = Data.TradeTheirOffer[i].Value.ToString();

                        // Draw currency but with k, m, b etc. using a convertion function
                        if (Conversions.ToLong(amount) < 1000000L)
                        {
                            color = (int) ColorName.White;
                        }
                        else if (Conversions.ToLong(amount) > 1000000L & Conversions.ToLong(amount) < 10000000L)
                        {
                            color = (int) ColorName.Yellow;
                        }
                        else if (Conversions.ToLong(amount) > 10000000L)
                        {
                            color = (int) ColorName.BrightGreen;
                        }

                        TextRenderer.RenderText(GameLogic.ConvertCurrency(Conversions.ToInteger(amount)), (int) x, (int) y, GameClient.QbColorToXnaColor(color), GameClient.QbColorToXnaColor(color));
                    }
                }
            }
        }
    }

    public static void UpdateActiveControl(Control modifiedControl)
    {
        if (ActiveWindow?.ActiveControl is not null)
        {
            var index = ActiveWindow.Controls.IndexOf(ActiveWindow.ActiveControl);

            // Update the control within the active window's Controls array
            ActiveWindow.Controls[index] = modifiedControl;
        }
    }

    public static void FocusNextControl()
    {
        if (ActiveWindow?.Controls is not {Count: > 0})
        {
            return;
        }

        var controls = ActiveWindow.Controls;
        var currentIndex = ActiveWindow.ActiveControl is null ? -1 : controls.IndexOf(ActiveWindow.ActiveControl);
        var nextIndex = (currentIndex + 1) % controls.Count;

        while (nextIndex != currentIndex)
        {
            var control = controls[nextIndex];
            if (control is {Enabled: true, Visible: true} and TextBox)
            {
                ActiveWindow.ActiveControl = control;
                return;
            }

            nextIndex = (nextIndex + 1) % controls.Count;
        }
    }
}