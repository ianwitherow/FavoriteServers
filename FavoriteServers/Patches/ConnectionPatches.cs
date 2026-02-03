using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FavoriteServers.Patches
{
    /// <summary>
    /// Patches for handling server connections and password injection
    /// </summary>
    [HarmonyPatch]
    public static class ConnectionPatches
    {
        // Cached reflection members using Harmony's AccessTools
        private static FieldInfo _serverPasswordSaltField;
        private static MethodInfo _sendPeerInfoMethod;
        private static bool _reflectionInitialized;
        private static bool _reflectionFailed;

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            FavoriteServersPlugin.Log.LogInfo("Initializing reflection for ZNet...");

            // Use Harmony's AccessTools - more robust than raw reflection
            _serverPasswordSaltField = AccessTools.Field(typeof(ZNet), "m_serverPasswordSalt");
            if (_serverPasswordSaltField == null)
            {
                FavoriteServersPlugin.Log.LogWarning("Could not find m_serverPasswordSalt field");
            }
            else
            {
                FavoriteServersPlugin.Log.LogInfo($"Found m_serverPasswordSalt: {_serverPasswordSaltField.FieldType}");
            }

            // Try to find SendPeerInfo method
            _sendPeerInfoMethod = AccessTools.Method(typeof(ZNet), "SendPeerInfo", new[] { typeof(ZRpc), typeof(string) });
            if (_sendPeerInfoMethod == null)
            {
                FavoriteServersPlugin.Log.LogWarning("Could not find SendPeerInfo(ZRpc, string) method");

                // Log all methods to help debug
                FavoriteServersPlugin.Log.LogInfo("Available ZNet methods containing 'Peer' or 'Send':");
                foreach (var method in typeof(ZNet).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (method.Name.Contains("Peer") || method.Name.Contains("Send"))
                    {
                        var parameters = string.Join(", ", System.Array.ConvertAll(method.GetParameters(), p => $"{p.ParameterType.Name} {p.Name}"));
                        FavoriteServersPlugin.Log.LogInfo($"  {method.Name}({parameters})");
                    }
                }
            }
            else
            {
                FavoriteServersPlugin.Log.LogInfo($"Found SendPeerInfo method");
            }

            _reflectionFailed = (_serverPasswordSaltField == null || _sendPeerInfoMethod == null);
        }

        /// <summary>
        /// Intercept password prompt and auto-submit if we have a saved password
        /// Uses Prefix to bypass the password dialog entirely
        /// </summary>
        [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
        [HarmonyPrefix]
        public static bool ZNet_RPC_ClientHandshake_Prefix(ZNet __instance, ZRpc rpc, bool needPassword, string serverPasswordSalt)
        {
            // Only intercept if we're connecting via our mod and have a password
            if (ConnectionManager.ConnectingServer == null)
                return true; // Let normal flow continue

            var server = ConnectionManager.ConnectingServer;

            if (needPassword && !string.IsNullOrEmpty(server.Password))
            {
                FavoriteServersPlugin.Log.LogInfo("Password required - auto-submitting saved password");

                // Initialize reflection on first use
                InitializeReflection();

                if (_reflectionFailed)
                {
                    FavoriteServersPlugin.Log.LogWarning("Reflection failed, falling back to normal password dialog");
                    return true;
                }

                try
                {
                    // Hide the connecting dialog if visible
                    var connectingDialog = __instance.m_connectingDialog;
                    if (connectingDialog != null)
                    {
                        connectingDialog.gameObject.SetActive(false);
                    }

                    // Set the password salt
                    _serverPasswordSaltField.SetValue(__instance, serverPasswordSalt);
                    FavoriteServersPlugin.Log.LogInfo("Set password salt");

                    // Call SendPeerInfo with our saved password
                    _sendPeerInfoMethod.Invoke(__instance, new object[] { rpc, server.Password });
                    FavoriteServersPlugin.Log.LogInfo("Password submitted successfully via SendPeerInfo");

                    // Clear the connecting server state
                    ConnectionManager.ClearConnectingServer();

                    // Skip original method
                    return false;
                }
                catch (System.Exception ex)
                {
                    FavoriteServersPlugin.Log.LogError($"Failed to auto-submit password: {ex}");
                    // Fall back to normal flow on error
                    return true;
                }
            }

            // No password needed or no saved password - let normal flow continue
            return true;
        }

        /// <summary>
        /// Handle connection success
        /// </summary>
        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        [HarmonyPostfix]
        public static void ZNet_RPC_PeerInfo_Postfix(ZNet __instance, ZRpc rpc)
        {
            if (ConnectionManager.IsConnecting)
            {
                ConnectionManager.OnConnectionComplete(true);
            }
        }

        /// <summary>
        /// Handle connection failure/disconnect
        /// </summary>
        [HarmonyPatch(typeof(ZNet), "Disconnect")]
        [HarmonyPostfix]
        public static void ZNet_Disconnect_Postfix()
        {
            if (ConnectionManager.IsConnecting)
            {
                ConnectionManager.OnConnectionComplete(false);
            }
        }

        /// <summary>
        /// Clean up when ZNet is destroyed
        /// </summary>
        [HarmonyPatch(typeof(ZNet), "OnDestroy")]
        [HarmonyPrefix]
        public static void ZNet_OnDestroy_Prefix()
        {
            ConnectionManager.ClearConnectingServer();
        }
    }
}
