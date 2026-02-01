using UnityEngine;

namespace Sporae.UI.UIToolkit.Lab
{
    /// <summary>
    /// Config per upgrade Lab (modulo Cellule Staminali, Reagenti Sintetici, ecc.).
    /// Acquistabili da mercato nero, fazioni o come ricompensa.
    /// </summary>
    [CreateAssetMenu(fileName = "LabUpgradesConfig", menuName = "Game/Lab Upgrades Config")]
    public class LabUpgradesConfig : ScriptableObject
    {
        [Header("Modulo Cellule Staminali (Extractor)")]
        [Tooltip("Se true, l'Extractor può produrre CELL-001/002/003 da pianta/residui/RES-PROT-001")]
        public bool HasStemCellModule;

        [Header("Placeholder: Reagenti Sintetici (Compost)")]
        [Tooltip("Futuro: modulo per produrre reagenti dal macchinario Compost")]
        public bool HasSyntheticReagentsModule;
    }
}
