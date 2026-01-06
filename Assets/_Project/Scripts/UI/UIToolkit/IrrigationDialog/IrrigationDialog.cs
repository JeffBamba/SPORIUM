using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.IrrigationDialog
{
    [RequireComponent(typeof(UIDocument))]
    public class IrrigationDialog : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private VisualElement _overlay;
        private Label _seedNameLabel;
        private Button _btnIrrigate;
        private Button _btnPlantOnly;

        private Action<bool> _onResult;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            // DEBUG_SAFE_FIX: Imposta sortingOrder sia su UIDocument che su Canvas parent (se presente)
            // Dialog modali devono stare sopra tutto, incluso PlantCard (300)
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = 500;
                
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 500;
                }
            }

            _root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (_root == null)
            {
                Debug.LogError("IrrigationDialog: rootVisualElement non trovato!");
                return;
            }

            _overlay = _root.Q<VisualElement>("irrig-overlay");
            _seedNameLabel = _root.Q<Label>("irrig-seedname");
            _btnIrrigate = _root.Q<Button>("btn-irrigate");
            _btnPlantOnly = _root.Q<Button>("btn-plant-only");

            if (_btnIrrigate != null) _btnIrrigate.clicked += () => Resolve(true);
            if (_btnPlantOnly != null) _btnPlantOnly.clicked += () => Resolve(false);
        }

        private void Start()
        {
            Hide();
        }

        public void Show(string seedDisplayName, Action<bool> onResult)
        {
            _onResult = onResult;
            if (_seedNameLabel != null)
                _seedNameLabel.text = string.IsNullOrEmpty(seedDisplayName) ? "SEED" : seedDisplayName.ToUpperInvariant();

            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.pickingMode = PickingMode.Position;
            }

            if (_root != null)
                _root.pickingMode = PickingMode.Position;

            if (_uiDocument != null)
            {
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvas.enabled = true;
            }
        }

        public void Hide()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Ignore;
            }

            if (_root != null)
                _root.pickingMode = PickingMode.Ignore;

            if (_uiDocument != null)
            {
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvas.enabled = false;
            }

            _onResult = null;
        }

        private void Resolve(bool irrigate)
        {
            var cb = _onResult;
            _onResult = null;
            cb?.Invoke(irrigate);
        }
    }
}


