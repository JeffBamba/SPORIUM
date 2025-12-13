namespace Sporae.DevTools
{
    /// <summary>
    /// Classe base per inspector di sistema
    /// Fornisce interfaccia comune per sezioni del GlobalStateInspector
    /// </summary>
    public abstract class SystemInspectorBase
    {
        /// <summary>
        /// Nome della sezione (mostrato nell'header)
        /// </summary>
        public abstract string SectionName { get; }
        
        /// <summary>
        /// Verifica se la sezione è disponibile (sistema presente)
        /// </summary>
        public abstract bool IsAvailable();
        
        /// <summary>
        /// Disegna il contenuto della sezione
        /// </summary>
        public abstract void DrawSection(float width);
        
        /// <summary>
        /// Ottiene l'altezza necessaria per la sezione (per layout)
        /// </summary>
        public abstract float GetHeight();
    }
}

