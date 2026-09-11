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
        public const string PluginVersion = "1.0.0";

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
        public readonly ConfigEntry<int> CoinsPerClick;
        public readonly ConfigEntry<bool> IgnorePotCap;
        public readonly ConfigEntry<float> ButtonSize;
        public readonly ConfigEntry<float> Spacing;
        public readonly ConfigEntry<float> FontSize;
        public readonly ConfigEntry<float> OffsetX;
        public readonly ConfigEntry<float> OffsetY;

        public Settings(ConfigFile file)
        {
            Enabled = file.Bind("General", "Enabled", true,
                "Show the + / - buttons for the winners pot during a match.");
            CoinsPerClick = file.Bind("General", "CoinsPerClick", 1,
                "How many coins one click adds or removes.");
            IgnorePotCap = file.Bind("General", "IgnorePotCap", false,
                "Allow adding coins beyond the winners pot size cap (the game still trims the excess at the end of the round).");

            ButtonSize = file.Bind("Display", "ButtonSize", 46f, "Button size in pixels.");
            Spacing = file.Bind("Display", "Spacing", 8f, "Space between the two buttons in pixels.");
            FontSize = file.Bind("Display", "FontSize", 32f, "Font size of the + / - labels.");
            OffsetX = file.Bind("Display", "OffsetX", 0f, "Horizontal offset from the winners pot counter, in pixels.");
            OffsetY = file.Bind("Display", "OffsetY", -52f, "Vertical offset from the winners pot counter, in pixels.");
        }
    }
}