using UnityEngine;

namespace Sporae.Dome.PotSystem.Condition
{
    /// <summary>
    /// Condizione di salute della pianta (Rigogliosa, Sana, Appassita, Critica)
    /// NOTA: Stressata rimosso dalla logica (mantenuto solo enum per retrocompatibilità con dati salvati)
    /// </summary>
    public enum PlantCondition
    {
        Rigogliosa = 0,   // 80-100: Verde scuro, forecast positivo
        Sana = 1,         // 40-80: Verde, forecast neutro/positivo (range ampliato)
        Stressata = 2,    // RIMOSSO dalla logica - mantenuto solo per retrocompatibilità (dati salvati vecchi)
        Appassita = 3,    // 20-40: Arancione, forecast negativo
        Critica = 4,      // 0-20: Rosso, forecast negativo grave (Burned/Infestata/Sterile)
        Morta = 5         // Stato irreversibile: rimane finché non si esegue UPROOT
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
                PlantCondition.Morta => new Color(0.25f, 0.25f, 0.25f),    // Grigio scuro
                _ => Color.gray
            };
        }
    }
}

