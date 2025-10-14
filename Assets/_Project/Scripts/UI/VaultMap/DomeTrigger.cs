using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class DomeTrigger : MonoBehaviour
    {
        [SerializeField] private Button _collectWaterButton;
        [SerializeField] private Transform _playerTransform;

        private void Update()
        {
            _collectWaterButton.interactable =
                _playerTransform.position is { x: < 3, y: 0 };
        }
    }
}