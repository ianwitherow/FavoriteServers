using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace FavoriteServers.UI
{
    /// <summary>
    /// Main panel showing the list of favorite servers
    /// </summary>
    public class FavoritesPanel : MonoBehaviour
    {
        public static FavoritesPanel Instance { get; private set; }

        private GameObject _panel;
        private GameObject _serverListContent;
        private ScrollRect _scrollRect;
        private bool _isVisible;

        private const float PanelWidth = 400f;
        private const float PanelHeight = 450f;
        private const float ServerRowHeight = 70f;

        public static void Create()
        {
            if (Instance != null) return;

            var go = new GameObject("FavoritesPanel");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<FavoritesPanel>();
        }

        private void Awake()
        {
            ServerManager.OnServersChanged += RefreshServerList;
        }

        private void OnDestroy()
        {
            if (_panel != null)
            {
                SaveWindowPosition();
            }
            ServerManager.OnServersChanged -= RefreshServerList;
        }

        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            if (_panel == null)
            {
                CreatePanel();
            }

            _panel.SetActive(true);
            _isVisible = true;
            GUIManager.BlockInput(true);
            RefreshServerList();
        }

        public void Hide()
        {
            if (_panel != null)
            {
                SaveWindowPosition();
                _panel.SetActive(false);
            }
            _isVisible = false;
            GUIManager.BlockInput(false);
        }

        /// <summary>
        /// Temporarily hide/show the panel without changing visibility state
        /// Used when showing dialogs on top
        /// </summary>
        public void SetPanelVisible(bool visible)
        {
            if (_panel != null)
            {
                _panel.SetActive(visible);
            }
        }

        public bool IsVisible => _isVisible;

        private void SaveWindowPosition()
        {
            var rect = _panel.GetComponent<RectTransform>();
            FavoriteServersPlugin.WindowPosX.Value = rect.position.x;
            FavoriteServersPlugin.WindowPosY.Value = rect.position.y;
            FavoriteServersPlugin.Instance.Config.Save();
        }

        private void CreatePanel()
        {
            // Create the main wood panel
            _panel = GUIManager.Instance.CreateWoodpanel(
                GUIManager.CustomGUIFront.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f),
                PanelWidth,
                PanelHeight,
                true // draggable
            );

            // Set initial position (center or saved position)
            var rect = _panel.GetComponent<RectTransform>();
            if (FavoriteServersPlugin.WindowPosX.Value >= 0 && FavoriteServersPlugin.WindowPosY.Value >= 0)
            {
                rect.position = new Vector3(
                    FavoriteServersPlugin.WindowPosX.Value,
                    FavoriteServersPlugin.WindowPosY.Value,
                    0
                );
            }

            // Create header
            var header = GUIManager.Instance.CreateText(
                "Favorite Servers",
                _panel.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -25f),
                GUIManager.Instance.AveriaSerifBold,
                22,
                GUIManager.Instance.ValheimOrange,
                true,
                Color.black,
                PanelWidth - 60f,
                40f,
                false
            );

            // Close button
            var closeBtn = GUIManager.Instance.CreateButton(
                "X",
                _panel.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-25f, -25f),
                40f,
                40f
            );
            closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

            // Create scroll view for server list
            CreateScrollView();

            // Add Server button at bottom
            var addBtn = GUIManager.Instance.CreateButton(
                "+ Add Server",
                _panel.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 35f),
                200f,
                40f
            );
            addBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                ServerEditPanel.Instance?.ShowAdd();
            });

            _panel.SetActive(false);
        }

        private void CreateScrollView()
        {
            // Create scroll view container
            var scrollViewGO = new GameObject("ScrollView");
            scrollViewGO.transform.SetParent(_panel.transform, false);

            var scrollRect = scrollViewGO.AddComponent<ScrollRect>();
            _scrollRect = scrollRect;

            var scrollRectTransform = scrollViewGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(15f, 70f); // Left, Bottom padding
            scrollRectTransform.offsetMax = new Vector2(-15f, -55f); // Right, Top padding

            // Add mask
            var maskGO = new GameObject("Mask");
            maskGO.transform.SetParent(scrollViewGO.transform, false);
            var maskRect = maskGO.AddComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            var maskImage = maskGO.AddComponent<Image>();
            maskImage.color = new Color(0, 0, 0, 0.3f);
            maskGO.AddComponent<Mask>().showMaskGraphic = true;

            // Create content container
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(maskGO.transform, false);
            _serverListContent = contentGO;

            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);

            var verticalLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.spacing = 5f;
            verticalLayout.padding = new RectOffset(5, 5, 5, 5);

            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = maskRect;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 30f;
        }

        private void RefreshServerList()
        {
            if (_serverListContent == null) return;

            // Clear existing items
            foreach (Transform child in _serverListContent.transform)
            {
                Destroy(child.gameObject);
            }

            var servers = ServerManager.GetServers();

            if (servers.Count == 0)
            {
                // Show empty message
                var emptyText = GUIManager.Instance.CreateText(
                    "No favorite servers yet.\nClick '+ Add Server' to get started!",
                    _serverListContent.transform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    GUIManager.Instance.AveriaSerifBold,
                    16,
                    Color.gray,
                    true,
                    Color.black,
                    PanelWidth - 50f,
                    80f,
                    false
                );
                emptyText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                foreach (var server in servers)
                {
                    CreateServerRow(server);
                }
            }
        }

        private void CreateServerRow(ServerEntry server)
        {
            // Row container
            var rowGO = new GameObject($"Server_{server.Id}");
            rowGO.transform.SetParent(_serverListContent.transform, false);

            var rowRect = rowGO.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, ServerRowHeight);

            var rowLayout = rowGO.AddComponent<LayoutElement>();
            rowLayout.minHeight = ServerRowHeight;
            rowLayout.preferredHeight = ServerRowHeight;

            // Background - make clickable
            var bgImage = rowGO.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.2f);

            // Add button to entire row for click-to-connect
            var rowButton = rowGO.AddComponent<Button>();
            rowButton.targetGraphic = bgImage;

            // Set up color transitions for hover effect
            var colors = rowButton.colors;
            colors.normalColor = new Color(0, 0, 0, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            colors.selectedColor = new Color(0, 0, 0, 0.2f);
            rowButton.colors = colors;

            // Capture server in closure for the click handler
            var serverToConnect = server;
            rowButton.onClick.AddListener(() =>
            {
                ConnectionManager.ConnectToServer(serverToConnect);
                Hide();
            });

            // Server name - use stretch anchors and explicit positioning
            var nameGO = new GameObject("ServerName");
            nameGO.transform.SetParent(rowGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(10f, 12f);
            nameRect.sizeDelta = new Vector2(200f, 25f);

            var nameText = nameGO.AddComponent<Text>();
            nameText.text = server.Name ?? "Unknown";
            nameText.font = GUIManager.Instance.AveriaSerifBold;
            nameText.fontSize = 16;
            nameText.color = GUIManager.Instance.ValheimOrange;
            nameText.alignment = TextAnchor.MiddleLeft;

            // Server address + character name
            var addrGO = new GameObject("ServerAddr");
            addrGO.transform.SetParent(rowGO.transform, false);
            var addrRect = addrGO.AddComponent<RectTransform>();
            addrRect.anchorMin = new Vector2(0f, 0.5f);
            addrRect.anchorMax = new Vector2(0f, 0.5f);
            addrRect.pivot = new Vector2(0f, 0.5f);
            addrRect.anchoredPosition = new Vector2(10f, -12f);
            addrRect.sizeDelta = new Vector2(280f, 20f);

            var addrText = addrGO.AddComponent<Text>();
            // Show address and character name if set
            var displayText = server.GetDisplayAddress();
            if (!string.IsNullOrEmpty(server.CharacterName))
            {
                displayText += $"  •  {ServerManager.ToTitleCase(server.CharacterName)}";
            }
            addrText.text = displayText;
            addrText.font = GUIManager.Instance.AveriaSerifBold;
            addrText.fontSize = 12;
            addrText.color = Color.gray;
            addrText.alignment = TextAnchor.MiddleLeft;

            // Buttons on the right - stack vertically
            float btnX = -10f;

            // Connect button (top right)
            var connectBtn = GUIManager.Instance.CreateButton(
                "Connect",
                rowGO.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(btnX - 45f, 15f),
                90f,
                28f
            );
            connectBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                ConnectionManager.ConnectToServer(server);
                Hide();
            });

            // Edit button (bottom right)
            var editBtn = GUIManager.Instance.CreateButton(
                "Edit",
                rowGO.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(btnX - 70f, -18f),
                55f,
                24f
            );
            editBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                ServerEditPanel.Instance?.ShowEdit(server);
            });

            // Delete button (bottom right, next to edit)
            var deleteBtn = GUIManager.Instance.CreateButton(
                "X",
                rowGO.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(btnX - 25f, -18f),
                30f,
                24f
            );
            deleteBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                ServerManager.DeleteServer(server.Id);
            });
        }
    }
}
