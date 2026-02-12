using System;
using System.Collections.Generic;
using UnityEngine;
using Sporae.UI.UIToolkit.PlantCard.Components;

namespace Sporae.UI.UIToolkit.PlantCard.Components
{
    /// <summary>
    /// Singleton manager per gestire note diario per tutte le piante.
    /// Storage separato per non modificare PotStateModel.
    /// </summary>
    public class PlantDiaryManager : MonoBehaviour
    {
        private static PlantDiaryManager _instance;
        
        // Storage: PotId -> Lista note
        private Dictionary<string, List<PlantDiaryNotes.DiaryNote>> _notesByPotId = new Dictionary<string, List<PlantDiaryNotes.DiaryNote>>();
        
        public static PlantDiaryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlantDiaryManager");
                    _instance = go.AddComponent<PlantDiaryManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Aggiunge una nota per un pot
        /// </summary>
        public void AddNote(string potId, PlantDiaryNotes.DiaryNote note)
        {
            if (string.IsNullOrEmpty(potId))
                return;
            
            if (!_notesByPotId.ContainsKey(potId))
            {
                _notesByPotId[potId] = new List<PlantDiaryNotes.DiaryNote>();
            }
            
            _notesByPotId[potId].Add(note);
        }
        
        /// <summary>
        /// Ottiene tutte le note per un pot
        /// </summary>
        public List<PlantDiaryNotes.DiaryNote> GetNotes(string potId)
        {
            if (string.IsNullOrEmpty(potId) || !_notesByPotId.ContainsKey(potId))
                return new List<PlantDiaryNotes.DiaryNote>();
            
            return new List<PlantDiaryNotes.DiaryNote>(_notesByPotId[potId]);
        }
        
        /// <summary>
        /// Rimuove tutte le note per un pot
        /// </summary>
        public void ClearNotes(string potId)
        {
            if (_notesByPotId.ContainsKey(potId))
            {
                _notesByPotId[potId].Clear();
            }
        }
        
        /// <summary>
        /// Raccolta note per SaveManager: per ogni nota invoca addNote(potId, day, text, timestampIso).
        /// </summary>
        public void CollectNotesForSave(Action<string, int, string, string> addNote)
        {
            if (addNote == null) return;
            foreach (var kv in _notesByPotId)
            {
                foreach (var n in kv.Value)
                {
                    addNote(kv.Key, n.Day, n.Text, n.Timestamp.ToString("o"));
                }
            }
        }

        /// <summary>
        /// Ripristina note da salvataggio (potId, day, text, timestampIso).
        /// </summary>
        public void ApplyNotesFromSave(IEnumerable<(string potId, int day, string text, string timestampIso)> notes)
        {
            if (notes == null) return;
            _notesByPotId.Clear();
            foreach (var n in notes)
            {
                if (string.IsNullOrEmpty(n.potId)) continue;
                if (!_notesByPotId.ContainsKey(n.potId))
                    _notesByPotId[n.potId] = new List<PlantDiaryNotes.DiaryNote>();
                _notesByPotId[n.potId].Add(PlantDiaryNotes.DiaryNote.FromSave(n.day, n.text, n.timestampIso));
            }
        }

        /// <summary>
        /// Salva note (delegato a SaveManager al salvataggio globale).
        /// </summary>
        public void SaveNotes()
        {
            // Integrato in SaveManager.CollectSaveData
        }

        /// <summary>
        /// Carica note (delegato a SaveManager al caricamento globale).
        /// </summary>
        public void LoadNotes()
        {
            // Integrato in SaveManager.ApplySaveData
        }
    }
}

