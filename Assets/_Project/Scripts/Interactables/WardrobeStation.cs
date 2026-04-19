using _Project.Sporae.Core;
using Sporae.UI.UIToolkit.HUD;
using Sporae.DevTools;
using UnityEngine;

namespace _Project
{
    /// <summary>
    /// Interazione con l'armadio in scena: apre il pannello UI Toolkit e completa la missione flag (Task 2) alla prima apertura.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    [DefaultExecutionOrder(-5)]
    public sealed class WardrobeStation : MonoBehaviour
    {
        [Tooltip("Raggio interazione consigliato per la camera da letto (world units).")]
        [SerializeField] private float _interactDistanceMeters = 4f;

        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            if (_interactable != null)
                _interactable.SetRepeatInteractionWhileInRange(true);
        }

        private void Start()
        {
            if (_interactable != null && _interactDistanceMeters > 0.25f)
                _interactable.SetInteractDistance(_interactDistanceMeters);
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.OnInteract += HandleInteract;
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.OnInteract -= HandleInteract;
        }

        private void HandleInteract()
        {
            var panel = ResolveWardrobePanel();
            if (panel == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                SporiumLogger.LogWarning(LogCategory.UI, "[Wardrobe] WardrobePanelController non trovato (ServiceContainer / scena).");
#endif
                return;
            }

            if (panel.IsOpen)
                return;

            panel.Open();
        }

        private static WardrobePanelController ResolveWardrobePanel()
        {
            var sc = ServiceContainer.Instance;
            if (sc != null)
            {
                var p = sc.Get<WardrobePanelController>(suppressWarning: true);
                if (p != null)
                    return p;
            }

            return UnityEngine.Object.FindObjectOfType<WardrobePanelController>();
        }
    }
}
