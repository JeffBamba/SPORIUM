using UnityEngine;

namespace Sporae.Dome.PotSystem.Condition
{
    /// <summary>
    /// Condizione di salute della pianta (Rigogliosa, Sana, Stressata, Appassita, Critica)
    /// </summary>
    public enum PlantCondition
    {
        Rigogliosa = 0,   // 90-100: Verde scuro, forecast positivo
        Sana = 1,         // 70-89: Verde, forecast neutro/positivo
        Stressata = 2,    // 40-69: Giallo, forecast neutro/negativo
        Appassita = 3,    // 20-39: Arancione, forecast negativo
        Critica = 4       // 0-19: Rosso, forecast negativo grave (Burned/Infestata/Sterile)
    }
    
    /// <summary>
    /// Direzione del forecast (tendenza di crescita/regressione)
    /// </summary>
    public enum ForecastDirection
    {
        Up = 0,      // ↑ Tendenza positiva (Δ > +5)
        Stable = 1,   // → Stabile (Δ tra -5 e +5)
        Down = 2     // ↓ Tendenza negativa (Δ < -5)
    }
    
    /// <summary>
    /// Contributo al calcolo dello score di condizione
    /// </summary>
    public struct ConditionContributor
    {
        public string Source;      // Nome del contributo (es. "Idratazione ottimale")
        public int Value;          // Valore del contributo (+20, -15, etc.)
        public bool IsPositive;    // True se positivo, false se negativo
        
        public ConditionContributor(string source, int value, bool isPositive)
        {
            Source = source;
            Value = value;
            IsPositive = isPositive;
        }
    }
    
    /// <summary>
    /// Risultato del calcolo della condizione
    /// </summary>
    public struct ConditionResult
    {
        public int Score;                          // Score 0-100
        public PlantCondition Condition;          // Condizione mappata
        public ForecastDirection Forecast;        // Direzione forecast
        public int ScoreDelta;                    // Δ rispetto al giorno precedente
        public ConditionContributor[] Contributors; // Array di contributi attivi
        public Color ConditionColor;              // Colore per la progress bar
        
        public ConditionResult(int score, PlantCondition condition, ForecastDirection forecast, 
            int scoreDelta, ConditionContributor[] contributors)
        {
            Score = score;
            Condition = condition;
            Forecast = forecast;
            ScoreDelta = scoreDelta;
            Contributors = contributors ?? new ConditionContributor[0];
            
            // Colore in base alla condizione
            ConditionColor = condition switch
            {
                PlantCondition.Rigogliosa => new Color(0f, 0.5f, 0f),      // Verde scuro
                PlantCondition.Sana => new Color(0f, 0.8f, 0f),          // Verde
                PlantCondition.Stressata => new Color(1f, 0.8f, 0f),      // Giallo
                PlantCondition.Appassita => new Color(1f, 0.5f, 0f),     // Arancione
                PlantCondition.Critica => new Color(0.8f, 0f, 0f),        // Rosso
                _ => Color.gray
            };
        }
    }
}

