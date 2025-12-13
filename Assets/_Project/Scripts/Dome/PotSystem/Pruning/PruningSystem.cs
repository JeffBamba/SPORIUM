using System.IO;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.Dome.PotSystem.Pruning
{
    /// <summary>
    /// Risultato di un tentativo di potatura
    /// </summary>
    public enum PruningResultType
    {
        Failure,        // Fallimento
        SuccessCure,    // Successo: rimozione infestazione
        SuccessResa      // Successo: bonus resa (solo Growth pre-Flowering)
    }
    
    /// <summary>
    /// Risultato di un tentativo di potatura
    /// </summary>
    public struct PruningResult
    {
        public bool Success;
        public PruningResultType ResultType;
        public string Message;
        
        public PruningResult(bool success, PruningResultType resultType, string message)
        {
            Success = success;
            ResultType = resultType;
            Message = message;
        }
    }
    
    /// <summary>
    /// Sistema di potatura (AZ-13).
    /// Gestisce calcolo RNG, reroll con Spray, e applicazione bonus resa.
    /// </summary>
    public static class PruningSystem
    {
        /// <summary>
        /// Tenta di eseguire potatura con calcolo RNG basato su stadio
        /// </summary>
        /// <param name="potState">Stato del vaso</param>
        /// <param name="currentStage">Stadio corrente della pianta</param>
        /// <param name="useSpray">Se true, usa Spray Antifungino per bonus e reroll</param>
        /// <param name="config">Configurazione potatura</param>
        /// <returns>Risultato del tentativo</returns>
        public static PruningResult TryPrune(PotStateModel potState, PlantStage currentStage, bool useSpray, PruningConfig config)
        {
            if (potState == null || config == null)
            {
                return new PruningResult(false, PruningResultType.Failure, "Parametri non validi");
            }
            
            // Ottieni probabilità base per stadio
            float baseSuccessRate = config.GetBaseSuccessRate(currentStage);
            float finalSuccessRate = baseSuccessRate;
            
            // Applica bonus Spray se usato
            if (useSpray)
            {
                float sprayBonus = config.GetSprayBonus(currentStage);
                finalSuccessRate = Mathf.Clamp(baseSuccessRate + sprayBonus, 0f, 100f);
            }
            
            // #region agent log
            try {
                var logData = new { baseSuccessRate = baseSuccessRate, finalSuccessRate = finalSuccessRate, useSpray = useSpray, currentStage = currentStage.ToString() };
                var logJson = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"BUG1-B\",\"location\":\"PruningSystem.cs:56\",\"message\":\"TryPrune: Success rates calculated\",\"data\":{JsonUtility.ToJson(logData)},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logJson);
            } catch { }
            // #endregion
            
            // Primo tentativo
            float roll1 = Random.Range(0f, 100f);
            bool firstAttemptSuccess = roll1 < finalSuccessRate;
            // #region agent log
            try {
                var logData2 = new { roll1 = roll1, firstAttemptSuccess = firstAttemptSuccess };
                var logJson2 = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"BUG1-B\",\"location\":\"PruningSystem.cs:66\",\"message\":\"TryPrune: First roll\",\"data\":{JsonUtility.ToJson(logData2)},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logJson2);
            } catch { }
            // #endregion
            
            // Se fallisce e usa Spray, esegui reroll
            if (!firstAttemptSuccess && useSpray)
            {
                float roll2 = Random.Range(0f, 100f);
                firstAttemptSuccess = roll2 < finalSuccessRate;
                // #region agent log
                try {
                    var logData3 = new { roll2 = roll2, firstAttemptSuccess = firstAttemptSuccess };
                    var logJson3 = $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"BUG1-B\",\"location\":\"PruningSystem.cs:71\",\"message\":\"TryPrune: Reroll with spray\",\"data\":{JsonUtility.ToJson(logData3)},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
                    System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logJson3);
                } catch { }
                // #endregion
            }
            
            // Determina tipo risultato
            if (firstAttemptSuccess)
            {
                // Se è Growth pre-Flowering, può essere bonus resa
                if (currentStage == PlantStage.Growth && !potState.HasPruningResaBonus)
                {
                    return new PruningResult(true, PruningResultType.SuccessResa, 
                        "Potatura pulita. Fioritura prevista più ricca.");
                }
                else
                {
                    return new PruningResult(true, PruningResultType.SuccessCure, 
                        useSpray ? "Potatura + Antifungino applicati. Pianta stabilizzata." : "Potatura riuscita. Infestazione rimossa.");
                }
            }
            else
            {
                return new PruningResult(false, PruningResultType.Failure, 
                    useSpray ? "Antifungino insufficiente. Nessun effetto." : "Potatura non riuscita. Nessun effetto.");
            }
        }
        
        /// <summary>
        /// Applica bonus resa se in Growth pre-Flowering
        /// </summary>
        /// <param name="potState">Stato del vaso</param>
        /// <param name="config">Configurazione potatura</param>
        /// <returns>True se bonus applicato, False se non applicabile</returns>
        public static bool ApplyResaBonus(PotStateModel potState, PruningConfig config)
        {
            if (potState == null || config == null)
                return false;
            
            // Verifica stadio Growth
            if ((PlantStage)potState.Stage != PlantStage.Growth)
                return false;
            
            // Verifica cap non cumulabile
            if (potState.HasPruningResaBonus)
                return false;
            
            // Applica bonus
            if (config.usePercentageBonus)
            {
                // +10% quantità (da applicare al momento del calcolo resa)
                // Questo sarà gestito nel sistema di harvest
                potState.HasPruningResaBonus = true;
                return true;
            }
            else
            {
                // +1 frutto (da applicare al momento della produzione frutti)
                // Questo sarà gestito nel sistema di produzione frutti
                potState.HasPruningResaBonus = true;
                return true;
            }
        }
    }
}

