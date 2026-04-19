using UnityEngine;
using UnityEngine.UIElements;
using _Project.Player;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Pannello Armadio (Task 3): ciclo outfit con rotella / frecce, chiusura ESC; notifica missione alla prima apertura.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-37)]
    public sealed class WardrobePanelController : MonoBehaviour
    {
        private const string VisualTreeResourcePath = "UI/UIToolkit/Wardrobe/WardrobePanel";
        private const string PanelSettingsResourcePath = "UI/UIToolkit/MainMenu/MainMenuPanelSettings";

        /// <summary>Sopra missioni/TopBar, sotto VO overlay (650).</summary>
        private const int SortingOrder = 500;

        private UIDocument _document;
        private VisualElement _root;
        private Label _outfitLabel;
        private PlayerOutfitController _outfit;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            var vta = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (vta == null)
            {
                Debug.LogError($"[Wardrobe] VisualTreeAsset mancante: {VisualTreeResourcePath}");
                return;
            }

            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (panelSettings == null)
            {
                Debug.LogError($"[Wardrobe] PanelSettings mancanti: {PanelSettingsResourcePath}");
                return;
            }

            _document = GetComponent<UIDocument>();
            if (_document == null)
                _document = gameObject.AddComponent<UIDocument>();

            _document.panelSettings = panelSettings;
            _document.visualTreeAsset = vta;
            _document.sortingOrder = SortingOrder;

            var ve = _document.rootVisualElement;
            _root = ve.Q<VisualElement>("wardrobe-root");
            _outfitLabel = ve.Q<Label>("wardrobe-outfit-label");
            var closeBtn = ve.Q<Button>("wardrobe-close");

            if (_root != null)
                _root.style.display = DisplayStyle.None;

            if (closeBtn != null)
                closeBtn.clicked += Close;

            ServiceContainer.Instance?.Register(this);
        }

        private void OnDestroy()
        {
            if (IsOpen)
                GameplayUiModalLock.SetBlockWorldInput(false);
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                CycleOutfit(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                CycleOutfit(1);

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                CycleOutfit(scroll > 0f ? -1 : 1);
        }

        public void Open()
        {
            if (_root == null)
                return;

            EnsureOutfitController();

            if (_outfit == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                SporiumLogger.LogWarning(LogCategory.UI, "[Wardrobe] PlayerOutfitController assente sul Player — pannello non aperto.");
#endif
                return;
            }

            _root.style.display = DisplayStyle.Flex;
            IsOpen = true;
            GameplayUiModalLock.SetBlockWorldInput(true);
            RefreshOutfitLabel();
        }

        public void Close()
        {
            if (_root == null)
                return;

            bool wasOpen = IsOpen;

            _root.style.display = DisplayStyle.None;
            IsOpen = false;
            GameplayUiModalLock.SetBlockWorldInput(false);

            if (wasOpen)
                WardrobeMission.NotifyWardrobeClosed();
        }

        private void EnsureOutfitController()
        {
            if (_outfit != null)
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            _outfit = player.GetComponent<PlayerOutfitController>();
            if (_outfit == null)
                _outfit = player.gameObject.AddComponent<PlayerOutfitController>();
        }

        private void CycleOutfit(int delta)
        {
            if (_outfit == null)
                return;
            _outfit.Cycle(delta);
            RefreshOutfitLabel();
        }

        private void RefreshOutfitLabel()
        {
            if (_outfitLabel != null && _outfit != null)
                _outfitLabel.text = _outfit.GetCurrentLabel();
        }
    }
}
