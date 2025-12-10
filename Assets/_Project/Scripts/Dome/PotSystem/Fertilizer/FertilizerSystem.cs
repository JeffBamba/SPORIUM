using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.Dome.PotSystem.Fertilizer
{
    /// <summary>
    /// BLK-03.01-T1: Sistema fertilizzante per piante.
    /// Gestisce i tre tipi di fertilizzanti (Standard, Pure, Prohibited) e la coerenza genetica.
    /// </summary>
    public enum FertilizerType
    {
        /// <summary>
        /// Fertilizzante standard: 25 CRY, +25% fertilizzante, fazioni neutrali
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// Fertilizzante puro: 75 CRY, +40% fertilizzante, Custode + Mercato Nero
        /// </summary>
        Pure = 1,
        
        /// <summary>
        /// Fertilizzante proibito: 75 CRY, +40% fertilizzante, Culto Muffa + Mercato Nero
        /// </summary>
        Prohibited = 2
    }
    
    /// <summary>
    /// BLK-03.01-T1: Sistema statico per gestire fertilizzanti.
    /// </summary>
    public static class FertilizerSystem
    {
        // Costanti percentuali fertilizzante per tipo
        private const int FERTILIZER_STANDARD_AMOUNT = 25;   // 25%
        private const int FERTILIZER_PURE_AMOUNT = 40;       // 40%
        private const int FERTILIZER_PROHIBITED_AMOUNT = 40; // 40%
        
        // Costanti costi
        private const int COST_STANDARD = 25;   // CRY
        private const int COST_PURE = 75;       // CRY
        private const int COST_PROHIBITED = 75; // CRY
        
        // Decadimento fertilizzante giornaliero (es. -5% al giorno)
        private const float DEFAULT_DECAY_RATE = 5f;
        
        /// <summary>
        /// Applica decadimento giornaliero al fertilizzante
        /// </summary>
        /// <param name="pot">Stato del vaso</param>
        /// <param name="decayRate">Percentuale di decadimento giornaliero (default: 5%)</param>
        public static void ApplyDailyDecay(PotStateModel pot, float decayRate = DEFAULT_DECAY_RATE)
        {
            if (pot == null || !pot.HasPlant)
                return;
            
            if (pot.FertilizerLevel > 0)
            {
                pot.FertilizerLevel = Mathf.Clamp(
                    Mathf.RoundToInt(pot.FertilizerLevel - decayRate),
                    0, 100);
                
                // Se raggiunge 0, reset contatore giorni
                if (pot.FertilizerLevel == 0)
                {
                    pot.DaysFertilizerActive = 0;
                }
            }
        }
        
        /// <summary>
        /// Verifica se il fertilizzante è nel range ottimale per lo stadio
        /// </summary>
        /// <param name="pot">Stato del vaso</param>
        /// <param name="stageReq">Requisiti dello stadio corrente</param>
        /// <returns>True se il fertilizzante è nel range ottimale</returns>
        public static bool IsFertilizerInOptimalRange(PotStateModel pot, StageRequirements stageReq)
        {
            if (pot == null || stageReq == null)
                return false;
            
            return stageReq.IsFertilizerInRange(pot.FertilizerLevel);
        }
        
        /// <summary>
        /// Calcola livello fertilizzante dopo applicazione
        /// </summary>
        /// <param name="currentLevel">Livello attuale (0-100)</param>
        /// <param name="fertilizerType">Tipo di fertilizzante applicato</param>
        /// <returns>Nuovo livello fertilizzante (clamp 0-100)</returns>
        public static int CalculateFertilizerLevel(int currentLevel, FertilizerType fertilizerType)
        {
            int amount = GetFertilizerAmount(fertilizerType);
            return Mathf.Clamp(currentLevel + amount, 0, 100);
        }
        
        /// <summary>
        /// Ottiene la percentuale di fertilizzante per tipo
        /// </summary>
        /// <param name="type">Tipo di fertilizzante</param>
        /// <returns>Percentuale di fertilizzante (25% Standard, 40% Pure/Prohibited)</returns>
        public static int GetFertilizerAmount(FertilizerType type)
        {
            return type switch
            {
                FertilizerType.Standard => FERTILIZER_STANDARD_AMOUNT,
                FertilizerType.Pure => FERTILIZER_PURE_AMOUNT,
                FertilizerType.Prohibited => FERTILIZER_PROHIBITED_AMOUNT,
                _ => 0
            };
        }
        
        /// <summary>
        /// Ottiene il costo in CRY per tipo di fertilizzante
        /// </summary>
        /// <param name="type">Tipo di fertilizzante</param>
        /// <returns>Costo in CRY</returns>
        public static int GetFertilizerCost(FertilizerType type)
        {
            return type switch
            {
                FertilizerType.Standard => COST_STANDARD,
                FertilizerType.Pure => COST_PURE,
                FertilizerType.Prohibited => COST_PROHIBITED,
                _ => 0
            };
        }
        
        /// <summary>
        /// REGOLA CRITICA: Verifica coerenza genetica tra fertilizzante e famiglia pianta.
        /// L'uso di fertilizzanti incompatibili causa la MORTE IMMEDIATA della pianta!
        /// </summary>
        /// <param name="fertilizerType">Tipo di fertilizzante da applicare</param>
        /// <param name="plantFamily">Famiglia della pianta</param>
        /// <returns>True se compatibile, False se incompatibile (morte immediata)</returns>
        public static bool IsFertilizerCompatible(FertilizerType fertilizerType, PlantFamily plantFamily)
        {
            return plantFamily switch
            {
                // Standard → solo Standard (più restrittiva)
                PlantFamily.Standard => fertilizerType == FertilizerType.Standard,
                
                // Pure → Pure o Standard (tollerante verso Standard)
                PlantFamily.Pure => fertilizerType == FertilizerType.Pure || fertilizerType == FertilizerType.Standard,
                
                // Evil → Prohibited o Standard (tollerante verso Standard)
                PlantFamily.Evil => fertilizerType == FertilizerType.Prohibited || fertilizerType == FertilizerType.Standard,
                
                _ => false
            };
        }
        
        /// <summary>
        /// Ottiene una descrizione testuale della compatibilità
        /// </summary>
        /// <param name="fertilizerType">Tipo di fertilizzante</param>
        /// <param name="plantFamily">Famiglia della pianta</param>
        /// <returns>Descrizione della compatibilità</returns>
        public static string GetCompatibilityDescription(FertilizerType fertilizerType, PlantFamily plantFamily)
        {
            bool isCompatible = IsFertilizerCompatible(fertilizerType, plantFamily);
            
            if (isCompatible)
            {
                return $"Fertilizzante {fertilizerType} compatibile con pianta {plantFamily}";
            }
            else
            {
                return $"⚠️ FERTILIZZANTE INCOMPATIBILE! {fertilizerType} su {plantFamily} = MORTE IMMEDIATA";
            }
        }
    }
}

