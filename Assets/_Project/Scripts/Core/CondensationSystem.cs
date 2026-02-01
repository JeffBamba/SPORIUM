using System.Collections.Generic;
using UnityEngine;
using Sporae.DevTools;
using Sporae.Dome.PotSystem.Growth;
using Sporae.Dome.PotSystem.Condition;

namespace _Project
{
    /// <summary>
    /// Sistema di condensazione completo: calcolo dinamico basato su piante, stage, salute, bonus LED.
    /// Accumulo in percentuale (0-100%) invece di valore assoluto.
    /// </summary>
    public class CondensationSystem
    {
        private CondensationConfig _config;
        private float _currentAccumulation = 0f; // 0-100% (non più 0-10)
        private float _dailyProduction = 0f;     // Produzione del giorno corrente
        private int _collectionCap = 8;           // Cap base, 12 con upgrade
        private bool _hasExtendedBasin = false;  // Upgrade Bacino di Raccolta
        
        // Retrocompatibilità: CondensationAmount restituisce percentuale (0-100%)
        public float CondensationAmount => _currentAccumulation;
        
        // Nuove proprietà
        public float CurrentAccumulation => _currentAccumulation; // 0-100%
        public float DailyProduction => _dailyProduction;
        public bool HasExtendedBasin => _hasExtendedBasin;
        
        // Valori default se config non trovato
        private const int DEFAULT_BASE_CAP = 8;
        private const int DEFAULT_EXTENDED_BASIN_CAP = 12;
        private const float DEFAULT_BASE_CONTRIBUTION_SANA = 2f;
        private const float DEFAULT_BASE_CONTRIBUTION_STRESSATA = 1f;
        private const float DEFAULT_LED_BONUS = 2f;
        
        public CondensationSystem()
        {
            _config = Resources.Load<CondensationConfig>("Configs/Condensation Config");
            if (_config == null)
            {
                SporiumLogger.LogError(LogCategory.Core, "CondensationConfig non trovato! Usando valori default.");
            }
            
            // Inizializza cap dal config
            _collectionCap = _config != null ? _config.BaseCap : DEFAULT_BASE_CAP;
        }
        
        /// <summary>
        /// Gestisce il cambio di giorno: calcola produzione giornaliera e aggiorna accumulo.
        /// </summary>
        /// <param name="activePots">Lista di vasi attivi con piante</param>
        /// <param name="hasActiveLed">True se almeno un LED è attivo (Blue o Red)</param>
        public void DayChanged(List<PotStateModel> activePots, bool hasActiveLed)
        {
            _dailyProduction = CalculateDailyProduction(activePots, hasActiveLed);
            _currentAccumulation += _dailyProduction;
            _currentAccumulation = Mathf.Clamp(_currentAccumulation, 0f, 100f);
        }
        
        /// <summary>
        /// Calcola la produzione giornaliera di WAT-RAW basata su piante attive e bonus LED.
        /// Formula: Σ(contributo pianta) + bonus LED, con cap applicato.
        /// </summary>
        private float CalculateDailyProduction(List<PotStateModel> pots, bool hasLed)
        {
            float total = 0f;
            
            if (pots != null)
            {
                foreach (var pot in pots)
                {
                    if (pot == null || !pot.HasPlant) continue;
                    
                    // Base per stato salute
                    float baseContribution = GetBaseContribution(pot);
                    
                    // Moltiplicatore stage
                    float stageMultiplier = GetStageMultiplier((PlantStage)pot.Stage);
                    
                    total += baseContribution * stageMultiplier;
                }
            }
            
            // Bonus LED
            if (hasLed)
            {
                float ledBonus = _config != null ? _config.LedBonus : DEFAULT_LED_BONUS;
                total += ledBonus;
            }
            
            // Applica cap (8 base, 12 con upgrade)
            int currentCap = _hasExtendedBasin ? 
                (_config != null ? _config.ExtendedBasinCap : DEFAULT_EXTENDED_BASIN_CAP) :
                (_config != null ? _config.BaseCap : DEFAULT_BASE_CAP);
            
            return Mathf.Min(total, currentCap);
        }
        
        /// <summary>
        /// Calcola contributo base per stato salute della pianta.
        /// - Rigogliosa/Sana: 2
        /// - Appassita: 1
        /// - Critica/Morta: 0
        /// </summary>
        private float GetBaseContribution(PotStateModel pot)
        {
            if (pot == null) return 0f;
            
            PlantCondition condition = (PlantCondition)pot.ConditionLabel;
            
            switch (condition)
            {
                case PlantCondition.Rigogliosa:
                case PlantCondition.Sana:
                    return _config != null ? _config.BaseContributionSana : DEFAULT_BASE_CONTRIBUTION_SANA;
                
                case PlantCondition.Appassita:
                    return _config != null ? _config.BaseContributionStressata : DEFAULT_BASE_CONTRIBUTION_STRESSATA;
                
                case PlantCondition.Critica:
                case PlantCondition.Morta:
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// Calcola moltiplicatore per stage della pianta.
        /// - Seed/Sprout: ×0 (non contribuiscono)
        /// - Growth: ×1
        /// - Flowering: ×2
        /// - HarvestReady: ×1
        /// - Resting: ×1
        /// </summary>
        private float GetStageMultiplier(PlantStage stage)
        {
            switch (stage)
            {
                case PlantStage.Seed:
                case PlantStage.Sprout:
                    return 0f; // Non contribuiscono
                
                case PlantStage.Growth:
                    return 1f;
                
                case PlantStage.Flowering:
                    return 2f; // Contributo doppio
                
                case PlantStage.HarvestReady:
                case PlantStage.Resting:
                    return 1f;
                
                case PlantStage.Empty:
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// Resetta l'accumulo di condensazione a 0.
        /// </summary>
        public void Reset()
        {
            _currentAccumulation = 0f;
            _dailyProduction = 0f;
        }
        
        /// <summary>
        /// Retrocompatibilità: restituisce 100 (max percentuale).
        /// </summary>
        public float GetMax()
        {
            return 100f; // Sempre 100% nel nuovo sistema
        }
        
        /// <summary>
        /// Attiva/disattiva upgrade Bacino di Raccolta esteso (cap 8→12).
        /// </summary>
        public void SetExtendedBasin(bool enabled)
        {
            _hasExtendedBasin = enabled;
            
            // Aggiorna cap immediatamente
            _collectionCap = _hasExtendedBasin ? 
                (_config != null ? _config.ExtendedBasinCap : DEFAULT_EXTENDED_BASIN_CAP) :
                (_config != null ? _config.BaseCap : DEFAULT_BASE_CAP);
        }
        
        // ===== METODI DEBUG/RUNTIME EDITING =====
        
        /// <summary>
        /// DEBUG: Imposta l'accumulo corrente di condensazione (0-100%).
        /// </summary>
        public void SetCurrentAccumulation(float value)
        {
            _currentAccumulation = Mathf.Clamp(value, 0f, 100f);
        }
        
        /// <summary>
        /// DEBUG: Imposta la produzione giornaliera corrente.
        /// </summary>
        public void SetDailyProduction(float value)
        {
            _dailyProduction = value;
        }
        
        /// <summary>
        /// DEBUG: Ottiene il cap corrente di raccolta.
        /// </summary>
        public int GetCollectionCap()
        {
            return _collectionCap;
        }
        
        /// <summary>
        /// DEBUG: Imposta il cap di raccolta (ignora upgrade se necessario).
        /// </summary>
        public void SetCollectionCap(int value)
        {
            _collectionCap = Mathf.Max(1, value);
        }
        
        /// <summary>
        /// DEBUG: Ottiene i valori del config (per visualizzazione).
        /// </summary>
        public (int baseCap, int extendedCap, float baseSana, float baseStressata, float ledBonus) GetConfigValues()
        {
            if (_config != null)
            {
                return (_config.BaseCap, _config.ExtendedBasinCap, 
                        _config.BaseContributionSana, _config.BaseContributionStressata, 
                        _config.LedBonus);
            }
            return (DEFAULT_BASE_CAP, DEFAULT_EXTENDED_BASIN_CAP, 
                    DEFAULT_BASE_CONTRIBUTION_SANA, DEFAULT_BASE_CONTRIBUTION_STRESSATA, 
                    DEFAULT_LED_BONUS);
        }
    }
}