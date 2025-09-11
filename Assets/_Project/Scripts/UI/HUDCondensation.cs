using Sporae.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class HUDCondensation : MonoBehaviour
    {
        [SerializeField] private ProgressBar _progressBar;
        [SerializeField] private Button _collectButton;

        private GameManager _gameManager;
        private Inventory _inventory;
        
        private void Awake()
        { 
            _gameManager = FindObjectOfType<GameManager>();
            if (_gameManager == null)
                Debug.LogWarning("There is no GameManager in the scene");
        }

        private void Start()
        {
            _gameManager.OnCondensationChanged += HandleChangeCondensation;
            _collectButton.onClick.AddListener(HandleCollect);

            HandleChangeCondensation(0);
        }

        private void OnDestroy()
        {
            _gameManager.OnCondensationChanged -= HandleChangeCondensation;
        }

        private void HandleChangeCondensation(float value)
        {
            _progressBar.Value = value / _gameManager.GetMaxCondensation();
        }

        private void HandleCollect()
        {
            int amountToCollect = (int)_gameManager.CollectCondensation();
            if (amountToCollect != 0)
                _gameManager.AddItem("WAT-Raw", amountToCollect);
        }
    }
}