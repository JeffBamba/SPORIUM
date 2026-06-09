using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Core.Localization;
using Sporae.UI.UIToolkit.HUD;

namespace _Project
{
    /// <summary>
    /// Aggrega i prompt [E] degli Interactable in range e li mostra in CompactBottomBar (zone-post-center).
    /// </summary>
    public class PlayerInteractAdvice : MonoBehaviour
    {
        private int _interactablesInRange;
        private string _lastSuggestedTarget = string.Empty;
        private CompactBottomBarController _bottomBar;

        private void Awake()
        {
            if (ServiceContainer.Instance == null)
                return;

            ServiceContainer.Instance.Register(this);
        }

        public void AddInteractable(string suggestedTargetName = null)
        {
            _interactablesInRange++;
            if (!string.IsNullOrWhiteSpace(suggestedTargetName))
                _lastSuggestedTarget = suggestedTargetName;
        }

        private void LateUpdate()
        {
            if (_bottomBar == null)
                _bottomBar = ServiceContainer.Instance?.Get<CompactBottomBarController>(suppressWarning: true);

            if (_bottomBar == null)
            {
                _interactablesInRange = 0;
                _lastSuggestedTarget = string.Empty;
                return;
            }

            if (_interactablesInRange > 0)
            {
                string text = !string.IsNullOrWhiteSpace(_lastSuggestedTarget)
                    ? LocalizationManager.GetString("gameplay.interact.press_e_with",
                        new System.Collections.Generic.Dictionary<string, string> { { "name", _lastSuggestedTarget } })
                    : LocalizationManager.GetString("gameplay.interact.press_e");
                _bottomBar.SetInteractionHint(text.Trim().ToUpperInvariant());
            }
            else
            {
                _bottomBar.ClearInteractionHint();
            }

            _interactablesInRange = 0;
            _lastSuggestedTarget = string.Empty;
        }
    }
}
