using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;

namespace FavoriteServers
{
    /// <summary>
    /// Represents a saved server entry
    /// </summary>
    public class ServerEntry
    {
        public string Id;
        public string Name;
        public string Hostname;
        public int Port = 2456;
        public string Password;
        public string CharacterName; // Optional - auto-select this character when connecting

        public ServerEntry()
        {
            Id = Guid.NewGuid().ToString();
        }

        public ServerEntry(string name, string hostname, int port = 2456, string password = "", string characterName = "")
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Hostname = hostname;
            Port = port;
            Password = password ?? "";
            CharacterName = characterName ?? "";
        }

        public string GetDisplayAddress()
        {
            return Port == 2456 ? Hostname : $"{Hostname}:{Port}";
        }
    }

    /// <summary>
    /// Manages loading, saving, and accessing favorite servers
    /// </summary>
    public static class ServerManager
    {
        private static List<ServerEntry> _servers = new List<ServerEntry>();
        private static string _configPath;

        public static event Action OnServersChanged;

        public static void Initialize()
        {
            _configPath = Path.Combine(Paths.ConfigPath, "FavoriteServers.json");
            Load();
        }

        /// <summary>
        /// Get list of available character names from local and Steam cloud character folders
        /// </summary>
        public static List<string> GetAvailableCharacters()
        {
            var characters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 1. Try local save path
                try
                {
                    var localPath = Utils.GetSaveDataPath(FileHelpers.FileSource.Local);
                    var localCharsPath = Path.Combine(localPath, "characters");
                    FavoriteServersPlugin.Log.LogInfo($"Looking for characters in (Local): {localCharsPath}");
                    AddCharactersFromPath(localCharsPath, characters);
                }
                catch (Exception ex)
                {
                    FavoriteServersPlugin.Log.LogWarning($"Could not read local characters: {ex.Message}");
                }

                // 2. Try Steam userdata folders (cloud saves)
                try
                {
                    // Find Steam installation path
                    var steamPath = FindSteamPath();
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        var userdataPath = Path.Combine(steamPath, "userdata");
                        if (Directory.Exists(userdataPath))
                        {
                            // Search all user folders for Valheim (892970) character saves
                            foreach (var userDir in Directory.GetDirectories(userdataPath))
                            {
                                // Check remote/characters folder
                                var remoteCharsPath = Path.Combine(userDir, "892970", "remote", "characters");
                                if (Directory.Exists(remoteCharsPath))
                                {
                                    FavoriteServersPlugin.Log.LogInfo($"Looking for characters in (Steam Cloud): {remoteCharsPath}");
                                    AddCharactersFromPath(remoteCharsPath, characters);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    FavoriteServersPlugin.Log.LogWarning($"Could not read Steam cloud characters: {ex.Message}");
                }

                FavoriteServersPlugin.Log.LogInfo($"Found {characters.Count} total characters: {string.Join(", ", characters)}");
            }
            catch (Exception ex)
            {
                FavoriteServersPlugin.Log.LogError($"Failed to get characters: {ex.Message}");
            }

            return characters.OrderBy(c => c).ToList();
        }

        private static void AddCharactersFromPath(string charactersPath, HashSet<string> characters)
        {
            if (!Directory.Exists(charactersPath)) return;

            var files = Directory.GetFiles(charactersPath, "*.fch");
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                // Skip backup files and .old/.bak files
                if (!fileName.Contains("_backup") && !fileName.EndsWith(".old") && !fileName.EndsWith(".bak"))
                {
                    characters.Add(fileName);
                }
            }
        }

        private static string FindSteamPath()
        {
            // Common Steam installation paths
            var possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam",
                @"D:\Steam",
                @"D:\Program Files (x86)\Steam",
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam"
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    FavoriteServersPlugin.Log.LogInfo($"Found Steam at: {path}");
                    return path;
                }
            }

            FavoriteServersPlugin.Log.LogWarning("Could not find Steam installation");
            return null;
        }

        /// <summary>
        /// Convert character name to title case for display
        /// </summary>
        public static string ToTitleCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return char.ToUpper(name[0]) + (name.Length > 1 ? name.Substring(1) : "");
        }

        /// <summary>
        /// Check if a character exists
        /// </summary>
        public static bool CharacterExists(string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return false;
            return GetAvailableCharacters().Contains(characterName);
        }

        public static List<ServerEntry> GetServers()
        {
            return _servers;
        }

        public static ServerEntry GetServer(string id)
        {
            return _servers.Find(s => s.Id == id);
        }

        public static void AddServer(ServerEntry server)
        {
            if (string.IsNullOrEmpty(server.Id))
            {
                server.Id = Guid.NewGuid().ToString();
            }
            _servers.Add(server);
            Save();
            OnServersChanged?.Invoke();
        }

        public static void UpdateServer(ServerEntry server)
        {
            var index = _servers.FindIndex(s => s.Id == server.Id);
            if (index >= 0)
            {
                _servers[index] = server;
                Save();
                OnServersChanged?.Invoke();
            }
        }

        public static void DeleteServer(string id)
        {
            _servers.RemoveAll(s => s.Id == id);
            Save();
            OnServersChanged?.Invoke();
        }

        private static void Load()
        {
            _servers = new List<ServerEntry>();

            try
            {
                if (!File.Exists(_configPath))
                {
                    FavoriteServersPlugin.Log.LogInfo("No favorites file found, starting fresh");
                    return;
                }

                var lines = File.ReadAllLines(_configPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    // Format: id|name|hostname|port|password|characterName
                    var parts = line.Split('|');
                    if (parts.Length >= 4)
                    {
                        var server = new ServerEntry
                        {
                            Id = parts[0],
                            Name = parts[1],
                            Hostname = parts[2],
                            Port = int.TryParse(parts[3], out int port) ? port : 2456,
                            Password = parts.Length > 4 ? parts[4] : "",
                            CharacterName = parts.Length > 5 ? parts[5] : ""
                        };
                        _servers.Add(server);
                    }
                }

                FavoriteServersPlugin.Log.LogInfo($"Loaded {_servers.Count} favorite servers");
            }
            catch (Exception ex)
            {
                FavoriteServersPlugin.Log.LogError($"Failed to load favorites: {ex.Message}");
                _servers = new List<ServerEntry>();
            }
        }

        private static void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# FavoriteServers config - format: id|name|hostname|port|password|characterName");

                foreach (var server in _servers)
                {
                    // Escape pipe characters in values
                    var name = (server.Name ?? "").Replace("|", "\\|");
                    var hostname = (server.Hostname ?? "").Replace("|", "\\|");
                    var password = (server.Password ?? "").Replace("|", "\\|");
                    var characterName = (server.CharacterName ?? "").Replace("|", "\\|");

                    sb.AppendLine($"{server.Id}|{name}|{hostname}|{server.Port}|{password}|{characterName}");
                }

                File.WriteAllText(_configPath, sb.ToString());
                FavoriteServersPlugin.Log.LogInfo($"Saved {_servers.Count} favorite servers to {_configPath}");
            }
            catch (Exception ex)
            {
                FavoriteServersPlugin.Log.LogError($"Failed to save favorites: {ex.Message}");
            }
        }
    }
}
