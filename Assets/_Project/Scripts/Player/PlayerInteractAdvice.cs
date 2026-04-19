
using TMPro;
using UnityEngine;
using _Project.Sporae.Core;

namespace _Project
{
    public class PlayerInteractAdvice : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _adviceLabel;

        private int _interactablesInRange = 0;
        private string _lastSuggestedTarget = string.Empty;

        private void Awake()
        {
            if (ServiceContainer.Instance == null)
                return;

            ServiceContainer.Instance.Register(this);
        }
        
        public void AddInteractable(string suggestedTargetName = null)
        {
            if (_adviceLabel == null)
                return;
            _interactablesInRange++;
            if (!string.IsNullOrWhiteSpace(suggestedTargetName))
                _lastSuggestedTarget = suggestedTargetName;
        }

        private void LateUpdate()
        {
            if (_adviceLabel == null)
            {
                _interactablesInRange = 0;
                return;
            }

            bool isVisible = _interactablesInRange > 0;
            _adviceLabel.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                if (!string.IsNullOrWhiteSpace(_lastSuggestedTarget))
                    _adviceLabel.text = $"Premi 'E' per interagire con \"{_lastSuggestedTarget}\"";
                else
                    _adviceLabel.text = "Premi 'E' per interagire";
            }
            _interactablesInRange = 0;
            _lastSuggestedTarget = string.Empty;
        }
    }
}