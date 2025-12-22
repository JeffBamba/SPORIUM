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
        
        private void OnEnable()
        {
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
    }
}

