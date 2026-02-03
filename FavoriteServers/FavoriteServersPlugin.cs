using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using FavoriteServers.UI;

namespace FavoriteServers
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class FavoriteServersPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.github.favoriteservers";
        public const string PluginName = "FavoriteServers";
        public const string PluginVersion = "1.0.0";

        public static ManualLogSource Log;
        public static FavoriteServersPlugin Instance;

        // Config entries
        public static ConfigEntry<KeyboardShortcut> ToggleHotkey;
        public static ConfigEntry<float> WindowPosX;
        public static ConfigEntry<float> WindowPosY;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // Initialize config
            ToggleHotkey = Config.Bind(
                "General",
                "ToggleHotkey",
                new KeyboardShortcut(KeyCode.F6),
                "Hotkey to open/close the favorites panel"
            );

            WindowPosX = Config.Bind(
                "UI",
                "WindowPosX",
                -1f,
                "X position of the favorites window (-1 for center)"
            );

            WindowPosY = Config.Bind(
                "UI",
                "WindowPosY",
                -1f,
                "Y position of the favorites window (-1 for center)"
            );

            // Initialize server manager
            ServerManager.Initialize();

            // Apply Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Register for GUI events
            GUIManager.OnCustomGUIAvailable += OnGUIAvailable;

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded!");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            GUIManager.OnCustomGUIAvailable -= OnGUIAvailable;
        }

        private void Update()
        {
            // Check hotkey only when in menu (not in gameplay)
            if (ToggleHotkey.Value.IsDown() && IsInMenu())
            {
                FavoritesPanel.Instance?.Toggle();
            }
        }

        private void OnGUIAvailable()
        {
            // Create UI panels when GUI becomes available
            FavoritesPanel.Create();
            ServerEditPanel.Create();
        }

        /// <summary>
        /// Check if we're in a menu state (not actively playing)
        /// </summary>
        public static bool IsInMenu()
        {
            // Not in menu if there's no FejdStartup and we have a local player
            if (FejdStartup.instance != null)
            {
                return true; // Main menu
            }

            // In game but check if menu is open
            if (Game.instance != null && Menu.instance != null)
            {
                return Menu.IsVisible();
            }

            // Character selection screen
            if (FejdStartup.instance == null && Player.m_localPlayer == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if we're at the main menu (FejdStartup)
        /// </summary>
        public static bool IsAtMainMenu()
        {
            return FejdStartup.instance != null;
        }
    }
}
