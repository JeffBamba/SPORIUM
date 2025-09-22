
using TMPro;
using UnityEngine;

namespace _Project
{
    public class PlayerInteractAdvice : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _adviceLabel;

        private int _interactablesInRange = 0;
        
        public void AddInteractable()
        {
            _interactablesInRange++;
        }

        private void LateUpdate()
        {
            _adviceLabel.gameObject.SetActive(_interactablesInRange > 0);
            _interactablesInRange = 0;
        }
    }
}