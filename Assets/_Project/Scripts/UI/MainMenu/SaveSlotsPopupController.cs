using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sporae.Core;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project
{
    /// <summary>
    /// Popup slot Save/Load: mostra più slot con riepilogo (Giorno, Piante in Dome, CRY)
    /// e supporta modalità Load (Carica) e Save (Salva su slot).
    /// </summary>
    public class SaveSlotsPopupController : MonoBehaviour
    {
        private bool _isSaveMode;
        private List<Transform> _slotRows = new List<Transform>();
        private bool _rowsBuilt;

        /// <summary> true = Salva su slot, false = Carica partita </summary>
        public void SetSaveMode(bool isSaveMode)
        {
            _isSaveMode = isSaveMode;
        }

        private void OnEnable()
        {
            RefreshSlots();
        }

        public void RefreshSlots()
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;

            Transform panel = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (panel == null || panel.childCount == 0) return;

            EnsureRows(panel);

            for (int i = 0; i < SaveManager.SlotNames.Length && i < _slotRows.Count; i++)
            {
                string slotName = SaveManager.SlotNames[i];
                Transform row = _slotRows[i];
                var labels = row.GetComponentsInChildren<TMP_Text>(true);
                var buttons = row.GetComponentsInChildren<Button>(true);
                TMP_Text label = labels.Length > 0 ? labels[0] : null;
                Button primaryButton = buttons.Length > 0 ? buttons[0] : null;
                Button deleteButton = buttons.Length > 1 ? buttons[1] : null;

                string displayName = SaveManager.GetSlotDisplayName(slotName);
                var summary = saveManager.GetSaveSummary(slotName);
                bool hasSave = summary.HasValue;

                if (label != null)
                {
                    if (hasSave)
                    {
                        var s = summary.Value;
                        label.text = $"{displayName} — Giorno {s.day}, Piante in Dome {s.plantsInDome}, CRY {s.cry} — {s.timestamp}";
                    }
                    else
                        label.text = _isSaveMode ? $"{displayName} — Vuoto (salva qui)" : $"{displayName} — Nessun salvataggio";
                }

                if (primaryButton != null)
                {
                    primaryButton.onClick.RemoveAllListeners();
                    if (_isSaveMode)
                    {
                        primaryButton.gameObject.SetActive(true);
                        string slot = slotName;
                        primaryButton.onClick.AddListener(() => OnSaveSlot(slot));
                        var btnLabel = primaryButton.GetComponentInChildren<TMP_Text>(true);
                        if (btnLabel != null) btnLabel.text = "Salva";
                    }
                    else
                    {
                        if (hasSave)
                        {
                            primaryButton.gameObject.SetActive(true);
                            string slot = slotName;
                            primaryButton.onClick.AddListener(() => OnLoadSlot(slot));
                            var btnLabel = primaryButton.GetComponentInChildren<TMP_Text>(true);
                            if (btnLabel != null) btnLabel.text = "Carica";
                        }
                        else
                            primaryButton.gameObject.SetActive(false);
                    }
                }

                if (deleteButton != null)
                {
                    deleteButton.gameObject.SetActive(!_isSaveMode && hasSave);
                    if (!_isSaveMode && hasSave)
                    {
                        deleteButton.onClick.RemoveAllListeners();
                        string slot = slotName;
                        deleteButton.onClick.AddListener(() => OnDeleteSlot(slot));
                    }
                }

                row.gameObject.SetActive(true);
            }
        }

        private void EnsureRows(Transform panel)
        {
            if (_rowsBuilt) return;
            _rowsBuilt = true;
            Transform template = panel.GetChild(0);
            _slotRows.Add(template);
            int targetCount = SaveManager.SlotNames.Length;
            for (int i = 1; i < targetCount; i++)
            {
                var clone = Object.Instantiate(template.gameObject, panel);
                clone.name = $"Slot_{i + 1}";
                _slotRows.Add(clone.transform);
            }
        }

        private void OnSaveSlot(string slotName)
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;
            if (saveManager.SaveGame(slotName))
            {
                var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                if (foundation != null && foundation.Enabled)
                    foundation.PostToast("SYS-003", new NotificationPayload());
                RefreshSlots();
            }
        }

        private void OnLoadSlot(string slotName)
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;
            if (saveManager.LoadGame(slotName))
            {
                gameObject.SetActive(false);
                var screens = GetComponentInParent<MainMenuScreens>(true);
                if (screens != null)
                    screens.Hide();
            }
        }

        private void OnDeleteSlot(string slotName)
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;
            saveManager.DeleteSave(slotName);
            RefreshSlots();
        }
    }
}
