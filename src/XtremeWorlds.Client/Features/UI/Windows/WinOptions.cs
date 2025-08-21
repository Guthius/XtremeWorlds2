using Core.Configurations;
using Core.Globals;
using XtremeWorlds.Client.Features.Objects;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.UI.Controls;

namespace XtremeWorlds.Client.Features.UI.Windows;

public static class WinOptions
{
    public static void Initialize()
    {
        var window = WindowLoader.FromLayout("winOptions");

        window.GetChild("btnConfirm").Click += OnConfirm;

        GameLogic.SetOptionsScreen();
    }

    private static void OnConfirm()
    {
        var restartRequired = false;

        var winOptions = Gui.GetWindowByName("winOptions");
        if (winOptions is null)
        {
            return;
        }

        var checkBoxMusic = winOptions.GetChild<CheckBox>("chkMusic");
        var checkBoxSound = winOptions.GetChild<CheckBox>("chkSound");
        var checkBoxAutoTile = winOptions.GetChild<CheckBox>("chkAutotile");
        var checkBoxFullscreen = winOptions.GetChild<CheckBox>("chkFullscreen");
        var comboBoxResolution = winOptions.GetChild<ComboBox>("cmbRes");

        // Music
        var enabled = checkBoxMusic.IsChecked;
        if (SettingsManager.Instance.Music != enabled)
        {
            SettingsManager.Instance.Music = enabled;

            if (!enabled)
            {
                TextRenderer.AddText("Music turned off.", (int) ColorName.BrightGreen);

                Sound.StopMusic();
            }
            else
            {
                TextRenderer.AddText("Music tured on.", (int) ColorName.BrightGreen);

                var music = GameState.InGame ? Data.MyMap.Music : SettingsManager.Instance.Music.ToString();
                if (music != "None.")
                {
                    Sound.PlayMusic(music);
                }
                else
                {
                    Sound.StopMusic();
                }
            }
        }

        // Sound
        enabled = checkBoxSound.IsChecked;
        if (SettingsManager.Instance.Sound != enabled)
        {
            SettingsManager.Instance.Sound = enabled;

            TextRenderer.AddText(!enabled ? "Sound turned off." : "Sound tured on.", (int) ColorName.BrightGreen);
        }


        // autotiles
        enabled = checkBoxAutoTile.IsChecked;
        if (SettingsManager.Instance.Autotile != enabled)
        {
            SettingsManager.Instance.Autotile = enabled;
            if (!enabled)
            {
                if (GameState.InGame)
                {
                    TextRenderer.AddText("Autotiles turned off.", (int) ColorName.BrightGreen);
                    Autotile.InitAutotiles();
                }
            }
            else if (GameState.InGame)
            {
                TextRenderer.AddText("Autotiles turned on.", (int) ColorName.BrightGreen);
                Autotile.InitAutotiles();
            }
        }


        // Fullscreen
        enabled = checkBoxFullscreen.IsChecked;
        if (SettingsManager.Instance.Fullscreen != enabled)
        {
            SettingsManager.Instance.Fullscreen = enabled;

            restartRequired = true;
        }

        // Resolution
        if (comboBoxResolution.SelectedIndex > 0 & comboBoxResolution.SelectedIndex <= 13)
        {
            SettingsManager.Instance.Resolution = (byte) comboBoxResolution.SelectedIndex;

            restartRequired = true;
        }

        SettingsManager.Save();

        if (GameState.InGame && restartRequired)
        {
            TextRenderer.AddText("Some changes will take effect next time you load the game.", (int) ColorName.BrightGreen);
        }

        OnClose();
    }

    private static void OnClose()
    {
        Gui.HideWindow("winOptions");
        Gui.ShowWindow("winEscMenu");
    }
}