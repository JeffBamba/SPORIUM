using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using _Project;
using _Project.Sporae.Core;
using Sporae.UI.UIToolkit.PlantCard.Helpers;

namespace Sporae.UI.UIToolkit.PlantCard.Components
{
    /// <summary>
    /// Sistema per gestire note diario per piante.
    /// Storage separato per non modificare PotStateModel.
    /// </summary>
    public class PlantDiaryNotes
    {
        [Serializable]
        public struct DiaryNote
        {
            public int Day;
            public string Text;
            public DateTime Timestamp;

            public DiaryNote(int day, string text)
            {
                Day = day;
                Text = text;
                Timestamp = DateTime.Now;
            }

            /// <summary>Per ripristino da save (timestamp da stringa).</summary>
            public static DiaryNote FromSave(int day, string text, string timestampIso)
            {
                return new DiaryNote(day, text)
                {
                    Timestamp = DateTime.TryParse(timestampIso, out var t) ? t : DateTime.Now
                };
            }
        }
        
        private ScrollView _notesList;
        private VisualElement _addNotePanel;
        private TextField _noteTextarea;
        private Button _saveNoteButton;
        private Button _cancelNoteButton;
        private string _currentPotId;
        
        // Callbacks
        public event Action<string, DiaryNote> OnNoteAdded;
        
        public PlantDiaryNotes(ScrollView notesList, VisualElement addNotePanel)
        {
            _notesList = notesList;
            _addNotePanel = addNotePanel;
            
            InitializeElements();
        }
        
        private void InitializeElements()
        {
            if (_addNotePanel != null)
            {
                _noteTextarea = _addNotePanel.Q<TextField>("note-textarea");
                _saveNoteButton = _addNotePanel.Q<Button>("save-note-button");
                _cancelNoteButton = _addNotePanel.Q<Button>("cancel-note-button");
                
                if (_saveNoteButton != null)
                {
                    _saveNoteButton.clicked += OnSaveNote;
                }
                
                if (_cancelNoteButton != null)
                {
                    _cancelNoteButton.clicked += OnCancelNote;
                }
            }
        }
        
        /// <summary>
        /// Carica e mostra note per un pot specifico
        /// </summary>
        public void LoadNotesForPot(string potId)
        {
            _currentPotId = potId;
            
            if (_notesList == null) return;
            
            // Pulisci lista esistente
            _notesList.Clear();
            
            // Carica note da PlantDiaryManager
            var notes = PlantDiaryManager.Instance?.GetNotes(potId) ?? new List<DiaryNote>();
            
            // Aggiungi note alla lista
            foreach (var note in notes.OrderByDescending(n => n.Day))
            {
                AddNoteToUI(note);
            }
        }
        
        /// <summary>
        /// Mostra pannello aggiungi nota
        /// </summary>
        public void ShowAddNotePanel()
        {
            if (_addNotePanel != null)
            {
                _addNotePanel.style.display = DisplayStyle.Flex;
                if (_noteTextarea != null)
                {
                    _noteTextarea.value = "";
                    _noteTextarea.Focus();
                }
            }
        }
        
        /// <summary>
        /// Nasconde pannello aggiungi nota
        /// </summary>
        public void HideAddNotePanel()
        {
            if (_addNotePanel != null)
            {
                _addNotePanel.style.display = DisplayStyle.None;
            }
        }
        
        private void OnSaveNote()
        {
            if (_noteTextarea == null || string.IsNullOrWhiteSpace(_noteTextarea.value))
                return;
            
            if (string.IsNullOrEmpty(_currentPotId))
            {
                Debug.LogWarning("PlantDiaryNotes: Nessun pot selezionato");
                return;
            }
            
            // Ottieni giorno corrente da ServiceContainer
            int currentDay = 1;
            var dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
            if (dayCycleSystem != null)
            {
                currentDay = dayCycleSystem.CurrentDay;
            }
            
            // Crea nota
            DiaryNote note = new DiaryNote(currentDay, _noteTextarea.value.Trim());
            
            // Salva in PlantDiaryManager
            PlantDiaryManager.Instance?.AddNote(_currentPotId, note);
            
            // Aggiungi alla UI
            AddNoteToUI(note);
            
            // Pulisci e nascondi
            _noteTextarea.value = "";
            HideAddNotePanel();
            
            // Notifica callback
            OnNoteAdded?.Invoke(_currentPotId, note);
        }
        
        private void OnCancelNote()
        {
            if (_noteTextarea != null)
            {
                _noteTextarea.value = "";
            }
            HideAddNotePanel();
        }
        
        /// <summary>
        /// Aggiunge una nota alla UI
        /// </summary>
        private void AddNoteToUI(DiaryNote note)
        {
            if (_notesList == null) return;
            
            // Crea elemento nota
            VisualElement noteItem = new VisualElement();
            noteItem.AddToClassList("note-item");
            
            // Formatta testo
            string formattedText = Helpers.PlantCardFormatters.FormatDiaryNote(note.Day, note.Text);
            
            Label noteLabel = new Label(formattedText);
            noteLabel.AddToClassList("note-text");
            noteItem.Add(noteLabel);
            
            // Inserisci in cima (note più recenti prima)
            _notesList.Insert(0, noteItem);
        }
        
        /// <summary>
        /// Pulisce tutte le note dalla UI
        /// </summary>
        public void ClearNotes()
        {
            if (_notesList != null)
            {
                _notesList.Clear();
            }
        }
    }
}

