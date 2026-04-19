using System;
using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    public enum HydrationState
    {
        Dehydrated,   // 0-25%
        Low,          // 26-50%
        Normal,       // 51-75%
        WellHydrated  // 76-100%
    }

    /// <summary>
    /// Idratazione del player: influenza la <b>velocità di movimento</b> (piena sopra il 50% H; attenuata 26–50%;
    /// severa 0–25%). Le <b>azioni giornaliere</b> non derivano dall’idratazione (vedi colazione / GameManager).
    /// </summary>
    public class PlayerHydrationSystem
    {
        private float _hydrationPercent = 100f;
        private const float MaxHydration = 100f;

        private const float PassiveConsumptionPerDay = 15f;
        private const float ActiveConsumptionPerAction = 5f;
        private const float RecoverWaterPotable = 25f;
        private const float RecoverWaterRaw = 15f;
        private const float RecoverFood = 8f;
        private const float RecoverFruit = 12f;
        private const float RecoverFruitPure = 18f;

        public float HydrationPercent => _hydrationPercent;
        public HydrationState CurrentState => GetStateFor(_hydrationPercent);

        public event Action<float, float> OnHydrationChanged;

        private void NotifyChanged()
        {
            OnHydrationChanged?.Invoke(_hydrationPercent, MaxHydration);
        }

        private static HydrationState GetStateFor(float percent)
        {
            if (percent <= 25f) return HydrationState.Dehydrated;
            if (percent <= 50f) return HydrationState.Low;
            if (percent <= 75f) return HydrationState.Normal;
            return HydrationState.WellHydrated;
        }

        public void ConsumePassive()
        {
            _hydrationPercent = Mathf.Max(0f, _hydrationPercent - PassiveConsumptionPerDay);
            NotifyChanged();
        }

        public void ConsumeActive(int actionCount)
        {
            float consumed = actionCount * ActiveConsumptionPerAction;
            _hydrationPercent = Mathf.Max(0f, _hydrationPercent - consumed);
            NotifyChanged();
        }

        public void RecoverFromWater(int amount, bool isPotable)
        {
            float recovery = amount * (isPotable ? RecoverWaterPotable : RecoverWaterRaw);
            _hydrationPercent = Mathf.Min(MaxHydration, _hydrationPercent + recovery);
            NotifyChanged();
        }

        public void RecoverFromFood(int amount)
        {
            float recovery = amount * RecoverFood;
            _hydrationPercent = Mathf.Min(MaxHydration, _hydrationPercent + recovery);
            NotifyChanged();
        }

        public void RecoverFromFruit(int amount, bool isPure)
        {
            float perUnit = isPure ? RecoverFruitPure : RecoverFruit;
            float recovery = amount * perUnit;
            _hydrationPercent = Mathf.Min(MaxHydration, _hydrationPercent + recovery);
            NotifyChanged();
        }

        /// <summary>
        /// Moltiplicatore velocità movimento (1 = normale). Sopra il 50% nessuna penalità; prima attenuazione
        /// tra 26–50%; più severa da 25% in giù (rampa fino a minimo a H≈0).
        /// </summary>
        public float GetMovementSpeedMultiplier()
        {
            float h = _hydrationPercent;
            if (h <= 0.001f)
                return 0.08f;

            // Nessuna penalità finché resti sopra la metà barra
            if (h > 50f)
                return 1f;

            // Penalità moderata: ancora giocabile ma si sente
            if (h > 25f)
                return 0.78f;

            // Critico: da ~35% a ~8% della velocità piena tra 25% e 0% H
            return Mathf.Lerp(0.08f, 0.35f, h / 25f);
        }

        /// <summary>
        /// Fascia penalità movimento (allineata a <see cref="GetMovementSpeedMultiplier"/>): 0 = nessuna (&gt;50% H), 1 = moderata (26–50%), 2 = severa (≤25%).
        /// </summary>
        public static int GetMovementSpeedTierIndex(float hydrationPercent)
        {
            float h = hydrationPercent;
            if (h > 50f) return 0;
            if (h > 25f) return 1;
            return 2;
        }

        /// <summary>Chiamato a fine giornata: consumo passivo e notifica.</summary>
        public void ProcessDailyConsumption()
        {
            ConsumePassive();
        }

        /// <summary>Imposta idratazione (per Save/Load).</summary>
        public void SetHydrationPercent(float percent)
        {
            _hydrationPercent = Mathf.Clamp(percent, 0f, MaxHydration);
            NotifyChanged();
        }
    }
}
