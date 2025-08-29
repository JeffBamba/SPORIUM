using UnityEngine;

namespace Sporae.Core.Interfaces
{
    /// <summary>
    /// Interfaccia base per tutti i sistemi del gioco
    /// </summary>
    public interface IGameSystem
    {
        /// <summary>
        /// Inizializza il sistema
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Verifica se il sistema è inizializzato
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Verifica se il sistema è attivo
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Attiva il sistema
        /// </summary>
        void Activate();
        
        /// <summary>
        /// Disattiva il sistema
        /// </summary>
        void Deactivate();
        
        /// <summary>
        /// Pulisce le risorse del sistema
        /// </summary>
        void Cleanup();
        
        /// <summary>
        /// Ottiene informazioni di debug sul sistema
        /// </summary>
        string GetDebugInfo();
    }
}
