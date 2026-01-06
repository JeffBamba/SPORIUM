using UnityEngine;

namespace Sporae.UI.UIToolkit.PotActionsMenu
{
    public class PotActionsMenuOpener : MonoBehaviour
    {
        [SerializeField] private PotActionsMenu _potActionsMenu;

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
            if (_potActionsMenu == null)
            {
                Debug.LogWarning("PotActionsMenuOpener: PotActionsMenu non assegnato!");
                return;
            }

            _potActionsMenu.ShowForPot(pot);
        }
    }
}


