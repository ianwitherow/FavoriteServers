using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FavoriteServers.UI
{
    /// <summary>
    /// Panel for adding or editing a server entry
    /// </summary>
    public class ServerEditPanel : MonoBehaviour
    {
        public static ServerEditPanel Instance { get; private set; }

        private GameObject _panel;
        private InputField _nameInput;
        private InputField _hostnameInput;
        private InputField _portInput;
        private InputField _passwordInput;
        private Dropdown _characterDropdown;
        private Text _titleText;
        private Text _errorText;

        private ServerEntry _editingServer;
        private bool _isVisible;
        private List<string> _availableCharacters = new List<string>();
        private Button _saveButton;
        private Button _showInMainMenuToggle;
        private Text _showInMainMenuText;
        private bool _showInMainMenu;
        private GameObject _colorRow;
        private string _selectedColor = "#FFFFFF";
        private List<Image> _colorSwatchImages = new List<Image>();

        private static readonly string[] ColorPresets = {
            "#B0FFB0", // Green
            "#B0D0FF", // Blue
            "#FFD080", // Gold
            "#FF9090", // Red
            "#FFB0C0", // Pink
            "#D0B0FF", // Purple
            "#FFFFFF"  // White
        };

        private const float PanelWidth = 450f;
        private const float PanelHeight = 460f;

        private Selectable[] _tabSelectables; // For tab navigation (inputs, dropdown, save button)

        public static void Create()
        {
            if (Instance != null) return;

            var go = new GameObject("ServerEditPanel");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ServerEditPanel>();
        }

        private void Update()
        {
            if (!_isVisible || _tabSelectables == null) return;

            // Handle Tab key for field navigation
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                NavigateFields(shift ? -1 : 1);
            }

            // Handle Enter/Return to save
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnSave();
            }

            // Handle Escape to cancel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        private void NavigateFields(int direction)
        {
            var current = EventSystem.current?.currentSelectedGameObject;
            int currentIndex = -1;

            for (int i = 0; i < _tabSelectables.Length; i++)
            {
                if (_tabSelectables[i] != null && _tabSelectables[i].gameObject == current)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = currentIndex + direction;
            if (nextIndex < 0) nextIndex = _tabSelectables.Length - 1;
            if (nextIndex >= _tabSelectables.Length) nextIndex = 0;

            if (_tabSelectables[nextIndex] != null)
            {
                EventSystem.current?.SetSelectedGameObject(_tabSelectables[nextIndex].gameObject);

                // Activate input field if it's an InputField
                var inputField = _tabSelectables[nextIndex] as InputField;
                if (inputField != null)
                {
                    inputField.ActivateInputField();
                }
            }
        }

        public void ShowAdd()
        {
            _editingServer = null;
            Show();
            if (_titleText != null) _titleText.text = "Add Server";
            ClearInputs();
            _portInput.text = "2456";
        }

        public void ShowEdit(ServerEntry server)
        {
            _editingServer = server;
            Show();
            if (_titleText != null) _titleText.text = "Edit Server";
            PopulateInputs(server);
        }

        private void Show()
        {
            if (_panel == null)
            {
                CreatePanel();
            }

            // Hide the main favorites panel while editing
            FavoritesPanel.Instance?.SetPanelVisible(false);

            _panel.SetActive(true);
            _isVisible = true;
            GUIManager.BlockInput(true);
            ClearError();

            // Refresh character dropdown
            RefreshCharacterDropdown();

            // Focus the name input field to capture keyboard input
            if (_nameInput != null)
            {
                EventSystem.current?.SetSelectedGameObject(_nameInput.gameObject);
                _nameInput.ActivateInputField();
            }
        }

        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
            _isVisible = false;

            // Show the main favorites panel again and re-block input for it
            FavoritesPanel.Instance?.SetPanelVisible(true);
            // Input stays blocked since FavoritesPanel is still logically "shown"
        }

        public bool IsVisible => _isVisible;

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

            // Header
            var headerGO = GUIManager.Instance.CreateText(
                "Add Server",
                _panel.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -25f),
                GUIManager.Instance.AveriaSerifBold,
                20,
                GUIManager.Instance.ValheimOrange,
                true,
                Color.black,
                PanelWidth - 60f,
                35f,
                false
            );
            _titleText = headerGO.GetComponent<Text>();

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

            float yOffset = -65f;
            float inputWidth = 300f;
            float rowHeight = 45f;

            // Name field
            CreateLabel("Name:", yOffset);
            _nameInput = CreateInputField(yOffset, inputWidth);
            yOffset -= rowHeight;

            // Hostname field
            CreateLabel("Hostname:", yOffset);
            _hostnameInput = CreateInputField(yOffset, inputWidth);
            _hostnameInput.placeholder.GetComponent<Text>().text = "IP or domain";
            yOffset -= rowHeight;

            // Port field
            CreateLabel("Port:", yOffset);
            _portInput = CreateInputField(yOffset, 80f);
            _portInput.contentType = InputField.ContentType.IntegerNumber;
            _portInput.placeholder.GetComponent<Text>().text = "2456";
            yOffset -= rowHeight;

            // Password field
            CreateLabel("Password:", yOffset);
            _passwordInput = CreateInputField(yOffset, inputWidth);
            _passwordInput.contentType = InputField.ContentType.Password;
            _passwordInput.placeholder.GetComponent<Text>().text = "(optional)";
            yOffset -= rowHeight;

            // Character dropdown
            CreateLabel("Character:", yOffset);
            _characterDropdown = CreateDropdown(yOffset, inputWidth);
            yOffset -= rowHeight;

            // Show in Main Menu toggle
            var toggleBtn = GUIManager.Instance.CreateButton(
                "[ ] Show in Main Menu",
                _panel.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-25f - inputWidth / 2f, yOffset),
                inputWidth,
                30f
            );
            _showInMainMenuToggle = toggleBtn.GetComponent<Button>();
            _showInMainMenuText = toggleBtn.GetComponentInChildren<Text>();
            if (_showInMainMenuText == null)
            {
                var tmp = toggleBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.alignment = TMPro.TextAlignmentOptions.Left;
                    tmp.margin = new Vector4(10f, 0f, 0f, 0f);
                }
            }
            else
            {
                _showInMainMenuText.alignment = TextAnchor.MiddleLeft;
                var textRect = _showInMainMenuText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.offsetMin = new Vector2(textRect.offsetMin.x + 10f, textRect.offsetMin.y);
                }
            }
            _showInMainMenuToggle.onClick.AddListener(() =>
            {
                _showInMainMenu = !_showInMainMenu;
                UpdateToggleText();
                UpdateColorRowVisibility();
            });
            yOffset -= rowHeight;

            // Color swatch row (only visible when Show in Main Menu is checked)
            _colorRow = new GameObject("ColorRow");
            _colorRow.transform.SetParent(_panel.transform, false);
            var colorRowRect = _colorRow.AddComponent<RectTransform>();
            colorRowRect.anchorMin = new Vector2(1f, 1f);
            colorRowRect.anchorMax = new Vector2(1f, 1f);
            colorRowRect.pivot = new Vector2(1f, 0.5f);
            colorRowRect.anchoredPosition = new Vector2(-25f, yOffset);
            colorRowRect.sizeDelta = new Vector2(inputWidth, 30f);

            // Create label for color row (parented to _colorRow so it hides with it)
            var colorLabelGO = GUIManager.Instance.CreateText(
                "Color:",
                _colorRow.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(-55f, 0f),
                GUIManager.Instance.AveriaSerifBold,
                14,
                Color.white,
                true,
                Color.black,
                90f,
                30f,
                false
            );
            colorLabelGO.GetComponent<Text>().alignment = TextAnchor.MiddleRight;

            // Create color swatch buttons
            float swatchSize = 30f;
            float swatchSpacing = 8f;
            float totalSwatchWidth = ColorPresets.Length * swatchSize + (ColorPresets.Length - 1) * swatchSpacing;
            float swatchStartX = (inputWidth - totalSwatchWidth) / 2f;

            _colorSwatchImages.Clear();
            for (int i = 0; i < ColorPresets.Length; i++)
            {
                var colorHex = ColorPresets[i];
                ColorUtility.TryParseHtmlString(colorHex, out Color swatchColor);

                var swatchGO = new GameObject($"ColorSwatch_{i}");
                swatchGO.transform.SetParent(_colorRow.transform, false);

                var swatchRect = swatchGO.AddComponent<RectTransform>();
                swatchRect.anchorMin = new Vector2(0f, 0.5f);
                swatchRect.anchorMax = new Vector2(0f, 0.5f);
                swatchRect.pivot = new Vector2(0f, 0.5f);
                swatchRect.anchoredPosition = new Vector2(swatchStartX + i * (swatchSize + swatchSpacing), 0f);
                swatchRect.sizeDelta = new Vector2(swatchSize, swatchSize);

                var swatchImage = swatchGO.AddComponent<Image>();
                swatchImage.color = swatchColor;
                _colorSwatchImages.Add(swatchImage);

                var swatchButton = swatchGO.AddComponent<Button>();
                swatchButton.targetGraphic = swatchImage;
                var capturedHex = colorHex;
                swatchButton.onClick.AddListener(() =>
                {
                    _selectedColor = capturedHex;
                    UpdateSwatchHighlights();
                });
            }

            _colorRow.SetActive(false);
            yOffset -= rowHeight;

            // Error text
            var errorGO = GUIManager.Instance.CreateText(
                "",
                _panel.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 85f),
                GUIManager.Instance.AveriaSerifBold,
                14,
                Color.red,
                true,
                Color.black,
                PanelWidth - 40f,
                25f,
                false
            );
            _errorText = errorGO.GetComponent<Text>();
            _errorText.alignment = TextAnchor.MiddleCenter;

            // Cancel button
            var cancelBtn = GUIManager.Instance.CreateButton(
                "Cancel",
                _panel.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-60f, 40f),
                100f,
                35f
            );
            cancelBtn.GetComponent<Button>().onClick.AddListener(Hide);

            // Save button
            var saveBtn = GUIManager.Instance.CreateButton(
                "Save",
                _panel.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(60f, 40f),
                100f,
                35f
            );
            _saveButton = saveBtn.GetComponent<Button>();
            _saveButton.onClick.AddListener(OnSave);

            // Setup selectable array for tab navigation (inputs, dropdown, save button)
            _tabSelectables = new Selectable[] {
                _nameInput,
                _hostnameInput,
                _portInput,
                _passwordInput,
                _characterDropdown,
                _saveButton
            };

            _panel.SetActive(false);
        }

        private void CreateLabel(string text, float yOffset)
        {
            var labelGO = GUIManager.Instance.CreateText(
                text,
                _panel.transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(75f, yOffset),
                GUIManager.Instance.AveriaSerifBold,
                14,
                Color.white,
                true,
                Color.black,
                90f,
                30f,
                false
            );
            labelGO.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
        }

        private InputField CreateInputField(float yOffset, float width)
        {
            var inputGO = GUIManager.Instance.CreateInputField(
                _panel.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-25f - width / 2f, yOffset),
                InputField.ContentType.Standard,
                "",
                16,
                width,
                30f
            );

            return inputGO.GetComponent<InputField>();
        }

        private Dropdown CreateDropdown(float yOffset, float width)
        {
            // Create dropdown container
            var dropdownGO = GUIManager.Instance.CreateDropDown(
                _panel.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-25f - width / 2f, yOffset),
                16,
                width,
                30f
            );

            var dropdown = dropdownGO.GetComponent<Dropdown>();

            return dropdown;
        }

        private void RefreshCharacterDropdown()
        {
            if (_characterDropdown == null) return;

            _characterDropdown.ClearOptions();

            // First option is "Ask every time" (no auto-select)
            var options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("(Ask every time)")
            };

            // Get available characters
            _availableCharacters = ServerManager.GetAvailableCharacters();

            foreach (var character in _availableCharacters)
            {
                // Display in title case
                options.Add(new Dropdown.OptionData(ServerManager.ToTitleCase(character)));
            }

            _characterDropdown.AddOptions(options);
        }

        private void SetSelectedCharacter(string characterName)
        {
            if (_characterDropdown == null) return;

            if (string.IsNullOrEmpty(characterName))
            {
                _characterDropdown.value = 0; // "Ask every time"
                return;
            }

            // Find the character in the list (case-insensitive, offset by 1 for the "Ask every time" option)
            int index = _availableCharacters.FindIndex(c =>
                string.Equals(c, characterName, System.StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _characterDropdown.value = index + 1;
            }
            else
            {
                _characterDropdown.value = 0; // Character not found, default to ask
            }
        }

        private string GetSelectedCharacter()
        {
            if (_characterDropdown == null || _characterDropdown.value == 0)
            {
                return ""; // "Ask every time" or no dropdown
            }

            int charIndex = _characterDropdown.value - 1;
            if (charIndex >= 0 && charIndex < _availableCharacters.Count)
            {
                return _availableCharacters[charIndex];
            }

            return "";
        }

        private void UpdateColorRowVisibility()
        {
            if (_colorRow != null)
            {
                _colorRow.SetActive(_showInMainMenu);
            }
        }

        private void UpdateSwatchHighlights()
        {
            for (int i = 0; i < ColorPresets.Length && i < _colorSwatchImages.Count; i++)
            {
                ColorUtility.TryParseHtmlString(ColorPresets[i], out Color baseColor);
                bool selected = ColorPresets[i] == _selectedColor;
                _colorSwatchImages[i].color = baseColor;

                // Remove old outline, add new one if selected
                var outline = _colorSwatchImages[i].GetComponent<Outline>();
                if (selected && outline == null)
                {
                    outline = _colorSwatchImages[i].gameObject.AddComponent<Outline>();
                    outline.effectColor = GUIManager.Instance.ValheimOrange;
                    outline.effectDistance = new Vector2(2f, 2f);
                }
                else if (!selected && outline != null)
                {
                    Destroy(outline);
                }
            }
        }

        private void UpdateToggleText()
        {
            var label = _showInMainMenu ? "[x] Show in Main Menu" : "[ ] Show in Main Menu";
            if (_showInMainMenuText != null)
            {
                _showInMainMenuText.text = label;
            }
            else
            {
                var tmp = _showInMainMenuToggle?.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.text = label;
            }
        }

        private void ClearInputs()
        {
            if (_nameInput != null) _nameInput.text = "";
            if (_hostnameInput != null) _hostnameInput.text = "";
            if (_portInput != null) _portInput.text = "";
            if (_passwordInput != null) _passwordInput.text = "";
            SetSelectedCharacter("");
            _showInMainMenu = false;
            _selectedColor = "#FFFFFF";
            UpdateToggleText();
            UpdateColorRowVisibility();
            UpdateSwatchHighlights();
        }

        private void PopulateInputs(ServerEntry server)
        {
            if (_nameInput != null) _nameInput.text = server.Name ?? "";
            if (_hostnameInput != null) _hostnameInput.text = server.Hostname ?? "";
            if (_portInput != null) _portInput.text = server.Port.ToString();
            if (_passwordInput != null) _passwordInput.text = server.Password ?? "";
            SetSelectedCharacter(server.CharacterName);
            _showInMainMenu = server.ShowInMainMenu;
            _selectedColor = server.MenuColor ?? "#FFFFFF";
            UpdateToggleText();
            UpdateColorRowVisibility();
            UpdateSwatchHighlights();
        }

        private void ClearError()
        {
            if (_errorText != null) _errorText.text = "";
        }

        private void ShowError(string message)
        {
            if (_errorText != null) _errorText.text = message;
        }

        private void OnSave()
        {
            // Validate
            var name = _nameInput?.text?.Trim() ?? "";
            var hostname = _hostnameInput?.text?.Trim() ?? "";
            var portText = _portInput?.text?.Trim();
            if (string.IsNullOrEmpty(portText)) portText = "2456";
            var password = _passwordInput?.text ?? "";
            var characterName = GetSelectedCharacter();

            if (string.IsNullOrEmpty(name))
            {
                ShowError("Name is required");
                return;
            }

            if (string.IsNullOrEmpty(hostname))
            {
                ShowError("Hostname is required");
                return;
            }

            if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
            {
                ShowError("Invalid port number");
                return;
            }

            if (_editingServer != null)
            {
                // Update existing
                _editingServer.Name = name;
                _editingServer.Hostname = hostname;
                _editingServer.Port = port;
                _editingServer.Password = password;
                _editingServer.CharacterName = characterName;
                _editingServer.ShowInMainMenu = _showInMainMenu;
                _editingServer.MenuColor = _selectedColor;
                ServerManager.UpdateServer(_editingServer);
            }
            else
            {
                // Create new
                var server = new ServerEntry(name, hostname, port, password, characterName, _showInMainMenu, _selectedColor);
                ServerManager.AddServer(server);
            }

            Hide();
        }
    }
}
