using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using FavoriteServers.UI;
using TMPro;

namespace FavoriteServers.Patches
{
    /// <summary>
    /// Patches to add Favorites button to the main menu
    /// </summary>
    [HarmonyPatch]
    public static class MenuPatches
    {
        private static GameObject _favoritesButton;

        /// <summary>
        /// Add Favorites button when the main menu loads
        /// </summary>
        [HarmonyPatch(typeof(FejdStartup), "Awake")]
        [HarmonyPostfix]
        public static void FejdStartup_Awake_Postfix(FejdStartup __instance)
        {
            // Delay the button creation to ensure UI is ready
            __instance.StartCoroutine(DelayedAddButton(__instance));
        }

        private static System.Collections.IEnumerator DelayedAddButton(FejdStartup instance)
        {
            // Wait several frames for UI to fully initialize
            yield return null;
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.5f);
            AddFavoritesButton(instance);
        }

        private static void AddFavoritesButton(FejdStartup instance)
        {
            if (_favoritesButton != null) return;

            try
            {
                // Log the hierarchy to help debug
                FavoriteServersPlugin.Log.LogInfo("Searching for menu buttons...");

                // Search the entire scene for buttons
                var allButtons = Object.FindObjectsOfType<Button>(true);
                FavoriteServersPlugin.Log.LogInfo($"Found {allButtons.Length} buttons total");

                Button templateButton = null;
                string buttonText = "";

                foreach (var btn in allButtons)
                {
                    var path = btn.transform.GetPath();

                    // Skip buttons that are in submenus (like StartGame/Panel/Join)
                    if (path.Contains("/Panel/") || path.Contains("/ServerList/"))
                    {
                        continue;
                    }

                    // Check for TextMeshPro text (Valheim uses TMP)
                    var tmpText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
                    {
                        var text = tmpText.text.ToLower();
                        FavoriteServersPlugin.Log.LogInfo($"Checking button: '{tmpText.text}' at {path}");

                        // Look for MAIN menu buttons specifically (START GAME, SETTINGS, QUIT)
                        // These are typically direct children of the menu, not in subpanels
                        if (text.Contains("start game") || text.Contains("settings") || text.Contains("quit") || text.Contains("credits"))
                        {
                            templateButton = btn;
                            buttonText = tmpText.text;
                            FavoriteServersPlugin.Log.LogInfo($"Found MAIN MENU button: '{tmpText.text}' at {path}");

                            // Prefer "start game" button specifically
                            if (text.Contains("start game"))
                            {
                                break;
                            }
                        }
                    }

                    // Also check legacy Text component
                    var legacyText = btn.GetComponentInChildren<Text>(true);
                    if (legacyText != null && !string.IsNullOrEmpty(legacyText.text))
                    {
                        var text = legacyText.text.ToLower();
                        if (text.Contains("start game") || text.Contains("settings") || text.Contains("quit"))
                        {
                            templateButton = btn;
                            buttonText = legacyText.text;
                            FavoriteServersPlugin.Log.LogInfo($"Found MAIN MENU button (legacy): '{legacyText.text}'");
                        }
                    }
                }

                if (templateButton == null)
                {
                    FavoriteServersPlugin.Log.LogWarning("Could not find a suitable template button");
                    return;
                }

                FavoriteServersPlugin.Log.LogInfo($"Using template button: '{buttonText}'");

                // Clone the button
                _favoritesButton = Object.Instantiate(templateButton.gameObject, templateButton.transform.parent);
                _favoritesButton.name = "FavoritesButton";

                // Remove any scripts that might cause unwanted behavior (like navigation)
                // Keep only Button, RectTransform, Image, and text components
                var componentsToRemove = _favoritesButton.GetComponents<MonoBehaviour>();
                foreach (var comp in componentsToRemove)
                {
                    // Keep Button component, destroy others that might trigger navigation
                    if (!(comp is Button) && !(comp is UnityEngine.UI.Image) && !(comp is Selectable))
                    {
                        var typeName = comp.GetType().Name;
                        if (typeName != "Button" && typeName != "Image")
                        {
                            FavoriteServersPlugin.Log.LogInfo($"Removing component: {typeName}");
                            Object.Destroy(comp);
                        }
                    }
                }

                // Position it - try to place it after the template
                var rect = _favoritesButton.GetComponent<RectTransform>();
                var templateRect = templateButton.GetComponent<RectTransform>();

                // Adjust position based on layout
                var siblingIndex = templateButton.transform.GetSiblingIndex();
                _favoritesButton.transform.SetSiblingIndex(siblingIndex + 1);

                // Also adjust anchored position if needed
                if (rect != null && templateRect != null)
                {
                    rect.anchoredPosition = templateRect.anchoredPosition - new Vector2(0, 40f);
                }

                // Update text - try TMP first, then legacy
                var newTmpText = _favoritesButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (newTmpText != null)
                {
                    newTmpText.text = "Favorites";
                }
                else
                {
                    var newLegacyText = _favoritesButton.GetComponentInChildren<Text>(true);
                    if (newLegacyText != null)
                    {
                        newLegacyText.text = "Favorites";
                    }
                }

                // Clear ALL old listeners and add only ours
                var button = _favoritesButton.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent(); // Create fresh event
                button.onClick.AddListener(() =>
                {
                    FavoriteServersPlugin.Log.LogInfo("Favorites button clicked!");
                    FavoritesPanel.Instance?.Show();
                });

                _favoritesButton.SetActive(true);
                FavoriteServersPlugin.Log.LogInfo("Favorites button added to main menu!");
            }
            catch (System.Exception ex)
            {
                FavoriteServersPlugin.Log.LogError($"Failed to add Favorites button: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Hide the favorites panel when entering the game
        /// </summary>
        [HarmonyPatch(typeof(FejdStartup), "OnWorldStart")]
        [HarmonyPrefix]
        public static void FejdStartup_OnWorldStart_Prefix()
        {
            FavoritesPanel.Instance?.Hide();
            ServerEditPanel.Instance?.Hide();
        }

        /// <summary>
        /// Clean up when returning to main menu
        /// </summary>
        [HarmonyPatch(typeof(FejdStartup), "OnDestroy")]
        [HarmonyPrefix]
        public static void FejdStartup_OnDestroy_Prefix()
        {
            _favoritesButton = null;
        }

        /// <summary>
        /// Reset connection state when returning to the main menu (backing out completely)
        /// </summary>
        [HarmonyPatch(typeof(FejdStartup), "ShowStartGame")]
        [HarmonyPostfix]
        public static void FejdStartup_ShowStartGame_Postfix()
        {
            // Only reset when returning to the Start Game menu (user backed out)
            ConnectionManager.ResetState();
        }

        /// <summary>
        /// Auto-select character when connecting if one is saved for this server
        /// </summary>
        [HarmonyPatch(typeof(FejdStartup), "ShowCharacterSelection")]
        [HarmonyPostfix]
        public static void FejdStartup_ShowCharacterSelection_Postfix(FejdStartup __instance)
        {
            // Check if we're connecting via our mod with a saved character
            var server = ConnectionManager.ConnectingServer;
            if (server == null || string.IsNullOrEmpty(server.CharacterName))
            {
                return; // No auto-select
            }

            FavoriteServersPlugin.Log.LogInfo($"Auto-selecting character: {server.CharacterName}");

            // Verify character exists
            if (!ServerManager.CharacterExists(server.CharacterName))
            {
                FavoriteServersPlugin.Log.LogWarning($"Saved character '{server.CharacterName}' not found, showing selection");
                return;
            }

            // Use coroutine to let the UI initialize first
            __instance.StartCoroutine(AutoSelectCharacterCoroutine(__instance, server.CharacterName));
        }

        private static System.Collections.IEnumerator AutoSelectCharacterCoroutine(FejdStartup fejd, string characterName)
        {
            // Wait a frame for the UI to fully initialize
            yield return null;
            yield return null;

            // Get m_profiles field via reflection
            var profilesField = AccessTools.Field(typeof(FejdStartup), "m_profiles");
            if (profilesField == null)
            {
                FavoriteServersPlugin.Log.LogError("Could not find m_profiles field");
                yield break;
            }

            var profiles = profilesField.GetValue(fejd) as System.Collections.Generic.List<PlayerProfile>;
            if (profiles == null || profiles.Count == 0)
            {
                FavoriteServersPlugin.Log.LogWarning("No profiles loaded");
                yield break;
            }

            FavoriteServersPlugin.Log.LogInfo($"Found {profiles.Count} player profiles");

            // Find the matching profile index (case-insensitive)
            int targetIndex = -1;
            for (int i = 0; i < profiles.Count; i++)
            {
                if (string.Equals(profiles[i].GetName(), characterName, System.StringComparison.OrdinalIgnoreCase))
                {
                    targetIndex = i;
                    FavoriteServersPlugin.Log.LogInfo($"Matched profile '{profiles[i].GetName()}' at index {i}");
                    break;
                }
            }

            if (targetIndex < 0)
            {
                FavoriteServersPlugin.Log.LogWarning($"Could not find profile for character: {characterName}");
                yield break;
            }

            // Set the profile index
            var profileIndexField = AccessTools.Field(typeof(FejdStartup), "m_profileIndex");
            if (profileIndexField != null)
            {
                FavoriteServersPlugin.Log.LogInfo($"Setting profile index to {targetIndex}");
                profileIndexField.SetValue(fejd, targetIndex);

                // Call UpdateCharacterList to refresh the UI
                var updateMethod = AccessTools.Method(typeof(FejdStartup), "UpdateCharacterList");
                updateMethod?.Invoke(fejd, null);
            }

            // Wait another frame for selection to apply
            yield return null;

            // Click the Start button to proceed
            FavoriteServersPlugin.Log.LogInfo("Auto-clicking Start button");
            fejd.OnCharacterStart();
        }
    }

    /// <summary>
    /// Extension to get full transform path for debugging
    /// </summary>
    public static class TransformExtensions
    {
        public static string GetPath(this Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
