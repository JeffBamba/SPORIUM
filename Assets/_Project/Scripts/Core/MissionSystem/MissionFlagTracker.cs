using System.Collections.Generic;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Flag runtime per obiettivi missione (apertura armadio, ecc.). Stesso binario demo / Nuova partita.
    /// </summary>
    public sealed class MissionFlagTracker
    {
        private readonly HashSet<string> _flags = new HashSet<string>();

        public void SetFlag(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            _flags.Add(key);
        }

        public bool HasFlag(string key) => !string.IsNullOrEmpty(key) && _flags.Contains(key);

        public void ClearFlag(string key)
        {
            if (!string.IsNullOrEmpty(key))
                _flags.Remove(key);
        }

        public void ClearAll() => _flags.Clear();
    }
}
