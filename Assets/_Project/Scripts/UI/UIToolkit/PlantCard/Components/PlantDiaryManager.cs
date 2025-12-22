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
        /// Salva note (per integrazione futura con SaveManager)
        /// </summary>
        public void SaveNotes()
        {
            // TODO: Integrazione con SaveManager se necessario
        }
        
        /// <summary>
        /// Carica note (per integrazione futura con SaveManager)
        /// </summary>
        public void LoadNotes()
        {
            // TODO: Integrazione con SaveManager se necessario
        }
    }
}

