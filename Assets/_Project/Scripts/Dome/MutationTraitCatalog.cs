using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sporae.Dome
{
    /// <summary>
    /// Task 7 — catalogo data-driven dei tag gameplay assegnabili in mutazione spontanea.
    /// Nessuna classe di simulazione per riga: il comportamento resta nelle primitive
    /// (<see cref="PotStateModel.SelectedTraitsCsv"/> / <see cref="LabHybridGameplayModifiers"/>).
    /// Risorse: <c>Resources/MutationTraitCatalog</c> oppure fallback codice in <see cref="DomeSpontaneousMutation"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "MutationTraitCatalog", menuName = "Sporae/Dome/Mutation Trait Catalog")]
    public class MutationTraitCatalog : ScriptableObject
    {
        [Header("Finestra livello (mutazione spontanea)")]
        [Tooltip("Livello pianta minimo incluso (1 = tutte le piante con pianta).")]
        [Min(1)]
        public int minPlantLevelForSpontaneousMutation = 1;

        [Tooltip("0 = nessun tetto. Altrimenti livello massimo incluso.")]
        public int maxPlantLevelForSpontaneousMutation;

        [Header("Preavviso pressione (Foundation, anti-spam giornaliero)")]
        [Range(0f, 1f)]
        [Tooltip("Soglia IM minima (0–1) per tentare il toast DOME-MUT-WATCH a fine pass.")]
        public float watchToastMinIm = 0.42f;

        [Range(0f, 1f)]
        [Tooltip("Probabilità giornaliera se gli altri criteri sono ok (max 1 toast/giorno).")]
        public float watchToastChance = 0.22f;

        [SerializeField]
        private List<MutationTraitRow> rows = new List<MutationTraitRow>();

        public IReadOnlyList<MutationTraitRow> Rows => rows;

        /// <summary>Righe effettive: asset se presente e non vuoto, altrimenti null → usa builtin nel caller.</summary>
        public bool HasRuntimeRows => rows != null && rows.Count > 0;
    }

    [Serializable]
    public class MutationTraitRow
    {
        [Tooltip("Riferimento design (Notion/wiki); opzionale in runtime.")]
        public string traitId;

        [Tooltip("Deve combaciare con i token in SelectedTraitsCsv (es. GROWTH, YIELD).")]
        public string gameplayTag = "GROWTH";

        public float weightStandard = 1f;
        public float weightPure = 1f;
        public float weightEvil = 1f;
    }
}
