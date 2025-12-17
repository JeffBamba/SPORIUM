using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sporae.DevTools
{
    /// <summary>
    /// Sistema di tracking history per toast notifications (debug e analisi)
    /// </summary>
    public class ToastNotificationHistory
    {
        [System.Serializable]
        public class HistoryEntry
        {
            public int Id; // ID unico incrementale
            public string Code;
            public ToastNotificationType Type;
            public string Message;
            public Color Color;
            public DateTime Timestamp;
            public string Source; // Nome file/classe che ha triggerato
        }
        
        private List<HistoryEntry> _history = new List<HistoryEntry>();
        private int _maxEntries;
        
        public ToastNotificationHistory(int maxEntries = 100)
        {
            _maxEntries = maxEntries;
        }
        
        /// <summary>
        /// Aggiunge una entry alla history
        /// </summary>
        public void Add(HistoryEntry entry)
        {
            _history.Add(entry);
            if (_history.Count > _maxEntries)
            {
                _history.RemoveAt(0); // Rimuovi il più vecchio
            }
        }
        
        /// <summary>
        /// Ottiene le ultime N entry dalla history
        /// </summary>
        public List<HistoryEntry> GetHistory(int count = 10)
        {
            if (_history.Count == 0)
                return new List<HistoryEntry>();
            
            int startIndex = Mathf.Max(0, _history.Count - count);
            return _history.Skip(startIndex).Take(count).ToList();
        }
        
        /// <summary>
        /// Ottiene tutte le entry di un tipo specifico
        /// </summary>
        public List<HistoryEntry> GetHistoryByType(ToastNotificationType type)
        {
            return _history.Where(e => e.Type == type).ToList();
        }
        
        /// <summary>
        /// Ottiene tutte le entry con codice che inizia con il prefisso specificato
        /// </summary>
        public List<HistoryEntry> GetHistoryByCode(string codePrefix)
        {
            return _history.Where(e => !string.IsNullOrEmpty(e.Code) && e.Code.StartsWith(codePrefix)).ToList();
        }
        
        /// <summary>
        /// Pulisce tutta la history
        /// </summary>
        public void Clear()
        {
            _history.Clear();
        }
        
        /// <summary>
        /// Ottiene il numero totale di entry
        /// </summary>
        public int Count => _history.Count;
        
        /// <summary>
        /// Ottiene tutte le entry (per export)
        /// </summary>
        public List<HistoryEntry> GetAllEntries()
        {
            return new List<HistoryEntry>(_history);
        }
    }
}

