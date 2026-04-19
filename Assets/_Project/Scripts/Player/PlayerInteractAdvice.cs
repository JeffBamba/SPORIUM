
using TMPro;
using UnityEngine;
using _Project.Sporae.Core;

namespace _Project
{
    public class PlayerInteractAdvice : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _adviceLabel;

        private int _interactablesInRange = 0;

        private void Awake()
        {
            if (ServiceContainer.Instance == null)
                return;

            ServiceContainer.Instance.Register(this);
        }
        
        public void AddInteractable()
        {
            if (_adviceLabel == null)
                return;
            _interactablesInRange++;
        }

        private void LateUpdate()
        {
            if (_adviceLabel == null)
            {
                _interactablesInRange = 0;
                return;
            }

            _adviceLabel.gameObject.SetActive(_interactablesInRange > 0);
            _interactablesInRange = 0;
        }
    }
}