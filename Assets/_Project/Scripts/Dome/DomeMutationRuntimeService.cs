using System;
using UnityEngine;
using _Project;
using Sporae.Dome.PotSystem.Botanical;

namespace Sporae.Dome
{
    /// <summary>
    /// Fonte autoritativa per l'indice di mutazione in HUD (base designer + bonus Glasscap dalle piante attive).
    /// Task 7 — slice 1: centralizza il valore usato da TopBar, Orbit ed End-of-Day senza duplicare la formula.
    /// </summary>
    public sealed class DomeMutationRuntimeService
    {
        public const float BandStableMax = 0.33f;
        public const float BandBalancedMax = 0.66f;

        /// <summary>Base 0–1 impostata dal designer/TopBar prima del bonus botanico.</summary>
        public float DesignerBaseNormalized { get; private set; }

        /// <summary>Somma bonus mutazione da Glasscap attivo (botanical roster).</summary>
        public float GlasscapActiveBonusSum { get; private set; }

        /// <summary>Valore finale clamp 0–1 mostrato in UI e riportato in riepiloghi.</summary>
        public float DisplayNormalized { get; private set; }

        /// <summary>True dopo la prima <see cref="SyncDisplay"/> dalla TopBar (evita 0 fantasma in EoD se il servizio non è mai stato alimentato).</summary>
        public bool HasAuthoritativeSnapshot { get; private set; }

        public event Action<float> OnDisplayMutationChanged;

        public void PushDesignerBase(float designerBase01)
        {
            DesignerBaseNormalized = Mathf.Clamp01(designerBase01);
        }

        public void RefreshFromPh(PhSystem phSystem)
        {
            float bonus = 0f;
            if (phSystem != null)
                bonus = BotanicalRosterSnapshot.FromServices(phSystem).GlasscapActiveMutationBonusSum;
            GlasscapActiveBonusSum = bonus;
            float next = Mathf.Clamp01(DesignerBaseNormalized + bonus);
            if (!Mathf.Approximately(next, DisplayNormalized))
            {
                DisplayNormalized = next;
                OnDisplayMutationChanged?.Invoke(DisplayNormalized);
            }
        }

        /// <summary>Aggiorna base designer e ricalcola il display in un solo passaggio (flusso TopBar).</summary>
        public void SyncDisplay(float designerBase01, PhSystem phSystem)
        {
            HasAuthoritativeSnapshot = true;
            DesignerBaseNormalized = Mathf.Clamp01(designerBase01);
            RefreshFromPh(phSystem);
        }

        /// <summary>Banda testuale allineata alle soglie HUD (verde / giallo / rosso).</summary>
        public static int GetBandIndex(float displayNormalized01)
        {
            float v = Mathf.Clamp01(displayNormalized01);
            if (v <= BandStableMax) return 0;
            if (v <= BandBalancedMax) return 1;
            return 2;
        }

        public static string GetBandLabelItalian(float displayNormalized01)
        {
            return GetBandIndex(displayNormalized01) switch
            {
                0 => "Stabile",
                1 => "Bilanciato",
                _ => "Elevato"
            };
        }
    }
}
