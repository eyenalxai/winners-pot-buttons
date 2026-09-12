using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace BlackJacket.WinnersPotButtons
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class WinnersPotButtonsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.blackjacket.mods.winnerspotbuttons";
        public const string PluginName = "Black Jacket - Winners Pot Buttons";
        public const string PluginVersion = "1.1.1";

        internal static ManualLogSource Log;
        internal static Settings Cfg;

        private void Awake()
        {
            Log = Logger;
            Cfg = new Settings(Config);

            var go = new GameObject("WinnersPotButtonsOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<WinnersPotButtonsOverlay>();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }
    }

    internal sealed class Settings
    {
        public readonly ConfigEntry<bool> Enabled;
        public readonly ConfigEntry<bool> ShowWinnersPotButtons;
        public readonly ConfigEntry<bool> ShowPlayerButtons;
        public readonly ConfigEntry<bool> IgnorePotCap;
        public readonly ConfigEntry<float> ButtonSize;
        public readonly ConfigEntry<float> Spacing;
        public readonly ConfigEntry<float> FontSize;
        public readonly ConfigEntry<float> Margin;
        public readonly ConfigEntry<float> NudgeX;
        public readonly ConfigEntry<float> NudgeY;

        public Settings(ConfigFile file)
        {
            Enabled = file.Bind("General", "Enabled", true,
                "Show the coin buttons in matches, shops and on the campaign map.");
            ShowWinnersPotButtons = file.Bind("General", "ShowWinnersPotButtons", true,
                "Show the buttons for the winners pot.");
            ShowPlayerButtons = file.Bind("General", "ShowPlayerButtons", true,
                "Show the buttons for your own coins (player's pot).");
            IgnorePotCap = file.Bind("General", "IgnorePotCap", false,
                "Allow adding coins beyond the winners pot size cap. The game may still trim the excess at the end of a round.");

            ButtonSize = file.Bind("Display", "ButtonSize", 34f, "Button size in pixels.");
            Spacing = file.Bind("Display", "Spacing", 4f, "Space between buttons in pixels.");
            FontSize = file.Bind("Display", "FontSize", 22f, "Font size of the button labels.");
            Margin = file.Bind("Display", "Margin", 6f, "Gap between the counter and the button grid, in pixels.");
            NudgeX = file.Bind("Display", "NudgeX", 0f, "Fine-tune the horizontal position of the buttons, in pixels.");
            NudgeY = file.Bind("Display", "NudgeY", 0f, "Fine-tune the vertical position of the buttons, in pixels.");
        }
    }
}
