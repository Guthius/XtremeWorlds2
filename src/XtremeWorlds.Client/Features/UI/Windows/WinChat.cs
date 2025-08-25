﻿using Core.Configurations;
using Core.Globals;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.UI.Controls;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinChat
{
    public static void Initialize()
    {
        var window = WindowLoader.FromLayout("winChat");

        window.GetChild("chkGame").Click += OnGameChannelClicked;
        window.GetChild<CheckBox>("chkGame").IsChecked = SettingsManager.Instance.ChannelState[(int) ChatChannel.Game] == 1;
        window.GetChild("chkMap").Click += OnMapChannelClicked;
        window.GetChild<CheckBox>("chkMap").IsChecked = SettingsManager.Instance.ChannelState[(int) ChatChannel.Map] == 1;
        window.GetChild("chkGlobal").Click += OnBroadcastChannelClicked;
        window.GetChild<CheckBox>("chkGlobal").IsChecked = SettingsManager.Instance.ChannelState[(int) ChatChannel.Broadcast] == 1;
        window.GetChild("chkParty").Click += OnPartyChannelClicked;
        window.GetChild<CheckBox>("chkParty").IsChecked = SettingsManager.Instance.ChannelState[(int) ChatChannel.Party] == 1;
        window.GetChild("chkGuild").Click += OnGuildChannelClicked;
        window.GetChild<CheckBox>("chkGuild").IsChecked = SettingsManager.Instance.ChannelState[(int) ChatChannel.Guild] == 1;
        window.GetChild("chkPlayer").Click += OnPrivateChannelClicked;
        window.GetChild<CheckBox>("chkPlayer").IsChecked = SettingsManager.Instance.ChannelState[(int) ChatChannel.Private] == 1;
        window.GetChild("picNull").Draw += OnDraw;
        window.GetChild("btnChat").Click += OnSayClick;
        window.GetChild("btnUp").Click += OnUpButtonMouseDown;
        window.GetChild("btnUp").MouseUp += OnUpButtonMouseUp;
        window.GetChild("btnDown").Click += OnDownButtonMouseDown;
        window.GetChild("btnDown").MouseUp += OnDownButtonMouseUp;

        Gui.SetActiveControl(window, "txtChat");

        var window2 = WindowLoader.FromLayout("winChatSmall");

        window2.Draw += OnDrawSmall;
    }

    private static void OnSayClick()
    {
        GameLogic.HandlePressEnter();
    }

    private static void OnDraw()
    {
        var winIndex = Gui.GetWindowByName("winChat");
        if (winIndex is null)
        {
            return;
        }

        var x = winIndex.X;
        var y = winIndex.Y + 16;

        DesignRenderer.Render(Design.WindowDescription, x, y, 352, 152);

        var path = Path.Combine(DataPath.Gui, 46.ToString());

        GameClient.RenderTexture(ref path, x + 7, y + 123, 0, 0, 171, 22, 171, 22);
        GameClient.RenderTexture(ref path, x + 174, y + 123, 0, 22, 171, 22, 171, 22);

        TextRenderer.DrawChat();
    }

    private static void OnDrawSmall()
    {
        var winChatSmall = Gui.GetWindowByName("winChatSmall");
        if (winChatSmall is null)
        {
            return;
        }

        if (GameState.ActChatWidth < 160)
        {
            GameState.ActChatWidth = 160;
        }

        if (GameState.ActChatHeight < 10)
        {
            GameState.ActChatHeight = 10;
        }

        var x = winChatSmall.X + 10;
        var y = GameState.ResolutionHeight - 10;

        DesignRenderer.Render(Design.WindowWithShadow, x, y, 160, 10);
    }

    private static void UpdateChatChannel(string checkBoxName, ChatChannel channel)
    {
        var winChat = Gui.GetWindowByName("winChat");

        var checkBox = winChat?.GetChild<CheckBox>(checkBoxName);
        if (checkBox is null)
        {
            return;
        }

        SettingsManager.Instance.ChannelState[(int) channel] = (byte) (checkBox.IsChecked ? 1 : 0);
        SettingsManager.Save();
    }

    private static void OnGameChannelClicked()
    {
        UpdateChatChannel("chkGame", ChatChannel.Game);
    }

    private static void OnMapChannelClicked()
    {
        UpdateChatChannel("chkMap", ChatChannel.Map);
    }

    private static void OnBroadcastChannelClicked()
    {
        UpdateChatChannel("chkGlobal", ChatChannel.Broadcast);
    }

    private static void OnPartyChannelClicked()
    {
        UpdateChatChannel("chkParty", ChatChannel.Party);
    }

    private static void OnGuildChannelClicked()
    {
        UpdateChatChannel("chkGuild", ChatChannel.Guild);
    }

    private static void OnPrivateChannelClicked()
    {
        UpdateChatChannel("chkPlayer", ChatChannel.Private);
    }

    private static void OnUpButtonMouseDown()
    {
        GameState.ChatButtonUp = true;
    }

    private static void OnUpButtonMouseUp()
    {
        GameState.ChatButtonUp = false;
    }

    private static void OnDownButtonMouseDown()
    {
        GameState.ChatButtonDown = true;
    }

    private static void OnDownButtonMouseUp()
    {
        GameState.ChatButtonDown = false;
    }

    public static void Show()
    {
        var winChat = Gui.GetWindowByName("winChat");
        if (winChat is null)
        {
            return;
        }

        Gui.ShowWindow(winChat, resetPosition: false);
        Gui.MoveToFront(winChat);

        Gui.HideWindow("winChatSmall");

        winChat.GetChild("txtChat").Visible = true;

        Gui.SetActiveControl(winChat, "txtChat");

        GameState.InSmallChat = false;
        GameState.ChatScroll = 0;
    }

    public static void Hide()
    {
        var winChat = Gui.GetWindowByName("winChat");
        if (winChat is null)
        {
            return;
        }

        var winChatSmall = Gui.GetWindowByName("winChatSmall");
        if (winChatSmall is null)
        {
            return;
        }

        Gui.ShowWindow(winChatSmall, resetPosition: false);
        Gui.HideWindow(winChat);

        winChat.GetChild("txtChat").Visible = false;

        GameState.InSmallChat = true;
        GameState.ChatScroll = 0;
    }
}