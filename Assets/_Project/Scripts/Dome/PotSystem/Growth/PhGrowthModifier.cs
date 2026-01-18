using _Project;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// FASE 2: Modificatori crescita e resa basati su pH e famiglia pianta
    /// </summary>
    public static class PhGrowthModifier
    {
        /// <summary>
        /// Calcola moltiplicatore crescita basato su banda pH e famiglia pianta
        /// </summary>
        /// <param name="phBand">Banda pH corrente</param>
        /// <param name="family">Famiglia della pianta</param>
        /// <returns>Moltiplicatore crescita (es. 1.5f = +50%)</returns>
        public static float GetGrowthMultiplier(PhSystem.PhBand phBand, PlantFamily family)
        {
            // Thriving: pH favorevole alla famiglia
            // Pure in Ultra Basico o Stable Basic: +50% crescita
            // Evil in Ultra Acido o Stable Acid: +50% crescita
            // Weakening: pH opposto ma non estremo: -30% crescita
            // Stable: pH neutrale: 0% (normale)
            // Collapsing: pH estremo opposto (già gestito da countdown morte): 0% (ma countdown attivo)
            
            switch (family)
            {
                case PlantFamily.Pure:
                    // Pure preferiscono pH basico
                    if (phBand == PhSystem.PhBand.UltraBasic || phBand == PhSystem.PhBand.StableBasic)
                    {
                        return 1.5f; // +50% crescita (Thriving)
                    }
                    else if (phBand == PhSystem.PhBand.UltraAcid || phBand == PhSystem.PhBand.StableAcid)
                    {
                        // Se è UltraAcid, potrebbe essere in countdown morte (gestito altrove)
                        // Se è StableAcid, è Weakening
                        if (phBand == PhSystem.PhBand.StableAcid)
                        {
                            return 0.7f; // -30% crescita (Weakening)
                        }
                        // UltraAcid: potrebbe essere in countdown, ma per ora restituiamo 0% (gestito da countdown)
                        return 1.0f;
                    }
                    break;
                    
                case PlantFamily.Evil:
                    // Evil preferiscono pH acido
                    if (phBand == PhSystem.PhBand.UltraAcid || phBand == PhSystem.PhBand.StableAcid)
                    {
                        return 1.5f; // +50% crescita (Thriving)
                    }
                    else if (phBand == PhSystem.PhBand.UltraBasic || phBand == PhSystem.PhBand.StableBasic)
                    {
                        // Se è UltraBasic, potrebbe essere in countdown morte (gestito altrove)
                        // Se è StableBasic, è Weakening
                        if (phBand == PhSystem.PhBand.StableBasic)
                        {
                            return 0.7f; // -30% crescita (Weakening)
                        }
                        // UltraBasic: potrebbe essere in countdown, ma per ora restituiamo 0% (gestito da countdown)
                        return 1.0f;
                    }
                    break;
                    
                case PlantFamily.Standard:
                    // Standard preferiscono pH neutrale
                    if (phBand == PhSystem.PhBand.Neutral)
                    {
                        return 1.0f; // 0% (normale, ma potrebbe essere considerato Thriving)
                    }
                    // Standard non hanno preferenze forti, ma pH estremi possono essere problematici
                    if (phBand == PhSystem.PhBand.UltraAcid || phBand == PhSystem.PhBand.UltraBasic)
                    {
                        return 0.7f; // -30% crescita (Weakening)
                    }
                    break;
            }
            
            // Default: pH neutrale o non specificato
            return 1.0f;
        }
        
        /// <summary>
        /// Calcola moltiplicatore resa basato su banda pH e famiglia pianta
        /// </summary>
        /// <param name="phBand">Banda pH corrente</param>
        /// <param name="family">Famiglia della pianta</param>
        /// <returns>Moltiplicatore resa (es. 1.5f = +50%)</returns>
        public static float GetYieldMultiplier(PhSystem.PhBand phBand, PlantFamily family)
        {
            // Ultra Acido: Evil +50% resa, Pure collassano (countdown)
            // Ultra Basico: Pure +100% resa ma sterili, Evil collassano (countdown)
            // Altre bande: 0% (normale)
            
            switch (family)
            {
                case PlantFamily.Pure:
                    // Pure in Ultra Basico: +100% resa (ma sterili, gestito altrove)
                    if (phBand == PhSystem.PhBand.UltraBasic)
                    {
                        return 2.0f; // +100% resa
                    }
                    break;
                    
                case PlantFamily.Evil:
                    // Evil in Ultra Acido: +50% resa
                    if (phBand == PhSystem.PhBand.UltraAcid)
                    {
                        return 1.5f; // +50% resa
                    }
                    break;
                    
                case PlantFamily.Standard:
                    // Standard non hanno bonus resa da pH estremi
                    break;
            }
            
            // Default: nessun modificatore
            return 1.0f;
        }
        
        /// <summary>
        /// Verifica se la pianta è sterile a causa del pH (Pure in Ultra Basico)
        /// </summary>
        /// <param name="phBand">Banda pH corrente</param>
        /// <param name="family">Famiglia della pianta</param>
        /// <returns>True se la pianta è sterile</returns>
        public static bool IsSterile(PhSystem.PhBand phBand, PlantFamily family)
        {
            // Pure in Ultra Basico sono sterili (non possono produrre nuovi frutti per 3 giorni)
            return family == PlantFamily.Pure && phBand == PhSystem.PhBand.UltraBasic;
        }
    }
}
