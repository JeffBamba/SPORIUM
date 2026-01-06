using UnityEngine;
using Sporae.UI.UIToolkit.PlantCard;

namespace Sporae.UI.UIToolkit.PlantCard
{
    /// <summary>
    /// Helper script per aprire PlantCardV2 quando un pot viene selezionato.
    /// Collega l'evento PotSlot.OnPotSelected con PlantCardV2Controller.
    /// </summary>
    public class PlantCardV2Opener : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlantCardV2Controller _plantCardController;

        [Header("Behavior")]
        [SerializeField] private bool _autoOpenOnPotSelected = false;
        
        private void OnEnable()
        {
            if (_autoOpenOnPotSelected)
                PotSlot.OnPotSelected += OnPotSelected;
        }
        
        private void OnDisable()
        {
            PotSlot.OnPotSelected -= OnPotSelected;
        }
        
        private void OnPotSelected(PotSlot pot)
        {
            if (_plantCardController != null)
            {
                _plantCardController.ShowForPot(pot);
            }
            else
            {
                Debug.LogWarning("PlantCardV2Opener: PlantCardV2Controller non assegnato!");
            }
        }

        /// <summary>
        /// Apre esplicitamente la PlantCardV2 per il pot selezionato (usato dal Pot Ops menu).
        /// </summary>
        public void OpenForInspect(PotSlot pot)
        {
            if (_plantCardController != null)
            {
                _plantCardController.ShowForPot(pot);
            }
            else
            {
                Debug.LogWarning("PlantCardV2Opener: PlantCardV2Controller non assegnato!");
            }
        }
    }
}

