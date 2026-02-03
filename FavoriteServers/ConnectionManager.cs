using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace FavoriteServers
{
    /// <summary>
    /// Handles connecting to servers, including DNS resolution
    /// </summary>
    public static class ConnectionManager
    {
        private static ServerEntry _connectingServer;
        private static Task<IPHostEntry> _dnsResolveTask;
        private static bool _isConnecting;

        /// <summary>
        /// Current server we're connecting to (for password injection)
        /// </summary>
        public static ServerEntry ConnectingServer => _connectingServer;
        public static bool IsConnecting => _isConnecting;

        /// <summary>
        /// Reset connection state to allow new connections
        /// Does NOT clear _connectingServer (needed for password handshake)
        /// </summary>
        public static void ResetState()
        {
            FavoriteServersPlugin.Log.LogInfo("Resetting connection state (keeping server for password)");
            _isConnecting = false;
            _dnsResolveTask = null;
            // Keep _connectingServer - it's needed for password handshake!
        }

        /// <summary>
        /// Initiate connection to a server
        /// </summary>
        public static void ConnectToServer(ServerEntry server)
        {
            if (_isConnecting)
            {
                FavoriteServersPlugin.Log.LogWarning("Already connecting, resetting state");
                ResetState();
            }

            _connectingServer = server;
            _isConnecting = true;

            FavoriteServersPlugin.Log.LogInfo($"Connecting to {server.Name} ({server.Hostname}:{server.Port})");

            // Try to parse as IP address first
            if (IPAddress.TryParse(server.Hostname, out IPAddress ipAddress))
            {
                // Direct IP connection
                DoConnect(ipAddress.ToString(), server.Port);
            }
            else
            {
                // Need DNS resolution
                FavoriteServersPlugin.Log.LogInfo($"Resolving hostname: {server.Hostname}");
                _dnsResolveTask = Dns.GetHostEntryAsync(server.Hostname);

                // Start monitoring the task
                MonitorDnsResolution();
            }
        }

        private static async void MonitorDnsResolution()
        {
            try
            {
                var hostEntry = await _dnsResolveTask;

                if (hostEntry.AddressList.Length == 0)
                {
                    FavoriteServersPlugin.Log.LogError($"DNS resolution returned no addresses for {_connectingServer.Hostname}");
                    ShowConnectionError($"Could not resolve hostname: {_connectingServer.Hostname}");
                    ResetConnection();
                    return;
                }

                // Prefer IPv4 addresses
                IPAddress address = null;
                foreach (var addr in hostEntry.AddressList)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = addr;
                        break;
                    }
                }

                // Fall back to first address if no IPv4 found
                if (address == null)
                {
                    address = hostEntry.AddressList[0];
                }

                FavoriteServersPlugin.Log.LogInfo($"Resolved {_connectingServer.Hostname} to {address}");
                DoConnect(address.ToString(), _connectingServer.Port);
            }
            catch (Exception ex)
            {
                FavoriteServersPlugin.Log.LogError($"DNS resolution failed: {ex.Message}");
                ShowConnectionError($"Failed to resolve: {_connectingServer.Hostname}\n{ex.Message}");
                ResetConnection();
            }
        }

        private static void DoConnect(string address, int port)
        {
            try
            {
                // Check if we're at the main menu (FejdStartup)
                if (FejdStartup.instance == null)
                {
                    FavoriteServersPlugin.Log.LogError("Not at main menu - cannot connect");
                    ShowConnectionError("Please connect from the main menu");
                    ResetConnection();
                    return;
                }

                var joinAddress = $"{address}:{port}";
                FavoriteServersPlugin.Log.LogInfo($"Initiating connection to {joinAddress}");

                // Use ZSteamMatchmaking to connect directly
                // This bypasses the normal server browser
                ZSteamMatchmaking.instance.QueueServerJoin(joinAddress);
            }
            catch (Exception ex)
            {
                FavoriteServersPlugin.Log.LogError($"Connection failed: {ex.Message}");
                ShowConnectionError($"Connection failed: {ex.Message}");
                ResetConnection();
            }
        }

        /// <summary>
        /// Called when connection completes (success or failure)
        /// </summary>
        public static void OnConnectionComplete(bool success)
        {
            if (success)
            {
                FavoriteServersPlugin.Log.LogInfo($"Successfully connected to {_connectingServer?.Name}");
            }
            ResetConnection();
        }

        private static void ResetConnection()
        {
            _isConnecting = false;
            _dnsResolveTask = null;
            // Keep _connectingServer for a bit in case password is needed
        }

        /// <summary>
        /// Clear the connecting server reference
        /// </summary>
        public static void ClearConnectingServer()
        {
            _connectingServer = null;
        }

        private static void ShowConnectionError(string message)
        {
            // Show error to user via Valheim's message system if available
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
            }
            else
            {
                FavoriteServersPlugin.Log.LogError(message);
            }
        }
    }
}
