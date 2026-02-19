using System;
using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    public enum HydrationState
    {
        Dehydrated,   // 0-25%: -2 Azioni
        Low,          // 26-50%: -1 Azione
        Normal,       // 51-75%: 0
        WellHydrated  // 76-100%: +2 Azioni
    }

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

        /// <summary>Ritorna il modificatore azioni per il giorno successivo: -2, -1, 0, +2.</summary>
        public int GetActionModifier()
        {
            switch (CurrentState)
            {
                case HydrationState.Dehydrated: return -2;
                case HydrationState.Low: return -1;
                case HydrationState.Normal: return 0;
                case HydrationState.WellHydrated: return 2;
                default: return 0;
            }
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
